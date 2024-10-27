namespace Backend_hempromak.Models
{
    public class LoginModel
    {
        public required string EmailOrUsername { get; init; }
        public required string Password { get; init; }
        public bool rememberMe { get; init; } = false;
    }
}
