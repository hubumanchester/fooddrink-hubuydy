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

        private async void OnViewInsightsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//InsightsPage");
        }
    }
}
