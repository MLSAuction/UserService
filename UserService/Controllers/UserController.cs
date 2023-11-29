using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using UserService.Repositories;
using UserService.Models;

namespace UserService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;
        private readonly IUserRepository _userService;

        public UserController(ILogger<UserController> logger, IConfiguration configuration, IUserRepository userRepository, IMemoryCache cache)
        {
            _logger = logger;
            _configuration = configuration;
            _userService = userRepository;
            _cache = cache;
        }

        [HttpGet("{id}")]
        public IActionResult GetUser(int id)
        {
            UserDTO user = GetUserFromCache(id);

            if (user == null)
            {
                user = _userService.GetUser(id);

                if (user == null)
                {
                    return NotFound(); // Return 404 if user is not found
                }

                _logger.LogInformation($"User {user.UserId}, {user.FirstName} added to cache");
                SetUserInCache(user);
            }

            return Ok(user);
        }

        [HttpPost]
        public IActionResult AddUser([FromBody] UserDTO user)
        {
            if (user == null)
            {   
                //If NO "Whole-data". Example: If no texting data in the JSON. 
                return BadRequest("Invalid user data");
            }

            if (user.UserId == null)
            {
                //Check if there is ID 
                user.UserId = GenerateUniqueId();
            }

            if (_userService.GetUser((int)user.UserId) != null)
            {
                // Handle the case where the ID already exists (e.g., generate a new ID, so it doesnt match the already exist)
                user.UserId = GenerateUniqueId();
            }

            _userService.AddUser(user);
            SetUserInCache(user);

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }

        [HttpPut("{id}")]
        public IActionResult EditUser(int id, [FromBody] UserDTO user)
        {
            if (user == null)
            {
                return BadRequest("Invalid user data");
            }

            if (id != user.UserId)
            {
                return BadRequest("User ID in the request body does not match the route parameter");
            }

            _userService.UpdateUser(user);

            return Ok("User updated successfully");
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            var user = _userService.GetUser(id);

            if (user == null)
            {
                return NotFound(); // Return 404 if user is not found
            }

            _userService.DeleteUser(id);

            return Ok("User deleted successfully");
        }

        private UserDTO GetUserFromCache(int userId)
        {
            if (_cache.TryGetValue(userId, out UserDTO user))
            {
                return user;
            }

            return null;
        }

        private void SetUserInCache(UserDTO user)
        {
            var cacheExpiryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpiration = DateTime.Now.AddHours(1),
                SlidingExpiration = TimeSpan.FromMinutes(10),
                Priority = CacheItemPriority.High
            };
            _cache.Set(user.UserId, user, cacheExpiryOptions);
        }

        private int GenerateUniqueId()
        {
            return Math.Abs(Guid.NewGuid().GetHashCode());
        }
    }
}
