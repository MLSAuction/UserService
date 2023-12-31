using UserService.Models;

namespace UserService.Repositories
{
    public interface IUserRepository
    {
        IEnumerable<UserDTO> GetAllUsers();
        UserDTO GetUser(Guid id);
        UserDTO GetUserByUsername(string username);
        void AddUser(UserDTO user);
        void UpdateUser(UserDTO user);
        void DeleteUser(Guid id);
    }
}