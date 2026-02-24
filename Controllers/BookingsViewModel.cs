using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using PereMaria.GestorHotel.Commands;
using PereMaria.GestorHotel.Models;
using PereMaria.GestorHotel.Services;
using PereMaria.GestorHotel.Views;

namespace PereMaria.GestorHotel.Controllers;

/// <summary>
/// ViewModel principal para gestionar la tabla y formularios de reservas.
/// Maneja filtrado, ordenacion, paginacion y operaciones CRUD contra la API.
/// </summary>
public class BookingsViewModel : BaseViewModel
{
    /// <summary>
    /// Representa una opcion de ordenacion disponible en la interfaz.
    /// </summary>
    public sealed class SortOption
    {
        public string Key { get; init; } = "";
        public string Label { get; init; } = "";
    }

    // Singleton
    private static BookingsViewModel? _instance;
    public static BookingsViewModel Instance => _instance ??= new BookingsViewModel();

    private readonly SessionService _session = SessionService.Instance;

    private BookingsViewModel()
    {
        _currentBooking = new BookingModel();
        InitializeFormState(_currentBooking);

        _session.SessionChanged += () =>
        {
            OnPropertyChanged(nameof(IsAdmin));
            OnPropertyChanged(nameof(CanDeleteBooking));
        };

        FilteredCustomers = CollectionViewSource.GetDefaultView(Customers);
        FilteredCustomers.Filter = FilterCustomers;

        FilteredRooms = CollectionViewSource.GetDefaultView(Rooms);
        FilteredRooms.Filter = FilterRooms;

        SortOptions =
        [
            new SortOption { Key = "guest", Label = "Huesped" },
            new SortOption { Key = "room", Label = "Habitacion" },
            new SortOption { Key = "startDate", Label = "Entrada" },
            new SortOption { Key = "endDate", Label = "Salida" },
            new SortOption { Key = "totalPrice", Label = "Total" },
            new SortOption { Key = "payment", Label = "Pago" },
            new SortOption { Key = "status", Label = "Estado" },
        ];
        SelectedSortKey = "startDate";
        SelectedSortDirection = "desc";

        _ = LoadBookings();
        _ = LoadCustomers();
        _ = LoadRooms();
    }

    // La lista de todos los Bookings
    public ObservableCollection<BookingModel> Bookings { get; } = [];
    public ObservableCollection<BookingModel> PagedBookings { get; } = [];

    private DateTime? _filterStartDate;
    private DateTime? _filterEndDate;
    private string _dateFilterError = string.Empty;

    private string _tableSearchText = "";

    public string TableSearchText
    {
        get => _tableSearchText;
        set
        {
            var normalized = value ?? string.Empty;
            if (normalized == _tableSearchText) return;
            _tableSearchText = normalized;
            OnPropertyChanged(nameof(TableSearchText));
            CurrentPage = 1;
            UpdatePaging();
        }
    }

    public DateTime? FilterStartDate
    {
        get => _filterStartDate;
        set
        {
            if (Nullable.Equals(value, _filterStartDate)) return;
            _filterStartDate = value;
            OnPropertyChanged(nameof(FilterStartDate));
            CurrentPage = 1;
            UpdatePaging();
        }
    }

    public DateTime? FilterEndDate
    {
        get => _filterEndDate;
        set
        {
            if (Nullable.Equals(value, _filterEndDate)) return;
            _filterEndDate = value;
            OnPropertyChanged(nameof(FilterEndDate));
            CurrentPage = 1;
            UpdatePaging();
        }
    }

    public string DateFilterError
    {
        get => _dateFilterError;
        private set
        {
            if (string.Equals(value, _dateFilterError, StringComparison.Ordinal)) return;
            _dateFilterError = value;
            OnPropertyChanged(nameof(DateFilterError));
            OnPropertyChanged(nameof(HasDateFilterError));
        }
    }

    public bool HasDateFilterError => !string.IsNullOrWhiteSpace(DateFilterError);

    public ObservableCollection<SortOption> SortOptions { get; }

    private string _selectedSortKey = "";

    public string SelectedSortKey
    {
        get => _selectedSortKey;
        set
        {
            if (value == _selectedSortKey) return;
            _selectedSortKey = value;
            OnPropertyChanged(nameof(SelectedSortKey));
            UpdatePaging();
        }
    }

    public ObservableCollection<SortOption> SortDirectionOptions { get; } =
    [
        new SortOption { Key = "desc", Label = "Desc" },
        new SortOption { Key = "asc", Label = "Asc" },
    ];

    private string _selectedSortDirection = "desc";

    public string SelectedSortDirection
    {
        get => _selectedSortDirection;
        set
        {
            if (value == _selectedSortDirection) return;
            _selectedSortDirection = value;
            OnPropertyChanged(nameof(SelectedSortDirection));
            UpdatePaging();
        }
    }

    public ObservableCollection<int> PageSizeOptions { get; } = [5, 10, 20, 50, 100];

    private int _selectedPageSize = 10;

    public int SelectedPageSize
    {
        get => _selectedPageSize;
        set
        {
            if (value == _selectedPageSize) return;
            _selectedPageSize = value;
            OnPropertyChanged(nameof(SelectedPageSize));
            CurrentPage = 1;
            UpdatePaging();
        }
    }

    private int _currentPage = 1;

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (value == _currentPage) return;
            _currentPage = value;
            OnPropertyChanged(nameof(CurrentPage));
            OnPropertyChanged(nameof(PageSummary));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    private int _totalPages = 1;

    public int TotalPages
    {
        get => _totalPages;
        private set
        {
            if (value == _totalPages) return;
            _totalPages = value;
            OnPropertyChanged(nameof(TotalPages));
            OnPropertyChanged(nameof(PageSummary));
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string PageSummary => $"Pagina {CurrentPage} de {TotalPages}";

    public ObservableCollection<CustomerModel> Customers { get; } = [];
    public ObservableCollection<RoomModel> Rooms { get; } = [];

    public ICollectionView FilteredCustomers { get; }
    public ICollectionView FilteredRooms { get; }

    private bool _isCustomerPopupOpen;

    public bool IsCustomerPopupOpen
    {
        get => _isCustomerPopupOpen;
        set
        {
            if (value == _isCustomerPopupOpen) return;
            _isCustomerPopupOpen = value;
            OnPropertyChanged(nameof(IsCustomerPopupOpen));
        }
    }

    private bool _isRoomPopupOpen;

    public bool IsRoomPopupOpen
    {
        get => _isRoomPopupOpen;
        set
        {
            if (value == _isRoomPopupOpen) return;
            _isRoomPopupOpen = value;
            OnPropertyChanged(nameof(IsRoomPopupOpen));
        }
    }

    private string _customerSearchText = "";

    public string CustomerSearchText
    {
        get => _customerSearchText;
        set
        {
            var normalized = value ?? string.Empty;
            if (normalized == _customerSearchText) return;
            _customerSearchText = normalized;
            OnPropertyChanged(nameof(CustomerSearchText));
            FilteredCustomers.Refresh();
        }
    }

    private string _roomSearchText = "";
    private bool _suppressRoomSearchClear;
    private bool _isFormInitializing;

    public string RoomSearchText
    {
        get => _roomSearchText;
        set
        {
            var normalized = value ?? string.Empty;
            if (normalized == _roomSearchText) return;
            _roomSearchText = normalized;
            OnPropertyChanged(nameof(RoomSearchText));
            if (_suppressRoomSearchClear)
            {
                FilteredRooms.Refresh();
                return;
            }

            if (_selectedRoom != null && !string.IsNullOrWhiteSpace(_roomSearchText))
            {
                var name = _selectedRoom.Name ?? string.Empty;
                if (!name.StartsWith(_roomSearchText, StringComparison.OrdinalIgnoreCase))
                {
                    SelectedRoom = null;
                }
            }

            FilteredRooms.Refresh();
        }
    }

    private CustomerModel? _selectedCustomer;

    public CustomerModel? SelectedCustomer
    {
        get => _selectedCustomer;
        set
        {
            if (Equals(value, _selectedCustomer)) return;
            _selectedCustomer = value;
            OnPropertyChanged(nameof(SelectedCustomer));

            CurrentBooking.UserId = _selectedCustomer?.UserId ?? "";
            CustomerSearchText = _selectedCustomer?.Dni ?? "";
            ValidateBooking(false);
        }
    }

    private RoomModel? _selectedRoom;

    public RoomModel? SelectedRoom
    {
        get => _selectedRoom;
        set
        {
            if (Equals(value, _selectedRoom)) return;
            if (value == null && _isFormInitializing && !string.IsNullOrWhiteSpace(CurrentBooking.RoomId))
            {
                return;
            }

            _selectedRoom = value;
            OnPropertyChanged(nameof(SelectedRoom));

            CurrentBooking.RoomId = _selectedRoom?.RoomId ?? "";
            if (_selectedRoom != null)
            {
                _suppressRoomSearchClear = true;
                RoomSearchText = _selectedRoom.Name ?? "";
                _suppressRoomSearchClear = false;
            }

            if (IsCreating && _selectedRoom != null && DiscountText == "0")
            {
                DiscountText = _selectedRoom.Offer.ToString();
            }

            RecalculateTotals();
            ValidateBooking(false);
        }
    }

    // El booking que se está editando/creando actualmente
    private BookingModel _currentBooking;

    public BookingModel CurrentBooking
    {
        get => _currentBooking;
        set
        {
            if (value == _currentBooking) return;
            _currentBooking = value;
            OnPropertyChanged(nameof(CurrentBooking));
            OnPropertyChanged(nameof(IsEditing));
            OnPropertyChanged(nameof(IsCreating));
            OnPropertyChanged(nameof(FormTitle));
            OnPropertyChanged(nameof(IsBookingCanceled));
            OnPropertyChanged(nameof(IsFormEnabled));
            OnPropertyChanged(nameof(IsPaymentPending));
            OnPropertyChanged(nameof(CanDeleteBooking));
            CommandManager.InvalidateRequerySuggested();
            _isFormInitializing = true;
            SyncSelectedCustomer();
            SyncSelectedRoom();
            InitializeFormState(_currentBooking);
            _isFormInitializing = false;
        }
    }

    private bool _isLoading;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (value == _isLoading) return;
            _isLoading = value;
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(IsNotLoading));
        }
    }

    public bool IsNotLoading => !IsLoading;

    public string FormTitle => IsEditing ? "Modificar reserva" : "Crear reserva";
    public bool IsEditing => CurrentBooking.BookingId != null;
    public bool IsCreating => CurrentBooking.BookingId == null;

    public bool IsBookingCanceled =>
        string.Equals(CurrentBooking.Status, "canceled", StringComparison.OrdinalIgnoreCase);

    public bool IsFormEnabled => !IsBookingCanceled;

    public bool IsPaymentPending =>
        IsEditing && !IsBookingCanceled && !CurrentBooking.IsPaid;

    public bool IsAdmin =>
        string.Equals(_session.CurrentUser?.Role, "admin", StringComparison.OrdinalIgnoreCase);

    public bool CanDeleteBooking =>
        IsEditing && IsBookingCanceled && IsAdmin;

    private const string DateFormat = "dd/MM/yyyy";

    private string _startDateText = DateTime.Now.ToString(DateFormat);

    public string StartDateText
    {
        get => _startDateText;
        set
        {
            var normalized = value ?? string.Empty;
            if (normalized == _startDateText) return;
            _startDateText = normalized;
            CurrentBooking.StartDate = normalized;
            OnPropertyChanged(nameof(StartDateText));
            RecalculateTotals();
            ValidateBooking(false);
        }
    }

    private string _endDateText = DateTime.Now.AddDays(1).ToString(DateFormat);

    public string EndDateText
    {
        get => _endDateText;
        set
        {
            var normalized = value ?? string.Empty;
            if (normalized == _endDateText) return;
            _endDateText = normalized;
            CurrentBooking.EndDate = normalized;
            OnPropertyChanged(nameof(EndDateText));
            RecalculateTotals();
            ValidateBooking(false);
        }
    }

    private string _occupantsText = "1";

    public string OccupantsText
    {
        get => _occupantsText;
        set
        {
            var normalized = value ?? string.Empty;
            if (normalized == _occupantsText) return;
            _occupantsText = normalized;
            if (int.TryParse(_occupantsText, out var parsed))
            {
                CurrentBooking.Occupants = parsed;
            }

            OnPropertyChanged(nameof(OccupantsText));
            ValidateBooking(false);
        }
    }

    private string _discountText = "0";

    public string DiscountText
    {
        get => _discountText;
        set
        {
            var normalized = value ?? string.Empty;
            if (normalized == _discountText) return;
            _discountText = normalized;
            if (int.TryParse(_discountText, out var parsed))
            {
                CurrentBooking.Discount = parsed;
            }

            OnPropertyChanged(nameof(DiscountText));
            RecalculateTotals();
            ValidateBooking(false);
        }
    }

    private int _computedTotalNights;

    public int ComputedTotalNights
    {
        get => _computedTotalNights;
        private set
        {
            if (value == _computedTotalNights) return;
            _computedTotalNights = value;
            OnPropertyChanged(nameof(ComputedTotalNights));
        }
    }

    private decimal _computedPricePerNight;

    public decimal ComputedPricePerNight
    {
        get => _computedPricePerNight;
        private set
        {
            if (value == _computedPricePerNight) return;
            _computedPricePerNight = value;
            OnPropertyChanged(nameof(ComputedPricePerNight));
        }
    }

    private decimal _computedTotalPrice;

    public decimal ComputedTotalPrice
    {
        get => _computedTotalPrice;
        private set
        {
            if (value == _computedTotalPrice) return;
            _computedTotalPrice = value;
            OnPropertyChanged(nameof(ComputedTotalPrice));
        }
    }

    private string _customerError = string.Empty;

    public string CustomerError
    {
        get => _customerError;
        private set
        {
            if (value == _customerError) return;
            _customerError = value;
            OnPropertyChanged(nameof(CustomerError));
        }
    }

    private string _roomError = string.Empty;

    public string RoomError
    {
        get => _roomError;
        private set
        {
            if (value == _roomError) return;
            _roomError = value;
            OnPropertyChanged(nameof(RoomError));
        }
    }

    private string _startDateError = string.Empty;

    public string StartDateError
    {
        get => _startDateError;
        private set
        {
            if (value == _startDateError) return;
            _startDateError = value;
            OnPropertyChanged(nameof(StartDateError));
        }
    }

    private string _endDateError = string.Empty;

    public string EndDateError
    {
        get => _endDateError;
        private set
        {
            if (value == _endDateError) return;
            _endDateError = value;
            OnPropertyChanged(nameof(EndDateError));
        }
    }

    private string _occupantsError = string.Empty;

    public string OccupantsError
    {
        get => _occupantsError;
        private set
        {
            if (value == _occupantsError) return;
            _occupantsError = value;
            OnPropertyChanged(nameof(OccupantsError));
        }
    }

    private string _discountError = string.Empty;

    public string DiscountError
    {
        get => _discountError;
        private set
        {
            if (value == _discountError) return;
            _discountError = value;
            OnPropertyChanged(nameof(DiscountError));
        }
    }

    private string _dateConflictMessage = string.Empty;

    public string DateConflictMessage
    {
        get => _dateConflictMessage;
        private set
        {
            if (value == _dateConflictMessage) return;
            _dateConflictMessage = value;
            OnPropertyChanged(nameof(DateConflictMessage));
        }
    }

    /// <summary>
    /// Carga reservas desde backend, reinicia paginación y refresca listado visible.
    /// </summary>
    private async Task LoadBookings()
    {
        try
        {
            IsLoading = true;
            Bookings.Clear();

            var res = await BookingsService.Instance.GetBookings();

            res?.ForEach(b => Bookings.Add(b));
            CurrentPage = 1;
            UpdatePaging();
        }
        catch (Exception e)
        {
            if (e.Message.Contains("Ya has pagado", StringComparison.OrdinalIgnoreCase))
            {
                ShowMessageBox(e.Message, "Pago", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            ShowMessageBox(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Obtiene habitaciones disponibles para poblar selectores del formulario de reserva.
    /// </summary>
    private async Task LoadRooms()
    {
        try
        {
            var res = await new RoomService().GetAllRooms();

            if (!res.Success || res.Data == null)
            {
                var message = res.Error?.Message ?? "No se pudieron cargar las habitaciones";
                throw new Exception(message);
            }

            Rooms.Clear();
            res.Data.ForEach(r => Rooms.Add(r));

            FilteredRooms.Refresh();
            SyncSelectedRoom();
            RecalculateTotals();
        }
        catch (Exception e)
        {
            ShowMessageBox(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Obtiene clientes para el formulario y sincroniza la selección actual.
    /// </summary>
    private async Task LoadCustomers()
    {
        try
        {
            var res = await new UserService().GetAllCustomers();

            if (!res.Success || res.Data == null)
            {
                var message = res.Error?.Message ?? "No se pudieron cargar los clientes";
                throw new Exception(message);
            }

            Customers.Clear();
            res.Data.ForEach(c => Customers.Add(c));

            FilteredCustomers.Refresh();
            SyncSelectedCustomer();
        }
        catch (Exception e)
        {
            ShowMessageBox(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Sincroniza el cliente seleccionado con el estado de la reserva activa.
    /// </summary>
    private void SyncSelectedCustomer()
    {
        if (string.IsNullOrWhiteSpace(CurrentBooking.UserId))
        {
            SelectedCustomer = null;
            CustomerSearchText = "";
            return;
        }

        var match = Customers.FirstOrDefault(c => c.UserId == CurrentBooking.UserId);
        if (!Equals(match, SelectedCustomer))
        {
            SelectedCustomer = match;
        }

        var displayDni = match?.Dni ?? CurrentBooking.Customer?.Dni ?? CurrentBooking.UserId;
        CustomerSearchText = displayDni ?? "";
    }

    /// <summary>
    /// Sincroniza la habitación seleccionada con el estado de la reserva activa.
    /// </summary>
    private void SyncSelectedRoom()
    {
        if (string.IsNullOrWhiteSpace(CurrentBooking.RoomId))
        {
            SelectedRoom = null;
            _suppressRoomSearchClear = true;
            RoomSearchText = "";
            _suppressRoomSearchClear = false;
            return;
        }

        var match = Rooms.FirstOrDefault(r => r.RoomId == CurrentBooking.RoomId);
        if (match == null && CurrentBooking.Room != null)
        {
            var exists = Rooms.Any(r => r.RoomId == CurrentBooking.Room.RoomId);
            if (!exists)
            {
                Rooms.Add(CurrentBooking.Room);
            }

            match = CurrentBooking.Room;
        }

        if (!Equals(match, SelectedRoom))
        {
            SelectedRoom = match;
        }

        var displayName = match?.Name ?? CurrentBooking.Room?.Name ?? CurrentBooking.RoomId;
        _suppressRoomSearchClear = true;
        RoomSearchText = displayName ?? "";
        _suppressRoomSearchClear = false;
    }

    /// <summary>
    /// Filtro incremental para autocompletado de clientes por DNI.
    /// </summary>
    private bool FilterCustomers(object item)
    {
        if (item is not CustomerModel customer) return false;
        var searchText = CustomerSearchText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(searchText)) return true;

        return customer.Dni?.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Filtro incremental para autocompletado de habitaciones por nombre.
    /// </summary>
    private bool FilterRooms(object item)
    {
        if (item is not RoomModel room) return false;
        var searchText = RoomSearchText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(searchText)) return true;

        return room.Name?.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <summary>
    /// Abre el formulario de reservas en modo creación o edición.
    /// </summary>
    public RelayCommand NavigateFormCommand => new(parameter =>
    {
        NavigationViewModel.Instance.NavigateTo<BookingsFormView>();
        CurrentBooking = parameter as BookingModel ?? new BookingModel();
    });

    /// <summary>
    /// Valida y persiste la reserva actual (creación o actualización).
    /// </summary>
    public RelayCommand SaveBookingCommand => new(async void (_) =>
    {
        try
        {
            if (!ValidateBooking(true)) return;
            IsLoading = true;
            BookingModel? savedBooking = null;

            if (IsCreating)
            {
                savedBooking = await BookingsService.Instance.CreateBooking(CurrentBooking);
                if (savedBooking != null)
                {
                    ShowMessageBox("Reserva creada exitosamente", "Éxito", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
            else
            {
                // Actualizar reserva existente
                savedBooking =
                    await BookingsService.Instance.UpdateBooking(CurrentBooking.BookingId!, CurrentBooking);
                if (savedBooking != null)
                {
                    ShowMessageBox("Reserva actualizada exitosamente", "Éxito", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }

            await LoadBookings();
            if (savedBooking?.BookingId != null)
            {
                CurrentBooking = Bookings.ToList().Find(b => b.BookingId == savedBooking.BookingId) ??
                                 savedBooking;
            }
            else
            {
                CurrentBooking = new BookingModel();
            }
        }
        catch (Exception e)
        {
            ShowMessageBox(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    });

    public RelayCommand CancelCommand => NavigationViewModel.Instance.BackCommand;

    /// <summary>
    /// Cancela una reserva activa previa confirmación del usuario.
    /// </summary>
    public RelayCommand CancelBookingCommand => new(async void (_) =>
    {
        var result = ShowMessageBox(
            "¿Está seguro de que desea cancelar esta reserva?",
            "Confirmar cancelación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            IsLoading = true;
            var success = await BookingsService.Instance.CancelBooking(CurrentBooking.BookingId!);
            if (success)
            {
                ShowMessageBox("Reserva cancelada exitosamente", "Éxito", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                CurrentBooking.Status = "canceled";
                OnPropertyChanged(nameof(CurrentBooking));
                OnPropertyChanged(nameof(IsBookingCanceled));
                OnPropertyChanged(nameof(IsFormEnabled));
                OnPropertyChanged(nameof(IsPaymentPending));
                OnPropertyChanged(nameof(CanDeleteBooking));
                CommandManager.InvalidateRequerySuggested();
            }
        }
        catch (Exception e)
        {
            ShowMessageBox(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }, _ => IsEditing && CurrentBooking.Status != "canceled");

    /// <summary>
    /// Registra el pago de la reserva actual tras confirmar la operación.
    /// </summary>
    public RelayCommand PayBookingCommand => new(async void (_) =>
    {
        var result = ShowMessageBox(
            "¿Confirmar pago de esta reserva?",
            "Confirmar pago",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            IsLoading = true;
            var updated = await BookingsService.Instance.PayBooking(CurrentBooking.BookingId!);
            if (updated?.IsPaid == true)
            {
                CurrentBooking.IsPaid = true;
                ShowMessageBox("Pago registrado correctamente", "Éxito", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                OnPropertyChanged(nameof(CurrentBooking));
                OnPropertyChanged(nameof(IsPaymentPending));
                CommandManager.InvalidateRequerySuggested();
            }
        }
        catch (Exception e)
        {
            ShowMessageBox(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }, _ => IsPaymentPending);

    /// <summary>
    /// Elimina de forma permanente una reserva cancelada.
    /// </summary>
    public RelayCommand DeleteBookingCommand => new(async void (_) =>
    {
        var result = ShowMessageBox(
            "¿Eliminar esta reserva de forma permanente?",
            "Eliminar reserva",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            IsLoading = true;
            var success = await BookingsService.Instance.DeleteBooking(CurrentBooking.BookingId!);
            if (success)
            {
                ShowMessageBox("Reserva eliminada correctamente", "Éxito", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                await LoadBookings();
                CurrentBooking = new BookingModel();
                NavigationViewModel.Instance.BackCommand.Execute(null);
            }
        }
        catch (Exception e)
        {
            ShowMessageBox(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }, _ => CanDeleteBooking);

    /// <summary>
    /// Recarga la colección completa de reservas.
    /// </summary>
    public RelayCommand ReloadCommand => new(async void (_) => await LoadBookings());

    /// <summary>
    /// Limpia filtros de rango de fechas aplicados sobre la tabla de reservas.
    /// </summary>
    public RelayCommand ClearDateFiltersCommand => new(_ =>
    {
        FilterStartDate = null;
        FilterEndDate = null;
    });

    /// <summary>
    /// Establece el criterio de ordenación activo de la tabla.
    /// </summary>
    public RelayCommand SetSortCommand => new(parameter =>
    {
        if (parameter is not string key) return;
        SelectedSortKey = key;
    });

    /// <summary>
    /// Avanza a la siguiente página de resultados si existe.
    /// </summary>
    public RelayCommand NextPageCommand => new(_ =>
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            UpdatePaging();
        }
    }, _ => CurrentPage < TotalPages);

    /// <summary>
    /// Retrocede a la página anterior de resultados si existe.
    /// </summary>
    public RelayCommand PreviousPageCommand => new(_ =>
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            UpdatePaging();
        }
    }, _ => CurrentPage > 1);

    /// <summary>
    /// Recalcula el conjunto paginado aplicando filtros y ordenación actuales.
    /// </summary>
    private void UpdatePaging()
    {
        var filteredList = ApplyTableSearch(Bookings).ToList();

        if (HasDateFilterError)
        {
            PagedBookings.Clear();
            TotalPages = 1;
            CurrentPage = 1;
            OnPropertyChanged(nameof(PageSummary));
            return;
        }

        var totalItems = filteredList.Count;
        TotalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)SelectedPageSize));
        if (CurrentPage > TotalPages) CurrentPage = TotalPages;
        if (CurrentPage < 1) CurrentPage = 1;

        PagedBookings.Clear();
        var sorted = ApplySorting(filteredList);
        var pageItems = sorted
            .Skip((CurrentPage - 1) * SelectedPageSize)
            .Take(SelectedPageSize);

        foreach (var booking in pageItems)
        {
            PagedBookings.Add(booking);
        }

        OnPropertyChanged(nameof(PageSummary));
    }

    /// <summary>
    /// Aplica la dirección y clave de ordenación seleccionadas sobre una colección.
    /// </summary>
    private IEnumerable<BookingModel> ApplySorting(IEnumerable<BookingModel> source)
    {
        return SelectedSortDirection == "desc"
            ? source.OrderByDescending(GetSortValue)
            : source.OrderBy(GetSortValue);
    }

    /// <summary>
    /// Filtra reservas por texto libre tras aplicar filtros de fecha.
    /// </summary>
    private IEnumerable<BookingModel> ApplyTableSearch(IEnumerable<BookingModel> source)
    {
        var filtered = ApplyDateFilter(source);
        if (HasDateFilterError)
        {
            return Enumerable.Empty<BookingModel>();
        }

        var query = (TableSearchText ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(query)) return filtered;

        var normalized = query.ToLowerInvariant();
        return filtered.Where(booking => BuildSearchText(booking).Contains(normalized));
    }

    /// <summary>
    /// Aplica filtro de rango de fechas de entrada/salida sobre reservas.
    /// </summary>
    private IEnumerable<BookingModel> ApplyDateFilter(IEnumerable<BookingModel> source)
    {
        if (FilterStartDate.HasValue && FilterEndDate.HasValue && FilterStartDate > FilterEndDate)
        {
            DateFilterError = "La fecha inicial no puede ser posterior a la final";
            return Enumerable.Empty<BookingModel>();
        }

        DateFilterError = string.Empty;

        if (FilterStartDate.HasValue)
        {
            var start = FilterStartDate.Value.Date;
            source = source.Where(booking => ParseSortDate(booking.StartDate) >= start);
        }

        if (FilterEndDate.HasValue)
        {
            var end = FilterEndDate.Value.Date;
            source = source.Where(booking => ParseSortDate(booking.EndDate) <= end);
        }

        return source;
    }

    /// <summary>
    /// Construye un texto normalizado con campos relevantes para búsqueda full-text.
    /// </summary>
    private string BuildSearchText(BookingModel booking)
    {
        var guest = $"{booking.Customer?.FirstName ?? ""} {booking.Customer?.LastName ?? ""}";
        var dni = booking.Customer?.Dni ?? string.Empty;
        var room = booking.Room?.Name ?? string.Empty;
        var total = booking.TotalPrice.ToString("0.00", CultureInfo.InvariantCulture);
        var status = booking.Status ?? string.Empty;
        var vip = booking.Customer?.Vip == true ? "vip" : string.Empty;
        var payment = booking.IsPaid ? "pagada" : "pago pendiente";

        var parts = new[]
        {
            guest,
            dni,
            room,
            booking.StartDate,
            booking.EndDate,
            booking.BookingDate,
            total,
            payment,
            status,
            vip,
        };

        return string.Join(" ", parts).ToLowerInvariant();
    }

    /// <summary>
    /// Obtiene la clave de ordenación de una reserva según la columna activa.
    /// </summary>
    private object GetSortValue(BookingModel booking)
    {
        return SelectedSortKey switch
        {
            "guest" => $"{booking.Customer?.FirstName ?? ""} {booking.Customer?.LastName ?? ""}",
            "room" => booking.Room?.Name ?? "",
            "startDate" => ParseSortDate(booking.StartDate),
            "endDate" => ParseSortDate(booking.EndDate),
            "totalPrice" => booking.TotalPrice,
            "payment" => booking.IsPaid ? 1 : 0,
            "status" => booking.Status ?? "",
            _ => ParseSortDate(booking.StartDate),
        };
    }

    /// <summary>
    /// Convierte texto de fecha de reserva a DateTime para ordenar de forma estable.
    /// </summary>
    private DateTime ParseSortDate(string? dateText)
    {
        if (dateText == null) return DateTime.MinValue;
        return DateTime.TryParseExact(dateText, DateFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var parsed)
            ? parsed
            : DateTime.MinValue;
    }

    /// <summary>
    /// Inicializa campos del formulario con defaults y estado de la reserva actual.
    /// </summary>
    private void InitializeFormState(BookingModel booking)
    {
        if (string.IsNullOrWhiteSpace(booking.RoomId) && SelectedRoom != null)
        {
            booking.RoomId = SelectedRoom.RoomId;
        }

        StartDateText = string.IsNullOrWhiteSpace(booking.StartDate)
            ? DateTime.Now.ToString(DateFormat)
            : booking.StartDate;
        EndDateText = string.IsNullOrWhiteSpace(booking.EndDate)
            ? DateTime.Now.AddDays(1).ToString(DateFormat)
            : booking.EndDate;
        if (booking.Occupants <= 0)
        {
            booking.Occupants = 1;
        }

        OccupantsText = booking.Occupants.ToString();
        DiscountText = booking.Discount > 0 ? booking.Discount.ToString() : "0";
        RecalculateTotals();
        ClearValidationErrors();
    }

    /// <summary>
    /// Recalcula noches, precio por noche y total según fechas, habitación y descuento.
    /// </summary>
    private void RecalculateTotals()
    {
        ComputedTotalNights = 0;
        ComputedPricePerNight = 0;
        ComputedTotalPrice = 0;

        if (!TryParseDate(StartDateText, out var start) || !TryParseDate(EndDateText, out var end))
        {
            return;
        }

        if (end <= start)
        {
            return;
        }

        if (SelectedRoom == null)
        {
            return;
        }

        var nights = (int)Math.Ceiling((end - start).TotalDays);
        if (nights <= 0) return;

        var basePrice = Convert.ToDecimal(SelectedRoom.PricePerNight);
        var discount = ParseDiscount();
        var discountToApply = discount > 0 ? discount : 0;
        var pricePerNight = basePrice * (1 - (discountToApply / 100m));

        ComputedTotalNights = nights;
        ComputedPricePerNight = Math.Round(pricePerNight, 2);
        ComputedTotalPrice = Math.Round(pricePerNight * nights, 2);
    }

    /// <summary>
    /// Parsea el descuento textual y devuelve 0 cuando no es válido.
    /// </summary>
    private int ParseDiscount()
    {
        return int.TryParse(DiscountText, out var discount) ? discount : 0;
    }

    /// <summary>
    /// Intenta parsear una fecha con formato DD/MM/YYYY.
    /// </summary>
    private bool TryParseDate(string text, out DateTime date)
    {
        return DateTime.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out date);
    }

    /// <summary>
    /// Limpia mensajes de validación del formulario de reservas.
    /// </summary>
    private void ClearValidationErrors()
    {
        CustomerError = string.Empty;
        RoomError = string.Empty;
        StartDateError = string.Empty;
        EndDateError = string.Empty;
        OccupantsError = string.Empty;
        DiscountError = string.Empty;
        DateConflictMessage = string.Empty;
    }

    /// <summary>
    /// Ejecuta validaciones de negocio del formulario antes de persistir la reserva.
    /// </summary>
    private bool ValidateBooking(bool showSummary)
    {
        ClearValidationErrors();
        var errors = new List<string>();

        if (SelectedCustomer == null || string.IsNullOrWhiteSpace(CurrentBooking.UserId))
        {
            CustomerError = "Selecciona un cliente.";
            errors.Add(CustomerError);
        }

        if (SelectedRoom == null || string.IsNullOrWhiteSpace(CurrentBooking.RoomId))
        {
            RoomError = "Selecciona una habitación.";
            errors.Add(RoomError);
        }

        if (!TryParseDate(StartDateText, out var startDate))
        {
            StartDateError = "Fecha de entrada inválida.";
            errors.Add(StartDateError);
        }

        if (!TryParseDate(EndDateText, out var endDate))
        {
            EndDateError = "Fecha de salida inválida.";
            errors.Add(EndDateError);
        }

        if (StartDateError == string.Empty && EndDateError == string.Empty)
        {
            if (endDate <= startDate)
            {
                EndDateError = "La salida debe ser posterior a la entrada.";
                errors.Add(EndDateError);
            }

            if (IsCreating)
            {
                var today = DateTime.Today;
                if (startDate < today)
                {
                    StartDateError = "No se pueden crear reservas en el pasado.";
                    errors.Add(StartDateError);
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(StartDateError) || !string.IsNullOrWhiteSpace(EndDateError))
        {
            DateConflictMessage = "Conflicto de fechas: revisa entrada y salida.";
        }

        if (!int.TryParse(OccupantsText, out var occupants) || occupants <= 0)
        {
            OccupantsError = "Indica un numero de huespedes valido.";
            errors.Add(OccupantsError);
        }

        if (SelectedRoom != null && occupants > 0 && occupants > SelectedRoom.OccupancyLimit)
        {
            OccupantsError = $"La habitacion permite maximo {SelectedRoom.OccupancyLimit} huespedes.";
            errors.Add(OccupantsError);
        }

        if (!string.IsNullOrWhiteSpace(DiscountText))
        {
            if (!int.TryParse(DiscountText, out var discount) || discount < 0 || discount > 100)
            {
                DiscountError = "El descuento debe estar entre 0 y 100.";
                errors.Add(DiscountError);
            }
        }

        if (errors.Count == 0) return true;

        if (showSummary)
        {
            var summary = string.Join("\n", errors.Distinct());
            ShowMessageBox(summary, "Revisa el formulario", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        return false;
    }
}