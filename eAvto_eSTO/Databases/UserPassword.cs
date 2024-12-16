using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eAvto_eSTO.Databases
{
    [Table("user_passwords")]
    public class UserPassword
    {
        [Key]
        public int PasswordId { get; set; }
        public long UserId { get; set; }
        public string? Password { get; set; }
        public string? Salt { get; set; }
    }
}

