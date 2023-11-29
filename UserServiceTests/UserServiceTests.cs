using NUnit.Framework;
using Moq; // Add this if you are using Moq for mocking
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using UserService.Controllers;
using UserService.Models;
using UserService.Repositories;
using Microsoft.AspNetCore.Mvc;


namespace UserServiceTests
{
    [TestFixture]
    public class UserControllerTests
    {
        private Mock<IUserRepository> _userRepositoryMock;
        private UserController _userController;

        [SetUp]
        public void Setup()
        {
            // Vi opretter en Moq mock for IUserRepository og IMemoryCache.
            _userRepositoryMock = new Mock<IUserRepository>();
            var loggerMock = new Mock<ILogger<UserController>>();
            var configurationMock = new Mock<IConfiguration>();

            // Initialiser UserController med de mockede dependencies.
            _userController = new UserController(loggerMock.Object, configurationMock.Object, _userRepositoryMock.Object);
        }

        [Test]
        public void GetUser_ValidId_ReturnsOkResult()
        {
            // Arrange -> Definer en bruger med ID 1 og navn "John Doe".
            var userId = 1;
            var userDto = new UserDTO { UserId = userId, FirstName = "John", LastName = "Doe" };

            // Opsæt mock IUserRepository til at returnere brugeren, når GetUser kaldes med det specificerede ID.
            //For at teste, at denne test virker - Kan man prøve at få den til at fejle, ved at tilføje +1 efter 'userId'
            _userRepositoryMock.Setup(repo => repo.GetUser(userId)).Returns(userDto);

            // ACT -> Udfør handlingen ved at kalde GetUser-metoden på UserController med det specificerede bruger-ID.
            var result = _userController.GetUser(userId) as OkObjectResult;

            //Assert -> Bekræfter, at resultatet ikke er null, og at HTTP-statuskoden er 200 (OK).
            Assert.NotNull(result);
            Assert.AreEqual(200, result.StatusCode);

            // Bekræft, at værdien af resultatet er den forventede UserDTO.
            Assert.AreEqual(userDto, result.Value);
        }

       

    }
}