using FluentResults;
using Microsoft.AspNetCore.Mvc;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.Common;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Queries.Recipes;

namespace RecipeManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipesController : ControllerBase
    {
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly ICommandDispatcher _commandDispatcher;
        private readonly ILogger<RecipesController> _logger;

        public RecipesController(ILogger<RecipesController> logger, IQueryDispatcher queryDispatcher,
            ICommandDispatcher commandDispatcher)
        {
            _logger = logger;
            _queryDispatcher = queryDispatcher;
            _commandDispatcher = commandDispatcher;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RecipeDto>>> Get(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching all recipes...");

            var query = new GetAllRecipesQuery();
            var result =
                await _queryDispatcher.Dispatch<GetAllRecipesQuery, IEnumerable<RecipeDto>>(query, cancellationToken);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IEnumerable<RecipeDto>>> Get([FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Fetching recipe with ID {id}...");

            var query = new GetRecipeByIdQuery(id);
            var result =
                await _queryDispatcher.Dispatch<GetRecipeByIdQuery, Result<RecipeDto>>(query, cancellationToken);

            if (result.IsSuccess)
            {
                return Ok(result.Value);
            }

            var problemDetails = new ProblemDetails
            {
                Title = "Error while obtaining recipe",
                Detail = result.Reasons.Select(r => r.Message).FirstOrDefault(),
                Status = StatusCodes.Status400BadRequest
            };

            return BadRequest(problemDetails);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRecipeCommand command,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating recipe...");
            Result<RecipeDto> result =
                await _commandDispatcher.Dispatch<CreateRecipeCommand, Result<RecipeDto>>(command, cancellationToken);

            return Ok(result.Value);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Deleting recipe with ID {id}...");

            var command = new DeleteRecipeCommand(id);
            var result = await _commandDispatcher.Dispatch<DeleteRecipeCommand, Result>(command, cancellationToken);

            if (result.IsSuccess)
            {
                return NoContent();
            }

            var problemDetails = new ProblemDetails
            {
                Title = "Error while deleting recipe",
                Detail = result.Reasons.Select(r => r.Message).FirstOrDefault(),
                Status = StatusCodes.Status400BadRequest
            };

            return BadRequest(problemDetails);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateRecipeDto dto,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Updating recipe with ID {id}...");

            var command = new UpdateRecipeCommand(id, dto.Title, dto.Description, dto.PreparationTime, dto.CookingTime,
                dto.Servings, dto.Ingredients, dto.Instructions);

            var result = await _commandDispatcher.Dispatch<UpdateRecipeCommand, Result>(command, cancellationToken);

            if (result.IsSuccess)
            {
                return NoContent();
            }

            var problemDetails = new ProblemDetails
            {
                Title = "Error while updating recipe",
                Detail = result.Reasons.Select(r => r.Message).FirstOrDefault(),
                Status = StatusCodes.Status400BadRequest
            };

            return BadRequest(problemDetails);
        }
    }
}