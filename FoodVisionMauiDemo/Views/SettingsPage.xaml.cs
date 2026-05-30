using FoodVisionMauiDemo.ViewModels;

namespace FoodVisionMauiDemo.Views
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            BindingContext = new SettingsViewModel();
        }
    }
}
