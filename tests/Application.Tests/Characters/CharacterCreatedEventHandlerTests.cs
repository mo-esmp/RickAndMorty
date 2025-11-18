using Application.Characters;
using Application.Characters.Events;
using Application.Common;
using Contracts.Characters;
using Domain.Characters;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;
using Shouldly;

namespace Application.Tests.Characters;

public class CharacterCreatedEventHandlerTests
{
    private readonly IAppCache _cache;
    private readonly CharacterCreatedEventHandler _handler;

    public CharacterCreatedEventHandlerTests()
    {
        _cache = Substitute.For<IAppCache>();
        _handler = new(_cache);
    }

    [Fact]
    public async Task Handle_AddsCharacterToCache_WhenCacheExists()
    {
        // Arrange
        Character character = new(1, "Rick Sanchez", "Human", "Scientist", "Male", "Earth");

        List<CharacterResponse> existingCache =
        [
            new(2, "Morty Smith", "Human", null, "Male", "Earth")
        ];

        _cache.GetAsync<List<CharacterResponse>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existingCache);

        CharacterCreatedEvent notification = new(character);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _cache.Received(1).GetAsync<List<CharacterResponse>>(Constants.CharacterCacheKey, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            Constants.CharacterCacheKey,
            Arg.Is<List<CharacterResponse>>(list => list.Count == 2 && list.Any(c => c.Id == 1)),
            Arg.Any<DistributedCacheEntryOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotUpdateCache_WhenCacheIsNull()
    {
        // Arrange
        Character character = new(1, "Rick Sanchez", "Human", "Scientist", "Male", "Earth");

        _cache.GetAsync<List<CharacterResponse>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((List<CharacterResponse>?)null);

        CharacterCreatedEvent notification = new(character);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _cache.Received(1).GetAsync<List<CharacterResponse>>(Constants.CharacterCacheKey, Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<List<CharacterResponse>>(),
            Arg.Any<DistributedCacheEntryOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotUpdateCache_WhenCharacterAlreadyExists()
    {
        // Arrange
        Character character = new(1, "Rick Sanchez", "Human", "Scientist", "Male", "Earth");

        List<CharacterResponse> existingCache =
        [
            new(1, "Rick Sanchez", "Human", "Scientist", "Male", "Earth"),
            new(2, "Morty Smith", "Human", null, "Male", "Earth")
        ];

        _cache.GetAsync<List<CharacterResponse>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existingCache);

        CharacterCreatedEvent notification = new(character);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _cache.Received(1).GetAsync<List<CharacterResponse>>(Constants.CharacterCacheKey, Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().SetAsync(
            Arg.Any<string>(),
            Arg.Any<List<CharacterResponse>>(),
            Arg.Any<DistributedCacheEntryOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdatesBothGeneralAndLocationCaches_WhenLocationExists()
    {
        // Arrange
        Character character = new(1, "Rick Sanchez", "Human", "Scientist", "Male", "Earth");

        List<CharacterResponse> generalCache = [];
        List<CharacterResponse> locationCache = [];

        _cache.GetAsync<List<CharacterResponse>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                string key = callInfo.Arg<string>();
                return key == Constants.CharacterCacheKey ? generalCache : locationCache;
            });

        CharacterCreatedEvent notification = new(character);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _cache.Received(1).GetAsync<List<CharacterResponse>>(Constants.CharacterCacheKey, Arg.Any<CancellationToken>());
        await _cache.Received(1).GetAsync<List<CharacterResponse>>($"{Constants.CharacterCacheKey}_earth", Arg.Any<CancellationToken>());

        await _cache.Received(1).SetAsync(
            Constants.CharacterCacheKey,
            Arg.Is<List<CharacterResponse>>(list => list.Count == 1 && list.Any(c => c.Id == 1)),
            Arg.Any<DistributedCacheEntryOptions?>(),
            Arg.Any<CancellationToken>());

        await _cache.Received(1).SetAsync(
            $"{Constants.CharacterCacheKey}_earth",
            Arg.Is<List<CharacterResponse>>(list => list.Count == 1 && list.Any(c => c.Id == 1)),
            Arg.Any<DistributedCacheEntryOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdatesOnlyGeneralCache_WhenLocationIsNull()
    {
        // Arrange
        Character character = new(1, "Rick Sanchez", "Human", "Scientist", "Male", null);

        List<CharacterResponse> generalCache = [];

        _cache.GetAsync<List<CharacterResponse>>(Constants.CharacterCacheKey, Arg.Any<CancellationToken>())
            .Returns(generalCache);

        CharacterCreatedEvent notification = new(character);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _cache.Received(1).GetAsync<List<CharacterResponse>>(Constants.CharacterCacheKey, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            Constants.CharacterCacheKey,
            Arg.Is<List<CharacterResponse>>(list => list.Count == 1 && list.Any(c => c.Id == 1)),
            Arg.Any<DistributedCacheEntryOptions?>(),
            Arg.Any<CancellationToken>());

        // Should not access location-specific cache
        await _cache.DidNotReceive().GetAsync<List<CharacterResponse>>(
            Arg.Is<string>(key => key.StartsWith($"{Constants.CharacterCacheKey}_") && key != Constants.CharacterCacheKey),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UpdatesOnlyGeneralCache_WhenLocationIsEmpty()
    {
        // Arrange
        Character character = new(1, "Rick Sanchez", "Human", "Scientist", "Male", "");

        List<CharacterResponse> generalCache = [];

        _cache.GetAsync<List<CharacterResponse>>(Constants.CharacterCacheKey, Arg.Any<CancellationToken>())
            .Returns(generalCache);

        CharacterCreatedEvent notification = new(character);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        await _cache.Received(1).GetAsync<List<CharacterResponse>>(Constants.CharacterCacheKey, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            Constants.CharacterCacheKey,
            Arg.Any<List<CharacterResponse>>(),
            Arg.Any<DistributedCacheEntryOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MapsCharacterPropertiesCorrectly()
    {
        // Arrange
        Character character = new(42, "Pickle Rick", "Human", "Pickle", "Male", "Sewer");

        List<CharacterResponse> existingCache = [];

        CharacterResponse? capturedCharacter = null;

        _cache.GetAsync<List<CharacterResponse>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(existingCache);

        _cache.SetAsync(
                Arg.Any<string>(),
                Arg.Do<List<CharacterResponse>>(list => capturedCharacter = list.FirstOrDefault()),
                Arg.Any<DistributedCacheEntryOptions?>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        CharacterCreatedEvent notification = new(character);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert
        capturedCharacter.ShouldNotBeNull();
        capturedCharacter.Id.ShouldBe(42);
        capturedCharacter.Name.ShouldBe("Pickle Rick");
        capturedCharacter.Species.ShouldBe("Human");
        capturedCharacter.Type.ShouldBe("Pickle");
        capturedCharacter.Gender.ShouldBe("Male");
        capturedCharacter.Location.ShouldBe("Sewer");
    }

    [Fact]
    public async Task Handle_UsesCorrectLocationCacheKey()
    {
        // Arrange
        string expectedKey = $"{Constants.CharacterCacheKey}_citadel";
        Character character = new(1, "Evil Morty", "Human", "", "Male", "Citadel");

        List<CharacterResponse> cache = [new(2, "Evil Rick", "Human", "", "Male", "Citadel")];

        _cache.GetAsync<List<CharacterResponse>>(Arg.Is(expectedKey), Arg.Any<CancellationToken>())
            .Returns(cache);

        CharacterCreatedEvent notification = new(character);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert

        await _cache.Received(1).GetAsync<List<CharacterResponse>>(expectedKey, Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(
            expectedKey,
            Arg.Any<List<CharacterResponse>>(),
            Arg.Any<DistributedCacheEntryOptions?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken()
    {
        // Arrange
        Character character = new(1, "Rick Sanchez", "Human", "Scientist", "Male", "Earth");
        CancellationTokenSource cts = new();

        List<CharacterResponse> cache = [];

        _cache.GetAsync<List<CharacterResponse>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cache);

        CharacterCreatedEvent notification = new(character);

        // Act
        await _handler.Handle(notification, cts.Token);

        // Assert
        await _cache.Received().GetAsync<List<CharacterResponse>>(Arg.Any<string>(), cts.Token);
        await _cache.Received().SetAsync(
            Arg.Any<string>(),
            Arg.Any<List<CharacterResponse>>(),
            Arg.Any<DistributedCacheEntryOptions?>(),
            cts.Token);
    }
}