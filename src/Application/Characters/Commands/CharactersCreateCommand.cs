using Contracts.Characters;
using Domain.Characters;
using Mediator;

namespace Application.Characters.Commands;

public record CharactersCreateCommand(IEnumerable<CharacterCreateRequest> Request) : ICommand;

public class CharactersCreateCommandHandler(ICharacterRepository repository)
    : ICommandHandler<CharactersCreateCommand>
{
    public async ValueTask<Unit> Handle(CharactersCreateCommand command, CancellationToken cancellationToken)
    {
        List<Character> characters = command.Request.Select(request => new Character(
            id: 0,
            request.Name,
            request.Species,
            request.Type,
            request.Gender,
            request.Location
        )).ToList();

        await repository.AddRangeAsync(characters, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}