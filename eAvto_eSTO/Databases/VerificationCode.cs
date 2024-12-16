using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eAvto_eSTO.Databases
{
    [Table("verification_codes")]
    public class VerificationCode
    {
        [Key]
        public int CodeId { get; set; }
        public long UserId { get; set; }
        public int Code { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; }
    }
}

