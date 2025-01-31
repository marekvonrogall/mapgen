namespace MapService
{
    public class CreateRequest
    {
        public int? GridSize { get; set; }
        public string? GameMode { get; set; }
        public string? TeamNames { get; set; }
        public string? Difficulty { get; set; }
    }
}
