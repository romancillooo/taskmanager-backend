using TodoListApi.Data;
using TodoListApi.Models;
using Microsoft.EntityFrameworkCore;

namespace TodoListApi.Services
{
    public class TokenService
    {
        private readonly TodoListDbContext _context;

        public TokenService(TodoListDbContext context)
        {
            _context = context;
        }

        public async Task<RefreshToken> GenerateRefreshToken(string userId)
        {
            var refreshToken = new RefreshToken
            {
                Token = Guid.NewGuid().ToString(),
                Expires = DateTime.UtcNow.AddDays(7), // Tiempo de expiración del refresh token
                Created = DateTime.UtcNow,
                UserId = userId
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            return refreshToken;
        }

        public async Task<bool> ValidateRefreshToken(string token, string userId)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token && rt.UserId == userId);

            if (refreshToken == null || !refreshToken.IsActive)
                return false;

            return true;
        }
    }

}
