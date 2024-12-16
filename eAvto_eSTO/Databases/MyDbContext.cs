using Microsoft.EntityFrameworkCore;
using eAvto_eSTO.Json;

namespace eAvto_eSTO.Databases
{
    public class MyDbContext : DbContext
    {
        public DbSet<CarParking> CarParking { get; set; }
        public DbSet<CarRental> CarRental { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<ParkingSpot> ParkingSpots { get; set; }
        public DbSet<RegistrationString> RegistrationStrings { get; set; }
        public DbSet<UserLicense> UserLicenses { get; set; }
        public DbSet<UserPassword> UserPasswords { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<VerificationCode> VerificationCodes { get; set; }                  
        public DbSet<VerificationRequest> VerificationRequests { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var rootPath = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
            var filePath = rootPath + @"/Config/config.json";
            var config = JsonReader.ReadJsonAsync<ConfigStructure>(filePath).GetAwaiter().GetResult();

            optionsBuilder.UseMySQL(config.ConnectionString ?? throw new InvalidDataException("Connection String is null."));
        }
    }
}
