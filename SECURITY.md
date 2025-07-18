# Security Policy

## Reporting a Vulnerability

If you believe you've found a security vulnerability in WorkoutTracker, please report it to [INSERT SECURITY CONTACT EMAIL]. We take all security reports seriously.

## Sensitive Information

This application uses several types of sensitive information that must be properly configured:

### Database Connection Strings
- WorkoutTrackerWebContext: Main application database
- DefaultConnection: Identity database
- TempContext: Temporary operations database
- Redis: Cache database

These should be configured using environment variables or user secrets, never committed to source control.

### Email Settings
Required email configuration includes:
- Mail Server
- Mail Port
- Sender Name
- Sender Email
- Username
- Password
- SSL Settings
- Admin Email

These settings should be configured using environment variables or user secrets in development, and using external-secrets.io in Kubernetes environments.

### Environment Variables
Required environment variables:
- `DB_PASSWORD`: Database password
- Email configuration (see above)
- Redis configuration (if enabled)

### Kubernetes Configuration
- Secrets are managed using external-secrets.io
- Secrets are stored in a vault backend
- The external-secrets operator must be configured in your cluster

## Development Setup

1. Configure User Secrets:
```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:WorkoutTrackerWebContext" "your_connection_string"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your_connection_string"
dotnet user-secrets set "ConnectionStrings:TempContext" "your_connection_string"
```

2. Set up required environment variables in your development environment.

3. For local development with Redis, ensure proper Redis configuration in appsettings.Development.json.

## Production Deployment

1. Ensure all secrets are configured in your vault backend
2. Verify external-secrets.io operator is properly configured
3. Apply the Kubernetes configuration files in the k8s directory

## Security Best Practices

1. Never commit sensitive information to source control
2. Use environment variables or user secrets for local development
3. Use external-secrets.io for Kubernetes deployments
4. Regularly rotate all credentials
5. Monitor application logs for security events
6. Keep all dependencies updated

## Recent Security Updates

### 2025-07-15: MessagePack Security Update
- Updated MessagePack from 2.5.187 to 3.0.300 to address known security vulnerabilities
- Updated MessagePack.Annotations from 2.5.187 to 3.0.300 for consistency
- Added explicit package references to prevent transitive dependency vulnerabilities
- Verified all packages are free from known security issues

### 2025-07-15: Comprehensive Package Updates
- Updated MessagePack to 3.1.4 (latest stable version)
- Updated all Microsoft ASP.NET Core packages to 9.0.7 (latest patch release)
- Updated all Entity Framework Core packages to 9.0.7 
- Updated Microsoft Extensions packages to 9.0.7
- Updated Polly resilience library to 8.6.2
- Updated QuestPDF to 2025.7.0
- All updates maintain security best practices and compatibility
- No known vulnerabilities remain in project dependencies
