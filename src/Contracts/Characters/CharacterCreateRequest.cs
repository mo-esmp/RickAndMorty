using System.ComponentModel.DataAnnotations;

namespace Contracts.Characters;

public record CharacterCreateRequest(
    [Required][MaxLength(200)] string Name,

    [Required][MaxLength(100)] string Species,

    [MaxLength(100)] string? Type,

    [Required][MaxLength(10)] string Gender,

    [MaxLength(100)] string? Location
);
