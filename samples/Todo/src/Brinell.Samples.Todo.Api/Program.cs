using Brinell.Samples.Todo.Api;
using Brinell.Samples.Todo.Contracts;

var app = TodoApiHost.Build(args);

// The development address, unless one was given. "urls" is where both --urls and ASPNETCORE_URLS
// land; checking app.Urls instead (empty until start) silently replaced a given address with this.
if (string.IsNullOrEmpty(app.Configuration["urls"]))
{
    app.Urls.Add(TodoApi.DevelopmentBaseUrl);
}

app.Run();
