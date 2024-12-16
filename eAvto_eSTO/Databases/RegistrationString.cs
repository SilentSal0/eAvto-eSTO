using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eAvto_eSTO.Databases
{
    [Table("registration_strings")]
    public class RegistrationString
    {
        [Key]
        public long UserId { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Nickname { get; set; }
    }
}

