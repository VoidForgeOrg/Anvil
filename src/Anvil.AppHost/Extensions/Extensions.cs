using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Anvil.AppHost.Extensions;

internal static partial class Extensions
{
    public static IResourceBuilder<ProjectResource> WithCommonEnvironment(this IResourceBuilder<ProjectResource> builder) =>
        builder
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");
}