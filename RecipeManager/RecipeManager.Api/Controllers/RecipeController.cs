using MediatR;
using Microsoft.AspNetCore.Mvc;
using RecipeManager.Application.DTO.Recipes;
using RecipeManager.Application.Queries.Recipes;

namespace RecipeManager.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RecipeController : ControllerBase
    {
        #region Fields
        private readonly IMediator _mediator;
        #endregion

        #region Constructor
        public RecipeController(IMediator mediator)
        {
            _mediator = mediator;
        }
        #endregion

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RecipeDto>>> Get(CancellationToken cancellationToken)
        {
            Console.WriteLine("Getting all devices");

            var list = await _mediator.Send(new GetAllRecipesQuery());
            return Ok(list);
        }
    }
}
