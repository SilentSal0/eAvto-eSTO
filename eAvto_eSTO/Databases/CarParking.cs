using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace eAvto_eSTO.Databases
{
    [Table("car_parking")]
    public class CarParking
    {
        [Key]
        public int ParkingId { get; set; } 
        public int CarId { get; set; } 
        public int SpotId { get; set; } 
    }
}

