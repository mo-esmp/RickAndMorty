using System.Net;
using Application.Characters;
using Application.Common;
using Contracts.Characters;
using Domain.Characters;
using Infrastructure.DataPersistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using WebApp.Tests.TestSetup;

namespace WebApp.Tests;

[Collection("CharactersController")]
public class CharactersControllerTests(CustomWebApplicationFactory factory)
    : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        // Reset database and cache before each test
        await factory.ResetDatabaseAsync();
        await factory.ResetCacheAsync();
    }

    public Task DisposeAsync()
    {
        // Cleanup after each test if needed
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Index_WithoutLocationFilter_ReturnsCharacters()
    {
        // Arrange
        IServiceScope scope = factory.Server.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Characters.AddRange(
            new(0, "Rick Sanchez", "Human", "Scientist", "Male", "Earth"),
            new(0, "Morty Smith", "Human", "", "Male", "Earth"));

        await context.SaveChangesAsync();

        HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync("/Characters");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        string content = await response.Content.ReadAsStringAsync();
        content.ShouldContain("Rick Sanchez");
        content.ShouldContain("Morty Smith");
    }

    [Fact]
    public async Task Index_WithLocationFilter_PassesLocationToQuery()
    {
        // Arrange
        IServiceScope scope = factory.Server.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Characters.AddRange(
            new(0, "Rick Sanchez", "Human", "Scientist", "Male", "Earth"),
            new(0, "Morty Smith", "Human", "", "Male", "Earth"),
            new(0, "Alien Rick", "Alien", "", "Male", "Mars"));

        await context.SaveChangesAsync();

        HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync("/Characters?location=Earth");
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("Rick Sanchez");
        content.ShouldContain("Morty Smith");
        content.ShouldNotContain("Alien Rick");
    }

    [Fact]
    public async Task Index_WithEmptyResult_DisplaysNoCharactersMessage()
    {
        // Arrange
        HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync("/Characters");
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("No characters found");
    }

    [Fact]
    public async Task Index_FromDatabase_SetsHeaderToTrue()
    {
        // Arrange
        IServiceScope scope = factory.Server.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Characters.Add(new(0, "Rick Sanchez", "Human", "Scientist", "Male", "Earth"));
        await context.SaveChangesAsync();

        HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync("/Characters");

        // Assert
        response.Headers.TryGetValues("from-database", out IEnumerable<string>? values);
        Assert.NotNull(values);
        values.First().ShouldBe("true");
    }

    [Fact]
    public async Task Index_FromCache_SetsHeaderToFalse()
    {
        // Arrange
        IServiceScope scope = factory.Server.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Characters.Add(new(0, "Rick Sanchez", "Human", "Scientist", "Male", "Earth"));
        await context.SaveChangesAsync();

        HttpClient client = factory.CreateClient();

        // First request to populate cache
        await client.GetAsync("/Characters");

        // Act
        HttpResponseMessage response = await client.GetAsync("/Characters");

        // Assert
        response.Headers.TryGetValues("from-database", out IEnumerable<string>? values);
        Assert.NotNull(values);
        values.First().ShouldBe("false");
    }

    [Fact]
    public async Task Create_Get_ReturnsSuccessStatusCode()
    {
        // Arrange
        HttpClient client = factory.CreateClient();

        // Act
        HttpResponseMessage response = await client.GetAsync("/Characters/Create");
        string content = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");

        content.ShouldContain("<form");
        content.ShouldContain("Name");
        content.ShouldContain("Species");
        content.ShouldContain("Gender");
    }

    [Fact]
    public async Task Create_Post_WithMissingRequiredFields_ReturnsViewWithValidationErrors()
    {
        // Arrange
        HttpClient client = factory.CreateClient();

        HttpResponseMessage getResponse = await client.GetAsync("/Characters/Create");
        string getContent = await getResponse.Content.ReadAsStringAsync();
        string? token = ExtractAntiForgeryToken(getContent);

        Dictionary<string, string> formData = new()
        {
            ["Name"] = "",
            ["Species"] = "",
            ["Gender"] = "",
            ["__RequestVerificationToken"] = token!
        };

        // Act
        HttpResponseMessage postResponse =
            await client.PostAsync("/Characters/Create", new FormUrlEncodedContent(formData));
        string content = await postResponse.Content.ReadAsStringAsync();

        // Assert
        postResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        content.ShouldContain("The Name field is required.");
        content.ShouldContain("The Species field is required.");
        content.ShouldContain("The Gender field is required.");
    }

    [Fact]
    public async Task Create_Post_WithValidData_CreatesCharacterAndRedirects()
    {
        // Arrange
        HttpClient client = factory.CreateClient();

        // Get the create form to extract anti-forgery token
        HttpResponseMessage getResponse = await client.GetAsync("/Characters/Create");
        string getContent = await getResponse.Content.ReadAsStringAsync();
        string? token = ExtractAntiForgeryToken(getContent);

        Dictionary<string, string> formData = new()
        {
            ["Name"] = "Summer Smith",
            ["Species"] = "Human",
            ["Type"] = "Student",
            ["Gender"] = "Female",
            ["Location"] = "Earth",
            ["__RequestVerificationToken"] = token!
        };

        // Act
        HttpResponseMessage postResponse =
            await client.PostAsync("/Characters/Create", new FormUrlEncodedContent(formData));

        // Assert
        postResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        postResponse.Headers.Location?.ToString().ShouldContain("/Characters");

        // Verify character was created in database
        IServiceScope scope = factory.Server.Services.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Character? createdCharacter = await context.Characters
            .FirstOrDefaultAsync(c => c.Name == "Summer Smith");

        createdCharacter.ShouldNotBeNull();
        createdCharacter.Species.ShouldBe("Human");
        createdCharacter.Type.ShouldBe("Student");
        createdCharacter.Gender.ShouldBe("Female");
        createdCharacter.Location.ShouldBe("Earth");

        IAppCache cache = scope.ServiceProvider.GetRequiredService<IAppCache>();
        List<CharacterResponse>? cachedResult =
            await cache.GetAsync<List<CharacterResponse>>(Constants.CharacterCacheKey);
        CharacterResponse? cachedCharacter = cachedResult?.FirstOrDefault(c => c.Id == createdCharacter.Id);
        Assert.NotNull(cachedCharacter);
    }

    private static string? ExtractAntiForgeryToken(string htmlContent)
    {
        string tokenPattern = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        int startIndex = htmlContent.IndexOf(tokenPattern, StringComparison.Ordinal);
        if (startIndex == -1)
        {
            return null;
        }

        startIndex += tokenPattern.Length;
        int endIndex = htmlContent.IndexOf('"', startIndex);
        return htmlContent[startIndex..endIndex];
    }
}
