using FoodVisionMauiDemo.ViewModels;

namespace FoodVisionMauiDemo.Views
{
    public partial class NearbyFoodPage : ContentPage, IQueryAttributable
    {
        private NearbyFoodViewModel ViewModel => (NearbyFoodViewModel)BindingContext;

        public NearbyFoodPage()
        {
            InitializeComponent();
            BindingContext = new NearbyFoodViewModel();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            ViewModel.ApplyQueryAttributes(query);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ViewModel.LoadPlacesAsync();
        }
    }
}
