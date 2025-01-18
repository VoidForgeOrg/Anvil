using Anvil.AppHost.Extensions;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var containerLifetime = bool.Parse(builder.Configuration["PERSISTENT_CONTAINERS"] ?? "false") ? ContainerLifetime.Persistent : ContainerLifetime.Session;

// -------------- INFRA -------------- Postgres
var postgres = builder
    .AddPostgres("postgres")
    .PublishAsConnectionString()
    .WithPgAdminCustom(
        configure =>
        {
            configure.WithLifetime(containerLifetime);
        })
    .WithDataVolume()
    .WithArgs("-c", "max_connections=200")
    .WithLifetime(containerLifetime);

// -------------------- Universe
var universe = builder.AddProject<Universe>("universe", launchProfileName: null)
    .WithReference(postgres.AddDatabase("UniverseDb")).WaitFor(postgres);

builder.Build().Run();