# Vault Configuration for SQL Credential Rotation

This document describes the Vault secret structure required for the SQL credential rotation system.

## Required Vault Secrets

### 1. Main Application Secrets
**Path:** `workouttracker/secrets`

```json
{
  "ConnectionStrings__WorkoutTrackerWebContext": "Server=YOUR_SQL_SERVER;Database=WorkoutTrackerWeb;TrustServerCertificate=True;integrated security=False;User ID=your.username;Password=YourSecurePassword123!",
  "ConnectionStrings__DefaultConnection": "Server=YOUR_SQL_SERVER;Database=WorkoutTrackerWeb;TrustServerCertificate=True;integrated security=False;User ID=your.username;Password=YourSecurePassword123!",
  "EmailSettings:UseSsl": "true",
  "EmailSettings:UserName": "your.email@your-domain.com",
  "EmailSettings:SenderName": "Your App Name",
  "EmailSettings:SenderEmail": "noreply@your-domain.com",
  "EmailSettings:Password": "your-email-password",
  "EmailSettings:MailServer": "smtp.your-provider.com",
  "EmailSettings:MailPort": "587",
  "ApiKeys:ApiNinjas": "your-api-key"
}
```

### 2. Credential Rotation Configuration
**Path:** `workouttracker/credential-rotation`

```json
{
  "vault-url": "https://vault.your-company.com",
  "vault-token": "hvs.XXXXXXXXXXXXXXXXXXXXXX",
  "secret-path": "workouttracker/secrets"
}
```

## Setting Up Vault Secrets

### Using Vault CLI

```bash
# Set up the main application secrets
vault kv put workouttracker/secrets \
  ConnectionStrings__WorkoutTrackerWebContext="Server=YOUR_SQL_SERVER;Database=WorkoutTrackerWeb;TrustServerCertificate=True;integrated security=False;User ID=your.username;Password=YourSecurePassword123!" \
  ConnectionStrings__DefaultConnection="Server=YOUR_SQL_SERVER;Database=WorkoutTrackerWeb;TrustServerCertificate=True;integrated security=False;User ID=your.username;Password=YourSecurePassword123!" \
  EmailSettings:UseSsl="true" \
  EmailSettings:UserName="your.email@your-domain.com" \
  EmailSettings:SenderName="Your App Name" \
  EmailSettings:SenderEmail="noreply@your-domain.com" \
  EmailSettings:Password="your-email-password" \
  EmailSettings:MailServer="smtp.your-provider.com" \
  EmailSettings:MailPort="587" \
  ApiKeys:ApiNinjas="your-api-key"

# Set up the credential rotation configuration
vault kv put workouttracker/credential-rotation \
  vault-url="https://vault.your-company.com" \
  vault-token="hvs.XXXXXXXXXXXXXXXXXXXXXX" \
  secret-path="workouttracker/secrets"
```

### Using Vault UI

1. Navigate to your Vault UI
2. Go to the KV secrets engine
3. Create the paths and secrets as shown above

## Vault Token Requirements

The token used for credential rotation needs the following permissions:

```hcl
# Policy for credential rotation token
path "workouttracker/data/secrets" {
  capabilities = ["read", "update"]
}

path "workouttracker/data/secrets_backup_*" {
  capabilities = ["create", "read"]
}

path "workouttracker/metadata/secrets*" {
  capabilities = ["list"]
}

path "workouttracker/data/credential-rotation" {
  capabilities = ["read"]
}
```

## Security Considerations

1. **Token Rotation**: The Vault token itself should be rotated regularly
2. **Least Privilege**: The token should only have access to the specific paths needed
3. **Audit Logging**: Enable Vault audit logging to track all secret access
4. **Network Security**: Ensure Vault communication is over TLS
5. **Token TTL**: Set appropriate TTL for the rotation token

## External Secrets Configuration

The External Secrets Operator should be configured to watch both secret paths:

```yaml
# Main application secrets
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: workouttracker-secrets
spec:
  refreshInterval: 1m
  secretStoreRef:
    name: vault-backend
    kind: SecretStore
  target:
    name: workouttracker-secrets
    creationPolicy: Owner
  data:
  - secretKey: ConnectionStrings__WorkoutTrackerWebContext
    remoteRef:
      key: workouttracker/secrets
      property: ConnectionStrings__WorkoutTrackerWebContext
  # ... other properties

---
# Credential rotation configuration
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: credential-rotator-config
spec:
  refreshInterval: 15m
  secretStoreRef:
    name: vault-backend
    kind: SecretStore
  target:
    name: credential-rotator-config
    creationPolicy: Owner
  data:
  - secretKey: vault-url
    remoteRef:
      key: workouttracker/credential-rotation
      property: vault-url
  - secretKey: vault-token
    remoteRef:
      key: workouttracker/credential-rotation
      property: vault-token
  - secretKey: secret-path
    remoteRef:
      key: workouttracker/credential-rotation
      property: secret-path
```

## Backup Strategy

The rotation system automatically creates backups with timestamps:
- `workouttracker/secrets_backup_20250108_143000`
- `workouttracker/secrets_backup_20250208_143000`

Set up a cleanup policy to remove old backups after a retention period (e.g., 90 days).

## Monitoring

Monitor the following in Vault:
- Secret access patterns
- Failed authentication attempts
- Token usage and expiration
- Backup creation and cleanup

## Troubleshooting

### Common Issues

1. **Token Expired**: Check token TTL and renew if needed
2. **Permission Denied**: Verify token has required policies
3. **Path Not Found**: Ensure secrets exist at expected paths
4. **Network Issues**: Check Vault connectivity and TLS certificates

### Verification Commands

```bash
# Test Vault connectivity
vault status

# Test token permissions
vault kv get workouttracker/secrets
vault kv get workouttracker/credential-rotation

# List backups
vault kv list workouttracker/
```
