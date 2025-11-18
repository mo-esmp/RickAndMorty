using Application.Common;
using Contracts.Characters;
using Domain.Characters;
using Mediator;

namespace Application.Characters.Events;

public class CharacterCreatedEventHandler(IAppCache cache) : INotificationHandler<CharacterCreatedEvent>
{
    public async ValueTask Handle(CharacterCreatedEvent notification, CancellationToken cancellationToken)
    {
        CharacterResponse character = new(
            notification.Character.Id,
            notification.Character.Name,
            notification.Character.Species,
            notification.Character.Type,
            notification.Character.Gender,
            notification.Character.Location);

        await UpdateCacheAsync(Constants.CharacterCacheKey, character, cancellationToken);
        if (!string.IsNullOrEmpty(notification.Character.Location))
        {
            var key = $"{Constants.CharacterCacheKey}_{notification.Character.Location.ToLower()}";

            await UpdateCacheAsync(key, character, cancellationToken);
        }
    }

    private async Task UpdateCacheAsync(string cacheKey, CharacterResponse character, CancellationToken cancellationToken)
    {
        List<CharacterResponse>? characters = await cache.GetAsync<List<CharacterResponse>>(cacheKey, cancellationToken);
        if (characters is null || characters.Any(c => c.Id == character.Id))
        {
            return;
        }

        // Load from database
        characters.Add(character);

        // Cache the result (5 minutes expiration)
        await cache.SetAsync(cacheKey, characters, token: cancellationToken);
    }
}