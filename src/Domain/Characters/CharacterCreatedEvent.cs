namespace Domain.Characters;

public record CharacterCreatedEvent(Character Character) : IDomainEvent;