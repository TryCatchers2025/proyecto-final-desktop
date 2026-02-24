using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace PereMaria.GestorHotel.Models;

public class RoomModel
{
	[JsonProperty("_id")] public string RoomId { get; set; } = "";

	[JsonProperty("name")] public string Name { get; set; } = "";

    [JsonProperty("type")] public string Type { get; set; } = "";

    [JsonProperty("number")] public int Number { get; set; }

    [JsonProperty("offer")] public int Offer { get; set; }

	[JsonProperty("pricePerNight")] public double PricePerNight { get; set; }

	[JsonProperty("occuped")] public Boolean Occuped { get; set; }
    
    [JsonProperty("occupancyLimit")] public int OccupancyLimit { get; set; }

    [JsonProperty("description")] public string Description { get; set; } = "";


    public String ToString() {
        return $"ID:{RoomId}, Name: {Name}, Type: {Type}, Number: {Number}, Offer: {Offer}, Price: {PricePerNight}, Occuped: {Occuped}, Limit: {OccupancyLimit}, Description: {Description}";
    }
}