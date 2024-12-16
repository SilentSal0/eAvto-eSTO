using System.IO.Compression;
using System.Reflection;
using eAvto_eSTO.Databases;

namespace eAvto_eSTO.Services
{
    public static class ArchiveService
    {
        public static async Task ArchiveVerificationRequestAsync(VerificationRequest verificationRequest, string? discardReason = null)
        {
            var exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var archivePath = Path.Combine(exePath, "Archive");
            var user = await UserService.GetUserByIdAsync(verificationRequest.UserId);
            var userPath = Path.Combine(archivePath, user.Email);

            if (!Directory.Exists(userPath))
            {
                Directory.CreateDirectory(userPath);
            }

            await File.WriteAllBytesAsync(Path.Combine(userPath, "document.jpg"), verificationRequest.Document);
            await File.WriteAllBytesAsync(Path.Combine(userPath, "selfie.jpg"), verificationRequest.Selfie);
            await File.WriteAllTextAsync(Path.Combine(userPath, "seriesNumber.txt"), $"{verificationRequest.Series}{verificationRequest.Number}");

            if (discardReason != null)
            {
                await File.WriteAllTextAsync(Path.Combine(userPath, "discardReason.txt"), $"{discardReason}.");
            }

            ZipFile.CreateFromDirectory(userPath, $"{userPath}_{DateTime.Now:dd-MM-yyyy_H-mm-ss}.zip", CompressionLevel.SmallestSize, true);
            Directory.Delete(userPath, true);
        }
    }
}

