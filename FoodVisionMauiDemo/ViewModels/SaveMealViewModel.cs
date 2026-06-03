using System.Collections.ObjectModel;
using System.Diagnostics;
using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.Repositories;
using FoodVisionMauiDemo.Services;
using Microsoft.Maui.ApplicationModel;

namespace FoodVisionMauiDemo.ViewModels
{
    public class SaveMealViewModel : BaseViewModel
    {
        private readonly MealLogRepository _mealLogRepository;
        private readonly ImageStorageService _imageStorageService;
        private readonly VoiceNoteService _voiceNoteService;
        private readonly VoiceNotePlaybackService _voiceNotePlaybackService;
        private readonly FeedbackService _feedbackService;
        private readonly AppSettingsService _settingsService;
        private readonly List<FoodPrediction> _predictions = new();
        private IDispatcherTimer? _recordingTimer;
        private FoodKnowledgeNode? _knowledgeNode;
        private VoiceNoteInfo? _voiceNoteInfo;
        private string _foodLabel = string.Empty;
        private string _foodName = "Save Meal";
        private string _imagePath = string.Empty;
        private string? _selectedMealType;
        private string? _selectedPortion;
        private string _notes = string.Empty;
        private string _statusMessage = "Choose meal details before saving.";
        private string _voiceNoteStatus = "No voice note recorded.";
        private string _recordingDurationText = "00:00";
        private bool _isBusy;
        private bool _isRecording;
        private bool _isPlayingVoiceNote;
        private bool _hasValidationError;
        private bool _isKnowledgeFallback;
        private ImageSource? _imageSource;

        public SaveMealViewModel()
            : this(
                new MealLogRepository(),
                new ImageStorageService(),
                new VoiceNoteService(),
                new VoiceNotePlaybackService(),
                new FeedbackService(),
                new AppSettingsService())
        {
        }

        public SaveMealViewModel(
            MealLogRepository mealLogRepository,
            ImageStorageService imageStorageService,
            VoiceNoteService voiceNoteService,
            VoiceNotePlaybackService voiceNotePlaybackService,
            FeedbackService feedbackService,
            AppSettingsService settingsService)
        {
            _mealLogRepository = mealLogRepository;
            _imageStorageService = imageStorageService;
            _voiceNoteService = voiceNoteService;
            _voiceNotePlaybackService = voiceNotePlaybackService;
            _feedbackService = feedbackService;
            _settingsService = settingsService;
            SaveMealCommand = new Command(async () => await SaveMealAsync(), () => !IsBusy && !IsRecording);
            ToggleVoiceRecordingCommand = new Command(async () => await ToggleVoiceRecordingAsync(), () => !IsBusy);
            ToggleVoicePlaybackCommand = new Command(async () => await ToggleVoicePlaybackAsync(), () => !IsBusy && !IsRecording && HasVoiceNote);
            _voiceNotePlaybackService.PlaybackCompleted += OnVoicePlaybackCompleted;

            _recordingTimer = Application.Current?.Dispatcher.CreateTimer();
            if (_recordingTimer != null)
            {
                _recordingTimer.Interval = TimeSpan.FromSeconds(1);
                _recordingTimer.Tick += (_, _) =>
                {
                    RecordingDurationText = _voiceNoteService.Elapsed.ToString(@"mm\:ss");
                };
            }
        }

        public IReadOnlyList<string> MealTypes { get; } = new[] { "Breakfast", "Lunch", "Dinner", "Snack" };

        public IReadOnlyList<string> PortionOptions { get; } = new[] { "Small", "Medium", "Large" };

        public ObservableCollection<string> Tags { get; } = new();

        public ObservableCollection<string> Allergens { get; } = new();

        public ObservableCollection<string> Alternatives { get; } = new();

        public Command SaveMealCommand { get; }

        public Command ToggleVoiceRecordingCommand { get; }

        public Command ToggleVoicePlaybackCommand { get; }

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
            set
            {
                if (SetProperty(ref _selectedMealType, value))
                    ClearValidationErrorIfReady();
            }
        }

        public string? SelectedPortion
        {
            get => _selectedPortion;
            set
            {
                if (SetProperty(ref _selectedPortion, value))
                    ClearValidationErrorIfReady();
            }
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

        public bool HasValidationError
        {
            get => _hasValidationError;
            private set => SetProperty(ref _hasValidationError, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    SaveMealCommand.ChangeCanExecute();
                    ToggleVoiceRecordingCommand.ChangeCanExecute();
                    ToggleVoicePlaybackCommand.ChangeCanExecute();
                }
            }
        }

        public bool IsRecording
        {
            get => _isRecording;
            private set
            {
                if (SetProperty(ref _isRecording, value))
                {
                    OnPropertyChanged(nameof(VoiceRecordButtonText));
                    OnPropertyChanged(nameof(RecordingLabel));
                    SaveMealCommand.ChangeCanExecute();
                    ToggleVoiceRecordingCommand.ChangeCanExecute();
                    ToggleVoicePlaybackCommand.ChangeCanExecute();
                }
            }
        }

        public bool IsPlayingVoiceNote
        {
            get => _isPlayingVoiceNote;
            private set
            {
                if (SetProperty(ref _isPlayingVoiceNote, value))
                    OnPropertyChanged(nameof(VoicePlaybackButtonText));
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

        public string VoiceNoteStatus
        {
            get => _voiceNoteStatus;
            private set => SetProperty(ref _voiceNoteStatus, value);
        }

        public string RecordingDurationText
        {
            get => _recordingDurationText;
            private set => SetProperty(ref _recordingDurationText, value);
        }

        public bool HasVoiceNote => _voiceNoteInfo != null;

        public string VoiceRecordButtonText => IsRecording ? "Stop Recording Voice Note" : "Record Voice Note";

        public string VoicePlaybackButtonText => IsPlayingVoiceNote ? "Stop Voice Note" : "Play Voice Note";

        public string RecordingLabel => IsRecording ? $"Recording... {RecordingDurationText}" : RecordingDurationText;

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            SelectedMealType = null;
            SelectedPortion = null;
            Notes = string.Empty;
            _voiceNoteInfo = null;
            StopVoicePlaybackIfNeeded("No voice note recorded.");
            VoiceNoteStatus = "No voice note recorded.";
            RecordingDurationText = "00:00";
            IsRecording = false;
            OnPropertyChanged(nameof(HasVoiceNote));
            ToggleVoicePlaybackCommand.ChangeCanExecute();

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

            SetStatus("Choose meal details before saving.", false);
        }

        private async Task SaveMealAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedMealType))
            {
                SetStatus("Please choose a meal type before saving.", true);
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedPortion))
            {
                SetStatus("Please choose a portion size before saving.", true);
                return;
            }

            if (_knowledgeNode == null)
            {
                SetStatus("Food knowledge is missing. Please go back and try again.", true);
                return;
            }

            if (IsRecording)
            {
                SetStatus("Please stop recording before saving this meal.", true);
                return;
            }

            StopVoicePlaybackIfNeeded("Voice note ready.");

            if (!_settingsService.SaveScanHistory)
            {
                SetStatus("Scan history saving is disabled in Settings.", true);
                return;
            }

            try
            {
                IsBusy = true;
                SetStatus("Saving meal...", false);

                var permanentImagePath = await _imageStorageService.EnsureImageInAppDataAsync(_imagePath);

                var record = new ScanRecord
                {
                    CreatedAtUtc = DateTime.UtcNow,
                    ConfirmedLabel = _foodLabel,
                    FoodName = FoodName,
                    ImagePath = permanentImagePath,
                    MealType = SelectedMealType,
                    Portion = SelectedPortion,
                    Notes = Notes?.Trim() ?? string.Empty,
                    VoiceNotePath = _voiceNoteInfo?.FilePath ?? string.Empty,
                    VoiceNoteSizeBytes = _voiceNoteInfo?.FileSizeBytes ?? 0
                };

                var predictionResults = _predictions
                    .OrderBy(prediction => prediction.Rank)
                    .Select(PredictionResult.FromFoodPrediction)
                    .ToList();

                var snapshot = FoodNodeSnapshot.FromKnowledgeNode(_knowledgeNode, IsKnowledgeFallback);

                await _mealLogRepository.SaveMealAsync(record, predictionResults, snapshot);
                await _feedbackService.SuccessAsync();

                SetStatus("Meal saved.", false);
                await Shell.Current.GoToAsync("//DailyLogPage");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                SetStatus("Could not save this meal. Please try again.", true);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task ToggleVoiceRecordingAsync()
        {
            if (IsRecording)
            {
                await StopVoiceRecordingAsync();
                return;
            }

            await StartVoiceRecordingAsync();
        }

        private async Task StartVoiceRecordingAsync()
        {
            try
            {
                StopVoicePlaybackIfNeeded("Starting a new voice note...");
                await _voiceNoteService.StartRecordingAsync();
                IsRecording = true;
                RecordingDurationText = "00:00";
                VoiceNoteStatus = "Recording...";
                _recordingTimer?.Start();
            }
            catch (VoiceNoteException ex)
            {
                VoiceNoteStatus = ex.Message;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                VoiceNoteStatus = "Could not start voice recording. Please try again.";
            }
        }

        private async Task StopVoiceRecordingAsync()
        {
            try
            {
                _recordingTimer?.Stop();
                _voiceNoteInfo = await _voiceNoteService.StopRecordingAsync();
                IsRecording = false;
                RecordingDurationText = _voiceNoteInfo.DurationText;
                VoiceNoteStatus = $"Voice note saved. File size: {_voiceNoteInfo.FileSizeText}.";
                OnPropertyChanged(nameof(HasVoiceNote));
                ToggleVoicePlaybackCommand.ChangeCanExecute();
            }
            catch (VoiceNoteException ex)
            {
                _recordingTimer?.Stop();
                IsRecording = false;
                VoiceNoteStatus = ex.Message;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                _recordingTimer?.Stop();
                IsRecording = false;
                VoiceNoteStatus = "Could not save the voice note. Please try again.";
            }
        }

        public async Task StopRecordingIfNeededAsync()
        {
            StopVoicePlaybackIfNeeded();

            if (!IsRecording)
                return;

            await StopVoiceRecordingAsync();
        }

        private async Task ToggleVoicePlaybackAsync()
        {
            if (_voiceNoteInfo == null || string.IsNullOrWhiteSpace(_voiceNoteInfo.FilePath))
            {
                VoiceNoteStatus = "Record a voice note before playing it.";
                return;
            }

            if (IsPlayingVoiceNote)
            {
                StopVoicePlaybackIfNeeded("Voice note playback stopped.");
                return;
            }

            try
            {
                await _voiceNotePlaybackService.PlayAsync(_voiceNoteInfo.FilePath);
                IsPlayingVoiceNote = true;
                VoiceNoteStatus = "Playing voice note...";
            }
            catch (VoiceNotePlaybackException ex)
            {
                IsPlayingVoiceNote = false;
                VoiceNoteStatus = ex.Message;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                IsPlayingVoiceNote = false;
                VoiceNoteStatus = "Could not play this voice note.";
            }
        }

        private void StopVoicePlaybackIfNeeded(string? statusMessage = null)
        {
            if (IsPlayingVoiceNote || _voiceNotePlaybackService.IsPlaying)
                _voiceNotePlaybackService.Stop();

            IsPlayingVoiceNote = false;

            if (!string.IsNullOrWhiteSpace(statusMessage))
                VoiceNoteStatus = statusMessage;
        }

        private void OnVoicePlaybackCompleted(object? sender, string filePath)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsPlayingVoiceNote = false;
                VoiceNoteStatus = "Voice note playback finished.";
            });
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

        private void SetStatus(string message, bool isValidationError)
        {
            StatusMessage = message;
            HasValidationError = isValidationError;
        }

        private void ClearValidationErrorIfReady()
        {
            if (!HasValidationError)
                return;

            if (!string.IsNullOrWhiteSpace(SelectedMealType) &&
                !string.IsNullOrWhiteSpace(SelectedPortion))
            {
                SetStatus("Meal details are ready to save.", false);
            }
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
