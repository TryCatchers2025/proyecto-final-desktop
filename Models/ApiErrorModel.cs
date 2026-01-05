using System.Text.Json.Serialization;

namespace PereMaria.GestorHotel.Models;

/// <summary>
/// Modelo para los errores devueltos por la API
/// Soporta dos formatos:
/// 1. ValidationError: { "name": "ValidationError", "errors": { "campo": ["mensaje"] } }
/// 2. Error genérico: { "message": "mensaje de error" }
/// </summary>
public class ApiErrorModel
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("errors")]
    public Dictionary<string, List<string>>? Errors { get; set; }

    /// <summary>
    /// Indica si es un error de validación
    /// </summary>
    public bool IsValidationError => Name == "ValidationError" && Errors != null;

    /// <summary>
    /// Obtiene el mensaje formateado para mostrar al usuario
    /// </summary>
    public string GetFormattedMessage()
    {
        // Si es un error de validación, mostrar todos los errores
        if (IsValidationError && Errors != null)
        {
            var errorMessages = new List<string>();

            foreach (var error in Errors)
            {
                foreach (var message in error.Value)
                {
                    errorMessages.Add($"• {message}");
                }
            }

            return string.Join(Environment.NewLine, errorMessages);
        }

        // Si tiene message, devolver ese directamente
        if (!string.IsNullOrEmpty(Message))
            return Message;

        return "Error desconocido";
    }

    /// <summary>
    /// Obtiene los errores para un campo específico
    /// </summary>
    public List<string> GetFieldErrors(string fieldName)
    {
        if (Errors == null || !Errors.ContainsKey(fieldName))
            return new List<string>();

        return Errors[fieldName];
    }
}
