namespace FoodVisionMauiDemo.Models
{
    public class VoiceNoteInfo
    {
        public string FilePath { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        public TimeSpan Duration { get; set; }

        public string FileSizeText => FileSizeBytes <= 0
            ? "0 KB"
            : $"{FileSizeBytes / 1024.0:F1} KB";

        public string DurationText => Duration.ToString(@"mm\:ss");
    }
}
