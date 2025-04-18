#!/bin/bash

# Script to update application version for WorkoutTracker
# This should be run as part of the CI/CD pipeline before building the application

# Usage: ./update-version.sh [major|minor|patch]
# If no parameter is provided, only the build number will be incremented

# Move to the root directory of the project to ensure paths work correctly
cd "$(dirname "$0")/.."

# Configuration
VERSION_FILE="version.json"

# Get current version from version.json
function get_current_version() {
    if [[ -f "$VERSION_FILE" ]]; then
        # Read from version file
        major=$(jq -r '.major' "$VERSION_FILE")
        minor=$(jq -r '.minor' "$VERSION_FILE")
        patch=$(jq -r '.patch' "$VERSION_FILE")
        build=$(jq -r '.build' "$VERSION_FILE")
    else
        # Default version if no file exists
        major=1
        minor=0
        patch=0
        build=0
        
        # Create initial version file
        echo "{\"major\":$major,\"minor\":$minor,\"patch\":$patch,\"build\":$build}" > "$VERSION_FILE"
    fi
    
    echo "$major $minor $patch $build"
}

# Update version based on provided type
function update_version() {
    local update_type=$1
    read -r current_major current_minor current_patch current_build <<< "$(get_current_version)"
    
    # Get Git commit hash
    git_commit_hash=$(git rev-parse HEAD)
    
    # Determine what to increment
    case $update_type in
        major)
            new_major=$((current_major + 1))
            new_minor=0
            new_patch=0
            new_build=1
            description="Major version update"
            ;;
        minor)
            new_major=$current_major
            new_minor=$((current_minor + 1))
            new_patch=0
            new_build=1
            description="Minor version update"
            ;;
        patch)
            new_major=$current_major
            new_minor=$current_minor
            new_patch=$((current_patch + 1))
            new_build=1
            description="Patch update"
            ;;
        *)
            new_major=$current_major
            new_minor=$current_minor
            new_patch=$current_patch
            new_build=$((current_build + 1))
            description="Build update"
            ;;
    esac
    
    # Save new version to file
    echo "{\"major\":$new_major,\"minor\":$new_minor,\"patch\":$new_patch,\"build\":$new_build}" > "$VERSION_FILE"
    
    # Output new version for use in CI/CD pipeline
    echo "Updated version to $new_major.$new_minor.$new_patch.$new_build"
    
    # Export variables for other scripts to use
    export MAJOR=$new_major
    export MINOR=$new_minor
    export PATCH=$new_patch
    export BUILD=$new_build
    export VERSION="$new_major.$new_minor.$new_patch.$new_build"
    export GIT_COMMIT=$git_commit_hash
    
    # If running in GitHub Actions, set environment variables for the workflow
    if [[ -n "$GITHUB_ENV" ]]; then
        echo "MAJOR=$new_major" >> "$GITHUB_ENV"
        echo "MINOR=$new_minor" >> "$GITHUB_ENV"
        echo "PATCH=$new_patch" >> "$GITHUB_ENV"
        echo "BUILD=$new_build" >> "$GITHUB_ENV"
        echo "VERSION=$new_major.$new_minor.$new_patch.$new_build" >> "$GITHUB_ENV"
        echo "GIT_COMMIT=$git_commit_hash" >> "$GITHUB_ENV"
    fi
}

# Check if a version update type was provided
update_type=${1:-build}
update_version "$update_type"

exit 0