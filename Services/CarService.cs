using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Exceptions;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class CarService(AppDbContext db) : ICarService
{
    private readonly AppDbContext _db = db;

    public async Task<List<CarDto>> ListCarsAsync()
    {
        return await _db.Cars.Include(c => c.Owner)
            .Select(c => DtosMapper.CarToDto(c))
            .ToListAsync();
    }

    public async Task<CarDto> CreateCarAsync(CreateCarRequest request)
    {
        var vinExists = await _db.Cars.AnyAsync(c => c.Vin == request.Vin);
        if (vinExists)
            throw new NotUniqueVinException(request.Vin);

        var ownerExists = await _db.Owners.AnyAsync(o => o.Id == request.OwnerId);
        if (!ownerExists)
            throw new KeyNotFoundException($"Owner with ID {request.OwnerId} not found.");


        var newCar = new Car
        {
            Vin = request.Vin,
            Make = request.Make,
            Model = request.Model,
            YearOfManufacture = request.YearOfManufacture,
            OwnerId = request.OwnerId
        };

        _db.Cars.Add(newCar);
        await _db.SaveChangesAsync();

        // fetch the car with owner details
        var createdCar = await _db.Cars
            .Include(c => c.Owner)
            .FirstAsync(c => c.Id == newCar.Id);

        return DtosMapper.CarToDto(createdCar);
    }

    public async Task<bool> IsInsuranceValidAsync(long carId, DateOnly date)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        return await _db.Policies.AnyAsync(p =>
            p.CarId == carId &&
            p.StartDate <= date &&
            p.EndDate >= date
        );
    }

    public async Task<ClaimDto> RegisterClaimAsync(long carId, CreateClaimRequest request)
    {
        if (request.ClaimDate > DateOnly.FromDateTime(DateTime.Now))
            throw new InvalidOperationException("Claim date cannot be in the future.");
        
        var carWithValidPolicy = await _db.Cars
            .Where(c => c.Id == carId &&
                        c.Policies.Any(p => p.StartDate <= request.ClaimDate && p.EndDate >= request.ClaimDate))
            .FirstOrDefaultAsync();
        if (carWithValidPolicy == null)
        {
            var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
            if (!carExists)
                throw new KeyNotFoundException($"Car {carId} not found");
            // if the car exists it means it does not have valid insurance
            throw new InvalidOperationException($"Car {carId} does not have valid insurance");
        }
            

        var newClaim = new Claim
        {
            CarId = carId,
            ClaimDate = request.ClaimDate,
            Description = request.Description,
            Amount = request.Amount
        };

        _db.Claims.Add(newClaim);
        await _db.SaveChangesAsync();
        return DtosMapper.ClaimToDto(newClaim);
    }

    public async Task<List<HistoryDto>> GetCarHistoryAsync(long carId)
    {
        var car = await _db.Cars
            .Include(c => c.Policies)
            .Include(c => c.Claims)
            .FirstOrDefaultAsync(c => c.Id == carId);

        if (car == null) throw new KeyNotFoundException($"Car {carId} not found");

        var policies = car.Policies
            .Select(p => new HistoryDto(nameof(EventTypes.PolicyAdded), p.StartDate,
                $"Insurance with {p.Provider} from {p.StartDate} to {p.EndDate}."))
            .ToList();

        var claims = car.Claims
            .Select(c => new HistoryDto(nameof(EventTypes.ClaimRegistered), c.ClaimDate,
                $"Claim for {c.Amount} - '{c.Description}'."))
            .ToList();

        var history = policies.Concat(claims).OrderBy(h => h.EventDate).ToList();
        return history;
    }
}