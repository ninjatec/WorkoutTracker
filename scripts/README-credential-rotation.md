# SQL Server Credential Rotation System

This system provides automated and manual SQL Server credential rotation for the WorkoutTracker application, designed to support zero-downtime credential updates in a Kubernetes environment with HashiCorp Vault and External Secrets Operator.

## Overview

The credential rotation system consists of:

1. **Python Script** (`rotate-sql-credentials.py`) - Core rotation logic
2. **Shell Wrapper** (`rotate-credentials.sh`) - User-friendly interface
3. **Kubernetes CronJob** (`sql-credential-rotation.yaml`) - Automated scheduling
4. **Requirements** (`requirements-rotation.txt`) - Python dependencies

## Architecture

The rotation process follows these steps to ensure zero downtime:

1. **Retrieve Current Credentials** - Get existing credentials from Vault
2. **Generate New Credentials** - Create secure username/password pair
3. **Create New SQL User** - Add new user with same permissions as current user
4. **Test New Credentials** - Verify connectivity with new credentials
5. **Update Vault Secret** - Store new credentials (with backup of old ones)
6. **Trigger Secret Refresh** - Force External Secrets Operator to sync
7. **Wait for Pod Restart** - Monitor application pods for successful restart
8. **Cleanup Old User** - Remove the old SQL Server user

## Zero Downtime Strategy

The system ensures zero downtime by:

- Creating new credentials before removing old ones
- Testing connectivity before updating Vault
- Backing up old credentials for rollback capability
- Monitoring pod restart process
- Coordinating with Kubernetes rolling updates

## Prerequisites

### System Dependencies

```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install python3 python3-pip kubectl unixodbc-dev

# RHEL/CentOS
sudo yum install python3 python3-pip kubectl unixODBC-devel

# macOS (with Homebrew)
brew install python kubectl unixodbc
```

### Python Dependencies

```bash
pip3 install -r scripts/requirements-rotation.txt
```

### Required Access

- **Vault Access**: Token with read/write permissions to secrets path
- **SQL Server Access**: Current user must have ability to create/drop users and assign roles
- **Kubernetes Access**: ServiceAccount with permissions to:
  - Read/list pods
  - Read/patch ExternalSecrets
  - Read secrets (for monitoring)

## Usage

### Manual Rotation

#### Using Environment Variables

```bash
export VAULT_URL="https://vault.company.com"
export VAULT_TOKEN="hvs.XXXXXXXXXX"
export SECRET_PATH="workouttracker/secrets"

# Dry run first
./scripts/rotate-credentials.sh --dry-run

# Actual rotation
./scripts/rotate-credentials.sh
```

#### Using Command Line Parameters

```bash
./scripts/rotate-credentials.sh \
  --vault-url "https://vault.company.com" \
  --vault-token "hvs.XXXXXXXXXX" \
  --secret-path "workouttracker/secrets" \
  --sql-server "YOUR_SQL_SERVER" \
  --database "WorkoutTrackerWeb"
```

### Automated Rotation

Deploy the Kubernetes CronJob for automated rotation:

```bash
# First, ensure your Vault secrets are configured correctly
# See scripts/vault-configuration.md for detailed setup

# Update the Git repository URL in the YAML file
# Replace 'https://github.com/your-org/WorkoutTracker.git' with your actual repository

# Deploy the External Secret and CronJob
kubectl apply -f k8s/sql-credential-rotation.yaml
```

**Important Notes:**
- The CronJob uses an init container to fetch scripts from your Git repository
- Update the repository URL in the YAML file before deployment
- Ensure the Vault secrets are configured as described in `vault-configuration.md`
- The External Secret will automatically sync credentials from Vault

The CronJob is configured to run every 30 days at 2 AM. Adjust the schedule as needed:

```yaml
spec:
  # Run every 30 days at 2 AM
  schedule: "0 2 */30 * *"
```

### Rollback Operations

If issues occur after rotation, you can rollback to a previous backup:

```bash
# List available backups (check Vault UI or CLI)
# Rollback to specific timestamp
./scripts/rotate-credentials.sh --rollback "20250108_0200"
```

## Configuration

### Vault Configuration

The script expects secrets stored in Vault with this structure:

```json
{
  "ConnectionStrings__WorkoutTrackerWebContext": "Server=SERVER;Database=DATABASE;TrustServerCertificate=True;integrated security=False;User ID=USER;Password=SecurePassword123!",
  "ConnectionStrings__DefaultConnection": "Server=SERVER;Database=DATABASE;TrustServerCertificate=True;integrated security=False;User ID=USER;Password=SecurePassword123!",
  "EmailSettings:UseSsl": "true",
  "EmailSettings:UserName": "woy@workouttracker.online",
  "...": "other settings"
}
```

### External Secrets Configuration

Ensure your ExternalSecret is configured to watch the Vault path:

```yaml
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
```

### Application Configuration

Ensure your application deployment references the secret:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: workouttracker-web
spec:
  template:
    spec:
      containers:
      - name: web
        envFrom:
        - secretRef:
            name: workouttracker-secrets
```

## Security Considerations

### Password Generation

- Passwords are 16 characters by default
- Include uppercase, lowercase, numbers, and special characters
- Meet SQL Server complexity requirements
- Generated using cryptographically secure random functions

### Credential Storage

- Old credentials are backed up in Vault with timestamps
- Backups can be manually cleaned up after verification
- New usernames include timestamps to avoid conflicts

### Access Control

- Use least-privilege principle for Vault tokens
- Rotate Vault tokens regularly
- Monitor credential rotation logs
- Use Kubernetes RBAC for CronJob permissions

## Monitoring and Logging

### Log Files

- Rotation logs: `/tmp/sql-credential-rotation.log`
- Kubernetes logs: `kubectl logs -l app=credential-rotator`

### Health Checks

The script includes comprehensive error handling and validation:

- Vault connectivity
- SQL Server connectivity
- Kubernetes API access
- Credential functionality testing

### Alerts

Consider setting up monitoring for:

- Failed rotation jobs
- SQL connection failures
- Pod restart failures
- External secret sync failures

## Troubleshooting

### Common Issues

#### 1. SQL Permission Errors

```bash
# Check current user permissions
sqlcmd -S SERVER -U current_user -P current_password -Q "
SELECT 
    dp.name AS principal_name,
    dp.type_desc AS principal_type,
    o.name AS object_name,
    p.permission_name,
    p.state_desc AS permission_state
FROM sys.database_permissions p
JOIN sys.objects o ON p.major_id = o.object_id
JOIN sys.database_principals dp ON p.grantee_principal_id = dp.principal_id
WHERE dp.name = 'current_user';"
```

#### 2. Vault Access Issues

```bash
# Test Vault connectivity
export VAULT_ADDR="https://vault.company.com"
export VAULT_TOKEN="hvs.XXXXXXXXXX"
vault kv get workouttracker/secrets
```

#### 3. External Secret Not Updating

```bash
# Force refresh External Secret
kubectl annotate externalsecret workouttracker-secrets force-sync=$(date +%s)

# Check External Secret status
kubectl describe externalsecret workouttracker-secrets
```

#### 4. Pod Restart Issues

```bash
# Check pod status
kubectl get pods -l app=workouttracker

# Check pod logs
kubectl logs -l app=workouttracker --previous

# Check events
kubectl get events --sort-by=.metadata.creationTimestamp
```

### Manual Cleanup

If rotation fails partway through:

```bash
# Remove new SQL user (if created)
sqlcmd -S SERVER -U admin_user -P admin_password -Q "
DROP USER [new_username];
DROP LOGIN [new_username];"

# Rollback Vault secret
./scripts/rotate-credentials.sh --rollback "backup_timestamp"

# Force pod restart
kubectl rollout restart deployment/workouttracker-web
```

## Testing

### Dry Run Testing

Always test with dry run first:

```bash
./scripts/rotate-credentials.sh --dry-run
```

### Manual Testing

Test individual components:

```bash
# Test Vault access
python3 -c "
import hvac
client = hvac.Client(url='https://vault.company.com', token='hvs.XXX')
print('Authenticated:', client.is_authenticated())
"

# Test SQL connectivity
python3 -c "
import pyodbc
conn = pyodbc.connect('Server=YOUR_SQL_SERVER;Database=WorkoutTrackerWeb;...')
print('Connected successfully')
"

# Test Kubernetes access
kubectl auth can-i get pods
kubectl auth can-i patch externalsecrets
```

## Best Practices

1. **Always run dry-run first** in production environments
2. **Monitor the rotation process** during execution
3. **Test application functionality** after rotation
4. **Keep backup retention policy** for Vault secrets
5. **Regular rotation schedule** (monthly recommended)
6. **Document any manual interventions** required
7. **Test rollback procedures** in non-production environments

## Integration with CI/CD

You can integrate credential rotation into your CI/CD pipeline:

```yaml
# GitLab CI example
rotate-credentials:
  stage: maintenance
  script:
    - ./scripts/rotate-credentials.sh --dry-run
    - ./scripts/rotate-credentials.sh
  rules:
    - if: $CI_PIPELINE_SOURCE == "schedule"
  environment:
    name: production
```

## Support

For issues or questions:

1. Check the troubleshooting section above
2. Review rotation logs for detailed error messages
3. Verify all prerequisites are met
4. Test individual components in isolation

## Security Updates

Regularly update dependencies:

```bash
pip3 install --upgrade -r scripts/requirements-rotation.txt
```

Monitor for security advisories related to:
- hvac (Vault client)
- pyodbc (SQL Server driver)
- kubernetes (K8s client)
- cryptography (password generation)
