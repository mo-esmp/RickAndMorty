using Application.Characters.Commands;
using Contracts.Characters;
using Domain.Characters;
using NSubstitute;
using Shouldly;

namespace Application.Tests.Characters;

public class CharacterCreateCommandHandlerTests
{
    private readonly ICharacterRepository _repository;
    private readonly CharacterCreateCommandHandler _handler;

    public CharacterCreateCommandHandlerTests()
    {
        _repository = Substitute.For<ICharacterRepository>();
        _handler = new(_repository);
    }

    [Fact]
    public async Task Handle_CreatesCharacterSuccessfully_WithAllProperties()
    {
        // Arrange
        CharacterCreateRequest request = new(
            Name: "Rick Sanchez",
            Species: "Human",
            Type: "Scientist",
            Gender: "Male",
            Location: "Earth"
        );

        Character expectedCharacter = new(
            id: 1,
            name: "Rick Sanchez",
            species: "Human",
            type: "Scientist",
            gender: "Male",
            location: "Earth"
        );

        _repository.AddAsync(Arg.Any<Character>(), Arg.Any<CancellationToken>())
            .Returns(expectedCharacter);

        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        CharacterCreateCommand command = new(request);

        // Act
        CharacterResponse result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe("Rick Sanchez");
        result.Species.ShouldBe("Human");
        result.Type.ShouldBe("Scientist");
        result.Gender.ShouldBe("Male");
        result.Location.ShouldBe("Earth");

        await _repository.Received(1).AddAsync(Arg.Any<Character>(), Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CreatesCharacterSuccessfully_WithNullableProperties()
    {
        // Arrange
        CharacterCreateRequest request = new(
            Name: "Morty Smith",
            Species: "Human",
            Type: null,
            Gender: "Male",
            Location: null
        );

        Character expectedCharacter = new(
            id: 2,
            name: "Morty Smith",
            species: "Human",
            type: null,
            gender: "Male",
            location: null
        );

        _repository.AddAsync(Arg.Any<Character>(), Arg.Any<CancellationToken>())
            .Returns(expectedCharacter);

        _repository.SaveChangesAsync(Arg.Any<CancellationToken>())
            .Returns(1);

        CharacterCreateCommand command = new(request);

        // Act
        CharacterResponse result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(2);
        result.Name.ShouldBe("Morty Smith");
        result.Species.ShouldBe("Human");
        result.Type.ShouldBeNull();
        result.Gender.ShouldBe("Male");
        result.Location.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_PropagatesCancellationToken_ToRepositoryCalls()
    {
        // Arrange
        CharacterCreateRequest request = new(
            Name: "Birdperson",
            Species: "Bird",
            Type: null,
            Gender: "Male",
            Location: "Planet Squanch"
        );

        CancellationTokenSource cts = new();

        _repository.AddAsync(Arg.Any<Character>(), cts.Token)
            .Returns(new Character(4, "Birdperson", "Bird", null, "Male", "Planet Squanch"));

        _repository.SaveChangesAsync(cts.Token)
            .Returns(1);

        CharacterCreateCommand command = new(request);

        // Act
        await _handler.Handle(command, cts.Token);

        // Assert
        await _repository.Received(1).AddAsync(Arg.Any<Character>(), cts.Token);
        await _repository.Received(1).SaveChangesAsync(cts.Token);
    }
}