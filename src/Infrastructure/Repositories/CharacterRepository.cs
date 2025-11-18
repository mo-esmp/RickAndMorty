using Domain.Characters;
using Infrastructure.DataPersistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Infrastructure.Repositories;

public class CharacterRepository(ApplicationDbContext context) : ICharacterRepository
{
    public async Task<IEnumerable<Character>> GetAllAsync(CancellationToken cancellationToken = default)
        => await context.Characters.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<IEnumerable<Character>> GetByLocationAsync(string location, CancellationToken cancellationToken = default)
        => await context.Characters
            .AsNoTracking()
            .Where(c => c.Location!.ToLower() == location.ToLower())
            .ToListAsync(cancellationToken);

    public async Task<Character> AddAsync(Character character, CancellationToken cancellationToken = default)
    {
        EntityEntry<Character> entry = await context.Characters.AddAsync(character, cancellationToken);
        return entry.Entity;
    }

    public async Task AddRangeAsync(IEnumerable<Character> characters, CancellationToken cancellationToken = default)
    {
        context.ChangeTracker.AutoDetectChangesEnabled = false;
        await context.Characters.AddRangeAsync(characters, cancellationToken);
        context.ChangeTracker.AutoDetectChangesEnabled = true;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }
}
