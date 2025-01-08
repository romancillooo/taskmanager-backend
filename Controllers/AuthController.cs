using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TodoListApi.Data;
using TodoListApi.Models;

namespace TodoListApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly TodoListDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(TodoListDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(User user)
        {
            // Verificar si el usuario ya existe
            if (_context.Users.Any(u => u.Username == user.Username))
            {
                return BadRequest("El nombre de usuario ya existe.");
            }

            // Asignar rol
            if (string.IsNullOrEmpty(user.Role))
            {
                user.Role = "User"; // Valor predeterminado si no se proporciona un rol
            }

            // Agregar el usuario a la base de datos
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return Ok("Usuario registrado con éxito.");
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(User user)
        {
            // Buscar el usuario en la base de datos
            var dbUser = _context.Users.FirstOrDefault(u => u.Username == user.Username && u.Password == user.Password);

            if (dbUser == null)
            {
                return Unauthorized("Credenciales inválidas.");
            }

            // Generar el token JWT
            var accessToken = GenerateJwtToken(dbUser);

            // Generar el Refresh Token
            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                UserId = dbUser.Id.ToString()
            };

            // Guardar el Refresh Token en la base de datos
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            // Devolver ambos tokens
            return Ok(new
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token
            });
        }

        //POST: api/auth/refresh-token
        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken == null || !refreshToken.IsActive)
            {
                return Unauthorized("Refresh Token inválido o expirado.");
            }

            // Generar nuevo Access Token
            var userId = int.Parse(refreshToken.UserId);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return Unauthorized("Usuario no encontrado.");
            }

            var newAccessToken = GenerateJwtToken(user);


            // Generar nuevo Refresh Token
            var newRefreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                Expires = DateTime.UtcNow.AddDays(7),
                Created = DateTime.UtcNow,
                UserId = user.Id.ToString()
            };

            // Marcar el token anterior como revocado y guardar el nuevo
            refreshToken.Revoked = DateTime.UtcNow;
            _context.RefreshTokens.Add(newRefreshToken);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken.Token
            });
        }


        // Método para generar un token JWT
        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(ClaimTypes.Role, user.Role), // Aquí incluimos el rol en los claims
                new Claim(ClaimTypes.NameIdentifier, user.Username)
            };

            var key = _configuration["Jwt:Key"];
            if (string.IsNullOrEmpty(key) || key.Length < 32)
            {
                throw new InvalidOperationException("La clave JWT debe tener al menos 32 caracteres.");
            }

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var creds = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
