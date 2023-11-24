using Microsoft.AspNetCore.Mvc;
using UserService.Repositories;

namespace UserService.Controllers
{
    public class UserController : Controller
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ServiceRepository _repository;

        UserController (ILogger logger, IConfiguration configuration, ServiceRepository repository)
        {
            _logger = logger;
            _configuration = configuration;
            _repository = repository;
        }

        [HttpGet]

        [HttpPost]

        [HttpPut]

        [HttpDelete]


    }
}
