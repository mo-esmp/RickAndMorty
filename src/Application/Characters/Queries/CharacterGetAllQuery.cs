using Application.Common;
using Contracts.Characters;
using Domain.Characters;
using Mediator;

namespace Application.Characters.Queries;

public record CharacterGetAllQuery(
    string? Location = null,
    int PageNumber = 1,
    int PageSize = 20
) : IQuery<(bool fromDatabse, IEnumerable<CharacterResponse> Characters, int TotalCount)>;

public class CharacterGetAllQueryHandler(ICharacterRepository repository, IAppCache cache)
    : IQueryHandler<CharacterGetAllQuery, (bool, IEnumerable<CharacterResponse>, int)>
{
    public async ValueTask<(bool, IEnumerable<CharacterResponse>, int)> Handle(CharacterGetAllQuery query, CancellationToken cancellationToken)
    {
        bool fromDatabase = false;
        string cacheKey = string.IsNullOrWhiteSpace(query.Location) ? Constants.CharacterCacheKey : $"{Constants.CharacterCacheKey}_{query.Location.ToLower()}";

        (IEnumerable<CharacterResponse> filtered, int totalCount) = ([], 0);

        // Try to get from cache
        List<CharacterResponse>? cachedResult = await cache.GetAsync<List<CharacterResponse>>(cacheKey, cancellationToken);
        if (cachedResult is not null)
        {
            (filtered, totalCount) = PaginateCharacters(cachedResult, query.PageNumber, query.PageSize);
            return (fromDatabase, filtered, totalCount);
        }

        // Load from database
        fromDatabase = true;
        List<CharacterResponse> characters = await LoadCharactersFromDatabaseAsync(query, cancellationToken);

        // Cache the result
        await cache.SetAsync(cacheKey, characters, token: cancellationToken);

        (filtered, totalCount) = PaginateCharacters(characters, query.PageNumber, query.PageSize);
        return (fromDatabase, filtered, totalCount);
    }

    private static (IEnumerable<CharacterResponse>, int) PaginateCharacters(
        List<CharacterResponse> characters,
        int pageNumber,
        int pageSize)
    {
        int totalCount = characters.Count;
        IEnumerable<CharacterResponse> pagedCharacters = characters
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
        return (pagedCharacters, totalCount);
    }

    private async Task<List<CharacterResponse>> LoadCharactersFromDatabaseAsync(
        CharacterGetAllQuery query,
        CancellationToken cancellationToken)
    {
        IEnumerable<Character> characters = !string.IsNullOrWhiteSpace(query.Location)
            ? await repository.GetByLocationAsync(query.Location, cancellationToken)
            : await repository.GetAllAsync(cancellationToken);

        IEnumerable<CharacterResponse> characterResponses = characters.Select(c => new CharacterResponse(
            c.Id,
            c.Name,
            c.Species,
            c.Type,
            c.Gender,
            c.Location
        ));

        return characterResponses.ToList();
    }
}
