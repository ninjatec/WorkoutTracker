#!/bin/bash

# SQL Credential Rotation Wrapper Script
# This script provides an easy interface for rotating SQL Server credentials

set -euo pipefail

# Default values
VAULT_URL="${VAULT_URL:-https://vault.example.com}"
VAULT_TOKEN="${VAULT_TOKEN:-}"
SECRET_PATH="${SECRET_PATH:-workouttracker/secrets}"
SQL_SERVER="${SQL_SERVER:-YOUR_SQL_SERVER}"
DATABASE="${DATABASE:-WorkoutTrackerWeb}"
NAMESPACE="${NAMESPACE:-default}"
APP_LABEL="${APP_LABEL:-workouttracker}"
DRY_RUN="${DRY_RUN:-false}"
ROLLBACK_TIMESTAMP="${ROLLBACK_TIMESTAMP:-}"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

# Help function
show_help() {
    cat << EOF
SQL Credential Rotation Script

Usage: $0 [OPTIONS]

OPTIONS:
    -u, --vault-url URL         Vault server URL (default: \$VAULT_URL)
    -t, --vault-token TOKEN     Vault authentication token (default: \$VAULT_TOKEN)
    -p, --secret-path PATH      Path to secret in Vault (default: \$SECRET_PATH)
    -s, --sql-server SERVER     SQL Server hostname/IP (default: \$SQL_SERVER)
    -d, --database DB           Database name (default: \$DATABASE)
    -n, --namespace NS          Kubernetes namespace (default: \$NAMESPACE)
    -l, --app-label LABEL       App label for pod selection (default: \$APP_LABEL)
    -r, --rollback TIMESTAMP    Rollback to specific backup (YYYYMMDD_HHMMSS)
    --dry-run                   Perform dry run without changes
    --check-prereqs             Check prerequisites and exit
    -h, --help                  Show this help message

ENVIRONMENT VARIABLES:
    VAULT_URL                   Vault server URL
    VAULT_TOKEN                 Vault authentication token
    SECRET_PATH                 Path to secret in Vault
    SQL_SERVER                  SQL Server hostname/IP
    DATABASE                    Database name
    NAMESPACE                   Kubernetes namespace
    APP_LABEL                   App label for pod selection
    DRY_RUN                     Set to 'true' for dry run
    ROLLBACK_TIMESTAMP          Backup timestamp for rollback

EXAMPLES:
    # Dry run with environment variables
    export VAULT_URL="https://vault.company.com"
    export VAULT_TOKEN="hvs.XXXXXXXXXX"
    $0 --dry-run

    # Manual rotation with parameters
    $0 -u "https://vault.company.com" -t "hvs.XXXXXXXXXX" -p "workouttracker/secrets"

    # Rollback to previous backup
    $0 --rollback "20250108_0200"

    # Check prerequisites
    $0 --check-prereqs

EOF
}

# Check prerequisites
check_prerequisites() {
    log_info "Checking prerequisites..."
    
    local missing_deps=()
    
    # Check Python
    if ! command -v python3 &> /dev/null; then
        missing_deps+=("python3")
    fi
    
    # Check pip
    if ! command -v pip3 &> /dev/null; then
        missing_deps+=("pip3")
    fi
    
    # Check kubectl
    if ! command -v kubectl &> /dev/null; then
        missing_deps+=("kubectl")
    fi
    
    # Check Python packages
    if ! python3 -c "import hvac" &> /dev/null; then
        missing_deps+=("python package: hvac")
    fi
    
    if ! python3 -c "import pyodbc" &> /dev/null; then
        missing_deps+=("python package: pyodbc")
    fi
    
    if ! python3 -c "import kubernetes" &> /dev/null; then
        missing_deps+=("python package: kubernetes")
    fi
    
    if ! python3 -c "import cryptography" &> /dev/null; then
        missing_deps+=("python package: cryptography")
    fi
    
    # Check kubectl access
    if ! kubectl auth can-i get pods &> /dev/null; then
        missing_deps+=("kubectl cluster access")
    fi
    
    if [ ${#missing_deps[@]} -eq 0 ]; then
        log_success "All prerequisites met!"
        return 0
    else
        log_error "Missing prerequisites:"
        for dep in "${missing_deps[@]}"; do
            echo "  - $dep"
        done
        echo
        echo "To install Python dependencies:"
        echo "  pip3 install -r scripts/requirements-rotation.txt"
        echo
        echo "To install system dependencies on Ubuntu/Debian:"
        echo "  sudo apt-get update"
        echo "  sudo apt-get install python3 python3-pip kubectl unixodbc-dev"
        echo
        echo "To install system dependencies on RHEL/CentOS:"
        echo "  sudo yum install python3 python3-pip kubectl unixODBC-devel"
        echo
        return 1
    fi
}

# Validate required parameters
validate_parameters() {
    local errors=()
    
    if [ -z "$VAULT_URL" ]; then
        errors+=("VAULT_URL is required")
    fi
    
    if [ -z "$VAULT_TOKEN" ] && [ -z "$ROLLBACK_TIMESTAMP" ]; then
        errors+=("VAULT_TOKEN is required (except for rollback operations)")
    fi
    
    if [ -z "$SECRET_PATH" ]; then
        errors+=("SECRET_PATH is required")
    fi
    
    if [ -z "$SQL_SERVER" ]; then
        errors+=("SQL_SERVER is required")
    fi
    
    if [ ${#errors[@]} -ne 0 ]; then
        log_error "Parameter validation failed:"
        for error in "${errors[@]}"; do
            echo "  - $error"
        done
        echo
        echo "Use --help for more information"
        return 1
    fi
    
    return 0
}

# Get script directory
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PYTHON_SCRIPT="$SCRIPT_DIR/rotate-sql-credentials.py"

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -u|--vault-url)
            VAULT_URL="$2"
            shift 2
            ;;
        -t|--vault-token)
            VAULT_TOKEN="$2"
            shift 2
            ;;
        -p|--secret-path)
            SECRET_PATH="$2"
            shift 2
            ;;
        -s|--sql-server)
            SQL_SERVER="$2"
            shift 2
            ;;
        -d|--database)
            DATABASE="$2"
            shift 2
            ;;
        -n|--namespace)
            NAMESPACE="$2"
            shift 2
            ;;
        -l|--app-label)
            APP_LABEL="$2"
            shift 2
            ;;
        -r|--rollback)
            ROLLBACK_TIMESTAMP="$2"
            shift 2
            ;;
        --dry-run)
            DRY_RUN="true"
            shift
            ;;
        --check-prereqs)
            check_prerequisites
            exit $?
            ;;
        -h|--help)
            show_help
            exit 0
            ;;
        *)
            log_error "Unknown option: $1"
            echo "Use --help for usage information"
            exit 1
            ;;
    esac
done

# Main execution
main() {
    log_info "Starting SQL credential rotation process..."
    
    # Check prerequisites
    if ! check_prerequisites; then
        exit 1
    fi
    
    # Validate parameters
    if ! validate_parameters; then
        exit 1
    fi
    
    # Check if Python script exists
    if [ ! -f "$PYTHON_SCRIPT" ]; then
        log_error "Python script not found: $PYTHON_SCRIPT"
        exit 1
    fi
    
    # Build Python command
    local python_cmd=(
        python3 "$PYTHON_SCRIPT"
        --vault-url "$VAULT_URL"
        --secret-path "$SECRET_PATH"
        --sql-server "$SQL_SERVER"
        --database "$DATABASE"
        --namespace "$NAMESPACE"
        --app-label "$APP_LABEL"
    )
    
    if [ -n "$VAULT_TOKEN" ]; then
        python_cmd+=(--vault-token "$VAULT_TOKEN")
    fi
    
    if [ "$DRY_RUN" = "true" ]; then
        python_cmd+=(--dry-run)
        log_info "Running in DRY RUN mode - no changes will be made"
    fi
    
    if [ -n "$ROLLBACK_TIMESTAMP" ]; then
        python_cmd+=(--rollback "$ROLLBACK_TIMESTAMP")
        log_warn "Rolling back to timestamp: $ROLLBACK_TIMESTAMP"
    fi
    
    # Execute Python script
    log_info "Executing credential rotation..."
    if "${python_cmd[@]}"; then
        log_success "Credential rotation completed successfully!"
        
        if [ "$DRY_RUN" != "true" ] && [ -z "$ROLLBACK_TIMESTAMP" ]; then
            log_info "Next steps:"
            echo "  1. Verify application pods are running correctly"
            echo "  2. Test database connectivity"
            echo "  3. Monitor application logs for any issues"
            echo "  4. Update your local secrets if needed"
        fi
        
        exit 0
    else
        log_error "Credential rotation failed!"
        
        if [ "$DRY_RUN" != "true" ] && [ -z "$ROLLBACK_TIMESTAMP" ]; then
            echo
            log_warn "If the rotation partially completed, you may need to:"
            echo "  1. Check the application logs"
            echo "  2. Verify the new credentials in Vault"
            echo "  3. Consider rolling back if necessary"
            echo "  4. Clean up any orphaned SQL users manually"
        fi
        
        exit 1
    fi
}

# Run main function
main "$@"
