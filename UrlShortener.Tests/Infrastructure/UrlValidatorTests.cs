using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using UrlShortener.Infrastructure.Validation;
using Xunit;

namespace UrlShortener.Tests.Infrastructure;

public sealed class UrlValidatorTests
{
    private readonly IDnsResolver _dnsResolverMock = Substitute.For<IDnsResolver>();
    private readonly UrlValidator _sut;

    public UrlValidatorTests()
    {
        _sut = new UrlValidator(_dnsResolverMock);
    }

    [Theory]
    [InlineData("https://example.com", "93.184.215.14")]
    [InlineData("http://github.com/dotnet/aspnetcore", "140.82.121.4")]
    public async Task ValidateAsync_WithValidPublicIPv4_ShouldReturnTrue(string validUrl, string resolvedIp)
    {
        // Arrange
        _dnsResolverMock
            .GetHostAddressesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([IPAddress.Parse(resolvedIp)]);

        // Act
        var (isValid, errorMessage) = await _sut.ValidateAsync(validUrl);

        // Assert
        isValid.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_WithValidPublicIPv6_ShouldReturnTrue()
    {
        // Arrange
        _dnsResolverMock
            .GetHostAddressesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([IPAddress.Parse("2606:2800:220:1:248:1893:25c8:1946")]);

        // Act
        var (isValid, errorMessage) = await _sut.ValidateAsync("https://example.com");

        // Assert
        isValid.Should().BeTrue();
        errorMessage.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-valid-url")]
    [InlineData("ftp://fileserver.com/file.txt")]
    public async Task ValidateAsync_WithInvalidFormatOrScheme_ShouldReturnFalseWithoutDnsCall(string invalidUrl)
    {
        // Act
        var (isValid, errorMessage) = await _sut.ValidateAsync(invalidUrl);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().NotBeNullOrWhiteSpace();
        await _dnsResolverMock.DidNotReceiveWithAnyArgs().GetHostAddressesAsync(default!, default);
    }

    [Theory]
    [InlineData("http://127.0.0.1/admin", "127.0.0.1")]
    [InlineData("http://localhost:5000", "127.0.0.1")]
    [InlineData("http://internal.company.local", "10.0.4.15")]
    [InlineData("http://router.home", "192.168.1.1")]
    [InlineData("http://aws-metadata.internal", "169.254.169.254")]
    public async Task ValidateAsync_WithPrivateIPv4AndLoopback_ShouldBeRejected(string privateUrl, string resolvedIp)
    {
        // Arrange
        _dnsResolverMock
            .GetHostAddressesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([IPAddress.Parse(resolvedIp)]);

        // Act
        var (isValid, errorMessage) = await _sut.ValidateAsync(privateUrl);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Contain("SSRF Protection");
    }

    [Theory]
    [InlineData("http://[::1]/status", "::1")]
    [InlineData("http://[fe80::1]/config", "fe80::1")]
    [InlineData("http://[fc00::1]/db", "fc00::1")]
    [InlineData("http://doc-v6.internal", "2001:db8::1")]
    public async Task ValidateAsync_WithPrivateAndReservedIPv6_ShouldBeRejected(string privateIPv6Url, string resolvedIp)
    {
        // Arrange
        _dnsResolverMock
            .GetHostAddressesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns([IPAddress.Parse(resolvedIp)]);

        // Act
        var (isValid, errorMessage) = await _sut.ValidateAsync(privateIPv6Url);

        // Assert
        isValid.Should().BeFalse();
        errorMessage.Should().Contain("SSRF Protection");
    }

    [Fact]
    public async Task ValidateAsync_WhenCancelled_ShouldThrowOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _dnsResolverMock
            .GetHostAddressesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = () => _sut.ValidateAsync("https://example.com", cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}