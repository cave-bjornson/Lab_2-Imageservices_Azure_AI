namespace WebApp;

public class CustomVisionOptions
{
    public string PredictionEndpoint { get; set; } = string.Empty;
    public string PredictionKey { get; set; } = string.Empty;
    public Guid ProjectID { get; set; } = Guid.Empty;
    public string ModelName { get; set; } = string.Empty;
}