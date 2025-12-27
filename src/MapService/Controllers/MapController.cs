using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;
using MapService.DTOs;
using MapService.Services;

namespace MapService.Controllers;

[ApiController]
[Route("[controller]")]
public class MapController : ControllerBase
{
    private readonly MapGenerationService _mapService;
    
    private readonly HttpClient _httpClient;
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
        var result = await _mapService.CreateMapAsync(request);

        if (!result.Success)
            return BadRequest(new { errors = result.Errors } );

        return Ok(result.Data as CreateResponseDto);
    }
}
