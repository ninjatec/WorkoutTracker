/**
 * Responsive table enhancement for WorkoutTracker
 * Automatically transforms standard tables into mobile-friendly versions
 */
document.addEventListener('DOMContentLoaded', function() {
    // Apply responsive table transformations
    enhanceTablesForMobile();
    
    // Set up observers for dynamic content
    observeDynamicContent();
});

/**
 * Transforms standard tables into responsive mobile-friendly tables
 */
function enhanceTablesForMobile() {
    // Find all tables except those with specific classes to exclude
    const tables = document.querySelectorAll('table:not(.no-responsive):not(.dataTable)');
    
    tables.forEach(function(table) {
        // Add responsive class
        table.classList.add('table-responsive-stack');
        
        // Get all headers
        const headers = Array.from(table.querySelectorAll('thead th')).map(th => th.textContent.trim());
        
        // Process all rows in the tbody
        const rows = table.querySelectorAll('tbody tr');
        rows.forEach(function(row) {
            // Process each cell in the row
            const cells = row.querySelectorAll('td');
            cells.forEach(function(cell, index) {
                // Skip if we don't have a header for this column
                if (index >= headers.length) return;
                
                // Add data-title attribute for mobile view
                cell.setAttribute('data-title', headers[index]);
                
                // If this is an action column (contains buttons/links), add a special class
                if (cell.querySelector('a.btn') || cell.querySelector('button') || 
                    cell.querySelectorAll('a').length > 1) {
                    cell.classList.add('action-column');
                }
            });
        });
    });
    
    // Enhance DataTables if present
    enhanceDataTables();
}

/**
 * Enhance DataTables with mobile-friendly settings
 */
function enhanceDataTables() {
    // Check if DataTables is loaded
    if (typeof $.fn.dataTable !== 'undefined') {
        // Set default options for better mobile experience
        $.extend($.fn.dataTable.defaults, {
            responsive: true,
            autoWidth: false,
            language: {
                paginate: {
                    previous: '<i class="bi bi-chevron-left"></i>',
                    next: '<i class="bi bi-chevron-right"></i>'
                }
            },
            dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
                 '<"row"<"col-sm-12"tr>>' +
                 '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
        });
    }
}

/**
 * Set up observers to enhance dynamically loaded tables
 */
function observeDynamicContent() {
    // Set up a mutation observer to watch for new tables added to the DOM
    const observer = new MutationObserver(function(mutations) {
        let shouldEnhance = false;
        
        mutations.forEach(function(mutation) {
            // Check for new nodes
            mutation.addedNodes.forEach(function(node) {
                // Check if the node is an element and contains tables
                if (node.nodeType === 1) {
                    if (node.tagName === 'TABLE' || node.querySelector('table')) {
                        shouldEnhance = true;
                    }
                }
            });
        });
        
        // If we found tables, enhance them
        if (shouldEnhance) {
            enhanceTablesForMobile();
        }
    });
    
    // Start observing the body with the configured parameters
    observer.observe(document.body, { childList: true, subtree: true });
}