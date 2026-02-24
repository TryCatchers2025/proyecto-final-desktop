using PereMaria.GestorHotel.Commands;
using PereMaria.GestorHotel.Models;
using PereMaria.GestorHotel.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Intrinsics.Arm;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;

namespace PereMaria.GestorHotel.Controllers;

public class RoomsViewModel : BaseViewModel
{
    //Boton Cancelar
    public RelayCommand CancelCommand => NavigationViewModel.Instance.BackCommand;

    // Singleton
    private static RoomsViewModel? _instance;
    public static RoomsViewModel Instance => _instance ??= new RoomsViewModel();

    private RoomsViewModel()
    {
        _currentRoom = new RoomModel();
    }

    private readonly RoomService _roomService = new RoomService();

    // La lista de todas las rooms
    public ObservableCollection<RoomModel> Rooms { get; } = new();

    public async Task LoadRooms()
    {
        var result = await _roomService.GetAllRooms();

        if (result.Success && result.Data != null)
        {
            Rooms.Clear();
            foreach (var room in result.Data)
                Rooms.Add(room);
        }
        else
        {
            MessageBox.Show(result.Error?.Message ?? "Error cargando habitaciones");
        }
    }



    // La room que se está editando/creando actualmente
    private RoomModel _currentRoom;

    public RoomModel CurrentRoom
    {
        get => _currentRoom;
        set
        {
            if (value == _currentRoom) return;
            _currentRoom = value;
            OnPropertyChanged(nameof(CurrentRoom));
        }
    }

    private RelayCommand backCommand;
    public ICommand BackCommand => backCommand ??= new RelayCommand(Back);

    private void Back(object commandParameter)
    {
    }

    private async void Save(object? parameter)
    {
        await SaveAsync();
    }

    private RelayCommand saveCommand;
    public ICommand SaveCommand => saveCommand ??= new RelayCommand(Save);

    private async Task SaveAsync()
    {
        List<string> errors = new();

        if (string.IsNullOrWhiteSpace(CurrentRoom.Name))
            errors.Add("Nombre");

        if (string.IsNullOrWhiteSpace(CurrentRoom.Type))
            errors.Add("Tipo");

        if (CurrentRoom.PricePerNight <= 0)
            errors.Add("Precio");

        if (CurrentRoom.OccupancyLimit <= 0)
            errors.Add("Ocupacion Maxima");

        if (CurrentRoom.Number <= 0)
            errors.Add("Numero");

        if (errors.Any())
        {
            MessageBox.Show("Faltan o son incorrectos:\n" + string.Join("\n", errors),
                            "Error",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
            return;
        }

        ApiResult<RoomModel> result;

        if (string.IsNullOrEmpty(CurrentRoom.RoomId))
        {
            result = await _roomService.CreateRoom(CurrentRoom);
        }
        else
        {
            result = await _roomService.UpdateRoom(CurrentRoom);
        }

        if (!result.Success)
        {
            MessageBox.Show(result.Error?.Message ?? "Error guardando habitación");
            return;
        }

        MessageBox.Show("Guardado correctamente");

        await LoadRooms();
        NavigationViewModel.Instance.BackCommand.Execute(null);
    }


    private async Task DeleteAsync()
    {

        if (MessageBox.Show("¿Seguro que quieres eliminar?", "Confirmación", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            if (MessageBox.Show("¿Seguro?", "Confirmación", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                //ELIMINAR (DELETE)
                var result = await _roomService.DeleteRoom(CurrentRoom.RoomId);

                if (!result.Success)
                {
                    MessageBox.Show(result.Error?.Message ?? "Error eliminando habitación");
                    return;
                }

                await LoadRooms();
                NavigationViewModel.Instance.BackCommand.Execute(null);
            }
            else { return; }   
        }
        else { return; }

    }

    private async void Delete(object? parameter)
    {
        await DeleteAsync();
    }

    private RelayCommand deleteCommand;
    public ICommand DeleteCommand => deleteCommand ??= new RelayCommand(Delete);


}
