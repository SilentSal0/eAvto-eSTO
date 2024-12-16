using eAvto_eSTO.Databases;
using eAvto_eSTO.Enums;
using Microsoft.EntityFrameworkCore;
using ZstdSharp.Unsafe;

namespace eAvto_eSTO.Services
{
    public static class UserService
    {
        private static readonly MyDbContext _context = new();

        public static async Task<List<User>?> GetAdminsAsync()
        {
            return await _context.Users.Where(u => u.Access == AccessType.Admin).ToListAsync();
        }

        public static async Task<User?> GetUserByIdAsync(long userId)
        {
            return await Task.Run(() => _context.Users.FirstOrDefault(u => u.UserId == userId));
        }

        public static async Task<UserPassword?> GetUserPasswordByIdAsync(long userId)
        {
            return await Task.Run(() => _context.UserPasswords.FirstOrDefault(u => u.UserId == userId));
        }

        public static async Task UpdateUserByIdAsync(long userId, string? nickname = null, string? password = null)
        {
            if (nickname != null)
            {
                var user = await GetUserByIdAsync(userId);
                user.Nickname = nickname;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();
            }

            if (password != null)
            {
                var userPassword = await GetUserPasswordByIdAsync(userId);
                userPassword.Salt = HashingService.GenerateSalt();
                userPassword.Password = HashingService.HashPassword(password, userPassword.Salt);

                _context.UserPasswords.Update(userPassword);
                await _context.SaveChangesAsync();
            }
        }

        public static async Task RemoveUserByIdAsync(long userId)
        {
            var userPassword = await GetUserPasswordByIdAsync(userId);
            _context.UserPasswords.Remove(userPassword);
            await _context.SaveChangesAsync();

            var user = await GetUserByIdAsync(userId);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            await VerificationService.RemoveUserLicenseByUserIdAsync(userId);
        }

        public static bool IsUserRegistered(long userId)
        {
            return _context.Users.FirstOrDefault(u => u.UserId == userId) != null;
        }

        public static bool IsUserVerified(long userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            return user != null && user.Verified;
        }

        public static bool IsUserAdmin(long userId)
        {
            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
            return user != null && user.Access == AccessType.Admin;
        }
    }
}

