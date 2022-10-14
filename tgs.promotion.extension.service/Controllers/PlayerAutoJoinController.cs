using MediatR;
using Microsoft.AspNetCore.Mvc;
using tgs.promotion.extension.core.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace tgs.promotion.extension.service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PlayerAutoJoinController : ControllerBase
    {
        // GET: api/<PlayerAutoJoinController>
        [HttpGet]
        public Task PlayerAutoJoinPromotionAsync([FromServices] IMediator mediator)
        {
            var request = new PlayerAutoJoinRequest();

            return mediator.Send(request);
        }
    }
}
