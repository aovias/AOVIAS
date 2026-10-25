# Troubleshooting HTTP Status 0 Error

## Problem
Angular app receives `HttpErrorResponse` with `status: 0` when calling Gateway API endpoints.

## Common Causes

### 1. SSL Certificate Issue (Most Common)
The browser is blocking the self-signed SSL certificate for `https://localhost:7216`.

**Solution:**
1. Open `https://localhost:7216` directly in your browser
2. You'll see a security warning - click "Advanced" → "Proceed to localhost (unsafe)"
3. This will trust the certificate for this session
4. For permanent trust, you may need to install the certificate in your browser's certificate store

**Alternative for Development:**
- Use HTTP instead of HTTPS: `http://localhost:5212`
- Update your Angular environment configuration to use HTTP in development

### 2. CORS Configuration
Verify that:
- Angular app is running on `http://localhost:4200` or `https://localhost:4200`
- Gateway CORS is configured for these origins (already configured in `Gateway/appsettings.json`)

### 3. Gateway Service Not Running
Ensure the Gateway service is running:
- Check if `https://localhost:7216` is accessible
- Verify the service is started and listening on the correct port

### 4. JWT Token Issues
Verify that:
- The JWT token includes the `organization.api` scope
- The token is not expired
- The Authorization header is being sent correctly

## Quick Fixes

### Option 1: Trust SSL Certificate (Recommended for Development)
1. Navigate to `https://localhost:7216` in your browser
2. Accept the security warning
3. The certificate will be trusted for this browser session

### Option 2: Use HTTP in Development
Update your Angular environment configuration:
```typescript
// environment.ts
export const environment = {
  apiUrl: 'http://localhost:5212/api/v1', // Use HTTP instead of HTTPS
  // ...
};
```

### Option 3: Configure Angular to Bypass SSL (Not Recommended)
Only for development - add to your Angular `proxy.conf.json`:
```json
{
  "/api": {
    "target": "https://localhost:7216",
    "secure": false,
    "changeOrigin": true,
    "logLevel": "debug"
  }
}
```

## Verification Steps

1. **Check Gateway is Running:**
   ```bash
   curl https://localhost:7216/api/v1/tenant/ping
   # Or open in browser: https://localhost:7216
   ```

2. **Check CORS Headers:**
   Open browser DevTools → Network tab → Check response headers for:
   - `Access-Control-Allow-Origin: http://localhost:4200`
   - `Access-Control-Allow-Methods: *`
   - `Access-Control-Allow-Headers: *`

3. **Verify JWT Token:**
   - Decode your JWT token at https://jwt.io
   - Check that it contains `"scope": "compliance.api organization.api"`
   - Verify `organization_id` and `hospital_id` are not empty GUIDs

## Expected Behavior After Fix

After resolving the SSL certificate issue, you should see:
- HTTP status codes (200, 401, 404, etc.) instead of status 0
- Proper error messages from the server
- Successful API calls when authenticated
