// Initialize Intersection Observer for lazy loading
document.addEventListener('DOMContentLoaded', function() {
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const container = entry.target;
                if (container.dataset.loading === 'true') {
                    initializeChart(container.id);
                    container.dataset.loading = 'false';
                }
            }
        });
    }, {
        rootMargin: '50px'
    });

    // Observe chart containers
    ['weightProgressContainer', 'overallChartContainer', 'exerciseChartContainer'].forEach(id => {
        const container = document.getElementById(id);
        if (container) {
            observer.observe(container);
        }
    });
});

function initializeChart(containerId) {
    switch(containerId) {
        case 'weightProgressContainer':
            createWeightProgressChart();
            break;
        case 'overallChartContainer':
            createOverallChart();
            break;
        case 'exerciseChartContainer':
            createExerciseChart();
            break;
    }
}

// Chart creation functions will be set by the page that uses this script