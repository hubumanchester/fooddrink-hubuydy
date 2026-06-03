using FoodVisionMauiDemo.ViewModels;

namespace FoodVisionMauiDemo.Views
{
    public partial class SaveMealPage : ContentPage, IQueryAttributable
    {
        private SaveMealViewModel ViewModel => (SaveMealViewModel)BindingContext;

        public SaveMealPage()
        {
            InitializeComponent();
            BindingContext = new SaveMealViewModel();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            ViewModel.ApplyQueryAttributes(query);
        }

        protected override async void OnDisappearing()
        {
            await ViewModel.StopRecordingIfNeededAsync();
            base.OnDisappearing();
        }
    }
}
