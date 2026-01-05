using System.Windows.Controls;

namespace PereMaria.GestorHotel.Views;

public partial class BookingsView : UserControl
{
    public BookingsView()
    {
        InitializeComponent();
    }

    private void OnNewClick(object sender, System.Windows.RoutedEventArgs e)
    {
        // Navegar al formulario de nueva reserva
        Services.NavigationService.Instance.NavigateTo<BookingsFormView>();
    }
}