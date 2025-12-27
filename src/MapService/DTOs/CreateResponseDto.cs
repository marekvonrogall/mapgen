namespace MapService.DTOs
{
    public class CreateResponseDto
    {
        public required string MapUrl { get; set; }
        public required MapRawDto MapRaw { get; set; }
    }
}
