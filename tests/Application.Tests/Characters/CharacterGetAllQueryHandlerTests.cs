using System.Collections.Generic;
using Application.Characters;
using Application.Characters.Queries;
using Application.Common;
using Contracts.Characters;
using Domain.Characters;
using Microsoft.Extensions.Caching.Distributed;
using NSubstitute;
using Shouldly;

namespace Application.Tests.Characters;

public class CharacterGetAllQueryHandlerTests
{
    private readonly ICharacterRepository _repository;
    private readonly IAppCache _cache;
    private readonly CharacterGetAllQueryHandler _handler;

    public CharacterGetAllQueryHandlerTests()
    {
        _repository = Substitute.For<ICharacterRepository>();
        _cache = Substitute.For<IAppCache>();
        _handler = new(_repository, _cache);
    }

    [Fact]
    public async Task Handle_WithoutLocation_ReturnsAllCharactersFromDatabase_WhenCacheMiss()
    {
        // Arrange
        List<Character> characters =
        [
            new(1, "Rick Sanchez", "Human", "", "Male", "Earth"),
            new(2, "Morty Smith", "Human", "", "Male", "Earth"),
            new(3, "Summer Smith", "Human", "", "Female", "Earth")
        ];

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(characters);

        // Simulate cache miss
        _cache.GetAsync<List<CharacterResponse>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((List<CharacterResponse>?)null);

        CharacterGetAllQuery query = new();

        // Act
        (bool fromDatabase, IEnumerable<CharacterResponse> result, int totalCount) = await _handler.Handle(query, CancellationToken.None);

        // Assert
        fromDatabase.ShouldBeTrue();
        Assert.NotNull(result);
        result.Count().ShouldBe(3);
        totalCount.ShouldBe(3);
        result.ShouldContain(c => c.Name == "Rick Sanchez");
        result.ShouldContain(c => c.Name == "Morty Smith");
        result.ShouldContain(c => c.Name == "Summer Smith");

        await _repository.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<DistributedCacheEntryOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithLocation_ReturnsFilteredCharactersFromDatabase_WhenCacheMiss()
    {
        // Arrange
        string location = "Earth";
        List<Character> characters =
        [
            new(1, "Rick Sanchez", "Human", "", "Male", "Earth"),
            new(2, "Morty Smith", "Human", "", "Male", "Earth")
        ];

        _repository.GetByLocationAsync(location, Arg.Any<CancellationToken>())
            .Returns(characters);

        // Simulate cache miss
        _cache.GetAsync<List<CharacterResponse>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((List<CharacterResponse>?)null);

        CharacterGetAllQuery query = new(location);

        // Act
        (bool fromDatabase, IEnumerable<CharacterResponse> result, int totalCount) = await _handler.Handle(query, CancellationToken.None);

        // Assert
        fromDatabase.ShouldBeTrue();
        Assert.NotNull(result);
        result.Count().ShouldBe(2);
        totalCount.ShouldBe(2);
        result.ShouldAllBe(c => c.Location == "Earth");

        await _repository.Received(1).GetByLocationAsync(location, Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
        await _cache.Received(1).SetAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<DistributedCacheEntryOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithoutLocation_ReturnsFromCache_WhenCacheHit()
    {
        // Arrange
        List<CharacterResponse> cachedCharacters =
        [
            new(1, "Rick Sanchez", "Human", "", "Male", "Earth"),
            new(2, "Morty Smith", "Human", "", "Male", "Earth")
        ];

        // Simulate cache hit
        _cache.GetAsync<List<CharacterResponse>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(cachedCharacters);

        CharacterGetAllQuery query = new();

        // Act
        (bool fromDatabase, IEnumerable<CharacterResponse> result, int totalCount) = await _handler.Handle(query, CancellationToken.None);

        // Assert
        fromDatabase.ShouldBeFalse();
        Assert.NotNull(result);
        result.Count().ShouldBe(2);
        totalCount.ShouldBe(2);

        await _repository.DidNotReceive().GetAllAsync(Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().GetByLocationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _cache.DidNotReceive().SetAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<DistributedCacheEntryOptions?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UsesCorrectCacheKey_ForDifferentLocations()
    {
        // Arrange
        string location = "Citadel";
        List<Character> characters = [new(1, "Evil Morty", "Human", "", "Male", "Citadel")];

        _repository.GetByLocationAsync(location, Arg.Any<CancellationToken>())
            .Returns(characters);

        string? capturedCacheKey = null;

        _cache.GetAsync<List<CharacterResponse>>(Arg.Do<string>(key => capturedCacheKey = key), Arg.Any<CancellationToken>())
            .Returns((List<CharacterResponse>?)null);

        CharacterGetAllQuery query = new(location);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedCacheKey.ShouldNotBeNull();
        capturedCacheKey.ShouldBe($"{Constants.CharacterCacheKey}_{location.ToLower()}");
    }

    [Fact]
    public async Task Handle_UsesDefaultCacheKey_WhenLocationIsNull()
    {
        // Arrange
        List<Character> characters = [new(1, "Rick Sanchez", "Human", "", "Male", "Earth")];

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(characters);

        string? capturedCacheKey = null;

        _cache.GetAsync<List<CharacterResponse>>(Arg.Do<string>(key => capturedCacheKey = key), Arg.Any<CancellationToken>())
            .Returns((List<CharacterResponse>?)null);

        CharacterGetAllQuery query = new();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        capturedCacheKey.ShouldNotBeNull();
        capturedCacheKey.ShouldBe(Constants.CharacterCacheKey);
    }

    [Fact]
    public async Task Handle_ReturnsEmptyCollection_WhenNoCharactersExist()
    {
        // Arrange
        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new List<Character>());

        _cache.GetAsync<List<CharacterResponse>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((List<CharacterResponse>?)null);

        CharacterGetAllQuery query = new();

        // Act
        (bool fromDatabase, IEnumerable<CharacterResponse> result, int totalCount) = await _handler.Handle(query, CancellationToken.None);

        // Assert
        fromDatabase.ShouldBeTrue();
        Assert.NotNull(result);
        result.ShouldBeEmpty();
        totalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_MapsCharacterProperties_Correctly()
    {
        // Arrange
        List<Character> characters = [new(42, "Pickle Rick", "Human", "Pickle", "Male", "Sewer")];

        _repository.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(characters);

        _cache.GetAsync<List<CharacterResponse>>(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((List<CharacterResponse>?)null);

        CharacterGetAllQuery query = new();

        // Act
        (_, IEnumerable<CharacterResponse> result, _) = await _handler.Handle(query, CancellationToken.None);

        // Assert
        CharacterResponse character = result.Single();
        character.Id.ShouldBe(42);
        character.Name.ShouldBe("Pickle Rick");
        character.Species.ShouldBe("Human");
        character.Type.ShouldBe("Pickle");
        character.Gender.ShouldBe("Male");
        character.Location.ShouldBe("Sewer");
    }
}
