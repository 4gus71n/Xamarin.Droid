//
// YourApp.Network.Request.cs: This class basically takes care of performing the actual http request. It follows an object
// builder pattern. Can be used to perform POST, PUT, GET, DELETE and Multipart requests.
//
// Author:
//   Agustin Larghi (agustin.tomas.larghi@gmail.com)
//

using YourApp.Utils;
using Newtonsoft.Json;
using Plugin.Connectivity;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace YourApp
{
    public class Request<T>
    {
        public Request()
        {

        }

        //<summary>
        //Sets the http method that we are goint to use
        //</summary>
        //<param name="httpMethod">Can be any of HttpMethod.*.Method</param>
        public Request<T> SetHttpMethod(string httpMethod)
        {
            this.httpMethod = httpMethod;
            return this;
        }

        //<summary>
        //Sets the endpoint that we're gonna hit
        //</summary>
        //<param name="requestEndpoint">Any endpoint from the Settings class</param>
        public Request<T> SetEndpoint(string requestEndpoint)
        {
            this.requestEndpoint = requestEndpoint;
            return this;
        }

        public Request<T> SetAuthenticationToken(string authenticationToken)
        {
            this.authenticationToken = authenticationToken;
            return this;
        }

        //<summary>
        //Sets the raw body content. If a jsonPayloadName variable is passed, the jsonPayload will be set into a 
        //form with jsonPayloadName as the name of the form field.
        //</summary>
        public Request<T> SetJsonPayloadBody(string jsonPayload, string jsonPayloadName = null)
        {
            this.jsonPayload = jsonPayload;
            this.jsonPayloadName = jsonPayloadName;
            return this;
        }

        #region Request's lifecycle methods
        protected Action<T> onSuccess;
        protected Action<Exception> onError, onUnknownError, onJsonError;
        protected Action<WebHeaderCollection> onHeaderResult;
        protected Action onInternalServerError, onUnauthorize, onTimeOut, onAuthTokenError, onNotFound, onBadRequestError,
            onRequestCompleted, onRequestStarted, onNoInternetConnection;
        protected Action<HttpStatusCode> onHttpError;
        #endregion

        #region Request configuration variables
        private MyDateTimeConverter dateTimeConverter = new MyDateTimeConverter();
        private BoolConverter booleanConverter = new BoolConverter();
        private FileObject fileObject;
        private string httpMethod, requestEndpoint, jsonPayload, authenticationToken,
            fileName, fileContentType, fileParameterName, jsonPayloadName;

        #endregion

        #region Builder object methods
        //<summary>
        // Starts the request. Returns a task that can be awaited. The task has the response model within.
        //</summary>
        public virtual Task<T> Start()
        {
            return MakeRequest(httpMethod, requestEndpoint, jsonPayload, authenticationToken);
        }

        //<summary>
        // Triggered if there's no internet connection in the device when we make the request.
        //</summary>
        public Request<T> OnNoInternetConnection(Action Handler)
        {
            onNoInternetConnection = Handler;
            return this;
        }

        //<summary>
        // Triggered always no matter what, when the request is completed.
        //</summary>
        public Request<T> OnRequestCompleted(Action Handler)
        {
            onRequestCompleted = Handler;
            return this;
        }

        //<summary>
        // Triggered always no matter what, at the very begining of the request.
        //</summary>
        public Request<T> OnRequestStarted(Action Handler)
        {
            onRequestStarted = Handler;
            return this;
        }

        //<summary>
        // Triggered if the request response returned 400.
        //</summary>
        public Request<T> OnBadRequestError(Action Handler)
        {
            onBadRequestError = Handler;
            return this;
        }

        //<summary>
        // Triggered if an unhandled exception was fired when doing the request.
        //</summary>
        //<param name="Handler">The exception thrown</param>
        public Request<T> OnJsonError(Action<Exception> Handler)
        {
            onJsonError = Handler;
            return this;
        }

        //<summary>
        // Triggered if an unhandled exception was fired when doing the request.
        //</summary>
        //<param name="Handler">The exception thrown</param>
        public Request<T> OnUnknownError(Action<Exception> Handler)
        {
            onUnknownError = Handler;
            return this;
        }

        //<summary>
        // Triggered when we retrieved the headers of the request response. Hook from this method if you want to
        // get something from the response headers i.e. the "Set-Cookie" header.
        //</summary>
        //<param name="Handler">The response headers</param>
        public Request<T> OnHeaderResult(Action<WebHeaderCollection> Handler)
        {
            onHeaderResult = Handler;
            return this;
        }

        //<summary>
        // Triggered when the request response is properly parsed and returned status code 200.
        //</summary>
        //<param name="Handler">The model response</param>
        public Request<T> OnSuccess(Action<T> Handler)
        {
            onSuccess = Handler;
            return this;
        }

        //<summary>
        // Triggered when the request response returned 401
        //</summary>
        public Request<T> OnNotFound(Action Handler)
        {
            onNotFound = Handler;
            return this;
        }

        //<summary>
        // Triggered every time that an http error ocurred (400, 401, 501, etc.)
        //</summary>
        //<param name="Handler">The http status code</param>
        public Request<T> OnHttpError(Action<HttpStatusCode> Handler)
        {
            onHttpError = Handler;
            return this;
        }

        //<summary>
        // Triggered if the token is invalid at the time that we're trying to make the request.
        //</summary>
        public Request<T> OnAuthTokenError(Action Handler)
        {
            onAuthTokenError = Handler;
            return this;
        }

        //<summary>
        // Triggered if the request response caused an unauthorized error
        //</summary>
        public Request<T> OnUnauthorize(Action Handler)
        {
            onUnauthorize = Handler;
            return this;
        }

        //<summary>
        // Triggered if the request response caused a timeout error.
        //</summary>
        public Request<T> OnTimeOut(Action Handler)
        {
            onTimeOut = Handler;
            return this;
        }

        //<summary>
        // Executed every time that an erro ocurred
        //</summary>
        public Request<T> OnError(Action<Exception> Handler)
        {
            onError = Handler;
            return this;
        }

        //<summary>
        // Triggered if the request response is a 501 status code
        //</summary>
        public Request<T> OnInternalServerError(Action Handler)
        {
            onInternalServerError = Handler;
            return this;
        }
        #endregion

        #region Basic http methods
        //<summary>
        //Configures the headers for every request that we make
        //</summary>
        private static void SetHeaders(HttpWebRequest request)
        {
            request.Accept = "application/json";
            request.ContentType = "application/json; charset=UTF-8";
            request.Headers["Pragma"] = "no-cache";
            request.Headers["Accept-Encoding"] = "gzip, deflate, sdch";
            request.Headers["Accept-Language"] = "es-419,es;q=0.8";
            request.Headers["Upgrade-Insecure-Requests"] = "1";
            request.Headers["Cache-Control"] = "no-cache";
        }
        #endregion

        //<summary>
        // Makes a request and deserializes the result JSON as T class objects.
        // Check https://forums.xamarin.com/discussion/22732/3-pcl-rest-functions-post-get-multipart
        //</summary>
        //<param name="method">The http method can be "GET", "POST", "PUT" or "DELETE"</param>
        //<param name="endpoint">The endpoint name i.e. "/api/v1/feed"</param>
        //<param name="body">If the method is GET, the query string URI to set in the URL. Otherwise the json body.</param>
        //<param name="authToken">The AUTH_TOKEN cookie.</param>
        private async Task<T> MakeRequest(string method, string endpoint, string body, string authToken = null)
        {
            HttpWebResponse response = null;
            Settings.Log("Hitting: " + endpoint);
            Settings.Log("Body payload: " + body);
            onRequestStarted?.Invoke();
            if (!CrossConnectivity.Current.IsConnected)
            {
                return HandleNoConnectivity();
            }
            try
            {
                //If the method is GET we've to concat the query string uri, i.e. "/feeds" + "?id=something"
                if (method.Equals(HttpMethod.Get.Method) && !string.IsNullOrEmpty(body))
                {
                    endpoint += body;
                }
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(UriHelper.GetEndpointUri(endpoint)));
                SetHeaders(request);
                request.Method = method;
                if (!string.IsNullOrEmpty(authToken))
                {
                    request.Headers["Cookie"] = authToken;
                }

                if (method.Equals(HttpMethod.Post.Method) || method.Equals(HttpMethod.Put.Method))
                {
                    if (!string.IsNullOrEmpty(body))
                    {
                        var requestStream = await request.GetRequestStreamAsync();
                        using (var writer = new StreamWriter(requestStream))
                        {
                            writer.Write(body);
                            writer.Flush();
                            writer.Dispose();
                        }
                    }
                    if (fileObject != null)
                    {
                        ////Post Multi-part data
                        var fileStream = fileObject.Source;

                        //Expected
                        //Header
                        //Content-Length: 18101
                        //Content-Type: multipart/form-data; boundary = ---------------------------13455211745882
                        //Cookie: AUTH-TOKEN=eyJhbGciOiJIUz
                        //Body
                        //-----------------------------13455211745882
                        //Content-Disposition: form-data; name="file"; filename="Feed List View.png"
                        //Content-Type: image/png
                        //Byte body
                        //-----------------------------13455211745882--
                        string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");

                        request.ContentType = "multipart/form-data; boundary=" + boundary;
                        request.Credentials = CredentialCache.DefaultCredentials;

                        var requestStream = await request.GetRequestStreamAsync();

                        string headerTemplate = "--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\"\r\nContent-Type: {3}\r\n\r\n";
                        string header = string.Format(headerTemplate, boundary, fileParameterName, fileName, fileContentType);

                        Debug.WriteLine(header);

                        byte[] headerbytes = Encoding.UTF8.GetBytes(header);

                        using (var requestStreamWriter = new BinaryWriter(requestStream))
                        {
                            requestStreamWriter.Write(headerbytes, 0, headerbytes.Length);

                            var fileByteStream = ReadFully(fileStream);
                            Debug.WriteLine("Bytes read:" + fileByteStream.Length);
                            requestStreamWriter.Write(fileByteStream, 0, fileByteStream.Length);

                            byte[] trailer = Encoding.UTF8.GetBytes("\r\n--" + boundary + "--");
                            requestStreamWriter.Write(trailer, 0, trailer.Length);
                            Debug.WriteLine("(Using) Request ContentType: " + request.ContentType.ToString());
                        }
                    }
                }
                //if (method.Equals(HttpMethod.Put.Method))
                //{
                //    if (!string.IsNullOrEmpty(body)) WriteRequestStream(request, body);
                //}
                //if (method.Equals(HttpMethod.Delete.Method))
                //{
                //    if (!string.IsNullOrEmpty(body)) WriteRequestStream(request, body);
                //}

                Debug.WriteLine("Request ContentType: " + request.ContentType.ToString());
                response = (HttpWebResponse) await request.GetResponseAsync();
                Debug.WriteLine("Response Content-Lenght: " + response.ContentLength);
                onHeaderResult?.Invoke(response.Headers);
                var respStream = response.GetResponseStream();
                using (StreamReader sr = new StreamReader(respStream))
                {
                    //Need to return this response 
                    var stringResponse = sr.ReadToEnd();
                    Debug.WriteLine("Json response: " + stringResponse);
                    T result = JsonConvert.DeserializeObject<T>(stringResponse, dateTimeConverter, booleanConverter);
                    onSuccess?.Invoke(result);
                    onRequestCompleted?.Invoke();
                    return result;
                }

            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    return HandleUnknownError(new Exception("Null response"));
                }
                using (var stream = e.Response.GetResponseStream())
                using (var reader = new StreamReader(stream))
                {
                    Settings.Log("Server-side error:" + reader.ReadToEnd());
                }
                return HandleWebExceptionError(e);
            }
            catch (JsonSerializationException e)
            {
                return HandleJsonError(e);
            }
            catch (Exception e)
            {
                return HandleUnknownError(e);
            }
        }

        public Request<T> SetFile(FileObject file, string fileParameterName)
        {
            this.fileObject = file;
            this.fileParameterName = fileParameterName;
            this.fileName = file.Path.Split('/').Last();
            this.fileContentType = new MimeSharp.Mime().Lookup(fileName);
            return this;
        }


        private async void WriteRequestStream(HttpWebRequest request, string body)
        {
            var stream = await request.GetRequestStreamAsync();
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(body);
                writer.Flush();
                writer.Dispose();
            }
        }

        //See http://stackoverflow.com/a/221941/1403997
        public static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) != 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }

        private T HandleJsonError(Exception e)
        {
            onJsonError?.Invoke(e);
            onError?.Invoke(e);
            onRequestCompleted?.Invoke();
            return default(T);
        }

        private T HandleUnknownError(Exception e)
        {
            onUnknownError?.Invoke(e);
            onError?.Invoke(e);
            onRequestCompleted?.Invoke();
            return default(T);
        }

        private T HandleWebExceptionError(WebException e)
        {
            HttpWebResponse response = (HttpWebResponse)e.Response;
            if (!response.StatusCode.Equals(HttpStatusCode.OK))
            {
                onError?.Invoke(e);
                onHttpError?.Invoke(response.StatusCode);
                switch (response.StatusCode)
                {
                    case HttpStatusCode.BadRequest:
                        onBadRequestError?.Invoke();
                        break;
                    case HttpStatusCode.InternalServerError:
                        onInternalServerError?.Invoke();
                        break;
                    case HttpStatusCode.RequestTimeout:
                        onTimeOut?.Invoke();
                        break;
                    case HttpStatusCode.NotFound:
                        onNotFound?.Invoke();
                        break;
                    case HttpStatusCode.Unauthorized:
                        onUnauthorize?.Invoke();
                        break;
                }
            }
            onRequestCompleted?.Invoke();
            return default(T);
        }

        private T HandleNoConnectivity()
        {
            //If there's no internet connection
            onError?.Invoke(new WebException("No internet connection"));
            onNoInternetConnection?.Invoke();
            onRequestCompleted?.Invoke();
            return default(T);
        }
    }
}
