using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Exceptions;
using CarInsurance.Api.Models;
using CarInsurance.Api.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Tests;

public class CarServiceTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var dbContext = new AppDbContext(options);
        return dbContext;
    }

    [Fact]
    public async Task CreateCarAsync_WithUniqueVin_ShouldCreateAndReturnCar()
    {
        var dbContext = GetInMemoryDbContext();
        var owner = new Owner { Name = "Test Owner" };
        dbContext.Owners.Add(owner);
        await dbContext.SaveChangesAsync();

        var service = new CarService(dbContext);
        var request = new CreateCarRequest("VIN_UNIQUE", "Test", "Car", 2024, owner.Id);

        var result = await service.CreateCarAsync(request);

        result.Should().NotBeNull();
        result.Vin.Should().Be("VIN_UNIQUE");
        var carInDb = await dbContext.Cars.FirstOrDefaultAsync(c => c.Vin == "VIN_UNIQUE");
        carInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateCarAsync_WithDuplicateVin_ShouldThrowNotUniqueVinException()
    {
        var dbContext = GetInMemoryDbContext();
        var owner = new Owner { Name = "Test Owner" };
        var existingCar = new Car { Vin = "VIN_DUPLICATE", Owner = owner };
        dbContext.Cars.Add(existingCar);
        await dbContext.SaveChangesAsync();

        var service = new CarService(dbContext);
        var request = new CreateCarRequest("VIN_DUPLICATE", "Test", "Car", 2024, owner.Id);

        Func<Task> act = () => service.CreateCarAsync(request);

        await act.Should().ThrowAsync<NotUniqueVinException>()
            .WithMessage("Car with VIN VIN_DUPLICATE already exists.");
    }

    [Fact]
    public async Task RegisterClaimAsync_WhenCarHasNoValidInsurance_ShouldThrowInvalidOperationException()
    {
        var dbContext = GetInMemoryDbContext();
        var owner = new Owner { Name = "Test Owner" };
        var car = new Car { Vin = "VIN123", Owner = owner };
        var policy = new InsurancePolicy
        {
            Car = car, Provider = "Test", StartDate = new DateOnly(2023, 1, 1), EndDate = new DateOnly(2023, 12, 31)
        };
        dbContext.Cars.Add(car);
        dbContext.Policies.Add(policy);
        await dbContext.SaveChangesAsync();

        var service = new CarService(dbContext);
        var request = new CreateClaimRequest(new DateOnly(2024, 6, 1), "Test claim", 500);

        Func<Task> act = () => service.RegisterClaimAsync(car.Id, request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage(
                $"Car {car.Id} does not have valid insurance");
    }

    [Fact]
    public async Task GetCarHistoryAsync_WhenCarExists_ShouldReturnOrderedHistory()
    {
        var dbContext = GetInMemoryDbContext();
        var owner = new Owner { Name = "Test Owner" };
        var car = new Car { Vin = "VIN123", Owner = owner };
        var claim = new Claim
            { Car = car, ClaimDate = new DateOnly(2024, 6, 15), Description = "Accident", Amount = 1000 };
        var policy = new InsurancePolicy
        {
            Car = car, Provider = "Test", StartDate = new DateOnly(2024, 1, 1), EndDate = new DateOnly(2024, 12, 31)
        };
        dbContext.Cars.Add(car);
        dbContext.Claims.Add(claim);
        dbContext.Policies.Add(policy);
        await dbContext.SaveChangesAsync();

        var service = new CarService(dbContext);

        var history = await service.GetCarHistoryAsync(car.Id);

        history.Should().NotBeNull();
        history.Should().HaveCount(2);
        history[0].EventDate.Should().Be(policy.StartDate);
        history[0].EventType.Should().Be(nameof(EventTypes.PolicyAdded));
        history[1].EventDate.Should().Be(claim.ClaimDate);
        history[1].EventType.Should().Be(nameof(EventTypes.ClaimRegistered));
    }
}