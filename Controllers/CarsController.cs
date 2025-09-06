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

    [HttpGet("cars/{carId:long}/insurance-valid")]
    public async Task<ActionResult<InsuranceValidityResponse>> IsInsuranceValid(long carId, [FromQuery] DateOnly date)
    {
        try
        {
            var valid = await _service.IsInsuranceValidAsync(carId, date);
            return Ok(new InsuranceValidityResponse(carId, date.ToString("yyyy-MM-dd"), valid));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
    
    [HttpPost("cars/{carId:long}/claims")]
    public async Task<ActionResult<ClaimDto>> RegisterCarClaim(long carId, [FromBody]CreateClaimRequest request) 
    {
        if (request.ClaimDate > DateOnly.FromDateTime(DateTime.Now))
            return BadRequest("Claim date cannot be in the future.");
        
        try
        {
            var claim = await _service.RegisterClaimAsync(carId, request);
            return Ok(claim);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    
    [HttpGet("cars/{carId:long}/history")]
    public async Task<ActionResult<List<HistoryDto>>> GetCarHistory(long carId)
    {
        try
        {
            var history = await _service.GetCarHistoryAsync(carId);
            return Ok(history);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
