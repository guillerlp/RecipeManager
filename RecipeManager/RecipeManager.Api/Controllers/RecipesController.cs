using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using RecipeManager.Application.Commands.Recipes;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Queries.Recipes;

namespace RecipeManager.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipesController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<RecipesController> _logger;

        public RecipesController(IMediator mediator, ILogger<RecipesController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RecipeDto>>> Get(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching all recipes...");

            var list = await _mediator.Send(new GetAllRecipesQuery(), cancellationToken);
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<IEnumerable<RecipeDto>>> Get([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Fetching recipe with ID {id}...");

            var list = await _mediator.Send(new GetRecipeByIdQuery(id), cancellationToken);
            return Ok(list);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRecipeCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating recipe...");
            Result<RecipeDto> result = await _mediator.Send(command, cancellationToken);

            return Ok(result.Value);
        }
    }
}
