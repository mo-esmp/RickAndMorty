using ConsoleApp.Models;
using ConsoleApp.Services;
using Shouldly;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ConsoleApp.Tests.Services;

public class RickAndMortyApiServiceTests : IDisposable
{
    private readonly WireMockServer _mockServer;
    private readonly HttpClient _httpClient;
    private readonly IRickAndMortyApiService _service;

    public RickAndMortyApiServiceTests()
    {
        _mockServer = WireMockServer.Start();
        _httpClient = new HttpClient { BaseAddress = new Uri(_mockServer.Url!) };
        _service = new RickAndMortyApiService(_httpClient);
    }

    [Fact]
    public async Task FetchAllAliveCharactersAsync_ReturnsOnlyAliveCharacters()
    {
        // Arrange
        var apiResponse = new
        {
            info = new { count = 3, pages = 1, next = (string?)null, prev = (string?)null },
            results = new[]
            {
                new
                {
                    id = 1,
                    name = "Rick Sanchez",
                    status = "Alive",
                    species = "Human",
                    type = "",
                    gender = "Male",
                    location = new { name = "Earth", url = "" }
                },
                new
                {
                    id = 2,
                    name = "Morty Smith",
                    status = "Alive",
                    species = "Human",
                    type = "",
                    gender = "Male",
                    location = new { name = "Earth", url = "" }
                },
                new
                {
                    id = 3,
                    name = "Dead Character",
                    status = "Dead",
                    species = "Human",
                    type = "",
                    gender = "Male",
                    location = new { name = "Unknown", url = "" }
                }
            }
        };

        _mockServer
            .Given(Request.Create().WithPath("/api/character/").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(apiResponse));

        // Act
        List<ApiCharacter> result = await _service.FetchAllAliveCharactersAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldAllBe(c => c.Status.Equals("Alive", StringComparison.OrdinalIgnoreCase));
        result.Select(c => c.Name).ShouldContain("Rick Sanchez");
        result.Select(c => c.Name).ShouldContain("Morty Smith");
        result.Select(c => c.Name).ShouldNotContain("Dead Character");
    }

    [Fact]
    public async Task FetchAllAliveCharactersAsync_HandlesMultiplePages()
    {
        // Arrange
        var firstPageResponse = new
        {
            info = new { count = 3, pages = 2, next = $"{_mockServer.Url}/api/character?page=2", prev = (string?)null },
            results = new[]
            {
                new
                {
                    id = 1,
                    name = "Rick Sanchez",
                    status = "Alive",
                    species = "Human",
                    type = "",
                    gender = "Male",
                    location = new { name = "Earth", url = "" }
                }
            }
        };

        var secondPageResponse = new
        {
            info = new { count = 3, pages = 2, next = (string?)null, prev = $"{_mockServer.Url}/api/character" },
            results = new[]
            {
                new
                {
                    id = 2,
                    name = "Morty Smith",
                    status = "Alive",
                    species = "Human",
                    type = "",
                    gender = "Male",
                    location = new { name = "Earth", url = "" }
                }
            }
        };

        _mockServer
            .Given(Request.Create().WithPath("/api/character/").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(firstPageResponse));

        _mockServer
            .Given(Request.Create().WithPath("/api/character").WithParam("page", "2").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBodyAsJson(secondPageResponse));

        // Act
        List<ApiCharacter> result = await _service.FetchAllAliveCharactersAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _mockServer.Stop();
        _mockServer.Dispose();
    }
}
