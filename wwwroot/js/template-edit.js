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
    const originalBtnText = submitBtn.innerHTML;
    submitBtn.disabled = true;
    submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Adding...';

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
        submitBtn.innerHTML = originalBtnText;
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
    
    if (sets.length === 0) {
        setsTableContainer.innerHTML = '<p class="text-muted">No sets defined for this exercise.</p>';
        return;
    }
    
    let html = `
        <table class="table table-sm">
            <thead>
                <tr>
                    <th>#</th>
                    <th>Type</th>
                    <th>Reps</th>
                    <th>Weight</th>
                    <th>Actions</th>
                </tr>
            </thead>
            <tbody>
    `;
    
    sets.forEach(set => {
        html += `
            <tr>
                <td>${escapeHtml(set.sequenceNum)}</td>
                <td>${escapeHtml(set.type)}</td>
                <td>${escapeHtml(set.reps)}</td>
                <td>${escapeHtml(set.weight)} kg</td>
                <td>
                    <div class="btn-group" role="group">
                        <button type="button" class="btn btn-sm btn-outline-primary" 
                                data-bs-toggle="collapse" data-bs-target="#editSet-${escapeHtml(set.id)}" 
                                aria-expanded="false" aria-controls="editSet-${escapeHtml(set.id)}">
                            <i class="bi bi-pencil"></i>
                        </button>
                        <form method="post" action="?handler=CloneSet" class="d-inline">
                            <input type="hidden" name="TemplateId" value="${escapeHtml(document.querySelector('[name="TemplateId"]').value)}" />
                            <input type="hidden" name="SetId" value="${escapeHtml(set.id)}" />
                            ${window.antiForgeryToken ? `<input type="hidden" name="__RequestVerificationToken" value="${escapeHtml(window.antiForgeryToken)}" />` : ''}
                            <button type="submit" class="btn btn-sm btn-outline-secondary" title="Clone this set">
                                <i class="bi bi-files"></i>
                            </button>
                        </form>
                        <form method="post" action="?handler=DeleteSet" class="d-inline">
                            <input type="hidden" name="TemplateId" value="${escapeHtml(document.querySelector('[name="TemplateId"]').value)}" />
                            <input type="hidden" name="SetId" value="${escapeHtml(set.id)}" />
                            ${window.antiForgeryToken ? `<input type="hidden" name="__RequestVerificationToken" value="${escapeHtml(window.antiForgeryToken)}" />` : ''}
                            <button type="submit" class="btn btn-sm btn-outline-danger">
                                <i class="bi bi-trash"></i>
                            </button>
                        </form>
                    </div>
                </td>
            </tr>
            <tr>
                <td colspan="5" class="p-0">
                    <div class="collapse" id="editSet-${escapeHtml(set.id)}">
                        <div class="card card-body border-primary m-2">
                            <h6 class="card-title">Edit Set</h6>
                            <form method="post" action="?handler=EditSet">
                                <input type="hidden" name="TemplateId" value="${escapeHtml(document.querySelector('[name="TemplateId"]').value)}" />
                                <input type="hidden" name="SetId" value="${escapeHtml(set.id)}" />
                                ${window.antiForgeryToken ? `<input type="hidden" name="__RequestVerificationToken" value="${escapeHtml(window.antiForgeryToken)}" />` : ''}
                                
                                <div class="mb-2">
                                    <label for="editSettypeId-${escapeHtml(set.id)}" class="form-label">Set Type</label>
                                    <select id="editSettypeId-${escapeHtml(set.id)}" name="SettypeId" class="form-select form-select-sm" required>
                                        ${getSetTypeOptions(set.settypeId)}
                                    </select>
                                </div>
                                
                                <div class="row mb-2">
                                    <div class="col">
                                        <label for="editDefaultReps-${escapeHtml(set.id)}" class="form-label">Reps</label>
                                        <input type="number" id="editDefaultReps-${escapeHtml(set.id)}" name="DefaultReps" 
                                               class="form-control form-control-sm" value="${escapeHtml(set.reps)}" min="0" required />
                                    </div>
                                    <div class="col">
                                        <label for="editDefaultWeight-${escapeHtml(set.id)}" class="form-label">Weight (kg)</label>
                                        <input type="number" id="editDefaultWeight-${escapeHtml(set.id)}" name="DefaultWeight" 
                                               class="form-control form-control-sm" value="${escapeHtml(set.weight)}" min="0" step="0.5" required />
                                    </div>
                                </div>
                                
                                <div class="mb-2">
                                    <label for="editSequenceNum-${escapeHtml(set.id)}" class="form-label">Sequence #</label>
                                    <input type="number" id="editSequenceNum-${escapeHtml(set.id)}" name="SequenceNum" 
                                           class="form-control form-control-sm" value="${escapeHtml(set.sequenceNum)}" min="1" required />
                                </div>
                                
                                <div class="mb-2">
                                    <label for="editDescription-${escapeHtml(set.id)}" class="form-label">Description</label>
                                    <input type="text" id="editDescription-${escapeHtml(set.id)}" name="Description" 
                                           class="form-control form-control-sm" value="${escapeHtml(set.description || '')}" />
                                </div>
                                
                                <div class="d-flex justify-content-end">
                                    <button type="button" class="btn btn-sm btn-secondary me-2" 
                                            data-bs-toggle="collapse" data-bs-target="#editSet-${escapeHtml(set.id)}">
                                        Cancel
                                    </button>
                                    <button type="submit" class="btn btn-sm btn-primary">Save Changes</button>
                                </div>
                            </form>
                        </div>
                    </div>
                </td>
            </tr>
        `;
    });
    
    html += `
            </tbody>
        </table>
    `;
    
    setsTableContainer.innerHTML = html;
}

/**
 * Get the HTML for set type options
 * @param {number} selectedId - The selected set type ID
 * @returns {string} HTML options for the set types
 */
function getSetTypeOptions(selectedId) {
    // Dynamically extract set types from the page
    const setTypeSelects = document.querySelectorAll('select[name="SettypeId"]');
    if (setTypeSelects.length === 0) return '';
    
    const options = Array.from(setTypeSelects[0].options);
    
    return options.map(option => {
        const isSelected = parseInt(option.value) === selectedId ? 'selected' : '';
        return `<option value="${option.value}" ${isSelected}>${option.text}</option>`;
    }).join('');
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
    
    // Create toast element
    const toastId = 'toast-' + Date.now();
    const toast = document.createElement('div');
    toast.id = toastId;
    toast.className = `toast align-items-center border-0 text-white bg-${type}`;
    toast.setAttribute('role', 'alert');
    toast.setAttribute('aria-live', 'assertive');
    toast.setAttribute('aria-atomic', 'true');
    
    toast.innerHTML = `
        <div class="d-flex">
            <div class="toast-body">
                <strong>${escapeHtml(title)}</strong>: ${escapeHtml(message)}
            </div>
            <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
        </div>
    `;
    
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
