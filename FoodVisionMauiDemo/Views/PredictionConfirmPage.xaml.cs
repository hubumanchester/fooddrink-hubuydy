using FoodVisionMauiDemo.ViewModels;

namespace FoodVisionMauiDemo.Views
{
    public partial class PredictionConfirmPage : ContentPage, IQueryAttributable
    {
        private PredictionConfirmViewModel ViewModel => (PredictionConfirmViewModel)BindingContext;

        public PredictionConfirmPage()
        {
            InitializeComponent();
            BindingContext = new PredictionConfirmViewModel();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            ViewModel.ApplyQueryAttributes(query);
        }
    }
}
