using FluentResults;
using Microsoft.AspNetCore.Mvc;
using RecipeManager.Api.Extensions;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.Common.Interfaces.Messaging;
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

        public RecipesController(ILogger<RecipesController> logger,
            IQueryDispatcher queryDispatcher,
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

            GetAllRecipesQuery query = new();
            IEnumerable<RecipeDto> recipes =
                await _queryDispatcher.Dispatch<GetAllRecipesQuery, IEnumerable<RecipeDto>>(query, cancellationToken);

            return Ok(recipes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RecipeDto>> Get([FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Fetching recipe with ID {id}...");

            GetRecipeByIdQuery query = new(id);
            Result<RecipeDto> result =
                await _queryDispatcher.Dispatch<GetRecipeByIdQuery, Result<RecipeDto>>(query, cancellationToken);

            return result.ToActionResult();
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRecipeCommand command,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating recipe...");

            Result<RecipeDto> result =
                await _commandDispatcher.Dispatch<CreateRecipeCommand, Result<RecipeDto>>(command, cancellationToken);

            return result.ToCreatedAtActionResult(nameof(Get), new { id = result.ValueOrDefault?.Id });

        }

        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Deleting recipe with ID {id}...");

            DeleteRecipeCommand command = new(id);
            Result result = await _commandDispatcher.Dispatch<DeleteRecipeCommand, Result>(command, cancellationToken);

            return result.ToActionResult();
        }

        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateRecipeDto dto,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Updating recipe with ID {id}...");

            UpdateRecipeCommand command = new(id, dto.Title, dto.Description, dto.PreparationTime, dto.CookingTime,
                dto.Servings, dto.Ingredients, dto.Instructions);

            Result result = await _commandDispatcher.Dispatch<UpdateRecipeCommand, Result>(command, cancellationToken);

            return result.ToActionResult();
        }
    }
}