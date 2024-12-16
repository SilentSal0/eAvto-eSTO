using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eAvto_eSTO.Databases
{
    [Table("parking_spots")]
    public class ParkingSpot
    {
        [Key]
        public int SpotId { get; set; }
        public string? Name { get; set; }
        public string? Location { get; set; }
    }
}

