using FoodVisionMauiDemo.ViewModels;

namespace FoodVisionMauiDemo.Views
{
    public partial class FoodKnowledgePage : ContentPage, IQueryAttributable
    {
        private FoodKnowledgeViewModel ViewModel => (FoodKnowledgeViewModel)BindingContext;

        public FoodKnowledgePage()
        {
            InitializeComponent();
            BindingContext = new FoodKnowledgeViewModel();
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            ViewModel.ApplyQueryAttributes(query);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await ViewModel.LoadAsync();
        }
    }
}
