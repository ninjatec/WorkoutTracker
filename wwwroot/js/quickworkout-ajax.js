// Custom AJAX functionality for QuickWorkout page
$(document).ready(function() {
    // Constants
    const API_PATHS = {
        WORKOUT_SET: '/api/WorkoutSetsApi',
        SET_TYPES: '/api/WorkoutSetsApi/SettypeOptions',
        FINISH_WORKOUT: '/api/QuickWorkout/FinishSession'
    };

    // Cache DOM elements
    const $addSetForm = $('#addSetForm');
    const $finishWorkoutBtn = $('#finishWorkoutBtn');
    const $recentSetsContainer = $('#recentSetsContainer');
    const $alertContainer = $('#alertContainer');

    // Initialize
    initEventHandlers();
    loadSetTypes();

    function initEventHandlers() {
        // Add set form submission
        $addSetForm.on('submit', function(e) {
            e.preventDefault();
            addSet();
        });

        // Finish workout button
        $(document).on('click', '#finishWorkoutBtn', function(e) {
            e.preventDefault();
            const sessionId = $(this).data('session-id');
            finishWorkout(sessionId);
        });

        // Delete set button
        $(document).on('click', '.delete-set-btn', function() {
            const setId = $(this).data('set-id');
            $('#deleteSetId').val(setId);
            $('#confirmDeleteModal').modal('show');
        });

        // Confirm delete action
        $('#confirmDeleteBtn').click(function() {
            deleteSet($('#deleteSetId').val());
        });

        // Edit set button
        $(document).on('click', '.edit-set-btn', function() {
            const setId = $(this).data('set-id');
            loadSetDetails(setId);
        });

        // Save set changes button
        $('#saveSetChanges').click(function() {
            updateSet();
        });

        // Clone set button
        $(document).on('click', '.clone-set-btn', function() {
            const setId = $(this).data('set-id');
            cloneSet(setId);
        });
    }

    // Load set type options from API
    function loadSetTypes() {
        $.ajax({
            url: API_PATHS.SET_TYPES,
            method: 'GET',
            success: function(data) {
                populateSetTypeDropdowns(data);
            },
            error: function(xhr) {
                console.error('Error loading set types:', xhr);
                showAlert('Failed to load set types. Please refresh the page.', 'danger');
            }
        });
    }

    // Populate set type dropdowns
    function populateSetTypeDropdowns(data) {
        const $dropdowns = $('#editSetType, #setTypeDropdown');
        $dropdowns.each(function() {
            const $dropdown = $(this);
            $dropdown.empty();
            
            // Add default option
            $dropdown.append($('<option>', {
                value: '',
                text: 'Regular'
            }));
            
            // Add options from API
            data.forEach(option => {
                $dropdown.append($('<option>', {
                    value: option.id,
                    text: option.name
                }));
            });
        });
    }

    // Add a new set
    function addSet() {
        const formData = $addSetForm.serializeArray();
        let payload = {};
        
        // Convert form data to JSON payload
        formData.forEach(item => {
            const key = item.name.replace('QuickWorkout.', '');
            payload[key] = item.value;
        });
        
        // Format payload for API
        const apiPayload = {
            workoutExerciseId: parseInt(payload.workoutSessionId),
            exerciseTypeId: parseInt(payload.ExerciseTypeId),
            weight: parseFloat(payload.Weight) || 0,
            reps: parseInt(payload.NumberReps) || 0,
            settypeId: parseInt(payload.SettypeId) || null,
            isCompleted: true,
            timestamp: new Date().toISOString()
        };
        
        $.ajax({
            url: API_PATHS.WORKOUT_SET,
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(apiPayload),
            success: function(data) {
                showAlert('Set added successfully!', 'success');
                refreshRecentSets();
                resetForm();
            },
            error: function(xhr) {
                console.error('Error adding set:', xhr);
                showAlert('Failed to add set. Please try again.', 'danger');
            }
        });
    }

    // Refresh the recent sets container
    function refreshRecentSets() {
        // This would be better with a dedicated API endpoint
        // For now, we'll just reload the page
        location.reload();
    }

    // Reset the add set form
    function resetForm() {
        $addSetForm[0].reset();
    }

    // Load set details for editing
    function loadSetDetails(setId) {
        $.ajax({
            url: `${API_PATHS.WORKOUT_SET}/${setId}`,
            method: 'GET',
            success: function(data) {
                $('#editSetId').val(data.workoutSetId);
                $('#editExerciseId').val(data.workoutExerciseId);
                $('#editWeight').val(data.weight);
                $('#editReps').val(data.reps);
                $('#editNotes').val(data.notes);
                
                if (data.settypeId) {
                    $('#editSetType').val(data.settypeId);
                } else {
                    $('#editSetType').val('');
                }
                
                $('#editSetModal').modal('show');
            },
            error: function(xhr) {
                console.error('Error loading set details:', xhr);
                showAlert('Failed to load set details. Please try again.', 'danger');
            }
        });
    }

    // Update a set
    function updateSet() {
        const setId = $('#editSetId').val();
        const exerciseId = $('#editExerciseId').val();
        const weight = $('#editWeight').val();
        const reps = $('#editReps').val();
        const settypeId = $('#editSetType').val() || null;
        const notes = $('#editNotes').val();
        
        const payload = {
            workoutSetId: parseInt(setId),
            workoutExerciseId: parseInt(exerciseId),
            weight: weight ? parseFloat(weight) : null,
            reps: reps ? parseInt(reps) : null,
            settypeId: settypeId ? parseInt(settypeId) : null,
            notes: notes,
            isCompleted: true,
            timestamp: new Date().toISOString()
        };
        
        $.ajax({
            url: `${API_PATHS.WORKOUT_SET}/${setId}`,
            method: 'PUT',
            contentType: 'application/json',
            data: JSON.stringify(payload),
            success: function() {
                $('#editSetModal').modal('hide');
                showAlert('Set updated successfully!', 'success');
                refreshRecentSets();
            },
            error: function(xhr) {
                console.error('Error updating set:', xhr);
                showAlert('Failed to update set. Please try again.', 'danger');
            }
        });
    }

    // Delete a set
    function deleteSet(setId) {
        $.ajax({
            url: `${API_PATHS.WORKOUT_SET}/${setId}`,
            method: 'DELETE',
            success: function() {
                $('#confirmDeleteModal').modal('hide');
                showAlert('Set deleted successfully!', 'success');
                refreshRecentSets();
            },
            error: function(xhr) {
                console.error('Error deleting set:', xhr);
                $('#confirmDeleteModal').modal('hide');
                showAlert('Failed to delete set. Please try again.', 'danger');
            }
        });
    }

    // Clone a set
    function cloneSet(setId) {
        $.ajax({
            url: `${API_PATHS.WORKOUT_SET}/${setId}`,
            method: 'GET',
            success: function(data) {
                // Create a new payload without the ID
                const payload = {
                    workoutExerciseId: data.workoutExerciseId,
                    weight: data.weight,
                    reps: data.reps,
                    settypeId: data.settypeId,
                    notes: data.notes ? `${data.notes} (copy)` : '(copy)',
                    isCompleted: true,
                    timestamp: new Date().toISOString()
                };
                
                // Send POST request to create a clone
                $.ajax({
                    url: API_PATHS.WORKOUT_SET,
                    method: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(payload),
                    success: function() {
                        showAlert('Set cloned successfully!', 'success');
                        refreshRecentSets();
                    },
                    error: function(xhr) {
                        console.error('Error cloning set:', xhr);
                        showAlert('Failed to clone set. Please try again.', 'danger');
                    }
                });
            },
            error: function(xhr) {
                console.error('Error fetching set details:', xhr);
                showAlert('Failed to fetch set details for cloning. Please try again.', 'danger');
            }
        });
    }

    // Finish a workout
    function finishWorkout(sessionId) {
        $.ajax({
            url: `${API_PATHS.FINISH_WORKOUT}/${sessionId}`,
            method: 'POST',
            success: function() {
                showAlert('Workout finished successfully!', 'success');
                // Reload the page after a short delay
                setTimeout(() => {
                    location.reload();
                }, 1500);
            },
            error: function(xhr) {
                console.error('Error finishing workout:', xhr);
                showAlert('Failed to finish workout. Please try again.', 'danger');
            }
        });
    }

    // Show an alert message
    function showAlert(message, type = 'info') {
        const alert = `
            <div class="alert alert-${type} alert-dismissible fade show" role="alert">
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
            </div>
        `;
        
        $alertContainer.html(alert);
        
        // Auto dismiss after 5 seconds
        setTimeout(() => {
            $('.alert').alert('close');
        }, 5000);
    }
});
