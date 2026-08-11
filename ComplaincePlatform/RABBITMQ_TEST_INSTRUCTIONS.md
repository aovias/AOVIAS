# RabbitMQ Connection Test Instructions

## Quick Test Methods

### Method 1: Automatic Test on Startup
The application automatically tests the RabbitMQ connection when it starts. Check the application logs for:
- ✅ `RabbitMQ connection test PASSED` - Connection is working
- ⚠️ `RabbitMQ connection test FAILED` - Connection issues

### Method 2: API Endpoint Test
Call the test endpoint via HTTP:

**PowerShell:**
```powershell
.\test-rabbitmq.ps1
```

**cURL:**
```bash
curl -k https://localhost:7087/api/v1/test/rabbitmq
```

**Browser/Postman:**
```
GET https://localhost:7087/api/v1/test/rabbitmq
```

### Method 3: Check Application Logs
Look for these log entries in the application output:
```
[INFO] Testing RabbitMQ connection to localhost:5672
[INFO] ✅ RabbitMQ connection test PASSED - Connection: Open, Exchange: True, CanPublish: True
```

## Expected Test Results

### ✅ Success Response
```json
{
  "timestamp": "2025-01-XX...",
  "success": true,
  "isConnected": true,
  "connectionState": "Open",
  "canCreateChannel": true,
  "exchangeExists": true,
  "queuesStatus": {
    "compliance.outbox.relay": "Exists",
    "compliance.snapshot.framework": "Exists",
    "compliance.snapshot.chapter": "Exists",
    "compliance.snapshot.organization": "Exists",
    "compliance.dlq": "Exists"
  },
  "canPublish": true,
  "message": "RabbitMQ connection test successful - All checks passed"
}
```

### ❌ Failure Response
```json
{
  "timestamp": "2025-01-XX...",
  "success": false,
  "isConnected": false,
  "connectionState": "Closed",
  "error": "Connection refused or timeout",
  "message": "RabbitMQ connection test failed: Connection refused"
}
```

## Troubleshooting

### Connection Refused
- **Check:** Is RabbitMQ Docker container running?
  ```bash
  docker ps | grep rabbitmq
  ```
- **Check:** Is port 5672 accessible?
- **Check:** Are credentials correct in `appsettings.json`?

### Exchange/Queue Not Found
- **Check:** Application logs for setup errors
- **Check:** RabbitMQ Management UI (if enabled) at `http://localhost:15672`
- **Solution:** Restart the application to trigger automatic setup

### Cannot Publish
- **Check:** Exchange exists and is bound correctly
- **Check:** Routing key matches: `compliance.updated`
- **Check:** Application logs for publish errors

## Manual Verification

### Check RabbitMQ Container
```bash
# Check if container is running
docker ps

# Check container logs
docker logs <rabbitmq-container-name>

# Test connection from container
docker exec -it <rabbitmq-container-name> rabbitmqctl status
```

### Check RabbitMQ Management (if enabled)
1. Enable management plugin:
   ```bash
   docker exec -it <container-name> rabbitmq-plugins enable rabbitmq_management
   ```
2. Access UI: `http://localhost:15672`
3. Login: `guest` / `guest` (or your configured user)
4. Check:
   - Exchanges tab → Should see `praman.events` and `praman.dlx`
   - Queues tab → Should see all 5 queues
   - Connections tab → Should see active connection

## Test Checklist

- [ ] RabbitMQ Docker container is running
- [ ] Application starts without RabbitMQ errors
- [ ] Startup logs show "RabbitMQ connection test PASSED"
- [ ] API endpoint `/api/v1/test/rabbitmq` returns success
- [ ] All queues show "Exists" status
- [ ] CanPublish is true
- [ ] OutboxRelayWorker is scheduled in Hangfire dashboard

## Next Steps After Successful Test

1. **Verify Outbox Processing:**
   - Complete a task via API
   - Check Hangfire dashboard for OutboxRelayWorker execution
   - Verify messages appear in RabbitMQ queues

2. **Monitor Message Flow:**
   - Check `compliance.outbox.relay` queue for messages
   - Verify messages are consumed and processed

3. **Test Error Handling:**
   - Stop RabbitMQ container temporarily
   - Verify retry mechanism works
   - Check dead-letter queue for failed messages
