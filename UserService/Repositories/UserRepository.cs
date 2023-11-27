using Microsoft.AspNetCore.Mvc.Formatters;


namespace UserService.Repositories
{
    public class UserRepository
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public UserRepository (ILogger<UserRepository>logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }
    }
}
