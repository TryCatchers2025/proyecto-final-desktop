namespace PereMaria.GestorHotel.Models;

public class BookingModel
{
    public string? BookingId { get; set; }
    public string? UserId { get; set; } = null!;
    public string? RoomId { get; set; } = null!;

    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? BookingDate { get; set; }

    public int Occupants { get; set; }

    public decimal PricePerNight { get; set; }
    public decimal TotalPrice { get; set; }

    public int? Discount { get; set; }
    public int TotalNights { get; set; }

    public string Status { get; set; } = null!; // 'active' | 'canceled'
}