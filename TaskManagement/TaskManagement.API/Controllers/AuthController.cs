using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Data;
using TaskManagement.API.DTOs;
using TaskManagement.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TaskManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(AppDbContext context, IConfiguration config):ControllerBase
    {
        [HttpPost("register")]
        [EndpointSummary("User Register")]
        public async Task<ActionResult> Register(RegisterDto userDto)
        {
            bool existingEmail = await context.Users.AnyAsync(u => u.Email == userDto.Email);
            if (existingEmail)
            {
                return BadRequest("Email is already taken");
            }
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password);//change to bCrypt 
            var Data = new User
            {
                Username = userDto.UserName,
                Email = userDto.Email,
                PasswordHash=passwordHash,
            };
            
            context.Users.Add(Data);
            await context.SaveChangesAsync();
            return Ok(new AuthResponseDto
            {
                UserName=Data.Username,
                Email=Data.Email,
                Token=""
            });
        }

        [HttpPost("login")]
        [EndpointSummary("User Login")]
        public async Task<ActionResult> Login(LoginDto userDto)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
            if (user == null)
            {
                return BadRequest("user doesn't exist");
            }
            bool isPassword = BCrypt.Net.BCrypt.Verify(userDto.Password, user.PasswordHash);
            if (!isPassword)
            {
                return Unauthorized("Invalid Email or Password.");
            }
            string token = GenerateJwtToken(user);
            return Ok(new AuthResponseDto
            {
                UserName = user.Username,
                Email = user.Email,
                Token = token
            });
        }

        private string GenerateJwtToken(User user)
        {
            // 1. Token ထဲတွင် မြှုပ်နှံထည့်သွင်းမည့် User Claims များ
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            // 2. appsettings.json ထဲမှ Secret Key ကို ယူ၍ Security Key ပြုလုပ်ခြင်း
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // 3. Expiration Time နှင့် Token Description သတ်မှတ်ခြင်း
            var durationInDays = double.Parse(config["Jwt:DurationInDays"] ?? "7");
            var expires = DateTime.UtcNow.AddDays(durationInDays);

            var token = new JwtSecurityToken(
                issuer: config["Jwt:Issuer"],
                audience: config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            // 4. Token စာကြောင်းအဖြစ် ပြောင်းလဲ၍ Return ပြန်ပေးခြင်း
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
