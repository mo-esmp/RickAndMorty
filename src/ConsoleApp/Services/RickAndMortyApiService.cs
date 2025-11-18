using ConsoleApp.Models;
using System.Text.Json;

namespace ConsoleApp.Services;

public class RickAndMortyApiService(HttpClient httpClient) : IRickAndMortyApiService
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public async Task<List<ApiCharacter>> FetchAllAliveCharactersAsync(CancellationToken cancellationToken = default)
    {
        List<ApiCharacter> allCharacters = [];
        string url = "/api/character/";

        while (!string.IsNullOrWhiteSpace(url))
        {
            HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            string content = await response.Content.ReadAsStringAsync(cancellationToken);
            RickAndMortyApiResponse? apiResponse = JsonSerializer.Deserialize<RickAndMortyApiResponse>(content, s_jsonOptions);

            if (apiResponse?.Results != null)
            {
                List<ApiCharacter> aliveCharacters = apiResponse.Results
                    .Where(c => c.Status.Equals("Alive", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                allCharacters.AddRange(aliveCharacters);
            }

            url = apiResponse?.Info?.Next ?? string.Empty;
        }

        return allCharacters;
    }
}
