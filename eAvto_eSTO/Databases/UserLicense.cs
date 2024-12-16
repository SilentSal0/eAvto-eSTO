using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eAvto_eSTO.Databases
{
    [Table("user_licenses")]
    public class UserLicense
    {
        [Key]
        public int LicenseId { get; set; }
        public long UserId { get; set; }
        public string? Series { get; set; }
        public string? Number { get; set; }
        public bool Checked { get; set; }
    }
}

