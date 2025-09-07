using CarInsurance.Api.Dtos;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarInsurance.Api.Controllers;

[ApiController]
[Route("api")]
public class CarsController(ICarService service) : ControllerBase
{
    private readonly ICarService _service = service;

    [HttpGet("cars")]
    public async Task<ActionResult<List<CarDto>>> GetCars()
        => Ok(await _service.ListCarsAsync());

    [HttpPost("cars")]
    public async Task<ActionResult<CarDto>> CreateCar([FromBody] CreateCarRequest request)
    {
        return Ok(await _service.CreateCarAsync(request));
    }

    [HttpGet("cars/{carId:long}/insurance-valid")]
    public async Task<ActionResult<InsuranceValidityResponse>> IsInsuranceValid(long carId, [FromQuery] string date)
    {
        if (!DateOnly.TryParse(date, out var parsed))
            return BadRequest("Invalid date format. Use YYYY-MM-DD.");
        
        var valid = await _service.IsInsuranceValidAsync(carId, parsed);
        return Ok(new InsuranceValidityResponse(carId, parsed.ToString("yyyy-MM-dd"), valid));
    }

    [HttpPost("cars/{carId:long}/claims")]
    public async Task<ActionResult<ClaimDto>> RegisterCarClaim(long carId, [FromBody] CreateClaimRequest request)
    {
        var claim = await _service.RegisterClaimAsync(carId, request);
        return Ok(claim);
    }

    [HttpGet("cars/{carId:long}/history")]
    public async Task<ActionResult<List<HistoryDto>>> GetCarHistory(long carId)
    {
        var history = await _service.GetCarHistoryAsync(carId);
        return Ok(history);
    }
}