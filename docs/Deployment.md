# CarPlates Deployment Guide

## Prerequisites
- Visual Studio 2022 17.10+ or VS Code
- .NET 9 SDK
- Android SDK 35
- Android Emulator or Physical Device
- Git

## Build Instructions

### 1. Clone Repository
```bash
git clone https://github.com/your-org/carplates.git
cd CarPlates
```

### 2. Install Workloads
```bash
dotnet workload install maui-android
```

### 3. Restore Packages
```bash
dotnet restore
```

### 4. Build Solution
```bash
dotnet build
```

### 5. Run Tests
```bash
dotnet test
```

### 6. Deploy to Android
```bash
cd src/CarPlates.Mobile
dotnet build -t:Run -f net9.0-android
```

## Release Build

### Unsigned APK
```bash
cd src/CarPlates.Mobile
dotnet publish -f net9.0-android -c Release -p:AndroidPackageFormat=apk
```

### Signed AAB (Google Play)
```bash
cd src/CarPlates.Mobile
dotnet publish -f net9.0-android -c Release   -p:AndroidPackageFormat=aab   -p:AndroidKeyStore=true   -p:AndroidSigningKeyStore=your-keystore.jks   -p:AndroidSigningStorePass=your-password   -p:AndroidSigningKeyAlias=your-alias   -p:AndroidSigningKeyPass=your-password
```

## CI/CD Pipeline
GitHub Actions workflows are configured in `.github/workflows/`:
- `build.yml`: Builds and tests on every push/PR
- `release.yml`: Creates signed release on version tags

## Environment Configuration
Update `ApiConstants.DefaultApiUrl` in `src/CarPlates.Shared/Constants/ApiConstants.cs` with your production API URL.

## Database
SQLite database is created automatically on first run at:
```
/data/data/com.companyname.carplates/files/carplates.db3
```

## Logging
Logs are stored in:
```
/data/data/com.companyname.carplates/files/logs/
```
