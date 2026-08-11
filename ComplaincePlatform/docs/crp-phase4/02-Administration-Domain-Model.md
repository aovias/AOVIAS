# Administration Domain Model

| Entity | Store | Purpose |
|--------|-------|---------|
| Organization.Status | Org | Active/Suspended/Archived/Deleted |
| LicenseKey + ExtensionCount | Org | Trial/subscription |
| CommercialAdminAuditEvent | Org | Operator audit |
| EmailDeliveryLog | Org | Email attempts |
| AppUser | Identity | User lifecycle |
