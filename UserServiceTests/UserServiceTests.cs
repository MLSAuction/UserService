﻿using NUnit.Framework;
using Moq; // Add this if you are using Moq for mocking
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using UserService.Controllers;
using UserService.Models;
using UserService.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace UserServiceTests
{
    [TestFixture]
    public class UserServiceTests
    {
        private Mock<IUserRepository> _userRepositoryMock;
        private UserController _userController;

        [SetUp]
        public void Setup()
        {
            // Vi opretter en mock for IUserRepository og IMemoryCache.
            _userRepositoryMock = new Mock<IUserRepository>();
            var loggerMock = new Mock<ILogger<UserController>>();
            var configurationMock = new Mock<IConfiguration>();

            // Initialiser UserController med de mockede dependencies.
            _userController = new UserController(loggerMock.Object, configurationMock.Object, _userRepositoryMock.Object);
        }

        [Test]
        [TestCase(1, "John", "Doe", true)]
        [TestCase(999, "Jane", "Doe", false)]
        public void GetUserValidIdReturnsOkResult(int userId, string firstName, string lastName, bool expectedResult)
        {
            // Arrange -> Definer bruger.
            UserDTO? user = user = new UserDTO { UserId = userId, FirstName = firstName, LastName = lastName };

            // Opsæt mock IUserRepository til at returnere brugeren, når GetUser kaldes med det specificerede ID.
            //For at teste, at denne test virker - Kan man prøve at få den til at fejle, ved at tilføje +1 efter 'userId'
            _userRepositoryMock.Setup(repo => repo.GetUser((int)user.UserId)).Returns(user);
            _userRepositoryMock.Setup(repo => repo.GetUser(999)).Returns((UserDTO)null); // Nonexistent user ID

            // ACT -> Udfør handlingen ved at kalde GetUser-metoden på UserController med det specificerede bruger-ID.
            var result = _userController.GetUser((int)user.UserId);

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
        [TestCase(1, "John", "Doe", 2, "Jane", "Smith")]
        public void GetAllUsersReturnsAllUsers(int userId, string firstName, string lastName, int userId2, string firstName2, string lastName2)
        {
            //arrange
            UserDTO user1 = new UserDTO { UserId = userId, FirstName = firstName, LastName = lastName };
            UserDTO user2 = new UserDTO { UserId = userId2, FirstName = firstName2, LastName = lastName2 };

            var users = new List<UserDTO> { user1, user2 };

            _userRepositoryMock.Setup(repo => repo.GetAllUsers()).Returns(users);

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
        public void ValidateUserReturnsBadRequest(string? firstName, string email, bool expectedResult)
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
        [TestCase(10, false)]
        [TestCase(null, true)]
        public void GenerateUniqueUserIdGeneratesId (int? userId, bool expectedResult)
        {
            // arrange
            UserDTO user = new UserDTO { UserId = userId, Email = "@" }; //email is initialized here to bypass the validation, might wanna refactor so it doesnt need to be maintained

            UserDTO capturedUser = null;
            bool newIdFlag = false;

            _userRepositoryMock.Setup(repo => repo.AddUser(It.IsAny<UserDTO>()))
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
        [TestCase(1, "John", "Doe", true)]
        [TestCase(999, "Jane", "Doe", false)]
        public void EditUserReturnsOkResult(int userId, string firstName, string lastName, bool expectedResult)
        {
            // arrange
            UserDTO? user = new UserDTO { UserId = userId, FirstName = firstName, LastName = lastName };

            UserDTO capturedUser = null;

            _userRepositoryMock.Setup(repo => repo.GetUser((int)user.UserId)).Returns(user);
            _userRepositoryMock.Setup(repo => repo.GetUser(999)).Returns((UserDTO)null); // Nonexistent user ID
            _userRepositoryMock.Setup(repo => repo.UpdateUser(It.IsAny<UserDTO>()))
                                                  .Callback<UserDTO>(u => capturedUser = u);

            var updatedUser = new UserDTO { UserId = user.UserId, FirstName = "UpdatedFirstName", LastName = "UpdatedLastName" };

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
                Assert.AreNotEqual(capturedUser.LastName, user.LastName);
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