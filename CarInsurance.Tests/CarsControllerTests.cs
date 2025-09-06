using CarInsurance.Api.Controllers;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace CarInsurance.Tests;

public class CarsControllerTests
{
    private readonly Mock<ICarService> _mockCarService;
    private readonly CarsController _controller;

    public CarsControllerTests()
    {
        _mockCarService = new Mock<ICarService>();
        _controller = new CarsController(_mockCarService.Object);
    }

    [Fact]
    public async Task IsInsuranceValid_WhenCarIdNonExistent_ShouldResponseNotFound()
    {
        long nonExistentCarId = 999;
        var validDate = "2024-01-01";
        _mockCarService
            .Setup(s => s.IsInsuranceValidAsync(nonExistentCarId, It.IsAny<DateOnly>()))
            .ThrowsAsync(new KeyNotFoundException($"Car {nonExistentCarId} not found"));

        var result = await _controller.IsInsuranceValid(nonExistentCarId, validDate);

        Assert.IsAssignableFrom<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task IsInsuranceValid_WhenDateFormatInvalid_ShouldReturnsBadRequest()
    {
        long existingCarId = 1;
        string invalidDate = "2025-13-01";

        var result = await _controller.IsInsuranceValid(existingCarId, invalidDate);

        Assert.IsAssignableFrom<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task IsInsuranceValid_WhenValidPolicy_ShouldResponseOk()
    {
        long existingCarId = 1;
        var date = new DateOnly(2024, 06, 15);
        _mockCarService
            .Setup(s => s.IsInsuranceValidAsync(existingCarId, date))
            .ReturnsAsync(true);

        var actionResult = await _controller.IsInsuranceValid(existingCarId, date.ToString("yyyy-MM-dd"));

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var responseValue = Assert.IsType<InsuranceValidityResponse>(okResult.Value);
        Assert.True(responseValue.Valid);
    }

    [Fact]
    public async Task IsInsuranceValid_WhenEndDateIsDateOnPolicy_ShouldResponseOkWithTrue()
    {
        long existingCarId = 1;
        var endDate = new DateOnly(2024, 12, 31);
        _mockCarService
            .Setup(s => s.IsInsuranceValidAsync(existingCarId, endDate))
            .ReturnsAsync(true); 

        var actionResult = await _controller.IsInsuranceValid(existingCarId, endDate.ToString("yyyy-MM-dd"));

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var responseValue = Assert.IsType<InsuranceValidityResponse>(okResult.Value);
        Assert.True(responseValue.Valid);
    }

    [Fact]
    public async Task IsInsuranceValid_WhenEndDateIsDateAfterPolicy_ShouldResponseOkWithFalse()
    {
        long existingCarId = 1;
        var dateAfter = new DateOnly(2025, 01, 01);
        _mockCarService.Setup(s => s.IsInsuranceValidAsync(existingCarId, dateAfter))
            .ReturnsAsync(false);

        var actionResult = await _controller.IsInsuranceValid(existingCarId, dateAfter.ToString("yyyy-MM-dd"));

        var okResult = Assert.IsType<OkObjectResult>(actionResult.Result);
        var responseValue = Assert.IsType<InsuranceValidityResponse>(okResult.Value);
        Assert.False(responseValue.Valid);
    }
}