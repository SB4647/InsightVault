using Microsoft.AspNetCore.HttpOverrides;
using IpNetwork = System.Net.IPNetwork;

namespace InsightVault.Api.Networking;

/// <summary>
/// Restricts forwarded-header processing to explicitly configured reverse-proxy networks.
/// This prevents a public caller from forging its client address or HTTPS scheme with X-Forwarded headers.
/// </summary>
public static class TrustedProxyNetworkConfiguration
{
    /// <summary>
    /// Configures client address and protocol forwarding for the networks that contain approved reverse proxies.
    /// </summary>
    public static void Configure(ForwardedHeadersOptions options, IEnumerable<string> trustedProxyNetworks)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();

        foreach (var cidr in trustedProxyNetworks.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (!IpNetwork.TryParse(cidr, out var network))
            {
                throw new InvalidOperationException($"ForwardedHeaders:TrustedProxyNetworks contains an invalid CIDR: '{cidr}'.");
            }

            options.KnownIPNetworks.Add(network);
        }
    }
}
