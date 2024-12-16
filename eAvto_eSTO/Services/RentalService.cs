using Microsoft.EntityFrameworkCore;
using eAvto_eSTO.Databases;
using eAvto_eSTO.Enums;

namespace eAvto_eSTO.Services
{
    public static class RentalService
    {
        private static readonly MyDbContext _context = new();

        #region CarRental
        public static async Task<CarRental?> GetLastCarRentalByUserIdAsync(long userId)
        {
            return await Task.Run(() => _context.CarRental
                .Where(u => u.UserId == userId)
                .OrderByDescending(u => u.RentalId)
                .FirstOrDefault());
        }

        public static async Task SaveCarRentalAsync(CarRental carRental)
        {
            await _context.CarRental.AddAsync(carRental);
            await _context.SaveChangesAsync();
        }

        public static async Task RemoveCarRentalByUserIdAsync(long userId)
        {
            var carRental = _context.CarRental
                .Where(u => u.UserId == userId)
                .FirstOrDefault() ?? throw new InvalidDataException("User doesn't exist.");

            _context.CarRental.Remove(carRental);
            await _context.SaveChangesAsync();
        }

        public static async Task UpdateCarRentalAsync(CarRental carRental)
        {
            _context.CarRental.Update(carRental);
            await _context.SaveChangesAsync();
        }

        public static async Task UpdateCarRentalByUserIdAsync(long userId, int? spotId = null, int? carId = null, CarType? filter = null,
                                                              DateTime? rentalStart = null, DateTime? rentalEnd = null,
                                                              CarRentalStatusType? status = null)
        {
            var carRental = await GetLastCarRentalByUserIdAsync(userId);
            carRental.SpotId = (int)(spotId != null ? spotId : carRental.SpotId);
            carRental.CarId = (int)(carId != null ? carId : carRental.CarId);
            carRental.Filter = (CarType)(filter != null ? filter : carRental.Filter);
            carRental.RentalStart = (DateTime)(rentalStart != null ? rentalStart : carRental.RentalStart);
            carRental.RentalEnd = (DateTime)(rentalEnd != null ? rentalEnd : carRental.RentalEnd);
            carRental.Status = (CarRentalStatusType)(status != null ? status : carRental.Status);

            _context.CarRental.Update(carRental);
            await _context.SaveChangesAsync();
        }
        #endregion

        #region ParkingSpot
        public static async Task<List<ParkingSpot>> GetParkingSpotsAsync(CarType filter)
        {
            return await _context.ParkingSpots.Where(spot => _context.CarParking
                .Any(cp => cp.SpotId == spot.SpotId &&
                           _context.Cars.Any(car =>
                               car.CarId == cp.CarId &&
                               car.Available &&
                               (filter == CarType.None || car.Class == filter))))
                .ToListAsync();
        }

        public static async Task<ParkingSpot?> GetParkingSpotByIdAsync(int spotId)
        {
            return await Task.Run(() => _context.ParkingSpots.FirstOrDefault(u => u.SpotId == spotId));
        }
        #endregion

        #region Car
        public static async Task<List<Car>> GetCarsByParkingSpotAsync(int spotId)
        {
            return await _context.Cars
                .Where(car => _context.CarParking
                    .Any(cp => cp.CarId == car.CarId && cp.SpotId == spotId) && car.Available)
                .ToListAsync();
        }

        public static async Task<List<Car>> GetCarsByCarRentalAsync(CarRental carRental)
        {
            bool filter = carRental.Filter != CarType.None;

            var query = _context.Cars.AsQueryable();

            query = query.Where(car => car.Available);

            query = query.Where(car => _context.CarParking.Any(cp => cp.CarId == car.CarId && cp.SpotId == carRental.SpotId));

            query = query.Where(car => !_context.CarRental.Any(existingRental =>
                existingRental.CarId == car.CarId &&
                existingRental.Status != CarRentalStatusType.Canceled &&
                ((carRental.RentalStart >= existingRental.RentalStart && carRental.RentalStart < existingRental.RentalEnd) ||
                 (carRental.RentalEnd > existingRental.RentalStart && carRental.RentalEnd <= existingRental.RentalEnd) ||
                 (carRental.RentalStart <= existingRental.RentalStart && carRental.RentalEnd >= existingRental.RentalEnd))));

            if (filter)
            {
                query = query.Where(car => car.Class == carRental.Filter);
            }

            return await query.ToListAsync();
        }

        public static async Task<Car?> GetCarByIdAsync(int carId)
        {
            return await Task.Run(() => _context.Cars.FirstOrDefault(u => u.CarId == carId));
        }
        #endregion
    }
}
