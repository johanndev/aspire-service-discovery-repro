var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.AddServiceDefaults();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/testendpoint", () => "Hello from downstreamapi")
    .WithName("TestEndpoint");

app.MapDefaultEndpoints();

app.Run();

