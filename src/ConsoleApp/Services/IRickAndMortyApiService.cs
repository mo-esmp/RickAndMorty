using ConsoleApp.Models;

namespace ConsoleApp.Services;

public interface IRickAndMortyApiService
{
    Task<List<ApiCharacter>> FetchAllAliveCharactersAsync(CancellationToken cancellationToken = default);
}