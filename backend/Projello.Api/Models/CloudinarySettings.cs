public class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;

    // Treat "none" as unset: it was the placeholder suggested for Render when there are no keys
    public bool IsConfigured =>
        new[] { CloudName, ApiKey, ApiSecret }.All(v => !string.IsNullOrWhiteSpace(v) && v.Trim().ToLowerInvariant() != "none");
}