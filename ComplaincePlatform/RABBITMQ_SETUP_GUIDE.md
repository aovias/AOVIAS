# RabbitMQ Setup Guide

## Authentication Error Fix

The error `ACCESS_REFUSED - Login was refused using authentication mechanism PLAIN` indicates that the credentials in `appsettings.json` don't match your RabbitMQ configuration.

## Current Configuration

In `ComplianceService/appsettings.json`:
```json
"RabbitMQ": {
  "HostName": "localhost",
  "Port": 5672,
  "UserName": "Compliance",
  "Password": "StrongPassword@123",
  "VirtualHost": "/"
}
```

## Solution Options

### Option 1: Use Default Guest User (Development Only)

If you're using the default RabbitMQ Docker container, it comes with a `guest` user with password `guest`:

**Update `appsettings.json`:**
```json
"RabbitMQ": {
  "HostName": "localhost",
  "Port": 5672,
  "UserName": "guest",
  "Password": "guest",
  "VirtualHost": "/"
}
```

### Option 2: Create a User in RabbitMQ

If you want to use custom credentials, create the user in RabbitMQ:

**Using Docker:**
```bash
# Access RabbitMQ container
docker exec -it <rabbitmq-container-name> bash

# Create user
rabbitmqctl add_user Compliance StrongPassword@123

# Grant permissions
rabbitmqctl set_permissions -p / Compliance ".*" ".*" ".*"

# Set as administrator (optional)
rabbitmqctl set_user_tags Compliance administrator
```

**Using RabbitMQ Management UI:**
1. Access management UI: `http://localhost:15672`
2. Login with `guest`/`guest`
3. Go to **Admin** → **Users**
4. Click **Add a user**
5. Username: `Compliance`
6. Password: `StrongPassword@123`
7. Tags: `administrator` (or `management` for limited access)
8. Click **Add user**
9. Click on the user → **Set permissions**
10. Virtual host: `/`
11. Configure: `.*`
12. Write: `.*`
13. Read: `.*`
14. Click **Set permissions**

### Option 3: Check Docker Container Configuration

If you're using Docker Compose, verify your RabbitMQ service configuration:

```yaml
rabbitmq:
  image: rabbitmq:3-management
  ports:
    - "5672:5672"
    - "15672:15672"
  environment:
    RABBITMQ_DEFAULT_USER: Compliance
    RABBITMQ_DEFAULT_PASS: StrongPassword@123
  volumes:
    - rabbitmq_data:/var/lib/rabbitmq
```

**Note:** If you set `RABBITMQ_DEFAULT_USER` and `RABBITMQ_DEFAULT_PASS` in Docker, those are the credentials you should use in `appsettings.json`.

## Verification

After updating credentials, restart the application and check logs for:
- ✅ `RabbitMQ connection test PASSED`
- ✅ `RabbitMQ setup completed successfully`

Or test via API:
```bash
curl -k https://localhost:7087/api/v1/test/rabbitmq
```

## Common Issues

### Issue: Guest user can only connect from localhost
**Solution:** If using Docker, ensure you're connecting to `localhost` (not `127.0.0.1` or container IP), or create a custom user.

### Issue: Virtual host doesn't exist
**Solution:** The default virtual host is `/`. If you're using a different one, create it:
```bash
rabbitmqctl add_vhost /your-vhost
rabbitmqctl set_permissions -p /your-vhost Compliance ".*" ".*" ".*"
```

### Issue: Port 5672 not accessible
**Solution:** Check Docker port mapping:
```bash
docker ps | grep rabbitmq
# Should show: 0.0.0.0:5672->5672/tcp
```

## Security Note

⚠️ **For Production:**
- Never use `guest`/`guest` credentials
- Use strong passwords
- Limit user permissions to only what's needed
- Use SSL/TLS for connections
- Consider using environment variables or secrets management for credentials
