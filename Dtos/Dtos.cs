using System.ComponentModel.DataAnnotations;

namespace CarInsurance.Api.Dtos;

public record CarDto(
    long Id,
    string Vin,
    string? Make,
    string? Model,
    int Year,
    long OwnerId,
    string OwnerName,
    string? OwnerEmail
);

public record CreateCarRequest(
    [Required]
    [StringLength(17, MinimumLength = 17)] // standard VIN length
    string Vin,
    string? Make,
    string? Model,
    [Required] [Range(1700, 99999)] int YearOfManufacture,
    [Required] [Range(1, long.MaxValue)] long OwnerId
);

public record InsuranceValidityResponse(long CarId, string Date, bool Valid);

public record CreateClaimRequest(
    [Required] DateOnly ClaimDate,
    [Required(AllowEmptyStrings = false)] string Description,
    [Range(0.01, (double)decimal.MaxValue)]
    decimal Amount
);

public record ClaimDto(long Id, DateOnly ClaimDate, string Description, decimal Amount);

public record HistoryDto(string EventType, DateOnly EventDate, string Description);

public enum EventTypes
{
    PolicyAdded,
    ClaimRegistered
}