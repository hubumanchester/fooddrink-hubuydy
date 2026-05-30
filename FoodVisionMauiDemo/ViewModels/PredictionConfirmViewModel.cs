using System.Collections.ObjectModel;
using System.Diagnostics;
using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.Views;

namespace FoodVisionMauiDemo.ViewModels
{
    public class PredictionConfirmViewModel : BaseViewModel
    {
        private FoodPrediction? _selectedPrediction;
        private string _imagePath = string.Empty;
        private ImageSource? _thumbnailSource;
        private string _statusMessage = "No predictions were available. Please go back to Scan and analyse a food image again.";
        private bool _hasPredictions;

        public PredictionConfirmViewModel()
        {
            ConfirmSelectionCommand = new Command(async () => await ConfirmSelectionAsync(), () => SelectedPrediction != null);
            BackToScanCommand = new Command(async () => await Shell.Current.GoToAsync("//VisionScanPage"));
        }

        public ObservableCollection<FoodPrediction> Predictions { get; } = new();

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

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            try
            {
                Predictions.Clear();
                SelectedPrediction = null;
                _imagePath = string.Empty;
                ThumbnailSource = null;

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
                    StatusMessage = "Choose the prediction that best matches the food photo.";
                }
                else
                {
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
            if (SelectedPrediction == null)
            {
                StatusMessage = "Please choose a prediction before continuing.";
                return;
            }

            try
            {
                var parameters = new Dictionary<string, object>
                {
                    ["FoodLabel"] = SelectedPrediction.Label,
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
    }
}
