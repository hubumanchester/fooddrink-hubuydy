using FoodVisionMauiDemo.ViewModels;

namespace FoodVisionMauiDemo.Views
{
    public partial class DashboardPage : ContentPage
    {
        public DashboardPage()
        {
            InitializeComponent();
            BindingContext = new DashboardViewModel();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ((DashboardViewModel)BindingContext).LoadAsync();
        }
    }
}
