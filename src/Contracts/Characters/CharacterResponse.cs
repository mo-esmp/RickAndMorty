namespace Contracts.Characters;

public record CharacterResponse(
    int Id,
    string Name,
    string Species,
    string? Type,
    string Gender,
    string? Location
);