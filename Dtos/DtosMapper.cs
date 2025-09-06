using CarInsurance.Api.Models;

namespace CarInsurance.Api.Dtos;

public class DtosMapper
{
    public static CarDto CarToDto(Car car) => new CarDto(
        car.Id, car.Vin, car.Make, car.Model, car.YearOfManufacture,
        car.OwnerId, car.Owner.Name, car.Owner.Email);
    
    public static ClaimDto ClaimToDto(Claim claim) => new ClaimDto(
        claim.Id, claim.ClaimDate, claim.Description, claim.Amount);
}