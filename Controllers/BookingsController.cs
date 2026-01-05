using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using PereMaria.GestorHotel.Models;
using PereMaria.GestorHotel.Services;

namespace PereMaria.GestorHotel.Controllers;

public class BookingsController : INotifyPropertyChanged
{
    // Singleton
    private static BookingsController? _instance;
    public static BookingsController Instance => _instance ??= new BookingsController();
    private readonly ApiClientService db = ApiClientService.Instance;

    private BookingsController()
    {
        _currentBooking = new BookingModel();
        // Iniciar la carga de bookings de forma asíncrona sin bloquear
        _ = LoadAllBookingsAsync();
    }

    // La lista de todos los Bookings
    public ObservableCollection<BookingModel> Bookings { get; } = new();

    // El booking que se está editando/creando actualmente
    private BookingModel _currentBooking;
    public BookingModel CurrentBooking
    {
        get => _currentBooking;
        set
        {
            if (value == _currentBooking) return;
            _currentBooking = value;
            _ = LoadAllBookingsAsync();
            OnPropertyChanged(nameof(CurrentBooking));
        }
    }

    // ========== INotifyPropertyChanged ==========
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public Task<List<BookingModel>?> GetAllAsync()
        => db.GetAsync<List<BookingModel>>("/api/bookings");

    public Task<BookingModel?> AddAsync(BookingModel booking)
        => db.PostAsync<BookingModel>("/api/bookings", booking);

    public async Task LoadAllBookingsAsync()
    {
        try
        {
            var bookings = await GetAllAsync();
            if (bookings == null) return;
            Bookings.Clear();
            foreach (var booking in bookings)
            {
                Bookings.Add(booking);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public bool CanAdd()
    {
        return true;
    }

    public async Task Add()
    {
        try
        {
            var addedBooking = await AddAsync(CurrentBooking);
            if (addedBooking == null) return;
            CurrentBooking = addedBooking;

            await LoadAllBookingsAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        OnPropertyChanged(nameof(CurrentBooking));
    }
}
