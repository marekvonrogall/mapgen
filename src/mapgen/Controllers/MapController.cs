using Microsoft.AspNetCore.Mvc;
using MapService.DTOs;
using MapService.Services;

namespace MapService.Controllers;

[ApiController]
[Route("[controller]")]
public class MapController : ControllerBase
{
    private readonly ValidationService _validationService = new();
    private readonly MapGenerationService _mapService;
    
    public MapController(IHttpClientFactory httpClientFactory)
    {
        _mapService = new MapGenerationService(httpClientFactory.CreateClient());
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            message = "works! (mapgen)"
        });
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateRequestDto request)
    {
        var mapgenErrors = new List<string>();
        var validatedSettings = _validationService.ValidateRequest(request.Settings, mapgenErrors);
        
        if (mapgenErrors.Any())
            return BadRequest(new { errors = mapgenErrors });
        
        var result = await _mapService.CreateMapAsync(validatedSettings);

        if (!result.Success)
            return BadRequest(new { errors = result.Errors } );

        return Ok(result.Data);
    }
    
    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] UpdateRequestDto request)
    {
        var settings = request.MapRaw?.Settings ?? request.Settings;
        var items =  request.MapRaw?.Items ?? request.Items;
        
        if (settings == null || items == null)
            return BadRequest(new { errors = "Please provide Settings and Items!" });
        
        var mapgenErrors = new List<string>();
        var validatedSettings = _validationService.ValidateRequest(settings, mapgenErrors);
        
        if (mapgenErrors.Any())
            return BadRequest(new { errors = mapgenErrors });

        var payload = new MapRawDto { Settings = validatedSettings, Items = items };
        var result = await _mapService.UpdateMapAsync(payload);
        if (!result.Success)
            return BadRequest(new { errors = result.Errors } );
        
        return Ok(result.Data);
    }
}
