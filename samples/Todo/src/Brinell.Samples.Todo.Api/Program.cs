using Brinell.Samples.Todo.Api;
using Brinell.Samples.Todo.Contracts;

var app = TodoApiHost.Build(args);

if (app.Urls.Count == 0 && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    // The address the app uses when no launch setting names another.
    app.Urls.Add(TodoApi.DevelopmentBaseUrl);
}

app.Run();
