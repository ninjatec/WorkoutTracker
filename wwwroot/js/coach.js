/**
 * Coach Portal JavaScript Functionality
 * Provides interactive features for the coaching areas of the application
 */

$(document).ready(function() {
    // Initialize DataTables for any tables with the datatable class
    if ($.fn.DataTable) {
        $('table.datatable').DataTable({
            responsive: true,
            language: {
                search: "Filter:",
                lengthMenu: "Show _MENU_ entries per page",
                info: "Showing _START_ to _END_ of _TOTAL_ entries",
                paginate: {
                    first: "First",
                    last: "Last",
                    next: "Next",
                    previous: "Previous"
                }
            },
            dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
                 '<"row"<"col-sm-12"tr>>' +
                 '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
            stateSave: true
        });
    }

    // Template filters handling
    $('#templateSearch, #categoryFilter, #showPublicTemplates').on('input change', function() {
        filterTemplates();
    });

    // Assign template modal handling
    $('.assign-template-btn').on('click', function() {
        var templateId = $(this).data('template-id');
        var templateName = $(this).data('template-name');
        
        $('#templateId').val(templateId);
        $('#selectedTemplate').val(templateName);
        $('#name').val(templateName);
    });

    // Toggle scheduling options
    $('#scheduleWorkouts').on('change', function() {
        if ($(this).prop('checked')) {
            $('#schedulingOptions').removeClass('d-none');
        } else {
            $('#schedulingOptions').addClass('d-none');
        }
    });
    
    // Toggle recurrence pattern options
    $('input[name="recurrencePattern"]').on('change', function() {
        var pattern = $('input[name="recurrencePattern"]:checked').val();
        
        if (pattern === 'Weekly') {
            $('#weeklyDaysOptions').removeClass('d-none');
            $('#monthlyDayOptions').addClass('d-none');
        } else if (pattern === 'Monthly') {
            $('#weeklyDaysOptions').addClass('d-none');
            $('#monthlyDayOptions').removeClass('d-none');
        } else {
            $('#weeklyDaysOptions').addClass('d-none');
            $('#monthlyDayOptions').addClass('d-none');
        }
    });
    
    // Toggle reminder options
    $('#sendReminder').on('change', function() {
        if ($(this).prop('checked')) {
            $('#reminderOptions').removeClass('d-none');
        } else {
            $('#reminderOptions').addClass('d-none');
        }
    });

    // Exercise selection in workout templates
    $('.exercise-selector').on('change', function() {
        const exerciseId = $(this).val();
        const exerciseRow = $(this).closest('.exercise-row');
        
        if (exerciseId) {
            // Enable set controls
            exerciseRow.find('.set-controls').removeClass('d-none');
        } else {
            // Disable set controls if no exercise selected
            exerciseRow.find('.set-controls').addClass('d-none');
        }
    });

    // Schedule workout from template button
    $('.schedule-workout-btn').on('click', function(e) {
        e.preventDefault();
        var templateId = $(this).data('template-id');
        var templateName = $(this).data('template-name');
        
        // Redirect to the new Razor Page
        window.location.href = '/WorkoutSchedule?templateId=' + templateId + '&templateName=' + encodeURIComponent(templateName);
    });

    // Schedule workout from assigned template button
    $('.schedule-assigned-btn').on('click', function(e) {
        e.preventDefault();
        var assignmentId = $(this).data('assignment-id');
        
        // Redirect to the new Razor Page
        window.location.href = '/WorkoutSchedule?assignmentId=' + assignmentId;
    });

    // Dynamic form controls for template editing
    setupDynamicFormControls();
});

/**
 * Filters templates based on search criteria
 */
function filterTemplates() {
    const searchText = $('#templateSearch').val().toLowerCase();
    const selectedCategory = $('#categoryFilter').val();
    const showPublic = $('#showPublicTemplates').prop('checked');

    // Using the new Razor Page handler instead of API
    const requestData = {
        searchTerm: searchText,
        category: selectedCategory,
        includePublic: showPublic
    };

    // If we're on a page with AJAX template filtering
    if ($('#templatesTable').length > 0) {
        $.ajax({
            url: '/WorkoutTemplates/Index?handler=TemplatesJson',
            type: 'GET',
            data: requestData,
            headers: {
                "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
            },
            success: function(data) {
                updateTemplateTable(data);
            },
            error: function(xhr, status, error) {
                console.error('Error fetching templates:', error);
            }
        });
    } else {
        // Fallback to client-side filtering if not using AJAX
        clientSideTemplateFiltering(searchText, selectedCategory, showPublic);
    }
}

/**
 * Updates the template table with filtered data
 * @param {Array} templates - The filtered templates
 */
function updateTemplateTable(templates) {
    const tableBody = $('#templatesTable tbody');
    tableBody.empty();

    templates.forEach(function(template) {
        // Create visibility badge
        let visibilityBadge = '';
        if (template.isOwner) {
            visibilityBadge = '<span class="badge bg-primary">My Template</span>';
        } else if (template.isAssigned) {
            visibilityBadge = '<span class="badge bg-success">Assigned by ' + template.assignedBy + '</span>';
        } else if (template.isPublic) {
            visibilityBadge = '<span class="badge bg-info">Public Template</span>';
        }

        // Create action buttons
        let actionButtons = '<a href="/Templates/Details?id=' + template.workoutTemplateId + '" class="btn btn-sm btn-primary">View</a> ';
        if (template.isOwner) {
            actionButtons += '<a href="/Templates/Edit?id=' + template.workoutTemplateId + '" class="btn btn-sm btn-secondary">Edit</a> ';
        }
        actionButtons += '<a href="#" class="btn btn-sm btn-success schedule-workout-btn" data-template-id="' + template.workoutTemplateId + '" data-template-name="' + template.name + '">Schedule</a>';

        // Add the row
        tableBody.append(`
            <tr data-category="${template.category || ''}" data-public="${template.isPublic}">
                <td>${template.name}</td>
                <td>${template.description || ''}</td>
                <td>${template.category || ''}</td>
                <td>${template.exerciseCount}</td>
                <td>${visibilityBadge}</td>
                <td>${actionButtons}</td>
            </tr>
        `);
    });

    // Reinitialize event handlers for the newly added buttons
    $('.schedule-workout-btn').on('click', function(e) {
        e.preventDefault();
        var templateId = $(this).data('template-id');
        var templateName = $(this).data('template-name');
        
        window.location.href = '/WorkoutSchedule?templateId=' + templateId + '&templateName=' + encodeURIComponent(templateName);
    });
}

/**
 * Performs client-side filtering of templates (fallback method)
 */
function clientSideTemplateFiltering(searchText, selectedCategory, showPublic) {
    $('#templatesTable tbody tr').each(function() {
        const row = $(this);
        const name = row.find('td:first').text().toLowerCase();
        const category = row.data('category')?.toString().toLowerCase() || '';
        const isPublic = row.data('public') === 'true';
        
        const matchesSearch = !searchText || name.includes(searchText);
        const matchesCategory = !selectedCategory || category === selectedCategory.toLowerCase();
        const matchesVisibility = isPublic ? showPublic : true;
        
        if (matchesSearch && matchesCategory && matchesVisibility) {
            row.show();
        } else {
            row.hide();
        }
    });
}

/**
 * Sets up the dynamic form controls for template creation/editing
 */
function setupDynamicFormControls() {
    // Add exercise button
    $('#addExerciseBtn').on('click', function() {
        const exerciseCount = $('.exercise-container').length;
        const template = $('#exerciseTemplate').html();
        
        if (template) {
            const newExercise = template.replace(/\{index\}/g, exerciseCount);
            $('#exercisesContainer').append(newExercise);
            initExerciseControls($('#exercisesContainer .exercise-container').last());
        }
    });
    
    // Initialize any existing exercise rows
    $('.exercise-container').each(function() {
        initExerciseControls($(this));
    });
}

/**
 * Initializes controls for a specific exercise row
 * @param {jQuery} container - The exercise container element
 */
function initExerciseControls(container) {
    // Add set button
    container.find('.add-set-btn').on('click', function() {
        const setContainer = $(this).closest('.sets-container');
        const setTemplate = $('#setTemplate').html();
        
        if (setTemplate) {
            const exerciseIndex = $(this).closest('.exercise-container').data('index');
            const setCount = setContainer.find('.set-row').length;
            
            const newSet = setTemplate
                .replace(/\{exerciseIndex\}/g, exerciseIndex)
                .replace(/\{setIndex\}/g, setCount);
                
            setContainer.append(newSet);
        }
    });
    
    // Remove exercise button
    container.find('.remove-exercise-btn').on('click', function() {
        $(this).closest('.exercise-container').remove();
    });
    
    // Handle set type changes
    container.on('change', '.set-type-select', function() {
        const setRow = $(this).closest('.set-row');
        const setValue = setRow.find('.set-value');
        const setType = $(this).val();
        
        if (setType && setType.includes('Time')) {
            setValue.attr('placeholder', 'Duration (seconds)');
        } else {
            setValue.attr('placeholder', 'Reps');
        }
    });
    
    // Remove set button
    container.on('click', '.remove-set-btn', function() {
        $(this).closest('.set-row').remove();
    });
}