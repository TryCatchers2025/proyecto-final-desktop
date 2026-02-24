using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
using PereMaria.GestorHotel.Models;

namespace PereMaria.GestorHotel.Services;

/// <summary>
/// Encapsula la comunicación con la API para operaciones de reservas.
/// </summary>
public class BookingsService
{
    // Singleton
    private static BookingsService? _instance;
    public static BookingsService Instance => _instance ??= new BookingsService();

    private readonly ApiService _apiService = ApiService.Instance;
    private readonly UserService _userService = UserService.Instance;
    private readonly RoomService _roomService = RoomService.Instance;

    /// <summary>
    /// Obtiene todas las reservas accesibles para el usuario autenticado.
    /// </summary>
    public async Task<List<BookingModel>?> GetBookings()
    {
        try
        {
            var res = await _apiService.Get<BookingModel[]>("bookings");

            if (res.Error != null)
                throw new HttpRequestException(res.Error.Message);


            if (res.Data == null) return null;

            var bookings = res.Data.ToList();

            await PopulateBookings(bookings);

            return bookings;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new Exception("Error al conectar con el servidor");
        }
    }

    /// <summary>
    /// Obtiene una reserva por identificador y completa datos relacionados.
    /// </summary>
    public async Task<BookingModel?> GetBooking(string id)
    {
        try
        {
            var res = await _apiService.Get<BookingModel>($"bookings/{id}");

            if (res.Error != null)
                throw new Exception(res.Error.Message);

            if (res.Data == null)
                return null;

            await PopulateBooking(res.Data);

            return res.Data;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new Exception("Error al conectar con el servidor");
        }
    }

    /// <summary>
    /// Crea una nueva reserva en backend.
    /// </summary>
    public async Task<BookingModel?> CreateBooking(BookingModel booking)
    {
        Console.Write(booking);

        try
        {
            var res = await _apiService.Post<BookingModel>("bookings", booking);

            return res.Error != null ? throw new HttpRequestException(res.Error.Message) : res.Data;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new Exception("Error al conectar con el servidor");
        }
    }

    /// <summary>
    /// Actualiza una reserva existente enviando solo campos permitidos por API.
    /// </summary>
    public async Task<BookingModel?> UpdateBooking(string id, BookingModel booking)
    {
        try
        {
            var payload = BuildUpdatePayload(booking);
            var res = await _apiService.Put<BookingModel>($"bookings/{id}", payload);

            return res.Error != null ? throw new HttpRequestException(res.Error.Message) : res.Data;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new Exception("Error al conectar con el servidor");
        }
    }

    /// <summary>
    /// Cancela una reserva activa.
    /// </summary>
    public async Task<bool> CancelBooking(string id)
    {
        try
        {
            var res = await _apiService.Put<object>($"bookings/{id}/cancel", new object());

            if (res.Error != null)
                throw new Exception(res.Error.Message);

            return true;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new Exception("Error al conectar con el servidor");
        }
    }

    /// <summary>
    /// Extiende la fecha de salida de una reserva.
    /// </summary>
    public async Task<BookingModel?> ExtendBooking(string id, string endDate)
    {
        try
        {
            var res = await _apiService.Put<BookingModel>($"bookings/{id}/extend", new { endDate });

            return res.Error != null ? throw new Exception(res.Error.Message) : res.Data;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new Exception("Error al conectar con el servidor");
        }
    }

    /// <summary>
    /// Marca una reserva como pagada.
    /// </summary>
    public async Task<BookingModel?> PayBooking(string id)
    {
        try
        {
            var res = await _apiService.Put<BookingModel>($"bookings/{id}/pay", new object());

            return res.Error != null ? throw new Exception(res.Error.Message) : res.Data;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new Exception("Error al conectar con el servidor");
        }
    }

    /// <summary>
    /// Elimina definitivamente una reserva (operación restringida por backend).
    /// </summary>
    public async Task<bool> DeleteBooking(string id)
    {
        try
        {
            var res = await _apiService.Delete<object>($"bookings/{id}");

            if (res.Error != null)
                throw new Exception(res.Error.Message);

            return true;
        }
        catch (HttpRequestException ex)
        {
            throw new Exception(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            throw new Exception("Error al conectar con el servidor");
        }
    }

    /// <summary>
    /// Construye el cuerpo permitido para actualizar una reserva.
    /// </summary>
    /// <param name="booking">Reserva con datos editados.</param>
    private static Dictionary<string, object?> BuildUpdatePayload(BookingModel booking)
    {
        var payload = new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(booking.StartDate))
        {
            payload["startDate"] = booking.StartDate;
        }

        if (!string.IsNullOrWhiteSpace(booking.EndDate))
        {
            payload["endDate"] = booking.EndDate;
        }

        if (booking.Occupants > 0)
        {
            payload["occupants"] = booking.Occupants;
        }

        if (booking.Discount >= 0)
        {
            payload["discount"] = booking.Discount;
        }

        return payload;
    }

    /// <summary>
    /// Enriquece reservas con datos de clientes y habitaciones.
    /// </summary>
    /// <param name="bookings">Colección a completar.</param>
    private async Task PopulateBookings(List<BookingModel> bookings)
    {
        if (bookings.Count == 0) return;

        var customersResponse = await _userService.GetAllCustomers();
        if (!customersResponse.Success || customersResponse.Data == null)
        {
            var message = customersResponse.Error?.Message ?? "No se pudieron cargar los clientes";
            throw new Exception(message);
        }

        var roomsResponse = await _roomService.GetAllRooms();
        if (!roomsResponse.Success || roomsResponse.Data == null)
        {
            var message = roomsResponse.Error?.Message ?? "No se pudieron cargar las habitaciones";
            throw new Exception(message);
        }

        var customersById = customersResponse.Data.ToDictionary(c => c.UserId);
        var roomsById = roomsResponse.Data.ToDictionary(r => r.RoomId);

        foreach (var booking in bookings)
        {
            if (customersById.TryGetValue(booking.UserId, out var customer))
                booking.Customer = customer;

            if (roomsById.TryGetValue(booking.RoomId, out var room))
                booking.Room = room;
        }
    }

    /// <summary>
    /// Helper para completar una sola reserva.
    /// </summary>
    private async Task PopulateBooking(BookingModel booking)
    {
        await PopulateBookings([booking]);
    }
}