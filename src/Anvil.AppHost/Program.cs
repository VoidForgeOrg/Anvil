using Anvil.AppHost.Extensions;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var containerLifetime = bool.Parse(builder.Configuration["PERSISTENT_CONTAINERS"] ?? "false") ? ContainerLifetime.Persistent : ContainerLifetime.Session;

// -------------- INFRA -------------- Postgres
var pgUsername = builder.AddParameter("pgUsername", "admin");
var pgPassword = builder.AddParameter("pgPassword", "111111");
var postgres = builder
    .AddPostgres("postgres", port: 5432, userName: pgUsername, password: pgPassword)
    .WithDataVolume()
    .WithPgAdminCustom(portOverride: 9002, configureContainer: configure => configure.WithLifetime(containerLifetime))
    .PublishAsConnectionString()
    .WithLifetime(containerLifetime);

// -------------- INFRA -------------- Dex
var minio = builder.AddContainer("dex", "ghcr.io/dexidp/dex", "v2.37.0")
    .WithArgs("dex", "serve", "/etc/dex/config.yaml")
    .WithBindMount("./DockerThings/dex-config.yaml", "/etc/dex/config.yaml")
    .WithHttpsEndpoint(targetPort: 5556, name: "main", isProxied: false)
    .WithLifetime(containerLifetime);

// https://dexidp.io/docs/guides/using-dex/
// http://localhost:5556/dex/.well-known/openid-configuration


// -------------------- Universe
var universe = builder.AddProject<Universe>("universe", launchProfileName: null)
    .WithReference(postgres.AddDatabase("UniverseDb"))
    .WithHttpEndpoint(40000)
    .WithCommonEnvironment()
    .WaitFor(postgres);

builder.Build().Run();