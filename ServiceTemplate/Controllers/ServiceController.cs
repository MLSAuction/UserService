using Microsoft.AspNetCore.Mvc;
using ServiceTemplate.Repositories;

namespace ServiceTemplate.Controllers
{
    public class ServiceController : Controller
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly ServiceRepository _repository;

        ServiceController (ILogger logger, IConfiguration configuration, ServiceRepository repository)
        {
            _logger = logger;
            _configuration = configuration;
            _repository = repository;
        }
    }
}
