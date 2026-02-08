var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapGet("/callfastapi", async (HttpClient httpClient) =>
{
    var response = await httpClient.GetAsync("http://fastapi/testendpoint");
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    return content;
});

app.MapGet("/calldownstreamapi", async (HttpClient httpClient) =>
{
    var response = await httpClient.GetAsync("http://downstreamapi/testendpoint");
    response.EnsureSuccessStatusCode();
    var content = await response.Content.ReadAsStringAsync();
    return content;
});

app.Run();

