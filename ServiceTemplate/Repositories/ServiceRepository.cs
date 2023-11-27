namespace ServiceTemplate.Repositories
{
    public class ServiceRepository
    {
        private readonly ILogger<ServiceRepository> _logger;
        private readonly IConfiguration _configuration;

        public ServiceRepository (ILogger<ServiceRepository> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
    }
}
