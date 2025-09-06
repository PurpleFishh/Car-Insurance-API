using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class CarService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<List<CarDto>> ListCarsAsync()
    {
        return await _db.Cars.Include(c => c.Owner)
            .Select(c => DtosMapper.CarToDto(c))
            .ToListAsync();
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
        var car = await _db.Cars.FindAsync(carId);
        if (car == null) throw new KeyNotFoundException($"Car {carId} not found");

        var isInsuranceValid = await IsInsuranceValidAsync(carId, request.ClaimDate);
        if (!isInsuranceValid)
            throw new InvalidOperationException($"Car {carId} does not have valid insurance");

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
            .Select(p => new HistoryDto(nameof(EventType.PolicyAdded), p.StartDate,
                $"Insurance with {p.Provider} from {p.StartDate} to {p.EndDate}."))
            .ToList();

        var claims = car.Claims
            .Select(c => new HistoryDto(nameof(EventType.ClaimRegistered), c.ClaimDate,
                $"Claim for {c.Amount} - '{c.Description}'."))
            .ToList();

        var history = policies.Concat(claims).OrderBy(h => h.EventDate).ToList();
        return history;
    }
}