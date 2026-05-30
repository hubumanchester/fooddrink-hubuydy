using System.Collections.ObjectModel;
using System.Diagnostics;
using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.Repositories;

namespace FoodVisionMauiDemo.ViewModels
{
    public class DailyLogViewModel : BaseViewModel
    {
        private readonly MealLogRepository _mealLogRepository;
        private string _statusMessage = "Loading meals...";
        private bool _isBusy;

        public DailyLogViewModel()
            : this(new MealLogRepository())
        {
        }

        public DailyLogViewModel(MealLogRepository mealLogRepository)
        {
            _mealLogRepository = mealLogRepository;
        }

        public ObservableCollection<MealLogItem> TodayMeals { get; } = new();

        public ObservableCollection<MealLogItem> RecentMeals { get; } = new();

        public string DatabasePath => _mealLogRepository.DatabasePath;

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
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

                var todayMeals = await _mealLogRepository.GetTodayMealsAsync();
                var recentMeals = await _mealLogRepository.GetRecentMealsAsync();
                var todayIds = todayMeals.Select(meal => meal.Id).ToHashSet();
                var recentWithoutToday = recentMeals.Where(meal => !todayIds.Contains(meal.Id)).ToList();

                ReplaceCollection(TodayMeals, todayMeals);
                ReplaceCollection(RecentMeals, recentWithoutToday);
                NotifyMealStates();

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
                await _mealLogRepository.DeleteMealAsync(meal.Id);
                TodayMeals.Remove(meal);
                RecentMeals.Remove(meal);
                NotifyMealStates();
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
    }
}
