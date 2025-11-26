# Rate Limiting Test Scripts

This document contains various test scripts to validate rate limiting functionality.

## Table of Contents
- [Bash/cURL Tests](#bashcurl-tests)
- [PowerShell Tests](#powershell-tests)
- [HTTP Files (REST Client)](#http-files-rest-client)
- [Expected Results](#expected-results)

---

## Bash/cURL Tests

### Test 1: IP-Based Rate Limiting (IpPolicy)

**Policy:** 3 requests per 30 seconds

```bash
#!/bin/bash
echo "Testing IP-Based Rate Limiting (3 requests per 30 seconds)"
echo "=========================================================="

for i in {1..10}; do
  echo -e "\n--- Request $i ---"
  curl -i -s http://localhost:5000/api/IpRateLimit/GetRateLimitInfo | head -n 1
  sleep 0.5
done

echo -e "\n\nWaiting 30 seconds for rate limit reset..."
sleep 30

echo -e "\nTesting after reset:"
curl -i -s http://localhost:5000/api/IpRateLimit/GetRateLimitInfo | head -n 1
```

### Test 2: Client-Based Rate Limiting (ClientPolicy)

**Policy:** 10 requests per minute

```bash
#!/bin/bash
echo "Testing Client-Based Rate Limiting (10 requests per minute)"
echo "============================================================"

CLIENT_ID="test-client-$(date +%s)"

for i in {1..15}; do
  echo -e "\n--- Request $i with ClientId: $CLIENT_ID ---"
  curl -i -s -H "X-ClientId: $CLIENT_ID" \
    http://localhost:5000/api/ClientRateLimit/GetClientInfo | head -n 1
  sleep 0.5
done
```

### Test 3: Whitelisted Client (No Limits)

```bash
#!/bin/bash
echo "Testing Whitelisted Client (dev-id-1)"
echo "====================================="

for i in {1..20}; do
  echo "Request $i"
  curl -s -H "X-ClientId: dev-id-1" \
    http://localhost:5000/api/ClientRateLimit/GetClientInfo \
    | jq -r '.message'
  sleep 0.2
done
```

### Test 4: Sliding Window (ApiPolicy)

**Policy:** 6 requests per minute

```bash
#!/bin/bash
echo "Testing Sliding Window Rate Limiting (6 requests per minute)"
echo "============================================================"

for i in {1..10}; do
  echo -e "\n--- Request $i at $(date +%H:%M:%S) ---"
  curl -i -s http://localhost:5000/api/IpRateLimit/TestSlidingWindow | head -n 1
  sleep 5  # 5 seconds between requests
done
```

### Test 5: Token Bucket (Burst Traffic)

**Policy:** 100 tokens, 20 per minute replenishment

```bash
#!/bin/bash
echo "Testing Token Bucket (Burst Traffic)"
echo "===================================="

echo "Phase 1: Burst of 30 requests"
for i in {1..30}; do
  echo "Request $i"
  curl -s -X POST http://localhost:5000/api/IpRateLimit/TestTokenBucket \
    | jq -r '.message'
  sleep 0.1
done

echo -e "\nPhase 2: Wait 1 minute for token replenishment"
sleep 60

echo -e "\nPhase 3: Try again after replenishment"
for i in {1..25}; do
  echo "Request $i"
  curl -s -X POST http://localhost:5000/api/IpRateLimit/TestTokenBucket \
    | jq -r '.message'
  sleep 0.1
done
```

### Test 6: Concurrency Limiter

**Policy:** Maximum 5 concurrent requests

```bash
#!/bin/bash
echo "Testing Concurrency Limiter (5 concurrent requests max)"
echo "======================================================="

# Launch 10 concurrent requests
for i in {1..10}; do
  (
    echo "Starting request $i at $(date +%H:%M:%S.%N)"
    curl -s http://localhost:5000/api/IpRateLimit/TestConcurrency > /dev/null
    echo "Completed request $i at $(date +%H:%M:%S.%N)"
  ) &
done

wait
echo "All requests completed"
```

### Test 7: Combined Test Suite

```bash
#!/bin/bash
BASE_URL="http://localhost:5000"

echo "=== Rate Limiting Test Suite ==="
echo ""

# Test 1: IpPolicy
echo "1. Testing IpPolicy (3 req/30s)..."
for i in {1..5}; do
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BASE_URL/api/IpRateLimit/GetRateLimitInfo")
  echo "  Request $i: HTTP $STATUS"
  sleep 0.5
done

sleep 30

# Test 2: ClientPolicy
echo -e "\n2. Testing ClientPolicy (10 req/min)..."
for i in {1..12}; do
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
    -H "X-ClientId: test" \
    "$BASE_URL/api/ClientRateLimit/GetClientInfo")
  echo "  Request $i: HTTP $STATUS"
  sleep 0.5
done

# Test 3: Whitelisted
echo -e "\n3. Testing Whitelisted Client..."
for i in {1..5}; do
  STATUS=$(curl -s -o /dev/null -w "%{http_code}" \
    -H "X-ClientId: dev-id-1" \
    "$BASE_URL/api/ClientRateLimit/GetClientInfo")
  echo "  Request $i: HTTP $STATUS (should always be 200)"
  sleep 0.2
done

echo -e "\nTest suite completed!"
```

---

## PowerShell Tests

### Test 1: IP-Based Rate Limiting

```powershell
# Test IP-Based Rate Limiting (3 requests per 30 seconds)
Write-Host "Testing IP-Based Rate Limiting" -ForegroundColor Cyan
Write-Host "===============================" -ForegroundColor Cyan

$baseUrl = "http://localhost:5000"

1..10 | ForEach-Object {
    Write-Host "`nRequest $_" -ForegroundColor Yellow
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl/api/IpRateLimit/GetRateLimitInfo" -UseBasicParsing
        Write-Host "Status: $($response.StatusCode)" -ForegroundColor Green
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "Status: $statusCode (Rate Limited)" -ForegroundColor Red
        
        if ($statusCode -eq 429) {
            $retryAfter = $_.Exception.Response.Headers['Retry-After']
            Write-Host "Retry-After: $retryAfter seconds" -ForegroundColor Yellow
        }
    }
    Start-Sleep -Milliseconds 500
}
```

### Test 2: Client-Based Rate Limiting

```powershell
# Test Client-Based Rate Limiting (10 requests per minute)
Write-Host "Testing Client-Based Rate Limiting" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan

$baseUrl = "http://localhost:5000"
$clientId = "test-client-$(Get-Date -Format 'yyyyMMddHHmmss')"
$headers = @{ "X-ClientId" = $clientId }

1..15 | ForEach-Object {
    Write-Host "`nRequest $_ with ClientId: $clientId" -ForegroundColor Yellow
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/ClientRateLimit/GetClientInfo" `
            -Headers $headers -UseBasicParsing
        Write-Host "Success: $($response.message)" -ForegroundColor Green
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        Write-Host "Status: $statusCode (Rate Limited)" -ForegroundColor Red
    }
    Start-Sleep -Milliseconds 500
}
```

### Test 3: Parallel Concurrency Test

```powershell
# Test Concurrency Limiter (5 concurrent requests max)
Write-Host "Testing Concurrency Limiter" -ForegroundColor Cyan
Write-Host "===========================" -ForegroundColor Cyan

$baseUrl = "http://localhost:5000"

$jobs = 1..10 | ForEach-Object {
    Start-Job -ScriptBlock {
        param($url, $id)
        $start = Get-Date
        try {
            $response = Invoke-RestMethod -Uri $url -UseBasicParsing
            $end = Get-Date
            $duration = ($end - $start).TotalSeconds
            Write-Output "Job $id completed in $duration seconds"
        }
        catch {
            $end = Get-Date
            $duration = ($end - $start).TotalSeconds
            Write-Output "Job $id failed in $duration seconds"
        }
    } -ArgumentList "$baseUrl/api/IpRateLimit/TestConcurrency", $_
}

# Wait for all jobs and display results
$jobs | Wait-Job | Receive-Job
$jobs | Remove-Job
```

### Test 4: Comprehensive Test with Logging

```powershell
# Comprehensive Rate Limiting Test
$baseUrl = "http://localhost:5000"
$logFile = "rate-limit-test-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"

function Write-Log {
    param($Message, $Color = "White")
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logMessage = "$timestamp - $Message"
    Write-Host $logMessage -ForegroundColor $Color
    Add-Content -Path $logFile -Value $logMessage
}

Write-Log "=== Rate Limiting Test Started ===" "Cyan"

# Test 1: IpPolicy
Write-Log "`nTest 1: IpPolicy (3 req/30s)" "Yellow"
1..5 | ForEach-Object {
    try {
        $response = Invoke-WebRequest -Uri "$baseUrl/api/IpRateLimit/GetRateLimitInfo" -UseBasicParsing
        Write-Log "  Request $_: HTTP $($response.StatusCode)" "Green"
    }
    catch {
        $code = $_.Exception.Response.StatusCode.value__
        Write-Log "  Request $_: HTTP $code" "Red"
    }
    Start-Sleep -Milliseconds 500
}

Write-Log "`nWaiting 30 seconds for rate limit reset..." "Yellow"
Start-Sleep -Seconds 30

# Test 2: After reset
Write-Log "`nTest 2: After Reset" "Yellow"
try {
    $response = Invoke-WebRequest -Uri "$baseUrl/api/IpRateLimit/GetRateLimitInfo" -UseBasicParsing
    Write-Log "  Request: HTTP $($response.StatusCode)" "Green"
}
catch {
    Write-Log "  Request failed" "Red"
}

# Test 3: ClientPolicy
Write-Log "`nTest 3: ClientPolicy (10 req/min)" "Yellow"
$headers = @{ "X-ClientId" = "test-client" }
1..12 | ForEach-Object {
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/ClientRateLimit/GetClientInfo" `
            -Headers $headers -UseBasicParsing
        Write-Log "  Request $_: Success" "Green"
    }
    catch {
        Write-Log "  Request $_: Rate Limited" "Red"
    }
    Start-Sleep -Milliseconds 500
}

# Test 4: Whitelisted
Write-Log "`nTest 4: Whitelisted Client" "Yellow"
$headers = @{ "X-ClientId" = "dev-id-1" }
1..20 | ForEach-Object {
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/ClientRateLimit/GetClientInfo" `
            -Headers $headers -UseBasicParsing
        Write-Log "  Request $_: Success (Whitelisted)" "Green"
    }
    catch {
        Write-Log "  Request $_: Failed (Unexpected)" "Red"
    }
    Start-Sleep -Milliseconds 200
}

Write-Log "`n=== Rate Limiting Test Completed ===" "Cyan"
Write-Log "Log saved to: $logFile" "Green"
```

---

## HTTP Files (REST Client)

Save as `rate-limit-tests.http` for use with VS Code REST Client extension:

```http
### Variables
@baseUrl = http://localhost:5000
@clientId = test-client

### Test 1: Get IP Rate Limit Info
GET {{baseUrl}}/api/IpRateLimit/GetRateLimitInfo

### Test 2: Get Rate Limit Statistics
GET {{baseUrl}}/api/IpRateLimit/GetStatistics

### Test 3: Test Sliding Window
GET {{baseUrl}}/api/IpRateLimit/TestSlidingWindow

### Test 4: Test Token Bucket
POST {{baseUrl}}/api/IpRateLimit/TestTokenBucket

### Test 5: Test Concurrency (Long Running)
GET {{baseUrl}}/api/IpRateLimit/TestConcurrency

### Test 6: No Rate Limit Endpoint
GET {{baseUrl}}/api/IpRateLimit/NoLimit

### Test 7: Client Rate Limit with Header
GET {{baseUrl}}/api/ClientRateLimit/GetClientInfo
X-ClientId: {{clientId}}

### Test 8: Test Client Policy
POST {{baseUrl}}/api/ClientRateLimit/TestClientPolicy
X-ClientId: {{clientId}}

### Test 9: High Frequency Operation
GET {{baseUrl}}/api/ClientRateLimit/HighFrequencyOperation
X-ClientId: {{clientId}}

### Test 10: Whitelisted Client (No Limits)
GET {{baseUrl}}/api/ClientRateLimit/GetClientInfo
X-ClientId: dev-id-1

### Test 11: Combined Policy Test
POST {{baseUrl}}/api/ClientRateLimit/TestCombinedPolicy
X-ClientId: {{clientId}}

### Test 12: Weather Forecast (ApiPolicy)
GET {{baseUrl}}/WeatherForecast

### Test 13: Weather Forecast by City
GET {{baseUrl}}/WeatherForecast/city/London

### Test 14: Weather Forecast Unlimited
GET {{baseUrl}}/WeatherForecast/unlimited

### Test 15: Get Client Configuration
GET {{baseUrl}}/api/ClientRateLimit/GetConfiguration
X-ClientId: {{clientId}}
```

---

## Expected Results

### IpPolicy (3 requests per 30 seconds)
- **Request 1-3:** HTTP 200 OK
- **Request 4+:** HTTP 429 Too Many Requests
- **After 30s:** HTTP 200 OK (reset)

### ClientPolicy (10 requests per minute)
- **Request 1-10:** HTTP 200 OK
- **Request 11+:** HTTP 429 Too Many Requests
- **Whitelisted clients:** Always HTTP 200 OK

### ApiPolicy (6 requests per minute, sliding window)
- **Request 1-6:** HTTP 200 OK
- **Request 7+:** HTTP 429 Too Many Requests
- **Smoother recovery** due to sliding window

### TokenBucketPolicy (100 tokens, 20/min replenishment)
- **Initial burst:** 100 requests succeed
- **After depletion:** Rate limited
- **After 1 min:** 20 more requests succeed

### ConcurrencyPolicy (5 concurrent max)
- **First 5 requests:** Execute immediately
- **Request 6+:** Queued or rejected
- **After completion:** New slots available

### 429 Response Format
```json
{
  "message": "Too many requests. Please try again later.",
  "statusCode": 429,
  "retryAfterSeconds": 30
}
```

### Response Headers
```
HTTP/1.1 429 Too Many Requests
Retry-After: 30
Content-Type: application/json
```

---

## Performance Testing with Apache Bench

```bash
# Test 100 requests with 10 concurrent connections
ab -n 100 -c 10 http://localhost:5000/api/IpRateLimit/GetRateLimitInfo

# Test with custom header
ab -n 100 -c 10 -H "X-ClientId: test-client" \
  http://localhost:5000/api/ClientRateLimit/GetClientInfo
```

---

## Notes

- Replace `localhost:5000` with your actual server address
- Adjust sleep timers based on your rate limit configurations
- Some tests may fail if the server is not running
- Check `appsettings.json` for current rate limit configurations
- In Development environment, limits are more relaxed (see `appsettings.Development.json`)

---
