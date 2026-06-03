using System.ComponentModel;
using System.Runtime.CompilerServices;
using FoodVisionMauiDemo.Services;
using Microsoft.Maui.Controls;

namespace FoodVisionMauiDemo.Models
{
    public class MealLogItem : INotifyPropertyChanged
    {
        private bool _isVoiceNotePlaying;

        public event PropertyChangedEventHandler? PropertyChanged;

        public int Id { get; set; }

        public string FoodName { get; set; } = string.Empty;

        public string ConfirmedLabel { get; set; } = string.Empty;

        public string MealType { get; set; } = string.Empty;

        public string Portion { get; set; } = string.Empty;

        public string ImagePath { get; set; } = string.Empty;

        public string Notes { get; set; } = string.Empty;

        public string VoiceNotePath { get; set; } = string.Empty;

        public long VoiceNoteSizeBytes { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public List<string> RiskTags { get; set; } = new();

        public IReadOnlyList<string> MealTypes { get; } = new[] { "Breakfast", "Lunch", "Dinner", "Snack" };

        public IReadOnlyList<string> PortionOptions { get; } = new[] { "Small", "Medium", "Large" };

        public string EditableMealType { get; set; } = string.Empty;

        public string EditablePortion { get; set; } = string.Empty;

        public string EditableNotes { get; set; } = string.Empty;

        public string TimeText => CreatedAtUtc.ToLocalTime().ToString("HH:mm");

        public string DateTimeText => CreatedAtUtc.ToLocalTime().ToString("MMM d, HH:mm");

        public string RiskTagsText => RiskLabelFormatter.FormatTagList(RiskTags);

        public string MealSummary => $"{MealType} - {Portion}";

        public bool HasImage => !string.IsNullOrWhiteSpace(ImagePath) && File.Exists(ImagePath);

        public ImageSource? ImageSource => HasImage ? Microsoft.Maui.Controls.ImageSource.FromFile(ImagePath) : null;

        public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);

        public bool HasVoiceNote => !string.IsNullOrWhiteSpace(VoiceNotePath) && File.Exists(VoiceNotePath);

        public string VoiceNoteStatus => HasVoiceNote
            ? IsVoiceNotePlaying
                ? "Playing voice note..."
                : $"Voice note exists ({VoiceNoteSizeBytes / 1024.0:F1} KB)"
            : "No voice note";

        public bool IsVoiceNotePlaying
        {
            get => _isVoiceNotePlaying;
            private set
            {
                if (_isVoiceNotePlaying == value)
                    return;

                _isVoiceNotePlaying = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(VoiceNoteStatus));
                OnPropertyChanged(nameof(VoiceNotePlaybackButtonText));
            }
        }

        public string VoiceNotePlaybackButtonText => IsVoiceNotePlaying ? "Stop Voice Note" : "Play Voice Note";

        public void SetVoiceNotePlaying(bool isPlaying)
        {
            IsVoiceNotePlaying = isPlaying;
        }

        public void BeginEdit()
        {
            EditableMealType = MealType;
            EditablePortion = Portion;
            EditableNotes = Notes;
        }

        public void CommitEdit()
        {
            MealType = EditableMealType;
            Portion = EditablePortion;
            Notes = EditableNotes?.Trim() ?? string.Empty;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
