namespace CarPlates.Shared.Constants;

public static class ApiConstants
{
    public const string DefaultApiUrl = "https://api.carplates.example.com";
    public const string ApiVersion = "v1";
    public const int TimeoutSeconds = 30;
    public const int MaxRetryCount = 3;
    public const int RetryDelayMs = 1000;
}

public static class StorageConstants
{
    public const string DatabaseName = "carplates.db3";
    public const string LogsDirectory = "logs";
    public const string PhotosDirectory = "photos";
    public const int MaxCacheDays = 30;
    public const int MaxLogFiles = 10;
}

public static class ScannerConstants
{
    public const float DefaultOcrConfidence = 0.75f;
    public const float MinOcrConfidence = 0.50f;
    public const int DuplicateFilterSeconds = 5;
    public const int MaxScanHistory = 1000;
    public const string EgyptianPlatePattern = @"^[\u0660-\u0669]{1,4}\s*[-]?\s*[\u0621-\u064A]{1,3}$";
    public const string EnglishPlatePattern = @"^[A-Z0-9]{3,10}$";
}

public static class AuthConstants
{
    public const string AccessTokenKey = "access_token";
    public const string RefreshTokenKey = "refresh_token";
    public const string TokenExpiryKey = "token_expiry";
    public const int TokenRefreshMinutes = 5;
}
