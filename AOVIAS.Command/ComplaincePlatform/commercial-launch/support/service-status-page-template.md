# Service Status Page Template

**Usage:** Static HTML or status.io / equivalent. Host separately from production app when possible.

## Page skeleton

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <title>AOVIAS Command Status</title>
  <meta name="viewport" content="width=device-width, initial-scale=1" />
</head>
<body>
  <header>
    <h1>AOVIAS Command — System Status</h1>
    <p>Updated: <!-- UTC TIMESTAMP --> — <strong><!-- OVERALL: Operational | Degraded | Outage | Maintenance --></strong></p>
  </header>

  <section>
    <h2>Components</h2>
    <ul>
      <li>Web application — <!-- status --></li>
      <li>API Gateway — <!-- status --></li>
      <li>Identity / sign-in — <!-- status --></li>
      <li>Compliance services — <!-- status --></li>
      <li>Licensing — <!-- status --></li>
      <li>AI assist — <!-- status --></li>
      <li>Notifications — <!-- status --></li>
    </ul>
  </section>

  <section>
    <h2>Active incidents</h2>
    <!-- For each: title, severity, started UTC, summary, next update -->
    <p>None.</p>
  </section>

  <section>
    <h2>Scheduled maintenance</h2>
    <p><!-- window or None --></p>
  </section>

  <section>
    <h2>Past 7 days</h2>
    <ul>
      <li><!-- date — brief — resolved --></li>
    </ul>
  </section>

  <footer>
    <p>Support: support@aovias.example · Include your organization ID and Correlation ID.</p>
  </footer>
</body>
</html>
```

## Status vocabulary

| Status | Meaning |
|--------|---------|
| Operational | Normal |
| Degraded | Partial impact / workaround |
| Partial outage | Some components down |
| Major outage | Login or core compliance unavailable |
| Maintenance | Planned 503 window |

## Operating rules

- Update within 15 minutes of S1 acknowledge.  
- Never invent root cause; say “investigating” until confirmed.  
- Link maintenance to customer-comms T2.  
- Do not expose internal hostnames, keys, or PHI.
