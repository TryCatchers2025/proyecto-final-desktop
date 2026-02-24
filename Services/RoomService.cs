using PereMaria.GestorHotel.Models;
using System.Collections.ObjectModel;
using System.Net;
using System.Windows;

namespace PereMaria.GestorHotel.Services;

/// <summary>
/// Servicio de habitaciones para operaciones de catálogo y mantenimiento.
/// </summary>
public class RoomService
{
    private static RoomService? _instance;
    public static RoomService Instance => _instance ??= new RoomService();

    public RoomService() { }

    /// <summary>
    /// Recupera todas las habitaciones disponibles en backend.
    /// </summary>
    public async Task<ApiResult<List<RoomModel>>> GetAllRooms()
    {
        return await ApiService.Instance.Get<List<RoomModel>>("rooms");
    }

    /// <summary>
    /// Recupera una habitación por identificador.
    /// </summary>
    public async Task<ApiResult<RoomModel>> GetRoom(string id)
    {
        return await ApiService.Instance.Get<RoomModel>($"rooms/{id}");
    }

    /// <summary>
    /// Crea una nueva habitación.
    /// </summary>
    public async Task<ApiResult<RoomModel>> CreateRoom(RoomModel room)
    {
        return await ApiService.Instance.Post<RoomModel>("rooms", room);
    }

    /// <summary>
    /// Actualiza los datos de una habitación existente.
    /// </summary>
    public async Task<ApiResult<RoomModel>> UpdateRoom(RoomModel room)
    {
        return await ApiService.Instance.Put<RoomModel>($"rooms/{room.RoomId}", room);
    }

    /// <summary>
    /// Elimina una habitación del catálogo.
    /// </summary>
    public async Task<ApiResult<RoomModel>> DeleteRoom(string id)
    {
        return await ApiService.Instance.Delete<RoomModel>($"rooms/{id}");
    }
}