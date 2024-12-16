using System.Text;
using System.Security.Cryptography;

namespace eAvto_eSTO.Services
{
    public static class HashingService
    {
        public static string HashPassword(string password, string salt)
        {
            var saltedPassword = password + salt;
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(saltedPassword));
            StringBuilder builder = new();

            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }

            return builder.ToString();
        }

        public static string GenerateSalt(int size = 16)
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] salt = new byte[size];
            rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }
    }

}

