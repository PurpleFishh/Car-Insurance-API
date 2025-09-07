namespace CarInsurance.Api.Exceptions;

public class NotUniqueVinException(string vin)
    : Exception($"Car with VIN {vin} already exists.");