using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Windows;
using PereMaria.GestorHotel.Exceptions;
using PereMaria.GestorHotel.Models;

namespace PereMaria.GestorHotel.Services;

public class ApiClientService
{
    // Singleton
    private static ApiClientService? _instance;
    public static ApiClientService Instance => _instance ??= new ApiClientService("http://localhost:3000");

    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClientService(string baseUrl)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(baseUrl)
        };
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

    }

    private void SetAuthHeader()
    {
        if (!string.IsNullOrEmpty(SessionProvider.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue(SessionProvider.Token);
        }
    }

    /// <summary>
    /// Procesa la respuesta HTTP y lanza ApiException si hay errores
    /// </summary>
    private async Task HandleResponseAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var errorContent = await response.Content.ReadAsStringAsync();

        try
        {
            var apiError = JsonSerializer.Deserialize<ApiErrorModel>(errorContent, _jsonOptions);
            // Es válido si tiene name (ValidationError) o message (error genérico)
            if (apiError != null && (!string.IsNullOrEmpty(apiError.Name) || !string.IsNullOrEmpty(apiError.Message)))
            {
                throw new ApiException(response.StatusCode, apiError);
            }
        }
        catch (JsonException)
        {
            // Si no se puede deserializar como ApiErrorModel, lanzar excepción genérica
        }

        throw new ApiException(response.StatusCode,
            string.IsNullOrEmpty(errorContent)
                ? $"Error del servidor: {(int)response.StatusCode}"
                : errorContent);
    }

    /// <summary>
    /// Muestra un MessageBox con los errores de la API
    /// </summary>
    public static void ShowApiError(ApiException ex, string title = "Error")
    {
        var icon = ex.IsValidationError ? MessageBoxImage.Warning : MessageBoxImage.Error;
        var errorTitle = ex.IsValidationError ? "Error de validación" : title;

        MessageBox.Show(ex.GetDisplayMessage(), errorTitle, MessageBoxButton.OK, icon);
    }

    /// <summary>
    /// Muestra un MessageBox con un error genérico
    /// </summary>
    public static void ShowError(Exception ex, string title = "Error")
    {
        if (ex is ApiException apiEx)
        {
            ShowApiError(apiEx, title);
            return;
        }

        MessageBox.Show(ex.Message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    // GET
    public async Task<T?> GetAsync<T>(string endpoint)
    {
        SetAuthHeader();
        var response = await _httpClient.GetAsync(endpoint);
        await HandleResponseAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    // POST
    public async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        SetAuthHeader();
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        await HandleResponseAsync(response);
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);
    }

    // PUT
    public async Task<T?> PutAsync<T>(string endpoint, object data)
    {
        SetAuthHeader();
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync(endpoint, content);
        await HandleResponseAsync(response);
        var responseJson = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseJson, _jsonOptions);
    }

    // DELETE
    public async Task<bool> DeleteAsync(string endpoint)
    {
        SetAuthHeader();
        var response = await _httpClient.DeleteAsync(endpoint);
        await HandleResponseAsync(response);
        return response.IsSuccessStatusCode;
    }
}