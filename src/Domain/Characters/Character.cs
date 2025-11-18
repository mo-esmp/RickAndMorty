namespace Domain.Characters;

public class Character : EntityBase
{
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor.

    private Character()
#pragma warning restore CS8618
    {
    }

    public Character(int id, string name, string species, string? type, string gender, string? location)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(species);
        ArgumentException.ThrowIfNullOrWhiteSpace(gender);

        Id = id;
        Name = name;
        Species = species;
        Type = type;
        Gender = gender;
        Location = location;
    }

    public int Id { get; private set; }

    public string Name { get; private set; }

    public string Species { get; private set; }

    public string? Type { get; private set; }

    public string Gender { get; private set; }

    public string? Location { get; private set; }

    public static Character Create(string name, string species, string? type, string gender, string? location)
    {
        Character character = new(0, name, species, type, gender, location);
        character.DomainEvents.Add(new CharacterCreatedEvent(character));

        return character;
    }
}
