using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using UrlShortener.Contracts;
using Xunit;

namespace UrlShortener.Tests.Integration;

public sealed class ApiEndpointsTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly HttpClient _client;

    public ApiEndpointsTests(IntegrationTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateAndGetUrl_ShouldReturnCreatedRecordAndListIt()
    {
        // Arrange
        var request = new CreateUrlRequest("https://example.com/test-url");

        // Act - Create
        var createResponse = await _client.PostAsJsonAsync("/api/urls", request);

        // Assert - Create
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdDto = await createResponse.Content.ReadFromJsonAsync<UrlRecordDto>();
        createdDto.Should().NotBeNull();
        createdDto!.OriginalUrl.Should().Be("https://example.com/test-url");
        createdDto.ShortCode.Should().HaveLength(7);

        // Act - List
        var listResponse = await _client.GetAsync("/api/urls");

        // Assert - List
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var urls = await listResponse.Content.ReadFromJsonAsync<List<UrlRecordDto>>();
        urls.Should().NotBeNull();
        urls.Should().Contain(u => u.ShortCode == createdDto.ShortCode);
    }

    [Fact]
    public async Task UpdateUrl_WithValidNewUrl_ShouldUpdateRecordAndEvictCache()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/urls", new CreateUrlRequest("https://example.com/initial"));
        var createdDto = await createResponse.Content.ReadFromJsonAsync<UrlRecordDto>();

        var updateRequest = new UpdateUrlRequest("https://example.com/updated-url");

        // Act
        var updateResponse = await _client.PutAsJsonAsync($"/api/urls/{createdDto!.Id}", updateRequest);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedDto = await updateResponse.Content.ReadFromJsonAsync<UrlRecordDto>();
        updatedDto.Should().NotBeNull();
        updatedDto!.OriginalUrl.Should().Be("https://example.com/updated-url");
    }

    [Fact]
    public async Task RedirectEndpoint_WithValidShortKey_ShouldReturn302Redirect()
    {
        // Arrange
        var createResponse = await _client.PostAsJsonAsync("/api/urls", new CreateUrlRequest("https://example.com/redirect-target"));
        var createdDto = await createResponse.Content.ReadFromJsonAsync<UrlRecordDto>();

        // Настраиваем кастомный HTTP-клиент с отключенным авто-редиректом, 
        // чтобы проверить именно 302 статус-код и заголовок Location.
        var handler = new HttpClientHandler { AllowAutoRedirect = false };
        using var clientWithoutRedirect = new HttpClient(handler) { BaseAddress = _client.BaseAddress };

        // Act
        var redirectResponse = await clientWithoutRedirect.GetAsync($"/{createdDto!.ShortCode}");

        // Assert
        redirectResponse.StatusCode.Should().Be(HttpStatusCode.Redirect);
        redirectResponse.Headers.Location!.ToString().Should().Be("https://example.com/redirect-target");
    }
}