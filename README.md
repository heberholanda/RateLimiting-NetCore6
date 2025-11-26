# ASP.NET Core 9.0 - Native Rate Limiting Demo

A comprehensive demonstration of native ASP.NET Core Rate Limiting capabilities using .NET 9.0.

## ?? Features

This project showcases **5 different rate limiting algorithms**:

1. **Fixed Window** - Simple time-based limiting
2. **Sliding Window** - Smoother rate limiting with overlapping windows
3. **Token Bucket** - Allows burst traffic with gradual token replenishment
4. **Concurrency** - Limits concurrent requests
5. **Global Limiter** - Fallback rate limiting for all endpoints

## ?? Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022 (17.8+) or Visual Studio Code
- Optional: Postman or curl for testing

## ??? Getting Started

### 1. Clone the Repository

```bash
git clone https://github.com/HeberHolanda/RateLimiting-NetCore10
cd RateLimiting-NetCore10
```

### 2. Restore Dependencies

```bash
dotnet restore
```

### 3. Run the Application

```bash
dotnet run --project RateLimiting-NetCore6
```

The application will start on `https://localhost:5001` (or the port configured in launchSettings.json).

### 4. Access Swagger UI

Navigate to: `https://localhost:5001/swagger`

## ?? Rate Limiting Policies

### IpPolicy - Fixed Window
- **Limit:** 3 requests per 30 seconds
- **Partition:** IP Address
- **Use Case:** Basic IP-based throttling

### ClientPolicy - Fixed Window
- **Limit:** 10 requests per minute
- **Partition:** Client ID (via `X-ClientId` header)
- **Whitelist:** `dev-id-1`, `dev-id-2`
- **Use Case:** Per-client rate limiting

### ApiPolicy - Sliding Window
- **Limit:** 6 requests per minute
- **Partition:** IP Address + Endpoint
- **Segments:** 2 per window
- **Use Case:** Smoother API endpoint limiting

### TokenBucketPolicy
- **Tokens:** 100 initial, 20 per minute replenishment
- **Partition:** IP Address
- **Use Case:** Handle burst traffic gracefully

### ConcurrencyPolicy
- **Limit:** 5 concurrent requests
- **Partition:** IP Address
- **Use Case:** Resource-intensive operations

## ?? API Endpoints

### IP Rate Limit Controller

```http
GET /api/IpRateLimit/GetRateLimitInfo
```
Tests IP-based rate limiting (IpPolicy).

```http
GET /api/IpRateLimit/TestSlidingWindow
```
Tests sliding window algorithm (ApiPolicy).

```http
POST /api/IpRateLimit/TestTokenBucket
```
Tests token bucket algorithm.

```http
GET /api/IpRateLimit/TestConcurrency
```
Tests concurrency limiting (delays 2 seconds).

```http
GET /api/IpRateLimit/NoLimit
```
Endpoint without rate limiting (whitelisted).

```http
GET /api/IpRateLimit/GetStatistics
```
Returns information about available policies.

### Client Rate Limit Controller

```http
GET /api/ClientRateLimit/GetClientInfo
```
Tests client-based rate limiting (requires `X-ClientId` header).

```http
POST /api/ClientRateLimit/TestClientPolicy
```
Validates client policy and whitelist.

```http
GET /api/ClientRateLimit/HighFrequencyOperation
```
Simulates high-frequency requests for testing.

```http
GET /api/ClientRateLimit/GetConfiguration
```
Returns client rate limiting configuration.

```http
POST /api/ClientRateLimit/TestCombinedPolicy
```
Tests combined IP and Client policies.

### Weather Forecast Controller

```http
GET /WeatherForecast
```
Gets weather forecast (protected by ApiPolicy).

```http
GET /WeatherForecast/city/{city}
```
Gets weather for specific city (protected by ApiPolicy).

```http
GET /WeatherForecast/unlimited
```
Gets weather without rate limiting.

## ?? Testing Rate Limiting

### Test with curl

#### IP-based Rate Limiting
```bash
# Make 10 rapid requests (expect 429 after 3rd request)
for i in {1..10}; do 
  curl -i http://localhost:5000/api/IpRateLimit/GetRateLimitInfo
  echo "Request $i completed"
  sleep 0.5
done
```

#### Client-based Rate Limiting
```bash
# Test with custom client ID
for i in {1..15}; do 
  curl -H "X-ClientId: test-client" http://localhost:5000/api/ClientRateLimit/GetClientInfo
  echo "Request $i completed"
  sleep 0.5
done
```

#### Whitelisted Client
```bash
# No rate limiting for whitelisted clients
for i in {1..20}; do 
  curl -H "X-ClientId: dev-id-1" http://localhost:5000/api/ClientRateLimit/GetClientInfo
done
```

### Test with PowerShell

```powershell
# Test IP-based rate limiting
1..10 | ForEach-Object {
    Invoke-RestMethod -Uri "http://localhost:5000/api/IpRateLimit/GetRateLimitInfo"
    Write-Host "Request $_ completed"
    Start-Sleep -Milliseconds 500
}
```

### Test with Postman

1. Create a collection with the endpoints above
2. Use Collection Runner to execute multiple requests
3. Observe 429 responses when limits are exceeded
4. Check `Retry-After` header for wait time

## ?? Configuration

Edit `appsettings.json` to customize rate limiting:

```json
{
  "RateLimiting": {
    "GlobalPolicy": {
      "PermitLimit": 10,
      "Window": "00:01:00",
      "QueueLimit": 0
    },
    "IpPolicy": {
      "PermitLimit": 3,
      "Window": "00:00:30"
    },
    "ApiPolicy": {
      "PermitLimit": 6,
      "Window": "00:01:00"
    },
    "ClientPolicy": {
      "PermitLimit": 10,
      "Window": "00:01:00"
    },
    "EndpointWhitelist": [ 
      "GET:/api/license", 
      "GET:/api/status"
    ],
    "IpWhitelist": [ 
      "127.0.0.1", 
      "::1"
    ],
    "ClientWhitelist": [ 
      "dev-id-1", 
      "dev-id-2" 
    ]
  }
}
```

### Configuration Options

- **PermitLimit**: Maximum number of requests allowed
- **Window**: Time window (format: `hh:mm:ss`)
- **QueueLimit**: Number of queued requests (0 = no queue)
- **EndpointWhitelist**: List of endpoints exempt from rate limiting (format: `METHOD:/path`)
- **IpWhitelist**: List of IP addresses exempt from rate limiting
- **ClientWhitelist**: List of client IDs exempt from rate limiting

## ?? Response Format

### Success Response (200 OK)
```json
{
  "ipAddress": "192.168.1.1",
  "message": "This endpoint is protected by IP-based rate limiting",
  "policy": "IpPolicy: 3 requests per 30 seconds",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Rate Limit Exceeded (429 Too Many Requests)
```json
{
  "message": "Too many requests. Please try again later.",
  "statusCode": 429,
  "retryAfterSeconds": 30
}
```

**Response Headers:**
```
HTTP/1.1 429 Too Many Requests
Retry-After: 30
Content-Type: application/json
```

## ?? Monitoring and Logging

The application logs rate limiting events:

```csharp
"Microsoft.AspNetCore.RateLimiting": "Information"
```

View logs in console output:
```
info: Microsoft.AspNetCore.RateLimiting[2]
      Rate limit exceeded for IP: 192.168.1.1
```

## ??? Architecture

```
RateLimiting-NetCore6/
??? Controllers/
?   ??? IpRateLimitController.cs       # IP-based rate limiting demos
?   ??? ClientRateLimitController.cs   # Client-based rate limiting demos
?   ??? WeatherForecastController.cs   # API endpoint examples
??? ServiceCollection/
?   ??? RateLimitingServiceCollection.cs  # Rate limiting configuration
??? appsettings.json                   # Configuration file
??? Program.cs                         # Application entry point
??? WeatherForecast.cs                 # Model class
```

## ?? Learn More

- **Native Rate Limiting:** Uses `System.Threading.RateLimiting` namespace
- **No External Packages:** Built-in since .NET 7
- **Multiple Algorithms:** Fixed Window, Sliding Window, Token Bucket, Concurrency
- **Attribute-based:** Apply policies using `[EnableRateLimiting]` and `[DisableRateLimiting]`

## ?? Additional Resources

- [ASP.NET Core Rate Limiting Documentation](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [Rate Limiter Algorithms](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit#rate-limiter-algorithms)
- [.NET 10 What's New](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview))

## ?? Author

**Heber Holanda**
- GitHub: [@HeberPcL](https://github.com/HeberPcL)

---