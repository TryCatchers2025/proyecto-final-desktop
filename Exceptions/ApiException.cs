using System.Net;
using PereMaria.GestorHotel.Models;

namespace PereMaria.GestorHotel.Exceptions;

/// <summary>
/// Excepción personalizada para errores de la API
/// </summary>
public class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public ApiErrorModel? ApiError { get; }

    public ApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public ApiException(HttpStatusCode statusCode, ApiErrorModel apiError)
        : base(apiError.GetFormattedMessage())
    {
        StatusCode = statusCode;
        ApiError = apiError;
    }

    /// <summary>
    /// Indica si es un error de validación
    /// </summary>
    public bool IsValidationError => ApiError?.IsValidationError ?? false;

    /// <summary>
    /// Obtiene el mensaje para mostrar al usuario
    /// </summary>
    public string GetDisplayMessage()
    {
        if (ApiError != null)
            return ApiError.GetFormattedMessage();

        return Message;
    }

    /// <summary>
    /// Obtiene los errores de un campo específico
    /// </summary>
    public List<string> GetFieldErrors(string fieldName)
    {
        return ApiError?.GetFieldErrors(fieldName) ?? new List<string>();
    }
}
