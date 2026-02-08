#:sdk Aspire.AppHost.Sdk@13.2.0-preview.1.26109.1
#:package Aspire.Hosting.Python@13.2.0-preview.1.26109.1
#:package Scalar.Aspire@0.8.45

#:project ./mainapi/mainapi.csproj
#:project ./downstreamapi/downstreamapi.csproj

using Scalar.Aspire;

var builder = DistributedApplication.CreateBuilder(args);

var fastapi = builder.AddUvicornApp("fastapi", "./fastapi", "main:app")
    .WithUv()
    .WithExternalHttpEndpoints();

var downstreamapi = builder.AddProject<Projects.downstreamapi>("downstreamapi")
    .WithExternalHttpEndpoints()
    .WithReference(fastapi)
    .WaitFor(fastapi);

var mainapi = builder.AddProject<Projects.mainapi>("mainapi")
    .WithExternalHttpEndpoints()
    .WithReference(fastapi)
    .WaitFor(fastapi)
    .WithReference(downstreamapi)
    .WaitFor(downstreamapi);

var scalar = builder.AddScalarApiReference()
    .WithApiReference(fastapi, options =>
    {
        options
            .AddDocument("openapi", "FastApi OpenAPI")
            .WithOpenApiRoutePattern("/openapi.json");
    })
    .WithApiReference(downstreamapi)
    .WaitFor(mainapi);

builder.Build().Run();
