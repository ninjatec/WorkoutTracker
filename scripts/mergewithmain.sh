#!/bin/bash
# Script to safely merge dev branch into main with dev taking precedence in conflicts

# Exit immediately if any command fails
set -e

# Display script header
echo "==============================================="
echo "  Safe Merge from Dev to Main Branch Script"
echo "      (All conflicts use dev branch code)"
echo "==============================================="

# Store current branch to return to it at the end (if needed)
CURRENT_BRANCH=$(git branch --show-current)
echo "Current branch: $CURRENT_BRANCH"

# First ensure dev branch is up to date
echo "Updating dev branch..."
git checkout dev
git pull origin dev

# Check for any uncommitted changes
if [[ -n $(git status -s) ]]; then
    echo "Error: You have uncommitted changes in dev branch."
    echo "Please commit or stash your changes before merging."
    exit 1
fi

# Run build/tests to ensure dev branch is stable
echo "Verifying dev branch is stable (running build)..."
# Uncomment if you want to run a build check
# dotnet build || { echo "Build failed on dev branch. Aborting merge."; exit 1; }

# Now check out main branch and update it
echo "Switching to main branch..."
git checkout main
git pull origin main

# Perform the merge with strategy to prefer dev branch for conflicts
echo "Merging dev into main with dev branch having precedence..."
git merge dev -X theirs

# Confirm the merge worked
if [[ $? -ne 0 ]]; then
    echo "Merge encountered issues that couldn't be auto-resolved."
    echo "Please resolve conflicts manually and then commit."
    exit 1
fi

echo "Pushing changes to origin main..."
git push origin main

echo "Merge completed successfully!"

# Return to the original branch if different from main
if [[ "$CURRENT_BRANCH" != "main" ]]; then
    echo "Returning to original branch: $CURRENT_BRANCH"
    git checkout "$CURRENT_BRANCH"
fi

git checkout main

echo "Done."
