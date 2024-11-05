using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;
using Backend_hempromak.Models;
using Org.BouncyCastle.Crypto.Generators;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Identity.Data;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace Backend_hempromak.Services
{
    public class LoginService
    {
        private readonly DbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LoginService(DbContext dbContext, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
        }

        // 1. Register a New User
        public async Task<bool> RegisterUserAsync(User newUser)
        {
            try
            {
                var userExists = _dbContext.executeSqlQuery($"SELECT * FROM Users WHERE Username = '{newUser.Username}' OR Email = '{newUser.Email}'");
                if (userExists.Count >= 1) return false;

                var hashedPassword = HashPassword(newUser.Password);
                DateTime dateTimeNow = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"));
                string formattedDateTimeNow = dateTimeNow.ToString("yyyy-MM-dd HH:mm:ss");
                var query = $"INSERT INTO Users (Username, Email, Password, Role, isActive, date_created) VALUES ('{newUser.Username}', '{newUser.Email}', '{hashedPassword}', '{newUser.Role}', '{newUser.IsActive}', '{formattedDateTimeNow}')";

                _dbContext.executeSqlQuery(query);

                return true;
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // 2. Login User
        public async Task<string> LoginUserAsync(LoginModel loginRequest)
        {
            try
            {
                var query = $"SELECT Password FROM Users WHERE Username = '{loginRequest.EmailOrUsername}' OR Email = '{loginRequest.EmailOrUsername}'";
                var storedPassword = _dbContext.executeSqlQuery(query);

                if (storedPassword.Count == 0) return "Invalid username or password";

                var foundPassword = storedPassword[0]["Password"].ToString();

                if (storedPassword == null || !VerifyPassword(loginRequest.Password, foundPassword)) return null;

                var context = _httpContextAccessor.HttpContext;
                context.Session.SetString("email", loginRequest.EmailOrUsername.ToString());

                return GenerateJwtToken(loginRequest);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // 3. Hash Password (BCrypt)
        private string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password);
        //{
        //    byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);

        //    return Convert.ToBase64String(KeyDerivation.Pbkdf2(
        //        password: password!,
        //        salt: salt,
        //        prf: KeyDerivationPrf.HMACSHA256,
        //        iterationCount: 100000,
        //        numBytesRequested: 256 / 8));
        //}

        // 4. Verify Password (PBKDF2 with Salt)
        private bool VerifyPassword(string password, string storedHash) => BCrypt.Net.BCrypt.Verify(password, storedHash);
        //{
        //    var parts = storedHash.Split('.');
        //    if (parts.Length != 2) return false;

        //    byte[] salt = Convert.FromBase64String(parts[0]);

        //    string hashOfInput = Convert.ToBase64String(KeyDerivation.Pbkdf2(
        //        password: password,
        //        salt: salt,
        //        prf: KeyDerivationPrf.HMACSHA256,
        //        iterationCount: 100000,
        //        numBytesRequested: 256 / 8));

        //    return hashOfInput == parts[1];
        //}


        // 5. Generate JWT Token
        private string GenerateJwtToken(LoginModel loginRequest)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var expirationTime = loginRequest.rememberMe ? DateTime.Now.AddDays(30) : DateTime.Now.AddMinutes(120);

            var Sectoken = new JwtSecurityToken(_configuration["Jwt:Issuer"],
              _configuration["Jwt:Issuer"],
              null,
              expires: expirationTime,
              signingCredentials: credentials);

            var access_token = new JwtSecurityTokenHandler().WriteToken(Sectoken);

            Dictionary<string, string> response_token = new Dictionary<string, string>
            {
                { "access_token", access_token },
                { "token_type", "Bearer" },
                { "valid_to", Sectoken.ValidTo.ToString("O") }
            };

            return JsonSerializer.Serialize(response_token);
        }

        // 6. Reset Password
        public async Task<bool> ResetPasswordAsync(string username, string newPassword)
        {
            try
            {
                var hashedPassword = HashPassword(newPassword);
                var query = $"UPDATE Users SET PasswordHash = '{hashedPassword}' WHERE Username = '{username}'";

                _dbContext.executeSqlQuery(query);
                return true;
            }
            catch(Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        // 7. Validate JWT Token
        public ClaimsPrincipal ValidateJwtToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return principal;
            }
            catch
            {
                return null;
            }
        }
    }
}
