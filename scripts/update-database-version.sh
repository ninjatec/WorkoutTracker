#!/bin/bash
# update-database-version.sh - Script to update version information in the database

# Exit immediately if a command exits with a non-zero status
set -e

# Move to the root directory of the project
cd "$(dirname "$0")/.."

# Color definitions for better readability
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}======================================================${NC}"
echo -e "${BLUE}  WorkoutTracker Database Version Update Script       ${NC}"
echo -e "${BLUE}======================================================${NC}"

# Check if sqlcmd is installed
if ! command -v sqlcmd &> /dev/null; then
    echo -e "${RED}Error: sqlcmd is not installed. Please install the SQL Server command-line tools.${NC}"
    echo -e "${YELLOW}For macOS: brew install microsoft/mssql-release/mssql-tools${NC}"
    exit 1
fi

# Extract connection string from user secrets
echo -e "${YELLOW}Reading database connection string from user secrets...${NC}"
CONNECTION_STRING=$(dotnet user-secrets list --project WorkoutTrackerWeb.csproj | grep "ConnectionStrings:DefaultConnection" | cut -d '=' -f2- | xargs)

# If connection string not found in user secrets, fall back to environment variables or prompt
if [ -z "$CONNECTION_STRING" ]; then
    echo -e "${YELLOW}Connection string not found in user secrets, falling back to environment variables.${NC}"
    
    # Check for required environment variables or use defaults
    DB_SERVER=${DB_SERVER:-"192.168.0.172"}
    DB_NAME=${DB_NAME:-"WorkoutTrackerWeb"}
    DB_USER=${DB_USER:-""}
    DB_PASSWORD=${DB_PASSWORD:-""}

    # If no credentials provided, prompt for them
    if [ -z "$DB_USER" ] || [ -z "$DB_PASSWORD" ]; then
        echo -e "${YELLOW}Database credentials not found in environment variables.${NC}"
        read -p "Enter database username: " DB_USER
        read -s -p "Enter database password: " DB_PASSWORD
        echo
    fi
else
    # Parse connection string to extract server, database, username and password
    echo -e "${GREEN}Connection string found in user secrets.${NC}"
    
    # Extract server from connection string
    if [[ $CONNECTION_STRING =~ Server=([^;]+) ]]; then
        DB_SERVER="${BASH_REMATCH[1]}"
    elif [[ $CONNECTION_STRING =~ Data\ Source=([^;]+) ]]; then
        DB_SERVER="${BASH_REMATCH[1]}"
    else
        echo -e "${RED}Error: Could not extract server from connection string.${NC}"
        exit 1
    fi
    
    # Extract database name from connection string
    if [[ $CONNECTION_STRING =~ Initial\ Catalog=([^;]+) ]]; then
        DB_NAME="${BASH_REMATCH[1]}"
    elif [[ $CONNECTION_STRING =~ Database=([^;]+) ]]; then
        DB_NAME="${BASH_REMATCH[1]}"
    else
        echo -e "${RED}Error: Could not extract database name from connection string.${NC}"
        exit 1
    fi
    
    # Extract credentials from connection string if using SQL authentication
    if [[ $CONNECTION_STRING =~ User\ ID=([^;]+) ]]; then
        DB_USER="${BASH_REMATCH[1]}"
        
        if [[ $CONNECTION_STRING =~ Password=([^;]+) ]]; then
            DB_PASSWORD="${BASH_REMATCH[1]}"
        else
            echo -e "${YELLOW}Warning: User ID found but no password in connection string.${NC}"
            read -s -p "Enter database password: " DB_PASSWORD
            echo
        fi
    elif [[ $CONNECTION_STRING =~ Integrated\ Security=True || $CONNECTION_STRING =~ Trusted_Connection=True ]]; then
        echo -e "${YELLOW}Using Windows Authentication from connection string.${NC}"
        # We'll use -E flag for sqlcmd later
        DB_USER=""
        DB_PASSWORD=""
    fi
fi

echo -e "${YELLOW}Using database: ${DB_NAME} on server: ${DB_SERVER}${NC}"

# Check if version_update.sql exists
SQL_FILE="version_update.sql"
if [ ! -f "$SQL_FILE" ]; then
    echo -e "${RED}Error: $SQL_FILE not found in the current directory.${NC}"
    exit 1
fi

echo -e "${YELLOW}Executing SQL to update version information...${NC}"

# Execute the SQL file using sqlcmd
if [ -z "$DB_USER" ] && [ -z "$DB_PASSWORD" ]; then
    # Windows Auth
    sqlcmd -C -S "$DB_SERVER" -d "$DB_NAME" -E -i "$SQL_FILE"
else
    # SQL Auth
    sqlcmd -C -S "$DB_SERVER" -d "$DB_NAME" -U "$DB_USER" -P "$DB_PASSWORD" -i "$SQL_FILE"
fi

if [ $? -eq 0 ]; then
    echo -e "${GREEN}Version information successfully updated in the database.${NC}"
else
    echo -e "${RED}Failed to update version information in the database.${NC}"
    exit 1
fi

# Optional: Update the version in the Versions table for application display
# This assumes you're using the AppVersion model from your application

# Check if jq is installed (for reading version.json)
if command -v jq &> /dev/null; then
    # Read version information from version.json
    if [ -f "version.json" ]; then
        VERSION_MAJOR=$(jq -r '.major' version.json)
        VERSION_MINOR=$(jq -r '.minor' version.json)
        VERSION_PATCH=$(jq -r '.patch' version.json)
        VERSION_BUILD=$(jq -r '.build' version.json)
        VERSION="${VERSION_MAJOR}.${VERSION_MINOR}.${VERSION_PATCH}.${VERSION_BUILD}"
        
        # Get Git commit hash
        GIT_COMMIT=$(git rev-parse --short HEAD)
        
        # Create SQL query to update or insert into Versions table
        echo -e "${YELLOW}Updating application version to ${VERSION}...${NC}"
        
        # Create a temporary SQL file for the Versions table update
        TMP_SQL_FILE="tmp_version_update.sql"
        cat > "$TMP_SQL_FILE" << EOF
-- Check if version already exists, if not add it as current
IF NOT EXISTS (SELECT 1 FROM Versions 
               WHERE Major = ${VERSION_MAJOR} 
               AND Minor = ${VERSION_MINOR} 
               AND Patch = ${VERSION_PATCH} 
               AND BuildNumber = ${VERSION_BUILD})
BEGIN
    -- First make all versions not current
    UPDATE Versions SET IsCurrent = 0;
    
    -- Insert new version as current
    INSERT INTO Versions (Major, Minor, Patch, BuildNumber, ReleaseDate, Description, GitCommitHash, IsCurrent)
    VALUES (${VERSION_MAJOR}, ${VERSION_MINOR}, ${VERSION_PATCH}, ${VERSION_BUILD}, 
            GETDATE(), 'Automated deployment update', '${GIT_COMMIT}', 1);
    
    PRINT 'New version added and set as current';
END
ELSE
BEGIN
    -- Update existing version to be current
    UPDATE Versions SET IsCurrent = 0;
    
    UPDATE Versions 
    SET IsCurrent = 1,
        ReleaseDate = GETDATE(),
        GitCommitHash = '${GIT_COMMIT}'
    WHERE Major = ${VERSION_MAJOR} 
    AND Minor = ${VERSION_MINOR} 
    AND Patch = ${VERSION_PATCH} 
    AND BuildNumber = ${VERSION_BUILD};
    
    PRINT 'Existing version updated and set as current';
END
GO
EOF

        # Execute the temporary SQL file
        if [ -z "$DB_USER" ] && [ -z "$DB_PASSWORD" ]; then
            # Windows Auth
            sqlcmd -C -S "$DB_SERVER" -d "$DB_NAME" -E -i "$TMP_SQL_FILE"
        else
            # SQL Auth
            sqlcmd -C -S "$DB_SERVER" -d "$DB_NAME" -U "$DB_USER" -P "$DB_PASSWORD" -i "$TMP_SQL_FILE"
        fi
        
        # Clean up
        rm "$TMP_SQL_FILE"
        
        if [ $? -eq 0 ]; then
            echo -e "${GREEN}Application version successfully updated to ${VERSION}.${NC}"
        else
            echo -e "${YELLOW}Note: Failed to update application version table. Script will continue.${NC}"
        fi
    else
        echo -e "${YELLOW}Warning: version.json not found. Skipping application version update.${NC}"
    fi
else
    echo -e "${YELLOW}Warning: jq not installed. Skipping application version update.${NC}"
fi

echo -e "${GREEN}======================================================${NC}"
echo -e "${GREEN}  Database Version Update Complete!                  ${NC}"
echo -e "${GREEN}======================================================${NC}"