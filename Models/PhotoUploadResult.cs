namespace CampGearApp.Models;

public class PhotoUploadResult
{
    public string Result { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? FileId { get; set; }
    public string? Message { get; set; }
}