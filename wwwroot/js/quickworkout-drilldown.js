// Handles drill-down exercise selection for QuickWorkout
// Populates the exercise dropdown when an exercise is selected

document.addEventListener('DOMContentLoaded', function () {
    // Check if we have an active session (elements only exist when session is active)
    const muscleListContainer = document.getElementById('muscle-group-list');
    const exerciseListContainer = document.getElementById('exercise-list');
    const backToMusclesBtn = document.getElementById('back-to-muscles-btn');
    const exerciseDropdown = document.querySelector('select[name="QuickWorkout.ExerciseTypeId"]');
    const allExercises = window.quickWorkoutAllExercises || [];
    
    // If there's no active session, these elements won't exist, so we can return early
    if (!muscleListContainer || !exerciseListContainer) {
        return; // Exercise selection is hidden because no active session
    }

    // Group exercises by muscle (use all muscle groups from window.quickWorkoutMuscleGroups if available)
    let muscleGroups = {};
    let allMuscleNames = window.quickWorkoutMuscleGroups || [];
    allExercises.forEach(ex => {
        let muscle = ex.muscle || ex.primaryMuscleGroup || 'Other';
        if (!muscleGroups[muscle]) muscleGroups[muscle] = [];
        muscleGroups[muscle].push(ex);
        if (allMuscleNames.indexOf(muscle) === -1) allMuscleNames.push(muscle);
    });

    // Dynamically render muscle group buttons as a word cloud style (group of buttons, not a list)
    function renderMuscleGroupButtons() {
        if (!muscleListContainer) return;
        muscleListContainer.innerHTML = '';
        // Sort for consistency, but randomize size for word cloud effect
        allMuscleNames.sort((a, b) => a.localeCompare(b));
        allMuscleNames.forEach(muscle => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'btn btn-outline-primary wordcloud-btn me-1 mb-2';
            btn.setAttribute('data-muscle', muscle);
            btn.textContent = muscle.charAt(0).toUpperCase() + muscle.slice(1);
            // Word cloud effect: random font size and weight
            const size = 1 + Math.random() * 1.2; // 1em to 2.2em
            btn.style.fontSize = size.toFixed(2) + 'em';
            btn.style.fontWeight = size > 1.7 ? 'bold' : 'normal';
            btn.onclick = function () { showExercises(muscle); };
            muscleListContainer.appendChild(btn);
        });
        // Always add 'Other' at the end if not present
        if (!allMuscleNames.includes('Other')) {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'btn btn-outline-secondary wordcloud-btn me-1 mb-2';
            btn.setAttribute('data-muscle', 'Other');
            btn.textContent = 'Other';
            btn.style.fontSize = '1.1em';
            btn.onclick = function () { showExercises('Other'); };
            muscleListContainer.appendChild(btn);
        }
    }

    // Call render on load
    renderMuscleGroupButtons();

    // Show exercises for a muscle group as a word cloud (same style as muscle group cloud, not a list)
    function showExercises(muscle) {
        muscleListContainer.style.display = 'none';
        exerciseListContainer.innerHTML = '';
        exerciseListContainer.style.display = 'flex';
        exerciseListContainer.style.flexWrap = 'wrap';
        exerciseListContainer.style.gap = '0.5em 0.5em';
        backToMusclesBtn.style.display = 'inline-block';
        (muscleGroups[muscle] || []).forEach(ex => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'btn btn-outline-success wordcloud-btn me-1 mb-2';
            btn.textContent = ex.name;
            // Word cloud effect: random font size and weight
            const size = 1 + Math.random() * 1.2; // 1em to 2.2em
            btn.style.fontSize = size.toFixed(2) + 'em';
            btn.style.fontWeight = size > 1.7 ? 'bold' : 'normal';
            btn.onclick = function () {
                // Set dropdown value and trigger change
                if (exerciseDropdown) {
                    exerciseDropdown.value = ex.exerciseTypeId;
                    exerciseDropdown.dispatchEvent(new Event('change'));
                }
                // Return to muscle group list
                exerciseListContainer.style.display = 'none';
                muscleListContainer.style.display = 'flex';
                backToMusclesBtn.style.display = 'none';
            };
            exerciseListContainer.appendChild(btn);
        });
    }

    // Show muscle group list
    function showMuscleGroups() {
        muscleListContainer.style.display = 'flex';
        exerciseListContainer.style.display = 'none';
        backToMusclesBtn.style.display = 'none';
    }

    // Attach click handlers to muscle group buttons
    if (muscleListContainer) {
        muscleListContainer.querySelectorAll('button[data-muscle]').forEach(btn => {
            btn.onclick = function () {
                showExercises(btn.getAttribute('data-muscle'));
            };
        });
    }
    if (backToMusclesBtn) {
        backToMusclesBtn.onclick = showMuscleGroups;
    }

    // Expose for debugging
    window.quickWorkoutDrilldown = { showMuscleGroups, showExercises };
});
