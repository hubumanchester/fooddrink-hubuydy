using System.Collections.ObjectModel;
using System.Diagnostics;
using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.Views;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Storage;

namespace FoodVisionMauiDemo.ViewModels
{
    public class PredictionConfirmViewModel : BaseViewModel
    {
        private FoodPrediction? _selectedPrediction;
        private string _imagePath = string.Empty;
        private ImageSource? _thumbnailSource;
        private string _statusMessage = "No predictions were available. Please go back to Scan and analyse a food image again.";
        private string? _selectedManualLabel;
        private string _lowConfidenceMessage = string.Empty;
        private bool _hasPredictions;
        private bool _isLowConfidence;

        public PredictionConfirmViewModel()
        {
            ConfirmSelectionCommand = new Command(
                async () => await ConfirmSelectionAsync(),
                () => SelectedPrediction != null || !string.IsNullOrWhiteSpace(SelectedManualLabel));
            BackToScanCommand = new Command(async () => await Shell.Current.GoToAsync("//VisionScanPage"));
        }

        public ObservableCollection<FoodPrediction> Predictions { get; } = new();

        public ObservableCollection<string> ManualLabels { get; } = new();

        public Command ConfirmSelectionCommand { get; }

        public Command BackToScanCommand { get; }

        public FoodPrediction? SelectedPrediction
        {
            get => _selectedPrediction;
            set
            {
                if (SetProperty(ref _selectedPrediction, value))
                    ConfirmSelectionCommand.ChangeCanExecute();
            }
        }

        public ImageSource? ThumbnailSource
        {
            get => _thumbnailSource;
            private set
            {
                if (SetProperty(ref _thumbnailSource, value))
                    OnPropertyChanged(nameof(HasThumbnail));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public bool HasPredictions
        {
            get => _hasPredictions;
            private set
            {
                if (SetProperty(ref _hasPredictions, value))
                    OnPropertyChanged(nameof(HasNoPredictions));
            }
        }

        public bool HasNoPredictions => !HasPredictions;

        public bool HasThumbnail => ThumbnailSource != null;

        public string? SelectedManualLabel
        {
            get => _selectedManualLabel;
            set
            {
                if (SetProperty(ref _selectedManualLabel, value))
                    ConfirmSelectionCommand.ChangeCanExecute();
            }
        }

        public bool IsLowConfidence
        {
            get => _isLowConfidence;
            private set => SetProperty(ref _isLowConfidence, value);
        }

        public string LowConfidenceMessage
        {
            get => _lowConfidenceMessage;
            private set => SetProperty(ref _lowConfidenceMessage, value);
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            try
            {
                Predictions.Clear();
                SelectedPrediction = null;
                SelectedManualLabel = null;
                _imagePath = string.Empty;
                ThumbnailSource = null;
                _ = LoadManualLabelsAsync();

                if (query.TryGetValue("Predictions", out var predictionsValue) &&
                    predictionsValue is IEnumerable<FoodPrediction> predictions)
                {
                    foreach (var prediction in predictions)
                        Predictions.Add(prediction);
                }

                HasPredictions = Predictions.Count > 0;

                if (HasPredictions)
                {
                    SelectedPrediction = Predictions[0];
                    IsLowConfidence = Predictions[0].IsLowConfidence;
                    LowConfidenceMessage = IsLowConfidence
                        ? "The model is not fully confident. Please confirm carefully or choose a Food-101 label manually."
                        : string.Empty;
                    StatusMessage = IsLowConfidence
                        ? "Low confidence result. Please confirm the closest match."
                        : "Choose the prediction that best matches the food photo.";
                }
                else
                {
                    IsLowConfidence = false;
                    LowConfidenceMessage = string.Empty;
                    StatusMessage = "No predictions were available. Please go back to Scan and analyse a food image again.";
                }

                if (query.TryGetValue("ImagePath", out var imagePathValue) &&
                    imagePathValue is string imagePath &&
                    File.Exists(imagePath))
                {
                    _imagePath = imagePath;
                    ThumbnailSource = ImageSource.FromFile(imagePath);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                HasPredictions = false;
                StatusMessage = "The prediction results could not be opened. Please go back to Scan and try again.";
            }
        }

        private async Task ConfirmSelectionAsync()
        {
            var confirmedLabel = !string.IsNullOrWhiteSpace(SelectedManualLabel)
                ? SelectedManualLabel
                : SelectedPrediction?.Label;

            if (string.IsNullOrWhiteSpace(confirmedLabel))
            {
                StatusMessage = "Please choose a prediction or manual Food-101 label before continuing.";
                return;
            }

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["FoodLabel"] = confirmedLabel,
                    ["Predictions"] = Predictions.ToList()
                };

                if (!string.IsNullOrWhiteSpace(_imagePath))
                    parameters["ImagePath"] = _imagePath;

                await Shell.Current.GoToAsync(nameof(FoodKnowledgePage), parameters);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Could not open the food knowledge page. Please try again.";
            }
        }

        private async Task LoadManualLabelsAsync()
        {
            if (ManualLabels.Count > 0)
                return;

            try
            {
                await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("food101_labels.txt");
                using var reader = new StreamReader(stream);
                var text = await reader.ReadToEndAsync();
                var labels = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(label => label.Trim())
                    .Where(label => !string.IsNullOrWhiteSpace(label))
                    .OrderBy(label => label)
                    .ToList();

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    foreach (var label in labels)
                        ManualLabels.Add(label);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
