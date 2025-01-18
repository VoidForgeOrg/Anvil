using System.Text;
using System.Text.Json;
using Aspire.Hosting.Postgres;

namespace Anvil.AppHost.Extensions;

/// <summary>
/// Borrowed from: https://github.com/dotnet/aspire/blob/main/src/Aspire.Hosting.PostgreSQL/PostgresBuilderExtensions.cs
///
/// The original implementation has some problems running on MacOS with Colima-based docker and also
/// does not support overriding the port where PgAdmin is exposed.
/// </summary>
internal static partial class Extensions
{
    
    /// <summary>
    /// Adds a pgAdmin 4 administration and development platform for PostgreSQL to the application model.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="PgAdminContainerImageTags.PgAdminTag"/> tag of the <inheritdoc cref="PgAdminContainerImageTags.PgAdminImage"/> container image.
    /// </remarks>
    /// <param name="builder">The PostgreSQL server resource builder.</param>
    /// <param name="configureContainer">Callback to configure PgAdmin container resource.</param>
    /// <param name="containerName">The name of the container (Optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<T> WithPgAdminCustom<T>(this IResourceBuilder<T> builder, Action<IResourceBuilder<PgAdminContainerResource>>? configureContainer = null, string? containerName = null,
        int? portOverride = null) where T : PostgresServerResource {
        ArgumentNullException.ThrowIfNull(builder);

        if (builder.ApplicationBuilder.Resources.OfType<PgAdminContainerResource>().SingleOrDefault() is { } existingPgAdminResource) {
            var builderForExistingResource = builder.ApplicationBuilder.CreateResourceBuilder(existingPgAdminResource);
            configureContainer?.Invoke(builderForExistingResource);
            return builder;
        }
        else {
            containerName ??= $"{builder.Resource.Name}-pgadmin";

            var pgAdminContainer = new PgAdminContainerResource(containerName);
            var pgAdminContainerBuilder = builder.ApplicationBuilder.AddResource(pgAdminContainer)
                .WithImage(PgAdminContainerImageTags.PgAdminImage, PgAdminContainerImageTags.PgAdminTag)
                .WithImageRegistry(PgAdminContainerImageTags.PgAdminRegistry)
                .WithHttpEndpoint(targetPort: 80, name: "http", port: portOverride)
                .WithEnvironment(SetPgAdminEnvironmentVariables)
                .WithBindMount("./Properties/pgadmin.tmp.json", "/pgadmin4/servers.json")
                .WithHttpHealthCheck("/browser")
                .ExcludeFromManifest();

            builder.ApplicationBuilder.Eventing.Subscribe<AfterEndpointsAllocatedEvent>(
                (e, ct) =>
                {
                    var serverFileMount = pgAdminContainer.Annotations.OfType<ContainerMountAnnotation>().Single(v => v.Target == "/pgadmin4/servers.json");
                    var postgresInstances = builder.ApplicationBuilder.Resources.OfType<PostgresServerResource>();

                    var serverFileBuilder = new StringBuilder();

                    using var stream = new FileStream(serverFileMount.Source!, FileMode.Create);
                    using var writer = new Utf8JsonWriter(stream);
                    // Need to grant read access to the config file on unix like systems.
                    if (!OperatingSystem.IsWindows()) {
                        File.SetUnixFileMode(serverFileMount.Source!, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
                    }

                    var serverIndex = 1;

                    writer.WriteStartObject();
                    writer.WriteStartObject("Servers");

                    foreach (var postgresInstance in postgresInstances) {
                        if (postgresInstance.PrimaryEndpoint.IsAllocated) {
                            var endpoint = postgresInstance.PrimaryEndpoint;

                            writer.WriteStartObject($"{serverIndex}");
                            writer.WriteString("Name", postgresInstance.Name);
                            writer.WriteString("Group", "Servers");
                            // PgAdmin assumes Postgres is being accessed over a default Aspire container network and hardcodes the resource address
                            // This will need to be refactored once updated service discovery APIs are available
                            writer.WriteString("Host", endpoint.Resource.Name);
                            writer.WriteNumber("Port", (int)endpoint.TargetPort!);
                            writer.WriteString("Username", postgresInstance.UserNameParameter?.Value ?? "postgres");
                            writer.WriteString("SSLMode", "prefer");
                            writer.WriteString("MaintenanceDB", "postgres");
                            writer.WriteString("PasswordExecCommand", $"echo '{postgresInstance.PasswordParameter.Value}'"); // HACK: Generating a pass file and playing around with chmod is too painful.
                            writer.WriteEndObject();
                        }

                        serverIndex++;
                    }

                    writer.WriteEndObject();
                    writer.WriteEndObject();

                    return Task.CompletedTask;
                }
            );

            configureContainer?.Invoke(pgAdminContainerBuilder);

            pgAdminContainerBuilder.WithRelationship(builder.Resource, "PgAdmin");

            return builder;
        }
    }

    private static void SetPgAdminEnvironmentVariables(EnvironmentCallbackContext context) {
        // Disables pgAdmin authentication.
        context.EnvironmentVariables.Add("PGADMIN_CONFIG_MASTER_PASSWORD_REQUIRED", "False");
        context.EnvironmentVariables.Add("PGADMIN_CONFIG_SERVER_MODE", "False");

        // You need to define the PGADMIN_DEFAULT_EMAIL and PGADMIN_DEFAULT_PASSWORD or PGADMIN_DEFAULT_PASSWORD_FILE environment variables.
        context.EnvironmentVariables.Add("PGADMIN_DEFAULT_EMAIL", "admin@domain.com");
        context.EnvironmentVariables.Add("PGADMIN_DEFAULT_PASSWORD", "admin");
    }

    internal static class PgAdminContainerImageTags
    {
        /// <remarks>docker.io</remarks>
        public const string PgAdminRegistry = "docker.io";

        /// <remarks>dpage/pgadmin4</remarks>
        public const string PgAdminImage = "dpage/pgadmin4";

        /// <remarks>8.12</remarks>
        public const string PgAdminTag = "8.12";
    }
}