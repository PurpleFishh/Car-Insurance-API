using CarInsurance.Api.Dtos;

namespace CarInsurance.Api.Services;

public interface ICarService
{
    public Task<List<CarDto>> ListCarsAsync();
    public Task<CarDto> CreateCarAsync(CreateCarRequest request);
    public Task<bool> IsInsuranceValidAsync(long carId, DateOnly date);
    public Task<ClaimDto> RegisterClaimAsync(long carId, CreateClaimRequest request);
    public Task<List<HistoryDto>> GetCarHistoryAsync(long carId);
}