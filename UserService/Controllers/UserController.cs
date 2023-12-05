using Microsoft.AspNetCore.Mvc;
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
        private readonly IUserRepository _userService;

        public UserController(ILogger<UserController> logger, IConfiguration configuration, IUserRepository userRepository)
        {
            _logger = logger;
            _configuration = configuration;
            _userService = userRepository;
        }

        [HttpGet]
        public IActionResult GetAllUsers()
        {
            var users = _userService.GetAllUsers();

            if (users == null || !users.Any())
            {
                return NotFound(); // Return 404 if no users are found
            }

            return Ok(users);
        }

        [HttpGet("{id}")]
        public IActionResult GetUser(int id)
        {

            UserDTO user = _userService.GetUser(id);

            if (user == null)
            {
                return NotFound(); // Return 404 if user is not found
            }

            _logger.LogInformation($"User {user.UserId}, {user.FirstName} - Retrieved ");

            return Ok(user);
        }

        [HttpGet("name/{name}")]
        public IActionResult GetUserByName(string name)
        {

            UserDTO user = _userService.GetUserByName(name);

            if (user == null)
            {
                return NotFound(); // Return 404 if user is not found
            }

            _logger.LogInformation($"User {user.UserId}, {user.Username} - Retrieved ");

            return Ok(user);
        }

        [HttpPost("register/{user}")]
        public IActionResult RegisterUser(UserDTO user)
        {
            try
            {
                user = ValidateUser(user);
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Invalid user data, {ex}");
            }

            user.UserId = GenerateUniqueUserId();

            _userService.AddUser(user);

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }

        [HttpPost("login/{authdto}")]
        public IActionResult Login([FromBody] AuthDTO authDTO)
        {
            UserDTO user = _userService.GetUserByName(authDTO.Username);

            if (user.Username == authDTO.Username && user.Password == authDTO.Password)
            {
                return Ok("User authorized");
            }

            return BadRequest();
        }

        [HttpPost]
        public IActionResult AddUser([FromBody] UserDTO inputUser)
        {
            UserDTO user;

            try
            {
                user = ValidateUser(inputUser);
            }
            catch (ArgumentException ex)
            {
                return BadRequest($"Invalid user data, {ex}");
            }

            if (user.UserId == null)
            {
                user.UserId = GenerateUniqueUserId();
            }

            _userService.AddUser(user);

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);

        }

        private UserDTO ValidateUser(UserDTO user)
        {
            // exception for no data
            if (user == null)
            {
                throw new ArgumentException("Null user data");
            }

            //Exception for email
            if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains("@"))
            {
                throw new ArgumentException("Invalid email");
            }

            //exception for duplicate usernames
            if (_userService.GetUserByName(user.Username) != null)
            {
                throw new ArgumentException($"User with name {user.Username} already exists");
            }

            return user;
        }

        private int GenerateUniqueUserId()
        {
            int id = Math.Abs(Guid.NewGuid().GetHashCode());

            //While loop to repeatedly check for duplicate ids in db (statistically very unlikely)
            bool? duplicateFlag = null;
            while (duplicateFlag != false)
            {
                if (_userService.GetUser(id) != null)
                {
                    // Handle the case where the ID already exists (e.g., generate a new ID, so it doesnt match an already existing one)
                    id = GenerateUniqueUserId();
                    duplicateFlag = true;
                }
                else
                {
                    duplicateFlag = false;
                }
            }

            return id;
        }

        [HttpPut]
        public IActionResult EditUser([FromBody] UserDTO user)
        {
            if (user.UserId == null)
            {
                return BadRequest("Invalid user data");
            }

            if (_userService.GetUser((int)user.UserId) == null)
            {
                return BadRequest("User ID does not exist in the database");
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
    }
}
