using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eAvto_eSTO.Enums;

namespace eAvto_eSTO.Databases
{
    [Table("cars")]
    public class Car
    {
        [Key]
        public int CarId { get; set; }

        [Column(TypeName = "ENUM('None', 'Econom', 'Standard', 'Premium')")]
        public CarType Class { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public int Year { get; set; }
        public string? Color { get; set; }
        public decimal PricePerHour { get; set; }
        public bool Available { get; set; }

    }
}

