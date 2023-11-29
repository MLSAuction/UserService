using UserService.Models;

namespace UserService.Repositories
{
    public interface IUserRepository
    {
        IEnumerable<UserDTO> GetAllUsers();
        UserDTO GetUser(int id);
        void AddUser(UserDTO user);
        void UpdateUser(UserDTO user);
        void DeleteUser(int id);
    }
}