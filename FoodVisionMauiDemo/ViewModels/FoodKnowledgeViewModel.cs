using System.Collections.ObjectModel;
using System.Diagnostics;
using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.Services;
using FoodVisionMauiDemo.Views;

namespace FoodVisionMauiDemo.ViewModels
{
    public class FoodKnowledgeViewModel : BaseViewModel
    {
        private readonly KnowledgeGraphService _knowledgeGraphService;
        private string? _foodLabel;
        private string _imagePath = string.Empty;
        private List<FoodPrediction> _predictions = new();
        private FoodKnowledgeNode? _knowledgeNode;
        private string _foodName = "Food Knowledge";
        private string _cuisine = "Unknown";
        private string _explanation = string.Empty;
        private string _statusMessage = "Loading food knowledge...";
        private bool _isFallback;
        private bool _isBusy;
        private bool _hasLoaded;

        public FoodKnowledgeViewModel()
            : this(new KnowledgeGraphService())
        {
        }

        public FoodKnowledgeViewModel(KnowledgeGraphService knowledgeGraphService)
        {
            _knowledgeGraphService = knowledgeGraphService;
            ContinueToSaveMealCommand = new Command(async () => await ContinueToSaveMealAsync(), () => !IsBusy);
            BackToScanCommand = new Command(async () => await Shell.Current.GoToAsync("//VisionScanPage"));
        }

        public ObservableCollection<string> Tags { get; } = new();

        public ObservableCollection<string> Allergens { get; } = new();

        public ObservableCollection<string> Ingredients { get; } = new();

        public ObservableCollection<string> Alternatives { get; } = new();

        public Command ContinueToSaveMealCommand { get; }

        public Command BackToScanCommand { get; }

        public string FoodName
        {
            get => _foodName;
            private set => SetProperty(ref _foodName, value);
        }

        public string Cuisine
        {
            get => _cuisine;
            private set => SetProperty(ref _cuisine, value);
        }

        public string Explanation
        {
            get => _explanation;
            private set => SetProperty(ref _explanation, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public bool IsFallback
        {
            get => _isFallback;
            private set => SetProperty(ref _isFallback, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                    ContinueToSaveMealCommand.ChangeCanExecute();
            }
        }

        public bool HasAllergens => Allergens.Count > 0;

        public bool HasNoAllergens => Allergens.Count == 0;

        public bool HasIngredients => Ingredients.Count > 0;

        public bool HasAlternatives => Alternatives.Count > 0;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            _hasLoaded = false;
            _knowledgeNode = null;

            if (query.TryGetValue("FoodLabel", out var labelValue) && labelValue is string label)
                _foodLabel = label;
            else
                _foodLabel = null;

            _imagePath = query.TryGetValue("ImagePath", out var imagePathValue) && imagePathValue is string imagePath
                ? imagePath
                : string.Empty;

            _predictions = query.TryGetValue("Predictions", out var predictionsValue) &&
                           predictionsValue is IEnumerable<FoodPrediction> predictions
                ? predictions.ToList()
                : new List<FoodPrediction>();
        }

        public async Task LoadAsync()
        {
            if (_hasLoaded || IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Loading food knowledge...";

                var result = await _knowledgeGraphService.GetFoodKnowledgeAsync(_foodLabel);
                var node = result.Node;
                _knowledgeNode = node;

                FoodName = node.DisplayName;
                Cuisine = node.Cuisine;
                Explanation = node.Explanation;
                IsFallback = result.IsFallback;
                StatusMessage = result.Message;

                ReplaceCollection(Tags, node.Tags);
                ReplaceCollection(Allergens, node.Allergens);
                ReplaceCollection(Ingredients, node.Ingredients);
                ReplaceCollection(Alternatives, node.Alternatives);

                NotifyListStates();
                _hasLoaded = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                _knowledgeNode = new FoodKnowledgeNode
                {
                    FoodKey = _foodLabel ?? string.Empty,
                    DisplayName = "Food Knowledge",
                    Cuisine = "Unknown",
                    Tags = new List<string> { "Not Available" },
                    Allergens = new List<string>(),
                    Ingredients = new List<string>(),
                    Alternatives = new List<string> { "Choose a balanced portion" },
                    Explanation = "The food knowledge page could not load this item. Please go back to Scan and try again."
                };
                FoodName = "Food Knowledge";
                Cuisine = "Unknown";
                Explanation = _knowledgeNode.Explanation;
                StatusMessage = "The food knowledge page could not load this item.";
                IsFallback = true;
                ReplaceCollection(Tags, _knowledgeNode.Tags);
                ReplaceCollection(Allergens, _knowledgeNode.Allergens);
                ReplaceCollection(Ingredients, _knowledgeNode.Ingredients);
                ReplaceCollection(Alternatives, _knowledgeNode.Alternatives);
                NotifyListStates();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ContinueToSaveMealAsync()
        {
            if (_knowledgeNode == null)
            {
                StatusMessage = "Food knowledge is still loading. Please try again in a moment.";
                return;
            }

            var parameters = new Dictionary<string, object>
            {
                ["FoodLabel"] = _foodLabel ?? _knowledgeNode.FoodKey,
                ["FoodName"] = FoodName,
                ["KnowledgeNode"] = _knowledgeNode,
                ["IsKnowledgeFallback"] = IsFallback,
                ["Predictions"] = _predictions
            };

            if (!string.IsNullOrWhiteSpace(_imagePath))
                parameters["ImagePath"] = _imagePath;

            await Shell.Current.GoToAsync(nameof(SaveMealPage), parameters);
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
            OnPropertyChanged(nameof(HasIngredients));
            OnPropertyChanged(nameof(HasAlternatives));
        }
    }
}
