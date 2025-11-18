using Application.Characters.Commands;
using Application.Characters.Queries;
using Contracts.Characters;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;

namespace WebApp.Controllers;

public class CharactersController(IMediator mediator) : Controller
{
    public async Task<IActionResult> Index(string? location, int pageNumber = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        // Ensure valid page number and size
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);

        CharacterGetAllQuery query = new(location, pageNumber, pageSize);
        var (fromDatabase, characters, totalCount) = await mediator.Send(query, cancellationToken);

        PagedResult<CharacterResponse> pagedResult = new()
        {
            Items = characters,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            FromDatabase = fromDatabase
        };

        Response.Headers.Append("from-database", fromDatabase.ToString().ToLower());
        ViewData["Location"] = location;

        return View(pagedResult);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CharacterCreateRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        CharacterCreateCommand command = new(request);
        await mediator.Send(command, cancellationToken);

        TempData["SuccessMessage"] = "Character created successfully!";
        return RedirectToAction(nameof(Index));
    }
}
