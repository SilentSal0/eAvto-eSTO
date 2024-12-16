using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eAvto_eSTO.Enums;

namespace eAvto_eSTO.Databases
{
    [Table("users")]
    public class User
    {
        [Key]
        public long UserId { get; set; }
        public string? Email { get; set; }
        public string? Nickname { get; set; }
        public bool Verified { get; set; }

        [Column(TypeName = "ENUM('User', 'Admin')")]
        public AccessType Access { get; set; }
    }
}

