using UserService.Models;

namespace UserService.Repositories
{
    public interface IUserRepository
    {

        UserDTO GetUser(int id);
        void AddUser(UserDTO user);
        void UpdateUser(UserDTO user);
        void DeleteUser(int id);
    }
}