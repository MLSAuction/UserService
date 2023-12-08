using Microsoft.AspNetCore.Mvc;
using UserService.Repositories;
using UserService.Models;
using System.Xml.Linq;

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
            _logger.LogInformation("Attempting to retrieve all users");
            var users = _userService.GetAllUsers();

            if (users == null || !users.Any())
            {
                _logger.LogWarning("No users found");
                return NotFound(); // Return 404 if no users are found
            }
            _logger.LogInformation($"Retrieved {users.Count()} users");
            return Ok(users);
        }

        [HttpGet("{id}")]
        public IActionResult GetUser(int id)
        {
            _logger.LogInformation($"Attempting to retrieve user with ID: {id}");
            UserDTO user = _userService.GetUser(id);

            if (user == null)
            {
                _logger.LogWarning($"No users with ID: {id} found");
                return NotFound(); // Return 404 if user is not found
            }

            _logger.LogInformation($"User {user.UserId}, {user.Username} - Retrieved ");
            return Ok(user);
        }

        [HttpGet("username/{username}")]
        public IActionResult GetUserByUsername(string username)
        {
            _logger.LogInformation("Attempting to retrieve user by name");
            UserDTO user = _userService.GetUserByUsername(username);

            if (user == null)
            {
                _logger.LogWarning($"No users with username: {username} found");
                return NotFound($"No users with username: {username} found"); // Return 404 if user is not found
            }

            _logger.LogInformation($"User {user.UserId}, {user.Username} - Retrieved ");

            return Ok(user);
        }

        [HttpPost("register/{user}")]
        public IActionResult RegisterUser(UserDTO user)
        {
            _logger.LogInformation("Attempting to register a new user");
            try
            {
                user = ValidateUser(user);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError($"Registration failed: {ex.Message}");
                return BadRequest($"Invalid user data, {ex}");
            }

            user.UserId = GenerateUniqueUserId();

            _userService.AddUser(user);

            _logger.LogInformation($"User registered successfully with ID: {user.UserId}"); // Logning ved succes
            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);
        }

        [HttpPost("login/{authdto}")]
        public IActionResult Login([FromBody] AuthDTO authDTO)
        {
            UserDTO user = _userService.GetUserByUsername(authDTO.Username);
            _logger.LogInformation($"Login attempt for user: {authDTO.Username}");

            if (user.Username == authDTO.Username && user.Password == authDTO.Password)
            {
                _logger.LogInformation($"User {authDTO.Username} logged in successfully.");
                return Ok("User authorized");
            }

            _logger.LogWarning($"Login failed for user: {authDTO.Username}");
            return BadRequest();
        }


        [HttpPost] 
        public IActionResult AddUser([FromBody] UserDTO inputUser)
        {
            UserDTO user;
            _logger.LogInformation("Attempting to add a new user");
            try
            {
                user = ValidateUser(inputUser);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError($"User validation failed: {ex.Message}");
                return BadRequest($"Invalid user data, {ex}");
            }

            if (user.UserId == null)
            {
                user.UserId = GenerateUniqueUserId();
            }

            _userService.AddUser(user);

            _logger.LogInformation($"User added successfully with ID: {user.UserId}");
            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);

        }



        private UserDTO ValidateUser(UserDTO user)
        {
            _logger.LogInformation("Starting user validation");
            // exception for no data
            if (user == null)
            {
                _logger.LogError("Validation failed: User data is null");
                throw new ArgumentException("Null user data");
            }

            //Exception for email
            if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains("@"))
            {
                _logger.LogError($"Validation failed: Invalid email for user {user.Username} and {user.Email}");
                throw new ArgumentException("Invalid email");
            }

            //exception for duplicate usernames
            if (_userService.GetUserByUsername(user.Username) != null)
            {
                _logger.LogError($"Validation failed: Username {user.Username} already exists");
                throw new ArgumentException($"User with name {user.Username} already exists");
            }
            _logger.LogInformation($"User {user.Username} passed validation");
            return user;
        }

        private int GenerateUniqueUserId()
        {
            int id = Math.Abs(Guid.NewGuid().GetHashCode());
            _logger.LogInformation($"Generated initial user ID: {id}");

            //While loop to repeatedly check for duplicate ids in db (statistically very unlikely)
            bool? duplicateFlag = null;
            while (duplicateFlag != false)
            {
                if (_userService.GetUser(id) != null)
                {
                    // Handle the case where the ID already exists (e.g., generate a new ID, so it doesnt match an already existing one)
                    _logger.LogWarning($"Duplicate user ID found: {id}. Generating a new ID.");
                    id = GenerateUniqueUserId();
                    duplicateFlag = true;
                }
                else
                {
                    duplicateFlag = false;
                }
            }
            _logger.LogInformation($"Final unique user ID generated: {id}");
            return id;
        }

        [HttpPut]
        public IActionResult EditUser([FromBody] UserDTO user)
        {
            _logger.LogInformation($"Attempting to edit user with ID: {user.UserId}");
            if (user.UserId == null)
            {
                _logger.LogWarning("Edit user failed: User ID is null");
                return BadRequest("Invalid user data");
            }

            if (_userService.GetUser((int)user.UserId) == null)
            {
                _logger.LogWarning($"Edit user failed: No user found with ID {user.UserId}");
                return BadRequest("User ID does not exist in the database");
            }

            _userService.UpdateUser(user);
            _logger.LogInformation($"User with ID: {user.UserId} updated successfully");
            return Ok("User updated successfully");
        }

        [HttpPut("updatePassword")]
        public IActionResult UpdatePassword([FromBody] int userId, string password)
        {
            _logger.LogInformation($"Attempting to update password for user ID: {userId}");
            UserDTO user = _userService.GetUser(userId);

            if (user == null)
            {
                _logger.LogWarning($"Password update failed: No user found with ID {userId}");
                return BadRequest("User ID does not exist in the database");
            }

            user.Password = password;

            _userService.UpdateUser(user);
            _logger.LogInformation($"Password for user ID: {userId} updated successfully");

            return Ok("Password updated successfully");
        }

        [HttpDelete("{id}")]
        public IActionResult DeleteUser(int id)
        {
            _logger.LogInformation($"Attempting to delete user with ID: {id}");
            var user = _userService.GetUser(id);

            if (user == null)
            {
                _logger.LogWarning($"Delete user failed: No user found with ID {id}");
                return NotFound($"Delete user failed: No user found with ID {id}"); // Return 404 if user is not found
            }

            _userService.DeleteUser(id);
            _logger.LogInformation($"User with ID: {id} deleted successfully");

            return Ok("User deleted successfully");
        }
    }
}
