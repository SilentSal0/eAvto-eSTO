using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using eAvto_eSTO.Enums;

namespace eAvto_eSTO.Databases
{
    [Table("car_rental")]
    public class CarRental
    {
        [Key]
        public int RentalId { get; set; }
        public long UserId { get; set; }
        public int SpotId { get; set; }
        public int CarId { get; set; }

        [Column(TypeName = "ENUM('None', 'Econom', 'Standard', 'Premium')")]
        public CarType Filter { get; set; }
        public DateTime RentalStart { get; set; }
        public DateTime RentalEnd { get; set; }

        [Column(TypeName = "ENUM('Processing', 'Confirmed', 'Canceled', 'Active', 'Completed')")]
        public CarRentalStatusType Status { get; set; }

        public CarRental(long userId)
        {
            UserId = userId;
            Filter = CarType.None;
            RentalStart = DateTime.Today;
            RentalEnd = DateTime.Today;
            Status = CarRentalStatusType.Processing;
        }
    }
}

