#!/bin/bash
# rollback_release.sh - Script to roll back WorkoutTracker to the previous release

# Exit immediately if a command exits with a non-zero status
set -e

# Move to the root directory of the project
cd "$(dirname "$0")/.."

# Color definitions
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}======================================================${NC}"
echo -e "${BLUE}  WorkoutTracker Rollback Script                      ${NC}"
echo -e "${BLUE}======================================================${NC}"

# Docker repository name
DOCKER_REPO="ninjatec/workout-tracker"

# 1. Get current version from version.json
if [ -f "version.json" ]; then
    CURRENT_VERSION_MAJOR=$(jq -r '.major' version.json)
    CURRENT_VERSION_MINOR=$(jq -r '.minor' version.json)
    CURRENT_VERSION_PATCH=$(jq -r '.patch' version.json)
    CURRENT_VERSION_BUILD=$(jq -r '.build' version.json)
    CURRENT_VERSION="${CURRENT_VERSION_MAJOR}.${CURRENT_VERSION_MINOR}.${CURRENT_VERSION_PATCH}.${CURRENT_VERSION_BUILD}"
    echo -e "${GREEN}Current version is ${CURRENT_VERSION}${NC}"
else
    echo -e "${RED}Error: version.json not found${NC}"
    exit 1
fi

# 2. Retrieve the previous version from database
echo -e "${YELLOW}Retrieving previous version information...${NC}"

# Create a temporary SQL script to fetch previous version
cat > temp_get_prev_version.sql << EOF
SELECT TOP 2 Version, GitCommit
FROM Deployments
ORDER BY DeploymentDate DESC;
EOF

# Get database connection info from environment or provide interactively
DB_CONNECTION_STRING=${DB_CONNECTION_STRING:-""}
if [ -z "$DB_CONNECTION_STRING" ]; then
    echo -e "${YELLOW}Please enter database connection information${NC}"
    read -p "Database server: " DB_SERVER
    read -p "Database name: " DB_NAME
    read -p "Database user: " DB_USER
    read -sp "Database password: " DB_PASSWORD
    echo # Add a newline after password input
    
    DB_CONNECTION_STRING="Server=$DB_SERVER;Database=$DB_NAME;User Id=$DB_USER;Password=$DB_PASSWORD;"
fi

# Execute SQL to get previous version
echo -e "${YELLOW}Connecting to database to retrieve version history...${NC}"
# Depending on your environment, you might need to adjust how this SQL is executed
# This example assumes sqlcmd is available
PREV_VERSION_INFO=$(sqlcmd -S "${DB_SERVER}" -d "${DB_NAME}" -U "${DB_USER}" -P "${DB_PASSWORD}" -i temp_get_prev_version.sql -h -1 -W)

# Clean up temp file
rm temp_get_prev_version.sql

# Parse the results (adjust parsing based on actual output format)
PREV_VERSION=$(echo "$PREV_VERSION_INFO" | sed -n '2p' | awk '{print $1}')
PREV_GIT_COMMIT=$(echo "$PREV_VERSION_INFO" | sed -n '2p' | awk '{print $2}')

if [ -z "$PREV_VERSION" ]; then
    echo -e "${RED}Error: Could not retrieve previous version from database${NC}"
    echo -e "${YELLOW}Attempting fallback: Will use git history to determine previous version${NC}"
    
    # Fallback: Get previous version from git history
    PREV_VERSION=$(git log -2 --grep="Deploy version" --pretty=format:"%s" | grep -oE "[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+" | sed -n '2p')
    PREV_GIT_COMMIT=$(git log -2 --grep="Deploy version" --pretty=format:"%h" | sed -n '2p')
    
    if [ -z "$PREV_VERSION" ]; then
        echo -e "${RED}Error: Could not determine previous version from git history either. Aborting.${NC}"
        exit 1
    fi
fi

echo -e "${GREEN}Found previous version: ${PREV_VERSION} (commit: ${PREV_GIT_COMMIT})${NC}"

# 3. Confirm rollback with user
echo -e "${YELLOW}You are about to roll back from version ${CURRENT_VERSION} to ${PREV_VERSION}${NC}"
echo -e "${YELLOW}This will also revert the last git commit and restart Kubernetes deployments${NC}"
read -p "Are you sure you want to proceed? (y/n): " CONFIRM
if [[ "$CONFIRM" != "y" && "$CONFIRM" != "Y" ]]; then
    echo -e "${YELLOW}Rollback cancelled by user${NC}"
    exit 0
fi

# 4. Update Kubernetes deployments to use previous version
echo -e "${YELLOW}Updating Kubernetes deployments to version ${PREV_VERSION}...${NC}"

# Update the image tag in the main deployment YAML
echo -e "${YELLOW}Updating main application deployment...${NC}"
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS syntax
    sed -i '' "s|image: ${DOCKER_REPO}:.*|image: ${DOCKER_REPO}:${PREV_VERSION}|" ./k8s/deployment.yaml
else
    # Linux syntax
    sed -i "s|image: ${DOCKER_REPO}:.*|image: ${DOCKER_REPO}:${PREV_VERSION}|" ./k8s/deployment.yaml
fi

# Update the image tag in the Hangfire worker deployment YAML
echo -e "${YELLOW}Updating Hangfire worker deployment...${NC}"
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS syntax
    sed -i '' "s|image: ${DOCKER_REPO}:.*|image: ${DOCKER_REPO}:${PREV_VERSION}|" ./k8s/hangfire-worker.yaml
else
    # Linux syntax
    sed -i "s|image: ${DOCKER_REPO}:.*|image: ${DOCKER_REPO}:${PREV_VERSION}|" ./k8s/hangfire-worker.yaml
fi

# 5. Apply the updated Kubernetes manifests
echo -e "${YELLOW}Applying Kubernetes manifests with previous version...${NC}"
kubectl apply -f ./k8s/deployment.yaml
kubectl apply -f ./k8s/hangfire-worker.yaml

# 6. Record rollback in database
echo -e "${YELLOW}Recording rollback in database...${NC}"
cat > rollback_record.sql << EOF
-- Recording rollback
INSERT INTO Deployments (Version, GitCommit, DeploymentDate, Notes)
VALUES ('${PREV_VERSION}', '${PREV_GIT_COMMIT}', CURRENT_TIMESTAMP, 'Rollback from version ${CURRENT_VERSION}');
EOF

# Execute SQL to record rollback
echo -e "${YELLOW}Connecting to database to record rollback...${NC}"
sqlcmd -S "${DB_SERVER}" -d "${DB_NAME}" -U "${DB_USER}" -P "${DB_PASSWORD}" -i rollback_record.sql
rm rollback_record.sql

# 7. Revert the git commit
echo -e "${YELLOW}Reverting last git commit...${NC}"
git revert HEAD --no-edit

# 8. Update version.json to previous version
echo -e "${YELLOW}Updating version.json to previous version...${NC}"
# Parse the previous version components
IFS='.' read -r PREV_MAJOR PREV_MINOR PREV_PATCH PREV_BUILD <<< "$PREV_VERSION"

# Create new version.json content
cat > version.json << EOF
{"major":${PREV_MAJOR},"minor":${PREV_MINOR},"patch":${PREV_PATCH},"build":${PREV_BUILD}}
EOF

# 9. Commit version.json changes
echo -e "${YELLOW}Committing version.json changes...${NC}"
git add version.json
git commit -m "Rollback to version ${PREV_VERSION}"

# 10. Push changes to remote repository
echo -e "${YELLOW}Pushing changes to remote repository...${NC}"
git push

# 11. Wait for deployments to stabilize
echo -e "${YELLOW}Waiting for deployments to stabilize...${NC}"
kubectl rollout status deployment/workouttracker -n web
kubectl rollout status deployment/workouttracker-hangfire-worker -n web

echo -e "${GREEN}======================================================${NC}"
echo -e "${GREEN}  Rollback Complete!                                 ${NC}"
echo -e "${GREEN}  Rolled back from: ${CURRENT_VERSION}               ${NC}"
echo -e "${GREEN}  Rolled back to: ${PREV_VERSION}                    ${NC}"
echo -e "${GREEN}======================================================${NC}"