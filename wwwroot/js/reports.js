// Reports page chart loading with IntersectionObserver for lazy loading
document.addEventListener('DOMContentLoaded', function() {
    // Store global charts for reference
    window.charts = {};
    window.chartContainers = {};

    // Initialize containers for all charts
    window.chartContainers.weightProgress = document.getElementById('weightProgressContainer');
    window.chartContainers.overall = document.getElementById('overallChartContainer');
    window.chartContainers.exercise = document.getElementById('exerciseChartContainer');
    window.chartContainers.exerciseStatus = document.getElementById('exerciseStatusContainer');

    // Create IntersectionObserver for lazy loading charts
    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const container = entry.target;
                if (container && container.dataset.loading === 'true') {
                    initializeChart(container.id);
                    container.dataset.loading = 'false';
                }
            }
        });
    }, {
        rootMargin: '50px'
    });

    // Observe chart containers
    Object.values(window.chartContainers).forEach(container => {
        if (container) {
            observer.observe(container);
        }
    });

    // For backward compatibility - define the functions that might be called elsewhere
    window.createWeightProgressChart = function() {
        loadWeightProgressChart(document.getElementById('period')?.value || 90);
    };
    
    window.createExerciseChart = function() {
        // This will be triggered by loadExerciseStatusData
    };
});

function initializeChart(containerId) {
    const period = document.getElementById('period')?.value || 90;
    
    switch(containerId) {
        case 'weightProgressContainer':
            loadWeightProgressChart(period);
            break;
        case 'overallChartContainer':
            // This will be loaded when exercise status is loaded
            break;
        case 'exerciseStatusContainer':
            loadExerciseStatusData(period);
            break;
        case 'exerciseChartContainer':
            // This will be loaded when exercise status is loaded
            break;
    }
}

// Helper function to show loading spinner
function showSpinner(container) {
    if (!container) return;
    const spinner = container.querySelector('.loading-spinner');
    if (spinner) {
        spinner.style.display = 'block';
    }
}

// Helper function to hide loading spinner
function hideSpinner(container) {
    if (!container) return;
    const spinner = container.querySelector('.loading-spinner');
    if (spinner) {
        spinner.style.display = 'none';
    }
}

// Async functions to load data and create charts
async function loadWeightProgressChart(period) {
    const container = window.chartContainers.weightProgress;
    if (!container) return;
    
    const canvas = document.getElementById('weightProgressChart');
    if (!canvas) return;
    
    showSpinner(container);
    
    try {
        // Fetch data from API
        const response = await fetch(`/api/ReportsApi/weight-progress?days=${period}&limit=5`);
        if (!response.ok) {
            throw new Error('Failed to fetch weight progress data');
        }
        
        const data = await response.json();
        
        // Prepare chart datasets
        const datasets = [];
        const colors = [
            '#4CAF50', '#2196F3', '#F44336', '#FF9800', '#9C27B0', 
            '#795548', '#607D8B', '#E91E63', '#FFEB3B', '#009688'
        ];

        if (data && Array.isArray(data)) {
            data.forEach((exercise, index) => {
                if (!exercise || !exercise.progressPoints) return;
                
                const colorIndex = index % colors.length;
                const points = exercise.progressPoints.map(point => ({
                    x: point.date,
                    y: point.weight
                }));
                
                datasets.push({
                    label: exercise.exerciseName,
                    data: points,
                    borderColor: colors[colorIndex],
                    backgroundColor: colors[colorIndex] + '33',
                    fill: false,
                    borderWidth: 2,
                    tension: 0.1,
                    pointRadius: 4
                });
            });
        }

        // Create chart
        if (window.charts.weightProgress) {
            window.charts.weightProgress.destroy();
        }
        
        window.charts.weightProgress = new Chart(canvas, {
            type: 'line',
            data: { datasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: {
                    duration: 0
                },
                interaction: {
                    mode: 'nearest',
                    axis: 'x',
                    intersect: false
                },
                scales: {
                    x: {
                        type: 'time',
                        time: {
                            unit: 'day',
                            tooltipFormat: 'MMM D, YYYY',
                            displayFormats: {
                                day: 'MMM D'
                            }
                        },
                        title: {
                            display: true,
                            text: 'Date'
                        }
                    },
                    y: {
                        title: {
                            display: true,
                            text: 'Weight (kg)'
                        },
                        beginAtZero: true
                    }
                },
                plugins: {
                    tooltip: {
                        enabled: true,
                        mode: 'nearest',
                        intersect: false,
                        callbacks: {
                            title: function(context) { 
                                return context[0] && context[0].raw && context[0].raw.x ? 
                                    moment(context[0].raw.x).format('MMMM D, YYYY') : '';
                            },
                            label: function(context) { 
                                return context.dataset.label + ': ' + context.raw.y + ' kg'; 
                            }
                        }
                    },
                    legend: {
                        position: 'top',
                        labels: {
                            boxWidth: 12,
                            usePointStyle: true
                        }
                    }
                }
            }
        });
        
        hideSpinner(container);
        canvas.style.display = 'block';
        
    } catch (error) {
        console.error('Error loading weight progress chart:', error);
        hideSpinner(container);
        
        // Show error message
        const errorMsg = document.createElement('div');
        errorMsg.className = 'alert alert-danger';
        errorMsg.textContent = 'Failed to load weight progress data. Please try again later.';
        canvas.parentNode.appendChild(errorMsg);
    }
}

async function loadExerciseStatusData(period) {
    const container = window.chartContainers.exerciseStatus;
    if (!container) return;
    
    const tableBody = document.getElementById('exerciseStatusTable');
    const contentDiv = document.getElementById('exerciseStatusContent');
    
    if (!tableBody || !contentDiv) return;
    
    showSpinner(container);
    
    try {
        // Fetch data from API
        const response = await fetch(`/api/ReportsApi/exercise-status?days=${period}&limit=20`);
        if (!response.ok) {
            throw new Error('Failed to fetch exercise status data');
        }
        
        const data = await response.json();
        
        // Clear existing table rows
        tableBody.innerHTML = '';
        
        if (!data || !data.allExercises || data.allExercises.length === 0) {
            const row = document.createElement('tr');
            row.innerHTML = '<td colspan="4" class="text-center">No exercise data available.</td>';
            tableBody.appendChild(row);
        } else {
            // Create table rows
            data.allExercises.forEach(exercise => {
                const total = exercise.successReps + exercise.failedReps;
                const successRate = total > 0 ? (exercise.successReps / total * 100).toFixed(1) : '0.0';
                
                const row = document.createElement('tr');
                row.innerHTML = `
                    <td>${exercise.exerciseName}</td>
                    <td>${exercise.successReps}</td>
                    <td>${exercise.failedReps}</td>
                    <td>${successRate}%</td>
                `;
                tableBody.appendChild(row);
            });
        }
        
        // Now load the exercise chart with the same data
        if (data) {
            loadExerciseChart(data);
            
            // Create overall chart with the summary data
            createOverallChart(data.totalSuccess || 0, data.totalFailed || 0);
        }
        
        hideSpinner(container);
        contentDiv.style.display = 'block';
        
    } catch (error) {
        console.error('Error loading exercise status data:', error);
        hideSpinner(container);
        
        // Show error message
        const errorMsg = document.createElement('div');
        errorMsg.className = 'alert alert-danger';
        errorMsg.textContent = 'Failed to load exercise status data. Please try again later.';
        contentDiv.parentNode.appendChild(errorMsg);
    }
}

function createOverallChart(successReps, failedReps) {
    const container = window.chartContainers.overall;
    if (!container) return;
    
    const canvas = document.getElementById('overallChart');
    if (!canvas) return;
    
    showSpinner(container);
    
    try {
        // Destroy existing chart if it exists
        if (window.charts.overall) {
            window.charts.overall.destroy();
        }
        
        window.charts.overall = new Chart(canvas, {
            type: 'doughnut',
            data: {
                labels: ['Successful Reps', 'Failed Reps'],
                datasets: [{
                    data: [successReps, failedReps],
                    backgroundColor: ['#4caf50', '#f44336'],
                    hoverBackgroundColor: ['#45a049', '#e53935']
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: {
                    duration: 0
                },
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            boxWidth: 12,
                            usePointStyle: true
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                var value = context.raw;
                                var total = successReps + failedReps;
                                var percentage = total > 0 ? Math.round((value / total) * 100) : 0;
                                return context.label + ': ' + value + ' (' + percentage + '%)';
                            }
                        }
                    }
                }
            }
        });
        
        hideSpinner(container);
        canvas.style.display = 'block';
        
    } catch (error) {
        console.error('Error creating overall chart:', error);
        hideSpinner(container);
        
        // Show error message
        const errorMsg = document.createElement('div');
        errorMsg.className = 'alert alert-danger';
        errorMsg.textContent = 'Failed to load chart. Please try again later.';
        canvas.parentNode.appendChild(errorMsg);
    }
}

function loadExerciseChart(data) {
    const container = window.chartContainers.exercise;
    if (!container) return;
    
    const canvas = document.getElementById('exerciseChart');
    if (!canvas) return;
    
    showSpinner(container);
    
    try {
        // Use only top exercises for the chart
        const topExercises = data && data.topExercises ? data.topExercises : [];
        
        // Destroy existing chart if it exists
        if (window.charts.exercise) {
            window.charts.exercise.destroy();
        }
        
        if (topExercises.length === 0) {
            // No data available, create empty chart
            window.charts.exercise = new Chart(canvas, {
                type: 'bar',
                data: {
                    labels: ['No Data Available'],
                    datasets: [
                        {
                            label: 'Successful Reps',
                            data: [0],
                            backgroundColor: '#4caf50',
                        },
                        {
                            label: 'Failed Reps',
                            data: [0],
                            backgroundColor: '#f44336',
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        x: { stacked: true },
                        y: { stacked: true, beginAtZero: true }
                    }
                }
            });
        } else {
            // Create chart with real data
            const exerciseNames = topExercises.map(e => e.exerciseName);
            const successData = topExercises.map(e => e.successReps);
            const failedData = topExercises.map(e => e.failedReps);
            
            window.charts.exercise = new Chart(canvas, {
                type: 'bar',
                data: {
                    labels: exerciseNames,
                    datasets: [
                        {
                            label: 'Successful Reps',
                            data: successData,
                            backgroundColor: '#4caf50',
                        },
                        {
                            label: 'Failed Reps',
                            data: failedData,
                            backgroundColor: '#f44336',
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    animation: {
                        duration: 0
                    },
                    scales: {
                        x: {
                            stacked: true,
                            ticks: {
                                maxRotation: 45,
                                minRotation: 45
                            }
                        },
                        y: {
                            stacked: true,
                            beginAtZero: true
                        }
                    },
                    plugins: {
                        legend: {
                            position: 'top',
                            labels: {
                                boxWidth: 12,
                                usePointStyle: true
                            }
                        }
                    }
                }
            });
        }
        
        hideSpinner(container);
        canvas.style.display = 'block';
        
    } catch (error) {
        console.error('Error creating exercise chart:', error);
        hideSpinner(container);
        
        // Show error message
        const errorMsg = document.createElement('div');
        errorMsg.className = 'alert alert-danger';
        errorMsg.textContent = 'Failed to load exercise chart. Please try again later.';
        canvas.parentNode.appendChild(errorMsg);
    }
}