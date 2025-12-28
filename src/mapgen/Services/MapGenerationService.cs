using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MapService.DTOs;
using MapService.Classes;

namespace MapService.Services
{

    public class MapGenerationService
    {
        private readonly HttpClient _httpClient;

        public MapGenerationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(bool Success, UpdateResponseDto? Data, List<string>? Errors)> UpdateMapAsync(MapRawDto payload)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            var response = await _httpClient.PostAsync(
                "http://imggen:5000/generate",
                new StringContent(JsonSerializer.Serialize(payload, options), Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                string errorMessage = "Failed to generate image.";
                try
                {
                    var imggenErrorContent = await response.Content.ReadFromJsonAsync<JsonElement>();
                    if (imggenErrorContent.TryGetProperty("imggen", out var errorProp))
                        errorMessage = errorProp.GetString() ?? errorMessage;
                }
                catch {}

                return (false, null, new List<string> { errorMessage });
            }
            
            var updateResponse = await response.Content.ReadFromJsonAsync<UpdateResponseDto>();
            return (true, updateResponse, null);
        }

        public async Task<(bool Success, CreateResponseDto? Data, List<string>? Errors)> CreateMapAsync(SettingsDto settings)
        {
            Console.WriteLine(JsonSerializer.Serialize(settings));
            var bingoItems = JsonData.BingoItems();

            var payload = new MapRawDto
            {
                Settings = settings,
                Items = Items.GenerateItems(
                    settings.GameVersion!,
                    settings.GridSize,
                    bingoItems,
                    settings.Teams!.Select(t => t.Name).ToArray(),
                    settings.Difficulties!,
                    settings.Constraints!.MaxItemsPerGroup ?? 0,
                    settings.Constraints!.MaxItemsPerMaterial ?? 0,
                    settings.PlacementMode!
                )
            };

            var result = await UpdateMapAsync(payload);
            if (!result.Success)
                return(false, null, result.Errors!);
            
            var responseDto = new CreateResponseDto
            {
                MapUrl = result.Data!.Url,
                MapRaw = payload
            };
            
            return (true, responseDto, null);
        }
    }
}