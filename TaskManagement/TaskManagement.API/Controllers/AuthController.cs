using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskManagement.API.Data;
using TaskManagement.API.DTOs;
using TaskManagement.API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;

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
            var token = await GenerateJwtTokenAsync(Data);
            //return Ok(new AuthResponseDto
            //{
            //    UserName=Data.Username,
            //    Email=Data.Email,
            //    Token=""
            //});
            return Ok(token);
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
            var token =await GenerateJwtTokenAsync(user);
            //return Ok(new AuthResponseDto
            //{
            //    UserName = user.Username,
            //    Email = user.Email,
            //    Token = token
            //});
            return Ok(token);
        }

        [HttpPost("logout")]
        [EndpointSummary("logout")]
        public async Task<ActionResult> Logout(string refreshToken)
        {
            var token=await context.RefreshTokens.FirstOrDefaultAsync(t => t.Token==refreshToken && !t.IsRevoked);
            if(token == null)
            {
                return BadRequest("Invalid");
            }
            // Logout logic (e.g., invalidate the token, clear cookies, etc.)
            token.IsRevoked = true;
            await context.SaveChangesAsync();
            return Ok(new { message = "Logged out successfully" });
        }

        [AllowAnonymous]
        [HttpPost("refresh-token")]
        public async Task<ActionResult> RefreshToken(TokenRequestDto tokenRequest)
        {
            var result = await VerifyAndGenerateTokenAsync(tokenRequest);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        private async Task<AuthResultDto> GenerateJwtTokenAsync(User user)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(config["Jwt:Key"]!);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        }),
                Expires = DateTime.UtcNow.AddMinutes(5), // Access Token သက်တမ်း ၁၅ မိနစ်
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Issuer = config["Jwt:Issuer"],
                Audience = config["Jwt:Audience"]
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            // Refresh Token ဖန်တီးခြင်း
            var refreshToken = new RefreshToken
            {
                JwtId = token.Id,
                IsUsed = false,
                IsRevoked = false,
                UserId = user.Id,
                AddedDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(7), // Refresh Token သက်တမ်း ၇ ရက်
                Token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString()
            };

            await context.RefreshTokens.AddAsync(refreshToken);
            await context.SaveChangesAsync();

            return new AuthResultDto
            {
                Token = jwtToken,
                RefreshToken = refreshToken.Token,
                Success = true
            };
        }

        private async Task<AuthResultDto> VerifyAndGenerateTokenAsync(TokenRequestDto tokenRequest)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();

            try
            {
                // Token validation parameters
                var tokenValidationParams = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["Jwt:Issuer"],
                    ValidAudience = config["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
                    ValidateLifetime = false // Expired Token ကို စစ်ဆေးရန် Lifetime Check ကို ခေတ္တ ပိတ်ထားသည်
                };

                // 1. Access Token Format မှန်မမှန် စစ်ဆေးခြင်း
                var tokenInVerification = jwtTokenHandler.ValidateToken(tokenRequest.Token, tokenValidationParams, out var validatedToken);

                if (validatedToken is JwtSecurityToken jwtSecurityToken)
                {
                    var result = jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase);
                    if (!result) return new AuthResultDto { Success = false, Errors = new List<string> { "Invalid Token Algorithm" } };
                }

                // 2. Token Expire ဖြစ်မဖြစ် စစ်ဆေးခြင်း
                var utcExpiryDate = long.Parse(tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Exp)!.Value);
                var expiryDate = UnixTimeStampToDateTime(utcExpiryDate);

                if (expiryDate > DateTime.UtcNow)
                {
                    return new AuthResultDto { Success = false, Errors = new List<string> { "Access Token has not expired yet" } };
                }

                // 3. Refresh Token DB ထဲတွင် ရှိမရှိ စစ်ဆေးခြင်း
                var storedToken = await context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == tokenRequest.RefreshToken);

                if (storedToken == null || storedToken.IsUsed || storedToken.IsRevoked || storedToken.ExpiryDate < DateTime.UtcNow)
                {
                    return new AuthResultDto { Success = false, Errors = new List<string> { "Invalid or expired Refresh Token" } };
                }

                // 4. JTI မူလ Token နဲ့ ကိုက်ညီမှု ရှိမရှိ စစ်ဆေးခြင်း
                var jti = tokenInVerification.Claims.FirstOrDefault(x => x.Type == JwtRegisteredClaimNames.Jti)!.Value;
                if (storedToken.JwtId != jti)
                {
                    return new AuthResultDto { Success = false, Errors = new List<string> { "Token mismatch" } };
                }

                // Old Refresh Token ကို IsUsed = true ဟု ပြောင်းလဲခြင်း (Token Rotation)
                storedToken.IsUsed = true;
                context.RefreshTokens.Update(storedToken);
                await context.SaveChangesAsync();

                // Token အသစ် ထုတ်ပေးခြင်း
                var dbUser = await context.Users.FindAsync(storedToken.UserId);
                return await GenerateJwtTokenAsync(dbUser!);
            }
            catch (Exception)
            {
                return new AuthResultDto { Success = false, Errors = new List<string> { "Something went wrong" } };
            }
        }

        private DateTime UnixTimeStampToDateTime(long unixTimeStamp)
        {
            var dateTimeInterval = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dateTimeInterval.AddSeconds(unixTimeStamp).ToUniversalTime();
        }
    }
}
