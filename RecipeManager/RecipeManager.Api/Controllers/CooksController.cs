using FluentResults;
using Microsoft.AspNetCore.Mvc;
using RecipeManager.Api.Extensions;
using RecipeManager.Application.Commands.Cooks;
using RecipeManager.Application.Common.Interfaces.Messaging;
using RecipeManager.Application.DTO.Cooks;

namespace RecipeManager.Api.Controllers;

// One controller per aggregate (ADR-028); the route nests under the recipe because a cook belongs to one.
[ApiController]
[Route("api/recipes/{recipeId:guid}/cooks")]
public class CooksController : ControllerBase
{
    private readonly ICommandDispatcher _commandDispatcher;
    private readonly ILogger<CooksController> _logger;

    public CooksController(ILogger<CooksController> logger, ICommandDispatcher commandDispatcher)
    {
        _logger = logger;
        _commandDispatcher = commandDispatcher;
    }

    [HttpPost]
    public async Task<IActionResult> Log([FromRoute] Guid recipeId, [FromBody] LogCookDto dto,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Logging a cook of recipe {recipeId}...");

        Result<CookDto> result = await _commandDispatcher.Dispatch<LogCookCommand, Result<CookDto>>(
            new LogCookCommand(recipeId, dto.CookedAt), cancellationToken);

        // There is no GET for a single cook. Location only has to identify the new resource (RFC 9110 §10.2.2),
        // and the URI that does is the one DELETE answers.
        return result.ToCreatedAtActionResult(nameof(Delete),
            new { recipeId, cookId = result.ValueOrDefault?.Id });
    }

    [HttpDelete("{cookId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid recipeId, [FromRoute] Guid cookId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Deleting cook {cookId} of recipe {recipeId}...");

        Result result = await _commandDispatcher.Dispatch<DeleteCookCommand, Result>(
            new DeleteCookCommand(recipeId, cookId), cancellationToken);

        return result.ToActionResult();
    }
}
