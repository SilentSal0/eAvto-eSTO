using Telegram.Bot;
using Microsoft.EntityFrameworkCore;
using eAvto_eSTO.Databases;
using eAvto_eSTO.Enums;
using File = Telegram.Bot.Types.File;

namespace eAvto_eSTO.Services
{
    public static class VerificationService
    {
        private static readonly MyDbContext _context = new();

        public static async Task VerifyUserAsync(long userId)
        {
            var user = await UserService.GetUserByIdAsync(userId);
            user.Verified = true;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            var verificationRequest = await GetVerificationRequestByUserIdAsync(userId);
            var userLicense = new UserLicense
            {
                UserId = userId,
                Series = verificationRequest.Series,
                Number = verificationRequest.Number,
                Checked = true
            };

            await _context.UserLicenses.AddAsync(userLicense);
            await _context.SaveChangesAsync();
        }

        public static async Task UnverifyUserAsync(long userId)
        {
            var user = await UserService.GetUserByIdAsync(userId);
            user.Verified = false;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            await RemoveUserLicenseByUserIdAsync(userId);
        }

        #region VerificationRequest
        public static async Task<List<VerificationRequest>> GetVerificationRequestsAsync()
        {
            return await _context.VerificationRequests.ToListAsync();
        }

        public static async Task<VerificationRequest?> GetVerificationRequestByIdAsync(int requestId)
        {
            return await Task.Run(() => _context.VerificationRequests.FirstOrDefault(u => u.RequestId == requestId));
        }

        public static async Task<VerificationRequest?> GetVerificationRequestByUserIdAsync(long userId)
        {
            return await Task.Run(() => _context.VerificationRequests.FirstOrDefault(u => u.UserId == userId));
        }

        public static async Task SaveVerificationRequestAsync(VerificationRequest verificationRequest)
        {
            await _context.AddAsync(verificationRequest);
            await _context.SaveChangesAsync();
        }

        public static async Task RemoveVerificationRequestByUserIdAsync(long userId)
        {
            var verificationRequest = _context.VerificationRequests
                .Where(u => u.UserId == userId)
                .FirstOrDefault() ?? throw new InvalidDataException("User doesn't exist.");

            _context.VerificationRequests.Remove(verificationRequest);
            await _context.SaveChangesAsync();
        }

        public static async Task UpdateVerificationRequestWithImageAsync(VerificationRequest verificationRequest, byte[] image)
        {
            if (verificationRequest.Document == null)
            {
                verificationRequest.Document = image;
            }
            else
            {
                verificationRequest.Selfie = image;
                verificationRequest.Date = DateTime.Now;
            }

            _context.VerificationRequests.Update(verificationRequest);
            await _context.SaveChangesAsync();
        }
        #endregion

        #region UserLicense
        public static async Task<List<UserLicense>> GetUserLicensesAsync()
        {
            return await _context.UserLicenses.Where(u => !u.Checked).ToListAsync();
        }

        public static async Task<UserLicense?> GetUserLicenseByIdAsync(int licenseId)
        {
            return await Task.Run(() => _context.UserLicenses.FirstOrDefault(u => u.LicenseId == licenseId));
        }

        public static async Task<UserLicense?> GetUserLicenseByUserIdAsync(long userId)
        {
            return await Task.Run(() => _context.UserLicenses.FirstOrDefault(u => u.UserId == userId));
        }

        public static async Task RemoveUserLicenseByUserIdAsync(long userId)
        {
            var userLicense = _context.UserLicenses
                .Where(u => u.UserId == userId)
                .FirstOrDefault() ?? throw new InvalidDataException("User doesn't exist.");

            _context.UserLicenses.Remove(userLicense);
            await _context.SaveChangesAsync();
        }
        public static async Task UpdateUserLicenseStatusByUserIdAsync(long userId, bool status)
        {
            var userLicense = await GetUserLicenseByUserIdAsync(userId);
            userLicense.Checked = status;
            _context.UserLicenses.Update(userLicense);
            await _context.SaveChangesAsync();
        }
        #endregion

        public static VerificationActionType GenerateVerificationAction()
        {
            Random random = new();
            var action = random.Next(1, 4);

            return (VerificationActionType)action;
        }

        public static async Task<byte[]> DownloadImageAsync(ITelegramBotClient botClient, File file)
        {
            using var stream = new MemoryStream();

            await botClient.DownloadFile(file.FilePath, stream);
            return stream.ToArray();
        }
    }
}

