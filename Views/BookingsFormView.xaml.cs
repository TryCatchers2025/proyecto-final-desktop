using System.Windows;
using System.Windows.Controls;
using PereMaria.GestorHotel.Controllers;
using PereMaria.GestorHotel.Services;

namespace PereMaria.GestorHotel.Views;

public partial class BookingsFormView : UserControl
{
    public BookingsFormView()
    {
        InitializeComponent();
    }

    private void OnAddClick(object sender, RoutedEventArgs e)
    {
        if (!BookingsController.Instance.CanAdd())
        {
            MessageBox.Show("El nombre debe tener al menos 5 caracteres");
            return;
        }

        _ = BookingsController.Instance.Add().ContinueWith(task =>
        {
            if (task.Exception != null)
            {
                // Obtener la excepción real (no el AggregateException)
                var actualException = task.Exception.InnerException ?? task.Exception;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ApiClientService.ShowError(actualException, "Error al añadir la reserva");
                });
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show("Reserva añadida con éxito");
            });
        });
    }

    private void OnBackClick(object sender, RoutedEventArgs e)
    {
        NavigationService.Instance.NavigateTo<BookingsView>();
    }
}