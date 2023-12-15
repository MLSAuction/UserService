using Microsoft.AspNetCore.Mvc;
using UserService.Repositories;
using UserService.Models;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;

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

        /// <summary>
        /// Get all users
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("getAll")]
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

        /// <summary>
        /// Get user by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("{id}")]
        public IActionResult GetUser(Guid id)
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

        /// <summary>
        /// Get user by username
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        [AllowAnonymous]
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

        /// <summary>
        /// Register user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult RegisterUser([FromBody] UserDTO user)
        {
            _logger.LogInformation("Attempting to register a new user");

            try
            {
                user = ValidateNewUser(user);
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

        /// <summary>
        /// Login
        /// </summary>
        /// <param name="authDTO"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("login")]
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

        /// <summary>
        /// Add user
        /// </summary>
        /// <param name="inputUser"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public IActionResult AddUser([FromBody] UserDTO inputUser)
        {
            UserDTO user;
            _logger.LogInformation("Attempting to add a new user");
            try
            {
                user = ValidateNewUser(inputUser);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError($"User validation failed: {ex.Message}");
                return BadRequest($"Invalid user data, {ex}");
            }

            user.UserId = GenerateUniqueUserId();

            _userService.AddUser(user);

            _logger.LogInformation($"User added successfully with ID: {user.UserId}");
            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, user);

        }


        /// <summary>
        /// Validate new user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private UserDTO ValidateNewUser(UserDTO user)
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

        /// <summary>
        /// Edit user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        [Authorize]
        [HttpPut]
        public IActionResult EditUser([FromBody] UserDTO user)
        {
            _logger.LogInformation($"Attempting to edit user with ID: {user.UserId}");
            if (user.UserId == null)
            {
                _logger.LogWarning("Edit user failed: User ID is null");
                return BadRequest("Invalid user data");
            }

            //Exception for invalid email
            if (string.IsNullOrWhiteSpace(user.Email) || !user.Email.Contains("@"))
            {
                _logger.LogError($"Validation failed for EditUser: Invalid email for user {user.Username} and {user.Email}");
                throw new ArgumentException("Invalid email");
            }

            if (_userService.GetUser((Guid)user.UserId) == null)
            {
                _logger.LogWarning($"Edit user failed: No user found with ID {user.UserId}");
                return BadRequest("User ID does not exist in the database");
            }

            _userService.UpdateUser(user);
            _logger.LogInformation($"User with ID: {user.UserId} updated successfully");
            return Ok("User updated successfully");
        }

        /// <summary>
        /// Update password
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPut("updatePassword")]
        public IActionResult UpdatePassword(Guid userId, string password)
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

        /// <summary>
        /// Delete user by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [Authorize]
        [HttpDelete("{id}")]
        public IActionResult DeleteUser(Guid id)
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

        private Guid GenerateUniqueUserId()
        {
            Guid id = Guid.NewGuid();
            _logger.LogInformation($"Generated initial user ID: {id}");

            // While loop to repeatedly check for duplicate ids in the db (statistically very unlikely)
            bool duplicateFlag = true;
            while (duplicateFlag)
            {
                if (_userService.GetUser(id) != null)
                {
                    // Handle the case where the ID already exists (e.g., generate a new ID, so it doesn't match an already existing one)
                    _logger.LogWarning($"Duplicate user ID found: {id}. Generating a new ID.");
                    id = GenerateUniqueUserId();
                }
                else
                {
                    duplicateFlag = false;
                }
            }
            _logger.LogInformation($"Final unique user ID generated: {id}");
            return id;
        }





    }
}
