# Xamarin.Droid
Some useful Xamarin Android snippets

- Request class: A useful HttpClient that works with a Builder Object pattern.

- Installation: Just copy the Request and MockRequest classes into your project and change the namespace to your project.

- Usage:

    The T parameter is the json response object.


        await new Request<T>()
        .SetHttpMethod(HttpMethod.[Post|Put|Get|Delete].Method) //Obligatory
        .SetEndpoint("http://www.yourserver.com/profilepic/") //Obligatory
        .SetJsonPayload(someJsonObject) //Optional if you're using Get or Delete, Obligatory if you're using Put or Post
        .OnSuccess((serverResponse) => { 
           //Optional action triggered when you have a succesful 200 response from the server
          //serverResponse is of type T
        })
        .OnNoInternetConnection(() =>
        {
            // Optional action triggered when you try to make a request without internet connetion
        })
        .OnRequestStarted(() =>
        {
            // Optional action triggered always as soon as we start making the request i.e. very useful when
            // We want to start an UI related action such as showing a ProgressBar or a Spinner.
        })
        .OnRequestCompleted(() =>
        {
            // Optional action triggered always when a request finishes, no matter if it finished successufully or
            // It failed. It's useful for when you need to finish some UI related action such as hiding a ProgressBar or
            // a Spinner.
        })
        .OnError((exception) =>
        {
            // Optional action triggered always when something went wrong it can be caused by a server-side error, for
            // example a internal server error or for something in the callbacks, for example a NullPointerException.
        })
        .OnHttpError((httpErrorStatus) =>
        {
            // Optional action triggered when something when sending a request, for example, the server returned a internal
            // server error, a bad request error, an unauthorize error, etc. The httpErrorStatus variable is the error code.
        })
        .OnBadRequest(() =>
        {
            // Optional action triggered when the server returned a bad request error.
        })
        .OnUnauthorize(() =>
        {
            // Optional action triggered when the server returned an unauthorize error.
        })
        .OnInternalServerError(() =>
        {
            // Optional action triggered when the server returned an internal server error.
        })
        //AND THERE'S A LOT MORE OF CALLBACKS THAT YOU CAN HOOK OF, CHECK THE REQUEST CLASS TO MORE INFO.
        .Start();
    

- Examples:

**GET Request** : A GET request to http://www.yourserver.com/user/ will return a collection of User json objects.

    await Request<List<User>>()
    .SetHttpMethod(HttpMethod.Get.Method)
    .SetEndpoint("http://www.yourserver.com/user/")
    .OnNoInternetConnection(() =>
    {
        UserDialogs.Instance.Alert(AppResources.no_internet_message, 
            AppResources.error_title,
            AppResources.accept_button_text);
    })
    .OnSuccess((serverResponse) => {
      //serverResponse is a List<User>
    })
    .Start();

**POST Request** : A POST request to http://www.yourserver.com/user/ sending the json object { "name" : "Simon", "last_name" : "Smith"} will create a new User and it will return that created user as result.

    var jsonPayload = JsonConvert.SerializeObject(new { name = "Agustin", last_name = "Larghi" });
    await new Request<User>()
    .SetHttpMethod(HttpMethod.Post.Method)
    .SetEndpoint("http://www.yourserver.com/user/")
    .SetJsonPayloadBody(jsonPayload) //The jsonPayload will be set as the raw body in the request
    .OnSuccess((serverResponse) => {
      //serverResponse is the created User
    })
    .OnNoInternetConnection(() =>
    {
        App.Current.MainPage.DisplayAlert(AppResources.error_title,
            AppResources.no_internet_message,
            AppResources.accept_button_text);
    })
    .Start();

**DELETE Request**: Sending a DELETE request to the http://www.yourserver.com/user/{id} URL will delete an User from the server side, and it returns the deleted user.

    await Request<User>()
    .SetHttpMethod(HttpMethod.Delete.Method)
    .SetEndpoint("http://www.yourserver.com/user/13")
    .OnSuccess((serverResponse) => {
      //serverResponse is the deleted User
    })
    .OnNoInternetConnection(() =>
    {
        App.Current.MainPage.DisplayAlert(AppResources.error_title,
            AppResources.no_internet_message,
            AppResources.accept_button_text);
    })
    .Start();

**PUT Request**: Sending a PUT request to http://www.yourserver.com/user/ with the json object { "name" : "Simon", "last_name" : "Smith", "id" : 13} as payload will update that User record.

    var jsonPayload = JsonConvert.SerializeObject(new { name = "Agustin", last_name = "Larghi", id = 13 });
    await new Request<User>()
    .SetHttpMethod(HttpMethod.Put.Method)
    .SetEndpoint("http://www.yourserver.com/user/")
    .SetJsonPayloadBody(jsonPayload) //The jsonPayload will be set as the raw body in the request
    .OnSuccess((serverResponse) => {
      //serverResponse is the updated User
    })
    .OnNoInternetConnection(() =>
    {
        App.Current.MainPage.DisplayAlert(AppResources.error_title,
            AppResources.no_internet_message,
            AppResources.accept_button_text);
    })
    .Start();

**Multipart Request**: Sending a Multipart POST request to http://www.yourserver.com/profilepic/ will upload a file to the server.

    await new Request<Picture>()
    .SetHttpMethod(HttpMethod.Post.Method)
    .SetEndpoint("http://www.yourserver.com/profilepic/")
    .SetFile(mf, "file")
    .OnSuccess((serverResponse) => {
      //serverResponse is a Picture file
    })
    .OnNoInternetConnection(() =>
    {
        App.Current.MainPage.DisplayAlert(AppResources.error_title,
            AppResources.no_internet_message,
            AppResources.accept_button_text);
    })
    .Start();

**Mock Request**: Class created to mock a Request. For example, this way we can mock a Request, result is a `List<User>` you can use mock libraries such as NBuilder to build the mock response objects.

    await MockRequest<List<User>>().SetResult(result).Start();
    
- Architecture: The idea of this Http Client is having your app network layer split in two, the real Http Client and a Mock Http Cliend. For example let's continue with the /user webservice example. We have an interface were we have all the server side methods:

        public interface IRestService
        {
          Request<List<User>> GetUsersDataAsync();
        }
    
Then we have two concrete implementations of this interface, the mock implementation:

    class RestMockService : IRestService
    {
      public Request<List<User>> GetUsersDataAsync()
      {
          var result = new List<User>();
          result.add(new User("Agustin", "Larghi"));
          result.add(new User("Sam", "Simon"));
          result.add(new User("Brad", "Bradley"));
          return MockRequest<List<User>>().SetResult(result);
      }
    }
    
And the real rest client:

    class RestMockService : IRestService
    {
      public Request<List<User>> GetUsersDataAsync()
      {
          return Request<List<User>>()
            .SetHttpMethod(HttpMethod.Get.Method)
            .SetEndpoint("http://www.yourserver.com/user/")
            //See! We can handle globally some events directly in the network layer!
            .OnNoInternetConnection(() =>
            {
                DisplayAlert(AppResources.no_internet_message, 
                    AppResources.error_title,
                    AppResources.accept_button_text);
            });
      }
    }
    
So in our PCL project we'll call GetUsersDataAsync() regardless of which implementation it uses. We can easly switch from the real http layer to the mocked one.

    //...
    await restService.GetUsersDataAsync()
      .OnSuccess((listOfUsers) => {
        //And we have the list of users no matter if its the mocked one or the real one.
      })
      .Start();
    //...
    
