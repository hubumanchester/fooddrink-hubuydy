using System.Collections.ObjectModel;
using System.Diagnostics;
using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.Repositories;
using FoodVisionMauiDemo.Services;

namespace FoodVisionMauiDemo.ViewModels
{
    public class SaveMealViewModel : BaseViewModel
    {
        private readonly MealLogRepository _mealLogRepository;
        private readonly ImageStorageService _imageStorageService;
        private readonly List<FoodPrediction> _predictions = new();
        private FoodKnowledgeNode? _knowledgeNode;
        private string _foodLabel = string.Empty;
        private string _foodName = "Save Meal";
        private string _imagePath = string.Empty;
        private string? _selectedMealType;
        private string? _selectedPortion;
        private string _notes = string.Empty;
        private string _statusMessage = "Choose meal details before saving.";
        private bool _isBusy;
        private bool _isKnowledgeFallback;
        private ImageSource? _imageSource;

        public SaveMealViewModel()
            : this(new MealLogRepository(), new ImageStorageService())
        {
        }

        public SaveMealViewModel(MealLogRepository mealLogRepository, ImageStorageService imageStorageService)
        {
            _mealLogRepository = mealLogRepository;
            _imageStorageService = imageStorageService;
            SaveMealCommand = new Command(async () => await SaveMealAsync(), () => !IsBusy);
        }

        public IReadOnlyList<string> MealTypes { get; } = new[] { "Breakfast", "Lunch", "Dinner", "Snack" };

        public IReadOnlyList<string> PortionOptions { get; } = new[] { "Small", "Medium", "Large" };

        public ObservableCollection<string> Tags { get; } = new();

        public ObservableCollection<string> Allergens { get; } = new();

        public ObservableCollection<string> Alternatives { get; } = new();

        public Command SaveMealCommand { get; }

        public string FoodName
        {
            get => _foodName;
            private set => SetProperty(ref _foodName, value);
        }

        public ImageSource? ImageSource
        {
            get => _imageSource;
            private set
            {
                if (SetProperty(ref _imageSource, value))
                    OnPropertyChanged(nameof(HasImage));
            }
        }

        public string? SelectedMealType
        {
            get => _selectedMealType;
            set => SetProperty(ref _selectedMealType, value);
        }

        public string? SelectedPortion
        {
            get => _selectedPortion;
            set => SetProperty(ref _selectedPortion, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    SaveMealCommand.ChangeCanExecute();
            }
        }

        public bool HasImage => ImageSource != null;

        public bool HasAllergens => Allergens.Count > 0;

        public bool HasNoAllergens => Allergens.Count == 0;

        public bool HasAlternatives => Alternatives.Count > 0;

        public bool IsKnowledgeFallback
        {
            get => _isKnowledgeFallback;
            private set => SetProperty(ref _isKnowledgeFallback, value);
        }

        public string VoiceNotePlaceholder => "Voice note placeholder - microphone capture will be added in a later phase.";

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            SelectedMealType = null;
            SelectedPortion = null;
            Notes = string.Empty;

            _foodLabel = query.TryGetValue("FoodLabel", out var labelValue) && labelValue is string label
                ? label
                : string.Empty;

            FoodName = query.TryGetValue("FoodName", out var foodNameValue) && foodNameValue is string foodName
                ? foodName
                : ToDisplayText(_foodLabel);

            _imagePath = query.TryGetValue("ImagePath", out var imagePathValue) && imagePathValue is string imagePath
                ? imagePath
                : string.Empty;

            ImageSource = !string.IsNullOrWhiteSpace(_imagePath) && File.Exists(_imagePath)
                ? ImageSource.FromFile(_imagePath)
                : null;

            _predictions.Clear();
            if (query.TryGetValue("Predictions", out var predictionsValue) &&
                predictionsValue is IEnumerable<FoodPrediction> predictions)
            {
                _predictions.AddRange(predictions);
            }

            _knowledgeNode = query.TryGetValue("KnowledgeNode", out var nodeValue) &&
                             nodeValue is FoodKnowledgeNode node
                ? node
                : CreateFallbackNode();

            IsKnowledgeFallback = query.TryGetValue("IsKnowledgeFallback", out var fallbackValue) &&
                                  fallbackValue is bool isFallback &&
                                  isFallback;

            ReplaceCollection(Tags, _knowledgeNode.Tags);
            ReplaceCollection(Allergens, _knowledgeNode.Allergens);
            ReplaceCollection(Alternatives, _knowledgeNode.Alternatives);
            NotifyListStates();

            StatusMessage = "Choose meal details before saving.";
        }

        private async Task SaveMealAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedMealType))
            {
                StatusMessage = "Please choose a meal type before saving.";
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedPortion))
            {
                StatusMessage = "Please choose a portion size before saving.";
                return;
            }

            if (_knowledgeNode == null)
            {
                StatusMessage = "Food knowledge is missing. Please go back and try again.";
                return;
            }

            try
            {
                IsBusy = true;
                StatusMessage = "Saving meal...";

                var permanentImagePath = await _imageStorageService.EnsureImageInAppDataAsync(_imagePath);

                var record = new ScanRecord
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    ConfirmedLabel = _foodLabel,
                    FoodName = FoodName,
                    ImagePath = permanentImagePath,
                    MealType = SelectedMealType,
                    Portion = SelectedPortion,
                    Notes = Notes?.Trim() ?? string.Empty
                };

                var predictionResults = _predictions
                    .OrderBy(prediction => prediction.Rank)
                    .Select(PredictionResult.FromFoodPrediction)
                    .ToList();

                var snapshot = FoodNodeSnapshot.FromKnowledgeNode(_knowledgeNode, IsKnowledgeFallback);

                await _mealLogRepository.SaveMealAsync(record, predictionResults, snapshot);

                StatusMessage = "Meal saved.";
                await Shell.Current.GoToAsync("//DailyLogPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Could not save this meal. Please try again.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        private FoodKnowledgeNode CreateFallbackNode()
        {
            return new FoodKnowledgeNode
            {
                FoodKey = _foodLabel,
                DisplayName = FoodName,
                Cuisine = "Unknown",
                Tags = new List<string> { "Not Covered Yet" },
                Allergens = new List<string>(),
                Ingredients = new List<string>(),
                Alternatives = new List<string> { "Choose a balanced portion" },
                Explanation = "This food is not fully covered in the local knowledge graph yet."
            };
        }

        private static void ReplaceCollection(ObservableCollection<string> collection, IEnumerable<string> values)
        {
            collection.Clear();
            foreach (var value in values)
                collection.Add(value);
        }

        private void NotifyListStates()
        {
            OnPropertyChanged(nameof(HasAllergens));
            OnPropertyChanged(nameof(HasNoAllergens));
            OnPropertyChanged(nameof(HasAlternatives));
        }

        private static string ToDisplayText(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unknown Food";

            var words = value.Replace("-", "_").Split('_', StringSplitOptions.RemoveEmptyEntries);
            return words.Length == 0
                ? value
                : string.Join(" ", words.Select(word => char.ToUpperInvariant(word[0]) + word[1..]));
        }
    }
}
