using Application.Characters.Commands;
using ConsoleApp.Models;
using ConsoleApp.Services;
using Contracts.Characters;
using Infrastructure.DataPersistence;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConsoleApp.Workers;

public class CharacterImportWorker(
    ILogger<CharacterImportWorker> logger,
    IHostApplicationLifetime lifetime,
    IServiceScopeFactory serviceScopeFactory
    //IRickAndMortyApiService apiService,
    //IMediator mediator,
    //ApplicationDbContext dbContext,
    ) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using IServiceScope scope = serviceScopeFactory.CreateScope();
            logger.LogInformation("Starting character import process...");

            // Ensure database is created
            ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await dbContext.Database.EnsureCreatedAsync(stoppingToken);

            // Clear existing data
            logger.LogInformation("Clearing existing character data...");
            //await repository.ClearAllAsync(stoppingToken);

            // Fetch alive characters from API
            logger.LogInformation("Fetching alive characters from Rick and Morty API...");

            IRickAndMortyApiService apiService = scope.ServiceProvider.GetRequiredService<IRickAndMortyApiService>();
            List<ApiCharacter> apiCharacters = await apiService.FetchAllAliveCharactersAsync(stoppingToken);
            logger.LogInformation("Fetched {Count} alive characters from API", apiCharacters.Count);

            // Map to domain models
            List<CharacterCreateRequest> characters = apiCharacters.Select(ac => new CharacterCreateRequest(
                ac.Name,
                ac.Species,
                ac.Type,
                ac.Gender,
                ac.Location?.Name
            )).ToList();

            // Save to database
            IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(new CharactersCreateCommand(characters), stoppingToken);
            logger.LogInformation("Character import completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during character import");
        }
        finally
        {
            lifetime.StopApplication();
        }
    }
}