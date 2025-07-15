/**
 * Script for handling Template edit functionality
 */

/**
 * Escape HTML characters to prevent XSS attacks
 * @param {string} text - The text to escape
 * @returns {string} The escaped text
 */
function escapeHtml(text) {
    if (typeof text !== 'string') {
        return String(text);
    }
    
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

document.addEventListener('DOMContentLoaded', function () {
    // Set up event handlers for all set add forms
    setupSetFormHandlers();
    
    // Store the CSRF token for later use with dynamic forms
    window.antiForgeryToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
});

/**
 * Set up event handlers for all set add forms
 */
function setupSetFormHandlers() {
    // Find all set add forms by their class
    const setForms = document.querySelectorAll('.set-add-form');
    
    setForms.forEach(form => {
        form.addEventListener('submit', handleSetFormSubmit);
    });
}

/**
 * Handle the set form submission via AJAX
 * @param {Event} e - The submit event
 */
function handleSetFormSubmit(e) {
    e.preventDefault();
    
    const form = e.target;
    const exerciseId = form.querySelector('input[name="ExerciseId"]').value;
    const url = form.getAttribute('action');
    const formData = new FormData(form);
    
    // Show loading indicator
    const submitBtn = form.querySelector('button[type="submit"]');
    const originalBtnContent = Array.from(submitBtn.childNodes);
    submitBtn.disabled = true;
    
    // Clear button and add spinner
    submitBtn.textContent = '';
    const spinner = document.createElement('span');
    spinner.className = 'spinner-border spinner-border-sm';
    spinner.setAttribute('role', 'status');
    spinner.setAttribute('aria-hidden', 'true');
    submitBtn.appendChild(spinner);
    submitBtn.appendChild(document.createTextNode(' Adding...'));

    fetch(url, {
        method: 'POST',
        body: formData,
        headers: {
            'X-Requested-With': 'XMLHttpRequest',
            'RequestVerificationToken': window.antiForgeryToken || ''
        }
    })
    .then(response => {
        if (!response.ok) {
            throw new Error('Network response was not ok');
        }
        return response.json();
    })
    .then(data => {
        if (data.success) {
            // Update the sets table
            updateSetsTable(exerciseId, data.sets);
            
            // Reset form fields
            form.querySelector('select[name="SettypeId"]').selectedIndex = 0;
            form.querySelector('input[name="DefaultReps"]').value = '8';
            form.querySelector('input[name="DefaultWeight"]').value = '0';
            form.querySelector('input[name="Description"]').value = '';
            
            // Update sequence number field
            const sequenceNumInput = form.querySelector('input[name="SequenceNum"]');
            sequenceNumInput.value = parseInt(sequenceNumInput.value) + 1;
            
            // Show success message
            showToast('Success', 'Set added successfully!', 'success');
        } else {
            showToast('Error', data.message || 'Failed to add set', 'danger');
        }
    })
    .catch(error => {
        console.error('Error:', error);
        showToast('Error', 'Failed to add set. Check console for details.', 'danger');
    })
    .finally(() => {
        // Reset button state
        submitBtn.disabled = false;
        submitBtn.textContent = '';
        originalBtnContent.forEach(node => {
            submitBtn.appendChild(node.cloneNode(true));
        });
    });
}

/**
 * Update the sets table with the new data
 * @param {string} exerciseId - The exercise ID
 * @param {Array} sets - The sets data
 */
function updateSetsTable(exerciseId, sets) {
    const setsTableContainer = document.getElementById('sets-table-' + exerciseId);
    if (!setsTableContainer) return;
    
    // Clear existing content
    setsTableContainer.textContent = '';
    
    if (sets.length === 0) {
        const noSetsMsg = document.createElement('p');
        noSetsMsg.className = 'text-muted';
        noSetsMsg.textContent = 'No sets defined for this exercise.';
        setsTableContainer.appendChild(noSetsMsg);
        return;
    }
    
    // Create table structure using DOM methods
    const table = document.createElement('table');
    table.className = 'table table-sm';
    
    // Create table header
    const thead = document.createElement('thead');
    const headerRow = document.createElement('tr');
    
    const headers = ['#', 'Type', 'Reps', 'Weight', 'Actions'];
    headers.forEach(headerText => {
        const th = document.createElement('th');
        th.textContent = headerText;
        headerRow.appendChild(th);
    });
    
    thead.appendChild(headerRow);
    table.appendChild(thead);
    
    // Create table body
    const tbody = document.createElement('tbody');
    
    sets.forEach(set => {
        // Create main row
        const mainRow = document.createElement('tr');
        
        // Sequence number
        const seqCell = document.createElement('td');
        seqCell.textContent = set.sequenceNum;
        mainRow.appendChild(seqCell);
        
        // Type
        const typeCell = document.createElement('td');
        typeCell.textContent = set.type;
        mainRow.appendChild(typeCell);
        
        // Reps
        const repsCell = document.createElement('td');
        repsCell.textContent = set.reps;
        mainRow.appendChild(repsCell);
        
        // Weight
        const weightCell = document.createElement('td');
        weightCell.textContent = set.weight + ' kg';
        mainRow.appendChild(weightCell);
        
        // Actions
        const actionsCell = document.createElement('td');
        actionsCell.appendChild(createActionsButtonGroup(set));
        mainRow.appendChild(actionsCell);
        
        tbody.appendChild(mainRow);
        
        // Create edit row
        const editRow = document.createElement('tr');
        const editCell = document.createElement('td');
        editCell.colSpan = 5;
        editCell.className = 'p-0';
        editCell.appendChild(createEditForm(set));
        editRow.appendChild(editCell);
        
        tbody.appendChild(editRow);
    });
    
    table.appendChild(tbody);
    setsTableContainer.appendChild(table);
}

/**
 * Create the actions button group for a set
 * @param {Object} set - The set data
 * @returns {HTMLElement} The button group element
 */
function createActionsButtonGroup(set) {
    const buttonGroup = document.createElement('div');
    buttonGroup.className = 'btn-group';
    buttonGroup.setAttribute('role', 'group');
    
    // Edit button
    const editBtn = document.createElement('button');
    editBtn.type = 'button';
    editBtn.className = 'btn btn-sm btn-outline-primary';
    editBtn.setAttribute('data-bs-toggle', 'collapse');
    editBtn.setAttribute('data-bs-target', '#editSet-' + set.id);
    editBtn.setAttribute('aria-expanded', 'false');
    editBtn.setAttribute('aria-controls', 'editSet-' + set.id);
    
    const editIcon = document.createElement('i');
    editIcon.className = 'bi bi-pencil';
    editBtn.appendChild(editIcon);
    
    buttonGroup.appendChild(editBtn);
    
    // Clone form
    const cloneForm = createCloneForm(set);
    buttonGroup.appendChild(cloneForm);
    
    // Delete form
    const deleteForm = createDeleteForm(set);
    buttonGroup.appendChild(deleteForm);
    
    return buttonGroup;
}

/**
 * Create the clone form for a set
 * @param {Object} set - The set data
 * @returns {HTMLFormElement} The clone form element
 */
function createCloneForm(set) {
    const form = document.createElement('form');
    form.method = 'post';
    form.action = '?handler=CloneSet';
    form.className = 'd-inline';
    
    // Template ID hidden field
    const templateIdInput = document.createElement('input');
    templateIdInput.type = 'hidden';
    templateIdInput.name = 'TemplateId';
    templateIdInput.value = document.querySelector('[name="TemplateId"]').value;
    form.appendChild(templateIdInput);
    
    // Set ID hidden field
    const setIdInput = document.createElement('input');
    setIdInput.type = 'hidden';
    setIdInput.name = 'SetId';
    setIdInput.value = set.id;
    form.appendChild(setIdInput);
    
    // Anti-forgery token
    if (window.antiForgeryToken) {
        const tokenInput = document.createElement('input');
        tokenInput.type = 'hidden';
        tokenInput.name = '__RequestVerificationToken';
        tokenInput.value = window.antiForgeryToken;
        form.appendChild(tokenInput);
    }
    
    // Submit button
    const submitBtn = document.createElement('button');
    submitBtn.type = 'submit';
    submitBtn.className = 'btn btn-sm btn-outline-secondary';
    submitBtn.title = 'Clone this set';
    
    const icon = document.createElement('i');
    icon.className = 'bi bi-files';
    submitBtn.appendChild(icon);
    
    form.appendChild(submitBtn);
    
    return form;
}

/**
 * Create the delete form for a set
 * @param {Object} set - The set data
 * @returns {HTMLFormElement} The delete form element
 */
function createDeleteForm(set) {
    const form = document.createElement('form');
    form.method = 'post';
    form.action = '?handler=DeleteSet';
    form.className = 'd-inline';
    
    // Template ID hidden field
    const templateIdInput = document.createElement('input');
    templateIdInput.type = 'hidden';
    templateIdInput.name = 'TemplateId';
    templateIdInput.value = document.querySelector('[name="TemplateId"]').value;
    form.appendChild(templateIdInput);
    
    // Set ID hidden field
    const setIdInput = document.createElement('input');
    setIdInput.type = 'hidden';
    setIdInput.name = 'SetId';
    setIdInput.value = set.id;
    form.appendChild(setIdInput);
    
    // Anti-forgery token
    if (window.antiForgeryToken) {
        const tokenInput = document.createElement('input');
        tokenInput.type = 'hidden';
        tokenInput.name = '__RequestVerificationToken';
        tokenInput.value = window.antiForgeryToken;
        form.appendChild(tokenInput);
    }
    
    // Submit button
    const submitBtn = document.createElement('button');
    submitBtn.type = 'submit';
    submitBtn.className = 'btn btn-sm btn-outline-danger';
    
    const icon = document.createElement('i');
    icon.className = 'bi bi-trash';
    submitBtn.appendChild(icon);
    
    form.appendChild(submitBtn);
    
    return form;
}

/**
 * Create the edit form for a set
 * @param {Object} set - The set data
 * @returns {HTMLElement} The edit form container
 */
function createEditForm(set) {
    const collapseDiv = document.createElement('div');
    collapseDiv.className = 'collapse';
    collapseDiv.id = 'editSet-' + set.id;
    
    const cardDiv = document.createElement('div');
    cardDiv.className = 'card card-body border-primary m-2';
    
    const cardTitle = document.createElement('h6');
    cardTitle.className = 'card-title';
    cardTitle.textContent = 'Edit Set';
    cardDiv.appendChild(cardTitle);
    
    const form = document.createElement('form');
    form.method = 'post';
    form.action = '?handler=EditSet';
    
    // Hidden fields
    const templateIdInput = document.createElement('input');
    templateIdInput.type = 'hidden';
    templateIdInput.name = 'TemplateId';
    templateIdInput.value = document.querySelector('[name="TemplateId"]').value;
    form.appendChild(templateIdInput);
    
    const setIdInput = document.createElement('input');
    setIdInput.type = 'hidden';
    setIdInput.name = 'SetId';
    setIdInput.value = set.id;
    form.appendChild(setIdInput);
    
    if (window.antiForgeryToken) {
        const tokenInput = document.createElement('input');
        tokenInput.type = 'hidden';
        tokenInput.name = '__RequestVerificationToken';
        tokenInput.value = window.antiForgeryToken;
        form.appendChild(tokenInput);
    }
    
    // Set Type field
    const setTypeDiv = document.createElement('div');
    setTypeDiv.className = 'mb-2';
    
    const setTypeLabel = document.createElement('label');
    setTypeLabel.htmlFor = 'editSettypeId-' + set.id;
    setTypeLabel.className = 'form-label';
    setTypeLabel.textContent = 'Set Type';
    setTypeDiv.appendChild(setTypeLabel);
    
    const setTypeSelect = document.createElement('select');
    setTypeSelect.id = 'editSettypeId-' + set.id;
    setTypeSelect.name = 'SettypeId';
    setTypeSelect.className = 'form-select form-select-sm';
    setTypeSelect.required = true;
    populateSetTypeOptions(setTypeSelect, set.settypeId);
    setTypeDiv.appendChild(setTypeSelect);
    
    form.appendChild(setTypeDiv);
    
    // Reps and Weight row
    const rowDiv = document.createElement('div');
    rowDiv.className = 'row mb-2';
    
    // Reps field
    const repsColDiv = document.createElement('div');
    repsColDiv.className = 'col';
    
    const repsLabel = document.createElement('label');
    repsLabel.htmlFor = 'editDefaultReps-' + set.id;
    repsLabel.className = 'form-label';
    repsLabel.textContent = 'Reps';
    repsColDiv.appendChild(repsLabel);
    
    const repsInput = document.createElement('input');
    repsInput.type = 'number';
    repsInput.id = 'editDefaultReps-' + set.id;
    repsInput.name = 'DefaultReps';
    repsInput.className = 'form-control form-control-sm';
    repsInput.value = set.reps;
    repsInput.min = '0';
    repsInput.required = true;
    repsColDiv.appendChild(repsInput);
    
    rowDiv.appendChild(repsColDiv);
    
    // Weight field
    const weightColDiv = document.createElement('div');
    weightColDiv.className = 'col';
    
    const weightLabel = document.createElement('label');
    weightLabel.htmlFor = 'editDefaultWeight-' + set.id;
    weightLabel.className = 'form-label';
    weightLabel.textContent = 'Weight (kg)';
    weightColDiv.appendChild(weightLabel);
    
    const weightInput = document.createElement('input');
    weightInput.type = 'number';
    weightInput.id = 'editDefaultWeight-' + set.id;
    weightInput.name = 'DefaultWeight';
    weightInput.className = 'form-control form-control-sm';
    weightInput.value = set.weight;
    weightInput.min = '0';
    weightInput.step = '0.5';
    weightInput.required = true;
    weightColDiv.appendChild(weightInput);
    
    rowDiv.appendChild(weightColDiv);
    form.appendChild(rowDiv);
    
    // Sequence field
    const sequenceDiv = document.createElement('div');
    sequenceDiv.className = 'mb-2';
    
    const sequenceLabel = document.createElement('label');
    sequenceLabel.htmlFor = 'editSequenceNum-' + set.id;
    sequenceLabel.className = 'form-label';
    sequenceLabel.textContent = 'Sequence #';
    sequenceDiv.appendChild(sequenceLabel);
    
    const sequenceInput = document.createElement('input');
    sequenceInput.type = 'number';
    sequenceInput.id = 'editSequenceNum-' + set.id;
    sequenceInput.name = 'SequenceNum';
    sequenceInput.className = 'form-control form-control-sm';
    sequenceInput.value = set.sequenceNum;
    sequenceInput.min = '1';
    sequenceInput.required = true;
    sequenceDiv.appendChild(sequenceInput);
    
    form.appendChild(sequenceDiv);
    
    // Description field
    const descDiv = document.createElement('div');
    descDiv.className = 'mb-2';
    
    const descLabel = document.createElement('label');
    descLabel.htmlFor = 'editDescription-' + set.id;
    descLabel.className = 'form-label';
    descLabel.textContent = 'Description';
    descDiv.appendChild(descLabel);
    
    const descInput = document.createElement('input');
    descInput.type = 'text';
    descInput.id = 'editDescription-' + set.id;
    descInput.name = 'Description';
    descInput.className = 'form-control form-control-sm';
    descInput.value = set.description || '';
    descDiv.appendChild(descInput);
    
    form.appendChild(descDiv);
    
    // Buttons
    const buttonsDiv = document.createElement('div');
    buttonsDiv.className = 'd-flex justify-content-end';
    
    const cancelBtn = document.createElement('button');
    cancelBtn.type = 'button';
    cancelBtn.className = 'btn btn-sm btn-secondary me-2';
    cancelBtn.setAttribute('data-bs-toggle', 'collapse');
    cancelBtn.setAttribute('data-bs-target', '#editSet-' + set.id);
    cancelBtn.textContent = 'Cancel';
    buttonsDiv.appendChild(cancelBtn);
    
    const saveBtn = document.createElement('button');
    saveBtn.type = 'submit';
    saveBtn.className = 'btn btn-sm btn-primary';
    saveBtn.textContent = 'Save Changes';
    buttonsDiv.appendChild(saveBtn);
    
    form.appendChild(buttonsDiv);
    cardDiv.appendChild(form);
    collapseDiv.appendChild(cardDiv);
    
    return collapseDiv;
}

/**
 * Populate set type options for a select element
 * @param {HTMLSelectElement} selectElement - The select element to populate
 * @param {number} selectedId - The selected set type ID
 */
function populateSetTypeOptions(selectElement, selectedId) {
    // Clear existing options
    selectElement.innerHTML = '';
    
    // Dynamically extract set types from the page
    const setTypeSelects = document.querySelectorAll('select[name="SettypeId"]');
    if (setTypeSelects.length === 0) return;
    
    const sourceOptions = Array.from(setTypeSelects[0].options);
    
    sourceOptions.forEach(sourceOption => {
        const option = document.createElement('option');
        option.value = sourceOption.value;
        option.textContent = sourceOption.text;
        
        if (parseInt(sourceOption.value) === selectedId) {
            option.selected = true;
        }
        
        selectElement.appendChild(option);
    });
}

/**
 * Display a toast notification
 * @param {string} title - The toast title
 * @param {string} message - The toast message
 * @param {string} type - The type of toast (success, danger, warning, info)
 */
function showToast(title, message, type = 'info') {
    // Create toast container if it doesn't exist
    let toastContainer = document.getElementById('toast-container');
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.id = 'toast-container';
        toastContainer.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(toastContainer);
    }
    
    // Validate and sanitize the type parameter to prevent XSS
    const validTypes = ['primary', 'secondary', 'success', 'danger', 'warning', 'info', 'light', 'dark'];
    const safeType = validTypes.includes(type) ? type : 'info';
    
    // Create toast element using DOM methods
    const toastId = 'toast-' + Date.now();
    const toast = document.createElement('div');
    toast.id = toastId;
    toast.className = `toast align-items-center border-0 text-white bg-${safeType}`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    
    // Create toast content using DOM methods
    const flexDiv = document.createElement('div');
    flexDiv.className = 'd-flex';
    
    const toastBody = document.createElement('div');
    toastBody.className = 'toast-body';
    
    const titleStrong = document.createElement('strong');
    titleStrong.textContent = title;
    toastBody.appendChild(titleStrong);
    
    toastBody.appendChild(document.createTextNode(': ' + message));
    
    const closeBtn = document.createElement('button');
    closeBtn.type = 'button';
    closeBtn.className = 'btn-close btn-close-white me-2 m-auto';
    closeBtn.setAttribute('data-bs-dismiss', 'toast');
    closeBtn.setAttribute('aria-label', 'Close');
    
    flexDiv.appendChild(toastBody);
    flexDiv.appendChild(closeBtn);
    toast.appendChild(flexDiv);
    
    toastContainer.appendChild(toast);
    
    // Initialize and show the toast
    const bsToast = new bootstrap.Toast(toast, {
        animation: true,
        autohide: true,
        delay: 3000
    });
    bsToast.show();
    
    // Remove the toast after it's hidden
    toast.addEventListener('hidden.bs.toast', function () {
        toast.remove();
    });
}
