using Contracts.Characters;
using Domain.Characters;
using Mediator;

namespace Application.Characters.Commands;

public record CharacterCreateCommand(CharacterCreateRequest Request) : ICommand<CharacterResponse>;

public class CharacterCreateCommandHandler(ICharacterRepository repository)
    : ICommandHandler<CharacterCreateCommand, CharacterResponse>
{
    public async ValueTask<CharacterResponse> Handle(CharacterCreateCommand command, CancellationToken cancellationToken)
    {
        Character character = Character.Create(
            command.Request.Name,
            command.Request.Species,
            command.Request.Type,
            command.Request.Gender,
            command.Request.Location
        );

        var addedCharacter = await repository.AddAsync(character, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return new CharacterResponse(
            addedCharacter.Id,
            addedCharacter.Name,
            addedCharacter.Species,
            addedCharacter.Type,
            addedCharacter.Gender,
            addedCharacter.Location
        );
    }
}