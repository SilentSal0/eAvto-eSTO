using eAvto_eSTO.Databases;

namespace eAvto_eSTO.Services
{
    public static class VerificationCodeService
    {
        private static readonly MyDbContext _context = new();

        public static bool IsVerificationCodeValid(long userId, int code)
        {
            var verificationCode = GetLastVerificationCodeByUserIdAsync(userId).GetAwaiter().GetResult();
            return verificationCode.Code == code && verificationCode.ExpiresAt > DateTime.Now;
        }

        public static async Task<VerificationCode?> GetLastVerificationCodeByUserIdAsync(long userId)
        {
            return await Task.Run(() => _context.VerificationCodes
                .Where(u => u.UserId == userId)
                .OrderByDescending(u => u.CreatedAt)
                .FirstOrDefault());
        }

        public static async Task SaveVerificationCodeAsync(VerificationCode verificationCode)
        {
            await _context.VerificationCodes.AddAsync(verificationCode);
            await _context.SaveChangesAsync();
        }

        public static async Task MarkVerificationCodeAsUsedByUserIdAsync(long userId)
        {
            var verificationCode = await GetLastVerificationCodeByUserIdAsync(userId);
            verificationCode.IsUsed = true;

            _context.VerificationCodes.Update(verificationCode);
            await _context.SaveChangesAsync();
        }

        public static VerificationCode GenerateVerificationCode()
        {
            Random random = new();
            var code = random.Next(1000, 9999);
            var verificationCode = new VerificationCode
            {
                Code = code,
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(5),
                IsUsed = false
            };

            return verificationCode;
        }
    }
}

