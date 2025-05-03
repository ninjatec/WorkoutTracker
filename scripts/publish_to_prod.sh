#!/bin/bash
# publish_to_prod.sh - Script to build, version, and deploy WorkoutTracker to production

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
echo -e "${BLUE}  WorkoutTracker Deployment Script                    ${NC}"
echo -e "${BLUE}======================================================${NC}"

# 1. Update version based on parameter (major, minor, patch, or build)
VERSION_TYPE=${1:-build}
echo -e "${YELLOW}Updating version (${VERSION_TYPE})...${NC}"
./scripts/update-version.sh $VERSION_TYPE

# Read version from version.json
if [ -f "version.json" ]; then
    VERSION_MAJOR=$(jq -r '.major' version.json)
    VERSION_MINOR=$(jq -r '.minor' version.json)
    VERSION_PATCH=$(jq -r '.patch' version.json)
    VERSION_BUILD=$(jq -r '.build' version.json)
    VERSION="${VERSION_MAJOR}.${VERSION_MINOR}.${VERSION_PATCH}.${VERSION_BUILD}"
    echo -e "${GREEN}Version set to ${VERSION}${NC}"
else
    echo -e "${RED}Error: version.json not found${NC}"
    exit 1
fi

# 2. Get current git commit hash
GIT_COMMIT=$(git rev-parse --short HEAD)
echo -e "${YELLOW}Git commit: ${GIT_COMMIT}${NC}"

# 3. Build the application
echo -e "${YELLOW}Building application...${NC}"
dotnet build --configuration Release

# 4. Run tests (if they exist)
echo -e "${YELLOW}Running tests...${NC}"
dotnet test --no-build --configuration Release || { echo -e "${RED}Tests failed!${NC}"; exit 1; }

# 5. Build Docker image with both version tag and latest tag
echo -e "${YELLOW}Building Docker image for Linux x64 architecture...${NC}"
DOCKER_REPO="ninjatec/workout-tracker"

# Set platform variables
export DOCKER_DEFAULT_PLATFORM=linux/amd64
export DOTNET_RUNTIME_IDENTIFIER=linux-x64

# Use dotnet publish with explicit container and architecture parameters
echo -e "${YELLOW}Running dotnet publish with container target...${NC}"
dotnet publish \
  --os linux \
  --arch x64 \
  /t:PublishContainer \
  -p:ContainerImageName=${DOCKER_REPO} \
  -p:ContainerImageTag=${VERSION} \
  -p:PublishTrimmed=false \
  -p:ContainerRegistry="" \
  -p:TargetArch=x64 \
  -p:TargetOS=linux \
  --configuration Release

# List available images to verify what was created
echo -e "${YELLOW}Checking built images...${NC}"
BUILT_IMAGE_NAME=$(docker images --format "{{.Repository}}:{{.Tag}}" | grep -E "${DOCKER_REPO}|workouttrackerweb" | head -n 1)

if [ -z "$BUILT_IMAGE_NAME" ]; then
    echo -e "${RED}Error: Unable to find the built image. The build might have failed or used a different naming convention.${NC}"
    echo -e "${YELLOW}Available images:${NC}"
    docker images
    exit 1
fi

echo -e "${GREEN}Found image: $BUILT_IMAGE_NAME${NC}"

# Check image architecture
echo -e "${YELLOW}Verifying image architecture...${NC}"
IMAGE_ARCH=$(docker inspect $BUILT_IMAGE_NAME | grep "Architecture" | head -n 1 | awk -F': ' '{print $2}' | tr -d '",')
if [[ "$IMAGE_ARCH" != "amd64" && "$IMAGE_ARCH" != "x86_64" ]]; then
    echo -e "${RED}Error: Image architecture is $IMAGE_ARCH, not x64/amd64 as required${NC}"
    exit 1
fi
echo -e "${GREEN}Confirmed image architecture: $IMAGE_ARCH${NC}"

# Tag the image as both the versioned name and latest
echo -e "${YELLOW}Tagging image as ${DOCKER_REPO}:${VERSION} and latest...${NC}"
docker tag $BUILT_IMAGE_NAME ${DOCKER_REPO}:${VERSION}
docker tag $BUILT_IMAGE_NAME ${DOCKER_REPO}:latest

# 6. Push Docker images to repository
echo -e "${YELLOW}Pushing Docker images...${NC}"

# Function to push with retry
push_with_retry() {
    local image=$1
    local max_attempts=3
    local attempt=1
    
    while [ $attempt -le $max_attempts ]; do
        echo -e "${YELLOW}Push attempt $attempt/$max_attempts for $image${NC}"
        if docker push $image; then
            echo -e "${GREEN}Successfully pushed $image${NC}"
            return 0
        else
            echo -e "${YELLOW}Push failed for $image (attempt $attempt/$max_attempts)${NC}"
            attempt=$((attempt+1))
            
            if [ $attempt -le $max_attempts ]; then
                echo -e "${YELLOW}Waiting 10 seconds before retry...${NC}"
                sleep 10
                
                # Try to refresh Docker login credentials
                if [ $attempt -eq 2 ]; then
                    echo -e "${YELLOW}Refreshing Docker login credentials...${NC}"
                    docker logout
                    docker login || true
                fi
            fi
        fi
    done
    
    echo -e "${RED}Failed to push $image after $max_attempts attempts${NC}"
    return 1
}

# Push with retries
if ! push_with_retry "${DOCKER_REPO}:${VERSION}"; then
    echo -e "${RED}Failed to push versioned image. Aborting deployment.${NC}"
    exit 1
fi

if ! push_with_retry "${DOCKER_REPO}:latest"; then
    echo -e "${YELLOW}Warning: Failed to push latest tag, but continuing with deployment since versioned image was pushed.${NC}"
fi

# 7. Update Kubernetes deployments
echo -e "${YELLOW}Updating Kubernetes deployments...${NC}"

# Update the image tag in the main deployment YAML
echo -e "${YELLOW}Updating main application deployment...${NC}"
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS syntax
    sed -i '' "s|image: ${DOCKER_REPO}:.*|image: ${DOCKER_REPO}:${VERSION}|" ./k8s/deployment.yaml
else
    # Linux syntax
    sed -i "s|image: ${DOCKER_REPO}:.*|image: ${DOCKER_REPO}:${VERSION}|" ./k8s/deployment.yaml
fi

# Update the image tag in the Hangfire worker deployment YAML
echo -e "${YELLOW}Updating Hangfire worker deployment...${NC}"
if [[ "$OSTYPE" == "darwin"* ]]; then
    # macOS syntax
    sed -i '' "s|image: ${DOCKER_REPO}:.*|image: ${DOCKER_REPO}:${VERSION}|" ./k8s/hangfire-worker.yaml
else
    # Linux syntax
    sed -i "s|image: ${DOCKER_REPO}:.*|image: ${DOCKER_REPO}:${VERSION}|" ./k8s/hangfire-worker.yaml
fi

# 8. Add version details to database
echo -e "${YELLOW}Updating version in database...${NC}"
# Create a simple SQL file to update the version in the database
echo "-- Recording deployment version
INSERT INTO Deployments (Version, GitCommit, DeploymentDate)
VALUES ('${VERSION}', '${GIT_COMMIT}', CURRENT_TIMESTAMP);" > version_update.sql

# Execute the SQL using our database update script
echo -e "${YELLOW}Executing database version update...${NC}"
./scripts/update-database-version.sh

# Commit k8s changes to git
echo -e "${YELLOW}Committing version changes to git...${NC}"
git add ./k8s/deployment.yaml
git add ./k8s/hangfire-worker.yaml
git add ./version.json
git add ./version_update.sql
git commit -m "Deploy version ${VERSION}"
echo -e "${GREEN}Committed k8s changes for version ${VERSION}${NC}"

# Push changes to remote repository
echo -e "${YELLOW}Pushing changes to remote repository...${NC}"
git push
echo -e "${GREEN}Changes pushed to remote repository${NC}"

# Apply Kubernetes manifests individually instead of using kustomization
echo -e "${YELLOW}Applying Kubernetes manifests...${NC}"
kubectl apply -f ./k8s/namespace.yaml
kubectl apply -f ./k8s/regcred.yaml
kubectl apply -f ./k8s/secrets.yaml
kubectl apply -f ./k8s/deployment.yaml
kubectl apply -f ./k8s/service.yaml
kubectl apply -f ./k8s/mapping.yaml
# Apply the Hangfire worker deployment
kubectl apply -f ./k8s/hangfire-worker.yaml
got checkout dev
echo -e "${GREEN}======================================================${NC}"
echo -e "${GREEN}  Deployment Complete!                               ${NC}"
echo -e "${GREEN}  Version: ${VERSION}                                ${NC}"
echo -e "${GREEN}  Commit: ${GIT_COMMIT}                              ${NC}"
echo -e "${GREEN}======================================================${NC}"

