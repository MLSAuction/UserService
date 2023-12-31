﻿using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using UserService.Controllers;
using UserService.Models;
using UserService.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace UserServiceTests
{
    [TestFixture]
    public class UserServiceTests
    {
        private Mock<IUserRepository> _userRepositoryStub;
        private UserController _userController;

        [SetUp]
        public void Setup()
        {
            // Vi opretter en mock for IUserRepository og IMemoryCache.
            _userRepositoryStub = new Mock<IUserRepository>();
            var loggerMock = new Mock<ILogger<UserController>>();
            var configurationMock = new Mock<IConfiguration>();

            // Initialiser UserController med de mockede dependencies.
            _userController = new UserController(loggerMock.Object, configurationMock.Object, _userRepositoryStub.Object);
        }

        [Test]
        [TestCase("ed0d17b8-dbc3-4080-8f4a-abe63b9679b9", "John", "Doe", true)]
        [TestCase("ed0d17b8-dbc3-4080-8f4a-999999999999", "Jane", "Doe", false)]
        public void GetUserValidIdReturnsOkResult(Guid userId, string firstName, string surname, bool expectedResult)
        {
            // Arrange -> Definer bruger.
            UserDTO? user = new UserDTO { UserId = userId, FirstName = firstName, Surname = surname };

            // Opsæt mock IUserRepository til at returnere brugeren, når GetUser kaldes med det specificerede ID.
            // For at teste, at denne test virker - Kan man prøve at få den til at fejle, ved at tilføje +1 efter 'userId'
            _userRepositoryStub.Setup(repo => repo.GetUser(userId)).Returns(user);
            _userRepositoryStub.Setup(repo => repo.GetUser(Guid.Parse("ed0d17b8-dbc3-4080-8f4a-999999999999"))).Returns((UserDTO)null); // Nonexistent user ID

            // ACT -> Udfør handlingen ved at kalde GetUser-metoden på UserController med det specificerede bruger-ID.
            var result = _userController.GetUser(userId);

            //Assert
            Assert.IsNotNull(result);

            // Bekræft, at værdien af resultatet er den forventede UserDTO.
            if (expectedResult)
            {
                Assert.IsInstanceOf<OkObjectResult>(result);
                var okResult = (OkObjectResult)result;

                Assert.AreEqual(200, okResult.StatusCode);
                Assert.AreEqual(user, okResult.Value);
            }
            else
            {
                Assert.IsInstanceOf<NotFoundResult>(result);
            }
        }

        
        [Test]
        [TestCase("ed0d17b8-dbc3-4080-8f4a-abe63b9679b9", "John", "Doe", "ab0d17b8-dbc3-4080-8f4a-abe63b9679b9", "Jane", "Smith")]

        public void GetAllUsersReturnsAllUsers(Guid userId, string firstName, string surname, Guid userId2, string firstName2, string surname2)
        {
            //arrange
            UserDTO user1 = new UserDTO { UserId = userId, FirstName = firstName, Surname = surname };
            UserDTO user2 = new UserDTO { UserId = userId2, FirstName = firstName2, Surname = surname2 };

            var users = new List<UserDTO> { user1, user2 };

            _userRepositoryStub.Setup(repo => repo.GetAllUsers()).Returns(users);

            //act
            var result = _userController.GetAllUsers() as OkObjectResult;

            //assert
            Assert.NotNull(result);
            Assert.AreEqual(200, result.StatusCode);

            var resultCollection = result.Value as IEnumerable<UserDTO>;
            Assert.NotNull(resultCollection);

            CollectionAssert.AreEquivalent(users, resultCollection);
        }
        
        [Test]
        [TestCase("John", "Doe.com", true)] //invalid email
        [TestCase(null, "", true)] //null user
        [TestCase("Jane", "Doe@jd.com", false)] //valid user and email
        public void ValidateNewUserReturnsBadRequest(string? firstName, string email, bool expectedResult)
        {
            // arrange
            UserDTO? user;
            if (firstName != null) //initialize user if not testing for null argument
            {
                user = new UserDTO { FirstName = firstName, Email = email };
            }
            else
            {
                user = null;
            }

            // act
            IActionResult result = _userController.AddUser(user);

            // assert
            if (result is BadRequestObjectResult)
            {
                Assert.AreEqual(expectedResult, true);
            }
            else
            {
                Assert.AreEqual(expectedResult, false);
            }
        }
        
        [Test]
        [TestCase("ab0c17b8-dbc3-4080-8f4a-abe63b9679b9", true)]
        [TestCase(null, true)]
        public void GenerateUniqueUserIdGeneratesId(Guid? userId, bool expectedResult)
        {
            // arrange
            UserDTO user = new UserDTO { UserId = userId, Email = "@" }; //email is initialized here to bypass the validation, might wanna refactor so it doesnt need to be maintained

            UserDTO capturedUser = null;
            bool newIdFlag = false;

            _userRepositoryStub.Setup(repo => repo.AddUser(It.IsAny<UserDTO>()))
                               .Callback<UserDTO>(u => capturedUser = u); // callback to capture the user, whether changed or not

            // act
            _ = _userController.AddUser(user);
            if (userId != capturedUser.UserId)
            {
                newIdFlag = true;
            }

            // assert
            Assert.NotNull(capturedUser);
            Assert.AreEqual(newIdFlag, expectedResult);
        }
        
        [Test]
        [TestCase("ed0d17b8-dbc3-4080-8f4a-abe63b9679b9", "John", "Doe", true)]
        [TestCase("ab0d17b8-dbc3-4080-8f4a-abe63b9679b9", "Jane", "Doe", false)]
        public void EditUserReturnsOkResult(Guid userId, string firstName, string surname, bool expectedResult)
        {
            // arrange
            UserDTO? user = new UserDTO { UserId = userId, FirstName = firstName, Surname = surname, Email = "@" };

            UserDTO capturedUser = null;

            _userRepositoryStub.Setup(repo => repo.GetUser((Guid)user.UserId)).Returns(user);
            _userRepositoryStub.Setup(repo => repo.GetUser(Guid.Parse("ab0d17b8-dbc3-4080-8f4a-abe63b9679b9"))).Returns((UserDTO)null); // Nonexistent user ID
            _userRepositoryStub.Setup(repo => repo.UpdateUser(It.IsAny<UserDTO>()))
                                                  .Callback<UserDTO>(u => capturedUser = u);

            var updatedUser = new UserDTO { UserId = user.UserId, FirstName = "UpdatedFirstName", Surname = "UpdatedSurname", Email = user.Email };

            // act
            var result = _userController.EditUser(updatedUser);

            // assert
            if (expectedResult)
            {
                Assert.IsInstanceOf<OkObjectResult>(result);
                var okResult = (OkObjectResult)result;

                Assert.AreEqual(200, okResult.StatusCode);
                Assert.AreEqual("User updated successfully", okResult.Value);

                Assert.AreEqual(capturedUser.UserId, user.UserId);
                Assert.AreNotEqual(capturedUser.FirstName, user.FirstName);
                Assert.AreNotEqual(capturedUser.Surname, user.Surname);
            }
            else
            {
                Assert.IsInstanceOf<BadRequestObjectResult>(result);
                var badRequestResult = (BadRequestObjectResult)result;

                Assert.AreEqual("User ID does not exist in the database", badRequestResult.Value);
            }
        }
        
        [Test]
        [TestCase("ed0d17b8-dbc3-4080-8f4a-abe63b9679b9", true)]
        [TestCase("ed0d17b8-dbc3-4080-8f4a-999999999999", false)]
        public void DeleteUserReturnsResult(Guid userId, bool expectedResult)
        {
            // Arrange
            UserDTO user = new UserDTO { UserId = userId, FirstName = "John", Surname = "Doe" };

            _userRepositoryStub.Setup(repo => repo.GetUser(userId)).Returns(user);
            _userRepositoryStub.Setup(repo => repo.GetUser(Guid.Parse("ed0d17b8-dbc3-4080-8f4a-999999999999"))).Returns((UserDTO)null); // Nonexistent user ID
            _userRepositoryStub.Setup(repo => repo.DeleteUser(userId));

            // Act
            var result = _userController.DeleteUser(userId);

            // Assert
            if (expectedResult)
            {
                Assert.IsInstanceOf<OkObjectResult>(result);
                var okResult = (OkObjectResult)result;

                Assert.AreEqual(200, okResult.StatusCode);
                Assert.AreEqual("User deleted successfully", okResult.Value);
            }
            else
            {
                Assert.IsInstanceOf<NotFoundObjectResult>(result);
                var notFoundResult = (NotFoundObjectResult)result;

                Assert.AreEqual(404, notFoundResult.StatusCode);
            }
        }
        
        [Test]
        [TestCase("John", true)]
        [TestCase("Jane", false)]
        public void GetUserValidUsernameReturnsOkResult(string username, bool expectedResult)
        {
            // Arrange -> Definer bruger.
            UserDTO? user = user = new UserDTO { Username = username };

            // Opsæt mock IUserRepository til at returnere brugeren, når GetUser kaldes med det specificerede ID.
            //For at teste, at denne test virker - Kan man prøve at få den til at fejle, ved at tilføje +1 efter 'userId'
            _userRepositoryStub.Setup(repo => repo.GetUserByUsername(user.Username)).Returns(user);
            _userRepositoryStub.Setup(repo => repo.GetUserByUsername("Jane")).Returns((UserDTO)null); // Nonexistent user ID

            // ACT -> Udfør handlingen ved at kalde GetUser-metoden på UserController med det specificerede bruger-ID.
            var result = _userController.GetUserByUsername(user.Username);

            //Assert
            Assert.IsNotNull(result);

            // Bekræft, at værdien af resultatet er den forventede UserDTO.
            if (expectedResult)
            {
                Assert.IsInstanceOf<OkObjectResult>(result);
                var okResult = (OkObjectResult)result;

                Assert.AreEqual(200, okResult.StatusCode);
                Assert.AreEqual(user, okResult.Value);
            }
            else
            {
                Assert.IsInstanceOf<NotFoundObjectResult>(result);
            }
        }
        
        [Test]
        [TestCase("ed0d17b8-dbc3-4080-8f4a-abe63b9679b9", "oldPassword", true)] // Valid user ID and password
        [TestCase("ed0d17b8-dbc3-4080-8f4a-999999999999", "oldPassword", false)] // Invalid user ID
        public void UpdatePasswordReturnsResult(Guid userId, string password, bool expectedResult)
        {
            // Arrange
            UserDTO user = new UserDTO { UserId = userId, Password = password };

            UserDTO capturedUser = new UserDTO();

            _userRepositoryStub.Setup(repo => repo.GetUser(userId)).Returns(user);
            _userRepositoryStub.Setup(repo => repo.GetUser(Guid.Parse("ed0d17b8-dbc3-4080-8f4a-999999999999"))).Returns((UserDTO)null); // Nonexistent user ID
            _userRepositoryStub.Setup(repo => repo.UpdateUser(It.IsAny<UserDTO>()))
                                                  .Callback<UserDTO>(u => capturedUser = u);

            // Act
            var result = _userController.UpdatePassword(userId, "updatedPassword");

            // assert
            if (expectedResult)
            {
                Assert.IsInstanceOf<OkObjectResult>(result);
                var okResult = (OkObjectResult)result;

                Assert.AreEqual(200, okResult.StatusCode);
                Assert.AreEqual("Password updated successfully", okResult.Value);

                Assert.AreEqual(userId, capturedUser.UserId);
                Assert.AreNotEqual(password, capturedUser.Password);
            }
            else
            {
                Assert.IsInstanceOf<BadRequestObjectResult>(result);
                var badRequestResult = (BadRequestObjectResult)result;

                Assert.AreEqual("User ID does not exist in the database", badRequestResult.Value);
            }
        }
        
    }
}