namespace Domain.Characters;

public interface ICharacterRepository
{
    Task<IEnumerable<Character>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<Character>> GetByLocationAsync(string location, CancellationToken cancellationToken = default);

    Task<Character> AddAsync(Character character, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Character> characters, CancellationToken cancellationToken = default);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
