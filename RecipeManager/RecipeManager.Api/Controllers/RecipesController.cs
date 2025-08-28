using FluentResults;
using Microsoft.AspNetCore.Mvc;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.Common.Constants;
using RecipeManager.Application.Common.Interfaces.Caching;
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
        private readonly ICacheService _cacheService;
        
        public RecipesController(ILogger<RecipesController> logger, IQueryDispatcher queryDispatcher,
            ICommandDispatcher commandDispatcher, ICacheService cacheService)
        {
            _logger = logger;
            _queryDispatcher = queryDispatcher;
            _commandDispatcher = commandDispatcher;
            _cacheService = cacheService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RecipeDto>>> Get(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching all recipes...");

            IEnumerable<RecipeDto>? cachedRecipes =
                await _cacheService.GetAsync<IEnumerable<RecipeDto>>(CacheKeys.ALL_RECIPES, cancellationToken);
            
            if (cachedRecipes != null)
                return Ok(cachedRecipes);
            
            GetAllRecipesQuery query = new ();
            IEnumerable<RecipeDto> recipes = 
                await _queryDispatcher.Dispatch<GetAllRecipesQuery, IEnumerable<RecipeDto>>(query, cancellationToken);

            await _cacheService.SetAsync(CacheKeys.ALL_RECIPES, recipes, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(5), cancellationToken);

            return Ok(recipes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RecipeDto>> Get([FromRoute] Guid id,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Fetching recipe with ID {id}...");

            string cacheKey = CacheKeys.GetRecipeKey(id);
            RecipeDto? cachedRecipe = await _cacheService.GetAsync<RecipeDto>(cacheKey, cancellationToken);

            if (cachedRecipe != null)
            {
                return Ok(cachedRecipe);
            }
            
            GetRecipeByIdQuery query = new (id);
            Result<RecipeDto> result =
                await _queryDispatcher.Dispatch<GetRecipeByIdQuery, Result<RecipeDto>>(query, cancellationToken);

            if (result.IsSuccess)
            {
                await _cacheService.SetAsync(cacheKey, result.Value, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(15), cancellationToken);
                return Ok(result.Value);
            }

            ProblemDetails problemDetails = new ProblemDetails
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
            
            if (result.IsSuccess)
            {
                await _cacheService.RemoveAsync(CacheKeys.ALL_RECIPES, cancellationToken);
                string newRecipeCacheKey =  CacheKeys.GetRecipeKey(result.Value.Id);
                await _cacheService.SetAsync(newRecipeCacheKey, result.Value, TimeSpan.FromMinutes(30), TimeSpan.FromMinutes(15), cancellationToken);
                
                return CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value);
            }
            
            ProblemDetails problemDetails = new ProblemDetails
            {
                Title = "Error while creating recipe",
                Detail = result.Reasons.Select(r => r.Message).FirstOrDefault(),
                Status = StatusCodes.Status400BadRequest
            };

            return BadRequest(problemDetails);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Deleting recipe with ID {id}...");

            DeleteRecipeCommand command = new (id);
            Result result = await _commandDispatcher.Dispatch<DeleteRecipeCommand, Result>(command, cancellationToken);

            if (result.IsSuccess)
            {
                await _cacheService.RemoveAsync(CacheKeys.ALL_RECIPES, cancellationToken);
                await _cacheService.RemoveAsync(CacheKeys.GetRecipeKey(id), cancellationToken);

                return NoContent();
            }

            ProblemDetails problemDetails = new ProblemDetails
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

            UpdateRecipeCommand command = new (id, dto.Title, dto.Description, dto.PreparationTime, dto.CookingTime,
                dto.Servings, dto.Ingredients, dto.Instructions);

            Result result = await _commandDispatcher.Dispatch<UpdateRecipeCommand, Result>(command, cancellationToken);
            
            if (result.IsSuccess)
            {
                await _cacheService.RemoveAsync(CacheKeys.ALL_RECIPES, cancellationToken);
                await _cacheService.RemoveAsync(CacheKeys.GetRecipeKey(id), cancellationToken);

                return NoContent();
            }

            ProblemDetails problemDetails = new ProblemDetails
            {
                Title = "Error while updating recipe",
                Detail = result.Reasons.Select(r => r.Message).FirstOrDefault(),
                Status = StatusCodes.Status400BadRequest
            };

            return BadRequest(problemDetails);
        }
    }
}