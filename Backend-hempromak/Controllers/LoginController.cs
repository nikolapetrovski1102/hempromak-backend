using Backend_hempromak.Models;
using Backend_hempromak.Services;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace Backend_hempromak.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly DbContext _dbContext;
        private readonly ILoginService _loginService;

        public LoginController(IConfiguration config, ILoginService loginService)
        {
            _config = config;
            _dbContext = new DbContext();
            _loginService = loginService;
        }

        [HttpPost("/register")]
        public async Task<IActionResult> Register([FromBody] User registerRequest)
        {
            var isUserCreated = await _loginService.RegisterUserAsync(registerRequest);

            if (isUserCreated)
                return Ok();
            else
                return BadRequest("User already exists");

        }

        [HttpPost("/login")]
        public async Task<IActionResult> Login([FromBody] LoginModel loginRequest)
        {
            var userToken = await _loginService.LoginUserAsync(loginRequest);

            if (userToken != null)
            {
                return Ok(userToken);
            }
            else
            {
                return BadRequest("Invalid username or password");
            }

        }
    }

}
