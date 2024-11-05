using Backend_hempromak.Models;

namespace Backend_hempromak.Services
{
    public interface ILoginService
    {
        Task<string> LoginUserAsync(LoginModel loginRequest);
        Task<bool> RegisterUserAsync(User newUser);
    }

}
