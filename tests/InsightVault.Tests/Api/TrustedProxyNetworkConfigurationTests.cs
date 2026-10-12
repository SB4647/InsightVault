using InsightVault.Api.Networking;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;

namespace InsightVault.Tests.Api;

public sealed class TrustedProxyNetworkConfigurationTests
{
    [Fact]
    public void Configure_UsesOnlyConfiguredPrivateNetworks()
    {
        var options = new ForwardedHeadersOptions();

        TrustedProxyNetworkConfiguration.Configure(options, ["10.0.0.0/16"]);

        Assert.Contains(options.KnownIPNetworks, network => network.BaseAddress.ToString() == "10.0.0.0" && network.PrefixLength == 16);
        Assert.Empty(options.KnownProxies);
        Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedFor));
        Assert.True(options.ForwardedHeaders.HasFlag(ForwardedHeaders.XForwardedProto));
    }

    [Fact]
    public void Configure_RejectsInvalidCidr()
    {
        var options = new ForwardedHeadersOptions();

        Action configure = () => TrustedProxyNetworkConfiguration.Configure(options, ["not-a-network"]);

        var exception = Assert.Throws<InvalidOperationException>(configure);

        Assert.Contains("not-a-network", exception.Message);
    }
}
