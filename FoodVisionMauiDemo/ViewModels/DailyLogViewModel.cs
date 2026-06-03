using System.Collections.ObjectModel;
using System.Diagnostics;
using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.Repositories;
using FoodVisionMauiDemo.Services;
using Microsoft.Maui.ApplicationModel;

namespace FoodVisionMauiDemo.ViewModels
{
    public class DailyLogViewModel : BaseViewModel
    {
        private readonly MealLogRepository _mealLogRepository;
        private readonly VoiceNotePlaybackService _voiceNotePlaybackService;
        private MealLogItem? _playingVoiceNoteMeal;
        private string _statusMessage = "Loading meals...";
        private string _todayRiskSummary = "No meals logged today.";
        private bool _isBusy;

        public DailyLogViewModel()
            : this(new MealLogRepository(), new VoiceNotePlaybackService())
        {
        }

        public DailyLogViewModel(
            MealLogRepository mealLogRepository,
            VoiceNotePlaybackService voiceNotePlaybackService)
        {
            _mealLogRepository = mealLogRepository;
            _voiceNotePlaybackService = voiceNotePlaybackService;
            _voiceNotePlaybackService.PlaybackCompleted += OnVoicePlaybackCompleted;
        }

        public ObservableCollection<MealLogItem> TodayMeals { get; } = new();

        public ObservableCollection<MealLogItem> RecentMeals { get; } = new();

        public string DatabasePath => _mealLogRepository.DatabasePath;

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public string TodayRiskSummary
        {
            get => _todayRiskSummary;
            private set => SetProperty(ref _todayRiskSummary, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set => SetProperty(ref _isBusy, value);
        }

        public bool HasTodayMeals => TodayMeals.Count > 0;

        public bool HasRecentMeals => RecentMeals.Count > 0;

        public bool HasMeals => HasTodayMeals || HasRecentMeals;

        public bool HasNoMeals => !HasMeals;

        public async Task LoadAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;
                StatusMessage = "Loading meals...";
                StopVoiceNotePlayback();

                var todayMeals = await _mealLogRepository.GetTodayMealsAsync();
                var recentMeals = await _mealLogRepository.GetRecentMealsAsync();
                var todayIds = todayMeals.Select(meal => meal.Id).ToHashSet();
                var recentWithoutToday = recentMeals.Where(meal => !todayIds.Contains(meal.Id)).ToList();

                ReplaceCollection(TodayMeals, todayMeals);
                ReplaceCollection(RecentMeals, recentWithoutToday);
                NotifyMealStates();
                TodayRiskSummary = BuildTodayRiskSummary(todayMeals);

                StatusMessage = HasMeals
                    ? "Meal log loaded."
                    : "No meals logged yet. Scan your first food.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Could not load the meal log. Please try again.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task DeleteMealAsync(MealLogItem meal)
        {
            try
            {
                IsBusy = true;
                if (_playingVoiceNoteMeal == meal)
                    StopVoiceNotePlayback();

                await _mealLogRepository.DeleteMealAsync(meal.Id);
                TodayMeals.Remove(meal);
                RecentMeals.Remove(meal);
                NotifyMealStates();
                TodayRiskSummary = BuildTodayRiskSummary(TodayMeals);
                StatusMessage = HasMeals
                    ? "Meal deleted."
                    : "No meals logged yet. Scan your first food.";
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Could not delete this meal. Please try again.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task UpdateMealAsync(MealLogItem meal)
        {
            if (string.IsNullOrWhiteSpace(meal.EditableMealType))
            {
                StatusMessage = "Please choose a meal type before saving changes.";
                return;
            }

            if (string.IsNullOrWhiteSpace(meal.EditablePortion))
            {
                StatusMessage = "Please choose a portion size before saving changes.";
                return;
            }

            try
            {
                IsBusy = true;
                await _mealLogRepository.UpdateMealAsync(
                    meal.Id,
                    meal.EditableMealType,
                    meal.EditablePortion,
                    meal.EditableNotes);

                StatusMessage = "Meal updated.";
                IsBusy = false;
                await LoadAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                StatusMessage = "Could not update this meal. Please try again.";
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task ToggleVoiceNotePlaybackAsync(MealLogItem meal)
        {
            if (!meal.HasVoiceNote)
            {
                StatusMessage = "This meal does not have a saved voice note.";
                return;
            }

            if (_playingVoiceNoteMeal == meal && meal.IsVoiceNotePlaying)
            {
                StopVoiceNotePlayback("Voice note playback stopped.");
                return;
            }

            StopVoiceNotePlayback();

            try
            {
                await _voiceNotePlaybackService.PlayAsync(meal.VoiceNotePath);
                _playingVoiceNoteMeal = meal;
                meal.SetVoiceNotePlaying(true);
                StatusMessage = $"Playing voice note for {meal.FoodName}.";
            }
            catch (VoiceNotePlaybackException ex)
            {
                meal.SetVoiceNotePlaying(false);
                StatusMessage = ex.Message;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                meal.SetVoiceNotePlaying(false);
                StatusMessage = "Could not play this voice note.";
            }
        }

        public void StopVoiceNotePlayback(string? statusMessage = null)
        {
            _voiceNotePlaybackService.Stop();

            if (_playingVoiceNoteMeal != null)
                _playingVoiceNoteMeal.SetVoiceNotePlaying(false);

            _playingVoiceNoteMeal = null;

            if (!string.IsNullOrWhiteSpace(statusMessage))
                StatusMessage = statusMessage;
        }

        private static void ReplaceCollection(ObservableCollection<MealLogItem> collection, IEnumerable<MealLogItem> values)
        {
            collection.Clear();
            foreach (var value in values)
                collection.Add(value);
        }

        private void NotifyMealStates()
        {
            OnPropertyChanged(nameof(HasTodayMeals));
            OnPropertyChanged(nameof(HasRecentMeals));
            OnPropertyChanged(nameof(HasMeals));
            OnPropertyChanged(nameof(HasNoMeals));
        }

        private static string BuildTodayRiskSummary(IEnumerable<MealLogItem> meals)
        {
            var counts = meals
                .SelectMany(meal => meal.RiskTags)
                .Where(RiskLabelFormatter.IsRiskLoadTag)
                .GroupBy(RiskLabelFormatter.ToLoadLabel, StringComparer.OrdinalIgnoreCase)
                .Select(group => $"{group.Key}: {group.Count()} meal{(group.Count() == 1 ? string.Empty : "s")}")
                .ToList();

            return counts.Count > 0
                ? $"Today's load signals: {string.Join(", ", counts)}"
                : "No repeated risk load signals logged today.";
        }

        private void OnVoicePlaybackCompleted(object? sender, string filePath)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (_playingVoiceNoteMeal != null)
                    _playingVoiceNoteMeal.SetVoiceNotePlaying(false);

                _playingVoiceNoteMeal = null;
                StatusMessage = "Voice note playback finished.";
            });
        }
    }
}
