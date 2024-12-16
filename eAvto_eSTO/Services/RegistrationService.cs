using eAvto_eSTO.Databases;
using eAvto_eSTO.Enums;

namespace eAvto_eSTO.Services
{
    public static class RegistrationService
    {
        private static readonly MyDbContext _context = new();

        public static async Task RegisterUserAsync(long userId)
        {
            var registrationString = await GetRegistrationStringByUserIdAsync(userId);
            var user = new User
            {
                UserId = userId,
                Email = registrationString.Email,
                Nickname = registrationString.Nickname,
                Verified = false,
                Access = AccessType.User
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var salt = HashingService.GenerateSalt();
            var hashedPassword = HashingService.HashPassword(registrationString.Password, salt);

            var userPassword = new UserPassword
            {
                UserId = user.UserId,
                Password = hashedPassword,
                Salt = salt
            };

            await _context.AddAsync(userPassword);
            await _context.SaveChangesAsync();
        }

        public static async Task<RegistrationString?> GetRegistrationStringByUserIdAsync(long userId)
        {
            return await Task.Run(() => _context.RegistrationStrings.FirstOrDefault(u => u.UserId == userId));
        }

        public static async Task SaveRegistrationStringAsync(RegistrationString registrationString)
        {
            await _context.RegistrationStrings.AddAsync(registrationString);
            await _context.SaveChangesAsync();
        }

        public static async Task RemoveRegistrationStringByUserIdAsync(long userId)
        {
            var registrationString = _context.RegistrationStrings
                .Where(u => u.UserId == userId)
                .FirstOrDefault() ?? throw new InvalidDataException("User doesn't exist.");

            _context.RegistrationStrings.Remove(registrationString);
            await _context.SaveChangesAsync();
        }
    }
}

