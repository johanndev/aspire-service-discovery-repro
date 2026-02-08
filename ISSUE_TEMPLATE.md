## Title
Service discovery fails to resolve hostnames for Python resources via `ConfigureHttpClientDefaults`

## Description

When using `AddUvicornApp` for a Python FastAPI app and referencing it from a .NET web API project, the `ResolvingHttpDelegatingHandler` is invoked but fails to resolve the service hostname (`http://app`) to the actual endpoint URL.

### Environment

- Aspire CLI: 13.2.0-preview.1.26106.8
- Aspire SDK: 13.2.0-preview.1.26106.8
- .NET: 10.0.102
- OS: Windows 11

### Steps to Reproduce

1. Create an AppHost with a Python resource using `AddUvicornApp`:

```csharp
var app = builder.AddUvicornApp("app", "./app", "main:app")
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.webapi>("webapi")
    .WithExternalHttpEndpoints()
    .WithReference(app)
    .WaitFor(app);
```

2. In the webapi project, configure service discovery via `ConfigureHttpClientDefaults`:

```csharp
builder.Services.AddServiceDiscovery();

builder.Services.ConfigureHttpClientDefaults(http =>
{
    http.AddServiceDiscovery();
});
```

3. Make an HTTP request to the service using the service name:

```csharp
var httpClient = httpClientFactory.CreateClient();
var res = await httpClient.GetAsync("http://app/api/callme");
```

4. Run the AppHost and test the endpoint

### Expected Behavior

The request to `http://app/api/callme` should resolve to `https://localhost:XXXX/api/callme` (where XXXX is the actual port assigned by Aspire).

### Actual Behavior

The request fails with:
```
System.Net.Http.HttpRequestException: No such host is known. (app:80)
 ---> System.Net.Sockets.SocketException (11001): No such host is known.
```

### Debugging Information

I verified that:
1. ✅ Environment variable IS set: `services__app__http__0=https://localhost:50813`
2. ✅ Configuration CAN read it: `config["services:app:http:0"]` returns the correct URL
3. ✅ `ResolvingHttpDelegatingHandler` IS invoked (visible in stack traces)
4. ❌ The hostname `app` is NOT resolved before the HTTP request is made

Stack trace shows:
```
at Microsoft.Extensions.ServiceDiscovery.Http.ResolvingHttpDelegatingHandler.SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
at Microsoft.Extensions.Http.Resilience.ResilienceHandler.<>c.<<SendAsync>b__3_0>d.MoveNext()
```

### Workaround

Reading the service URL directly from configuration works:

```csharp
var appUrl = config["services:app:http:0"] ?? "http://app";
var res = await httpClient.GetAsync($"{appUrl}/api/callme");
```

### Minimal Reproduction

Repository: [Will be provided if needed]

**apphost.cs:**
```csharp
#:sdk Aspire.AppHost.Sdk@13.2.0-preview.1.26106.8
#:package Aspire.Hosting.AppHost@13.2.0-preview.1.26106.8
#:package Aspire.Hosting.Python@13.2.0-preview.1.26106.8

#:project ./webapi/webapi.csproj

var builder = DistributedApplication.CreateBuilder(args);
var app = builder.AddUvicornApp("app", "./app", "main:app")
    .WithExternalHttpEndpoints();
builder.AddProject<Projects.webapi>("webapi")
    .WithExternalHttpEndpoints()
    .WithReference(app)
    .WaitFor(app);
builder.Build().Run();
```

**webapi/Program.cs:**
```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults(); // Includes AddServiceDiscovery and ConfigureHttpClientDefaults

builder.Services.AddHttpClient();

var app = builder.Build();

app.MapGet("/callfastapi", async (IHttpClientFactory httpClientFactory) =>
{
    var httpClient = httpClientFactory.CreateClient();
    var res = await httpClient.GetAsync("http://app/api/callme");
    res.EnsureSuccessStatusCode();
    return await res.Content.ReadAsStringAsync();
});

app.Run();
```

**app/main.py:**
```python
from fastapi import FastAPI
app = FastAPI()

@app.get("/api/callme")
def callme():
    return "Hello from FastAPI!"
```

### Additional Context

The issue appears to be that `ConfigureHttpClientDefaults(http => http.AddServiceDiscovery())` does not properly register the service discovery resolver for the default HttpClient instances. When explicitly adding service discovery to a named HttpClient via `AddHttpClient("name").AddServiceDiscovery()`, the resolution also fails.

This suggests the issue may be in how the configuration-based endpoint provider reads environment variables when using Python resources via `AddUvicornApp`.
