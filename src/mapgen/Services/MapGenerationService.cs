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
        private readonly string _imgGenUrl;

        public MapGenerationService(HttpClient httpClient, EnvironmentVariables env)
        {
            _httpClient = httpClient;
            _imgGenUrl = env.ImgGenUrl;
        }

        public async Task<(bool Success, UpdateResponseDto? Data, List<string>? Errors)> UpdateMapAsync(MapRawDto payload)
        {
            var options = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            
            var response = await _httpClient.PostAsync(
                $"{_imgGenUrl}/generate",
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
            var bingoItems = JsonData.BingoItems();

            var generatedItems = Items.GenerateItems(
                settings,
                bingoItems
            );
                
            if (!generatedItems.Success)
                return(generatedItems.Success, null, generatedItems.Errors!);
                
            var payload = new MapRawDto
            {
                Settings = settings,
                Items = generatedItems.Items!
            };

            var result = await UpdateMapAsync(payload);
            if (!result.Success)
                return(result.Success, null, result.Errors!);
            
            var responseDto = new CreateResponseDto
            {
                MapUrl = result.Data!.Url,
                MapRaw = payload
            };
            
            return (true, responseDto, null);
        }
    }
}