namespace PereMaria.GestorHotel.Services;

public static class SessionProvider
{
    // Token de prueba temporal - TODO: Implementar login real
    private static string? _token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1c2VySWQiOiI1MDdmMWY3N2JjZjg2Y2Q3OTk0MzkwMTEiLCJyb2xlIjoiY3VzdG9tZXIifQ.UBpM2lu2z7MgBCL0gK7KcxcIGWcDHgoOfGtjau9Kowg";

    public static string? Token
    {
        get => _token;
        set => _token = value;
    }

    public static bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    public static void ClearSession()
    {
        _token = null;
    }
}