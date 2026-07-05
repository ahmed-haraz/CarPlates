# CarPlates Architecture

## Overview
CarPlates is a .NET MAUI Android application for Automatic License Plate Recognition (ALPR/ANPR) built with Clean Architecture principles.

## Architecture Layers

```
┌─────────────────────────────────────┐
│         CarPlates.Mobile            │  ← .NET MAUI UI Layer
│    (Views, ViewModels, Controls)    │     MVVM Pattern
├─────────────────────────────────────┤
│      CarPlates.Application          │  ← Use Cases Layer
│  (Commands, Queries, DTOs, Interfaces)│     CQRS with MediatR
├─────────────────────────────────────┤
│        CarPlates.Domain             │  ← Business Logic Layer
│   (Entities, ValueObjects, Enums)   │     Pure C#, no dependencies
├─────────────────────────────────────┤
│     CarPlates.Infrastructure        │  ← External Services Layer
│ (API, Database, Camera, OCR, Logs)│     Platform-specific
├─────────────────────────────────────┤
│        CarPlates.Shared             │  ← Common Utilities
│    (Constants, Extensions, Utils)   │     Cross-cutting concerns
└─────────────────────────────────────┘
```

## Design Patterns
- **MVVM**: Model-View-ViewModel with CommunityToolkit.Mvvm source generators
- **CQRS**: Command Query Responsibility Segregation via MediatR
- **Repository**: Abstract data access with SQLite implementation
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Pipeline Behaviors**: Validation and logging middleware

## Key Technologies
| Technology | Purpose |
|------------|---------|
| .NET 9 MAUI | Cross-platform UI framework |
| CommunityToolkit.Mvvm | MVVM with source generators |
| MediatR | CQRS and pipeline behaviors |
| FluentValidation | Input validation |
| AutoMapper | DTO mapping |
| SQLite | Offline data persistence |
| Polly | Resilience and retry policies |
| Serilog | Structured logging |
| CameraX | Android camera API |
| Google ML Kit OCR | Text recognition from images |

## Data Flow
```
CameraX → ImageAnalysis → ML Kit OCR → Text Extraction
                                              ↓
                                    Plate Recognition (Regex)
                                              ↓
                                    Vehicle Lookup API
                                              ↓
                                    SQLite Cache + UI Display
```

## Authentication Flow
```
Splash → Check Token → Valid? → Dashboard
                ↓ No
            Login → API Auth → JWT Tokens → SecureStorage
```

## Offline Support
- SQLite cache for all scan records
- Pending upload queue for sync when online
- Background sync service with retry logic
- Conflict resolution for duplicate records
