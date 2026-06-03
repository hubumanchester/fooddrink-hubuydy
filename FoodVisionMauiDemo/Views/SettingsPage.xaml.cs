using FoodVisionMauiDemo.ViewModels;

namespace FoodVisionMauiDemo.Views
{
    public partial class SettingsPage : ContentPage
    {
        private SettingsViewModel ViewModel => (SettingsViewModel)BindingContext;

        public SettingsPage()
        {
            InitializeComponent();
            BindingContext = new SettingsViewModel();
        }

        private async void OnClearLocalDataClicked(object sender, EventArgs e)
        {
            var shouldClear = await DisplayAlert(
                "Clear local data",
                "Clear saved meals, local images, and voice notes from this device?",
                "Clear",
                "Cancel");

            if (shouldClear)
                await ViewModel.ClearLocalDataAsync();
        }
    }
}
