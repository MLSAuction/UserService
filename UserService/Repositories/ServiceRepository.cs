namespace UserService.Repositories
{
    public class ServiceRepository
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        ServiceRepository (ILogger logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
    }
}
