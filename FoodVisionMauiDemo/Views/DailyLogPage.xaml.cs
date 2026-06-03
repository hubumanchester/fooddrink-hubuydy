using FoodVisionMauiDemo.Models;
using FoodVisionMauiDemo.ViewModels;

namespace FoodVisionMauiDemo.Views
{
    public partial class DailyLogPage : ContentPage
    {
        private DailyLogViewModel ViewModel => (DailyLogViewModel)BindingContext;

        public DailyLogPage()
        {
            InitializeComponent();
            BindingContext = new DailyLogViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ViewModel.LoadAsync();
        }

        private async void OnDeleteMealClicked(object sender, EventArgs e)
        {
            if (sender is not Button button || button.BindingContext is not MealLogItem meal)
                return;

            var shouldDelete = await DisplayAlert(
                "Delete meal",
                $"Delete {meal.FoodName} from your meal log?",
                "Delete",
                "Cancel");

            if (shouldDelete)
                await ViewModel.DeleteMealAsync(meal);
        }

        private async void OnUpdateMealClicked(object sender, EventArgs e)
        {
            if (sender is not Button button || button.BindingContext is not MealLogItem meal)
                return;

            await ViewModel.UpdateMealAsync(meal);
        }

        private async void OnVoiceNotePlaybackClicked(object sender, EventArgs e)
        {
            if (sender is not Button button || button.BindingContext is not MealLogItem meal)
                return;

            await ViewModel.ToggleVoiceNotePlaybackAsync(meal);
        }

        private async void OnViewInsightsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//InsightsPage");
        }

        private async void OnScanFirstFoodClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//VisionScanPage");
        }

        protected override void OnDisappearing()
        {
            ViewModel.StopVoiceNotePlayback();
            base.OnDisappearing();
        }
    }
}
