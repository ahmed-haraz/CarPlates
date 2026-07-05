# CarPlates API Documentation

## Base URL
```
https://api.carplates.example.com/v1
```

## Authentication
All endpoints require Bearer token authentication.

### POST /auth/login
Authenticate user and receive JWT tokens.

**Request:**
```json
{
  "username": "string",
  "password": "string"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "username": "john.doe",
    "email": "john@example.com",
    "fullName": "John Doe",
    "profilePhotoUrl": "https://...",
    "role": "Operator"
  }
}
```

### POST /auth/refresh
Refresh access token using refresh token.

**Request:**
```json
{
  "refreshToken": "eyJhbGciOiJIUzI1NiIs..."
}
```

### POST /auth/logout
Invalidate current session.

### GET /auth/me
Get current user information.

## Vehicle Lookup

### GET /vehicles/{plateNumber}
Retrieve vehicle information by plate number.

**Response:**
```json
{
  "plateNumber": "1234 ABC",
  "brand": "Toyota",
  "model": "Corolla",
  "color": "White",
  "ownerName": "John Doe",
  "accessStatus": "Allowed"
}
```

**Error Responses:**
- `404 Not Found`: Vehicle not found
- `401 Unauthorized`: Invalid or expired token
- `500 Internal Server Error`: Server error

## Scan Records

### POST /sync/batch
Upload pending scan records in batch.

**Request:**
```json
{
  "records": [
    {
      "plateNumber": "1234 ABC",
      "plateType": "English",
      "confidence": 0.85,
      "scanTime": "2026-01-01T12:00:00Z"
    }
  ]
}
```

## Health Check

### GET /health
Check API availability.

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2026-01-01T12:00:00Z"
}
```
