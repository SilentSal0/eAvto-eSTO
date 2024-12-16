using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eAvto_eSTO.Databases
{
    [Table("verification_requests")]
    public class VerificationRequest
    {
        [Key]
        public int RequestId { get; set; }
        public long UserId { get; set; }
        public string? Series { get; set; }
        public string? Number { get; set; }

        [Column(TypeName = "MEDIUMBLOB")]
        public byte[]? Document { get; set; }

        [Column(TypeName = "MEDIUMBLOB")]
        public byte[]? Selfie { get; set; }
        public DateTime Date { get; set; }
    }
}
