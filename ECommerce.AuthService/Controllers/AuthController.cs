using ECommerce.AuthService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[Route("api/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private static List<RefreshToken> _refreshTokens = new List<RefreshToken>();
    private readonly IConfiguration _config;

    public AuthController(IConfiguration config)
    {
        _config = config;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        var users = new List<User>
        {
            new User { Username = "admin", Role = "Admin" },
            new User { Username = "user", Role = "User" }
        };

        var user = users.FirstOrDefault(u => u.Username == request.Username);
        if (user == null) return Unauthorized(new { message = "Invalid user!" });

        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken(user.Username);

        _refreshTokens.Add(refreshToken);

        return Ok(new
        {
            token,
            refreshToken = refreshToken.Token
        });
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            }),
            Expires = DateTime.UtcNow.AddMinutes(15), // 🔥 15 valid for minutes!
            Issuer = _config["Jwt:Issuer"],
            Audience = _config["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private RefreshToken GenerateRefreshToken(string username)
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            Username = username,
            ExpiryDate = DateTime.UtcNow.AddDays(7) // 🔥 Refresh token is valid for 7 days
        };
    }

    [HttpPost("refresh")]
    public IActionResult Refresh([FromBody] RefreshRequest request)
    {
        var storedToken = _refreshTokens.FirstOrDefault(t => t.Token == request.RefreshToken);

        if (storedToken == null || storedToken.ExpiryDate < DateTime.UtcNow)
        {
            return Unauthorized(new { message = "Invalid or expired refresh token!" });
        }

        var user = new User { Username = storedToken.Username, Role = "User" }; //We find the user
        var newAccessToken = GenerateJwtToken(user);
        var newRefreshToken = GenerateRefreshToken(user.Username);

        _refreshTokens.Remove(storedToken);
        _refreshTokens.Add(newRefreshToken);

        return Ok(new
        {
            token = newAccessToken,
            refreshToken = newRefreshToken.Token
        });
    }
}
