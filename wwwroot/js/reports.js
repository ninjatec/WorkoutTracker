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
    window.chartContainers.calories = document.getElementById('caloriesChartContainer');

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

    // Replace window.createVolumeChart with our improved version
    window.createVolumeChart = createVolumeChart;

    // Replace window.createCaloriesChart with our improved version
    window.createCaloriesChart = createCaloriesChart;
    window.createCaloriesPieChart = function() {
        // This is handled by createCaloriesChart
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
        case 'caloriesChartContainer':
            createCaloriesChart();
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

// Optimize volume data loading with timeout and retry handling
async function loadVolumeChartData(period) {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 15000); // 15-second timeout
    
    try {
        console.log(`Loading volume data for period: ${period} days`);
        const response = await fetch(`/api/ReportsApi/volume-over-time?days=${period}`, {
            signal: controller.signal
        });
        clearTimeout(timeoutId);
        
        if (!response.ok) {
            throw new Error(`Failed to fetch volume data: ${response.status} ${response.statusText}`);
        }
        
        console.log('Volume data fetched successfully');
        return await response.json();
    } catch (error) {
        clearTimeout(timeoutId);
        console.error('Error loading volume data:', error);
        
        if (error.name === 'AbortError') {
            console.log('Volume data request timed out, trying with smaller period');
            // Try with a smaller time period if the original request timed out
            if (period > 90) {
                console.log('Retrying with 90 day period');
                return await loadVolumeChartData(90);
            } else if (period > 60) {
                console.log('Retrying with 60 day period');
                return await loadVolumeChartData(60);
            } else if (period > 30) {
                console.log('Retrying with 30 day period');
                return await loadVolumeChartData(30);
            }
        }
        
        throw error;
    }
}

// Improved volume chart creation function with better error handling
async function createVolumeChart() {
    const ctx = document.getElementById('volumeChart');
    if (!ctx) return;
    
    const container = ctx.closest('.card');
    const spinner = container?.querySelector('.loading-spinner');
    
    // If chart instance already exists, destroy it
    if (window.chartInstances?.volumeChart) {
        window.chartInstances.volumeChart.destroy();
        window.chartInstances.volumeChart = null;
    }
    
    if (spinner) spinner.style.display = 'block';
    if (ctx) ctx.style.display = 'none';

    try {
        const period = document.getElementById('period')?.value || 90;
        const datasets = [];
        const colors = [
            '#4CAF50', '#2196F3', '#F44336', '#FF9800', '#9C27B0', 
            '#795548', '#607D8B', '#E91E63', '#FFEB3B', '#009688'
        ];
        
        let hasValidData = false;
        
        // Fetch data with timeout and retry handling
        const data = await loadVolumeChartData(period);
        
        console.log('Processing volume data');
        
        if (data && Object.keys(data).length > 0) {
            // Get top 5 exercises by total volume to keep chart readable
            const topExercises = Object.entries(data)
                .sort((a, b) => b[1] - a[1])
                .slice(0, 5);
                
            topExercises.forEach(([date, volume], i) => {
                hasValidData = true;
                const colorIndex = i % colors.length;
                
                // Format date for Chart.js
                const formattedDate = moment(date).format('YYYY-MM-DD');
                
                datasets.push({
                    label: `Volume on ${formattedDate}`,
                    data: [{x: formattedDate, y: volume}],
                    borderColor: colors[colorIndex],
                    backgroundColor: colors[colorIndex] + '33',
                    fill: false,
                    borderWidth: 2,
                    pointRadius: 6
                });
            });
        }
        
        // If no valid data was found, add a placeholder dataset
        if (!hasValidData) {
            datasets.push({
                label: 'No Workout Data',
                data: [
                    { x: moment().subtract(30, 'days').format('YYYY-MM-DD'), y: 0 },
                    { x: moment().format('YYYY-MM-DD'), y: 0 }
                ],
                borderColor: '#cccccc',
                backgroundColor: '#cccccc33',
                fill: false,
                borderWidth: 2,
                borderDash: [5, 5],
                pointRadius: 0
            });
        }
        
        console.log(`Creating volume chart with ${datasets.length} datasets`);
        
        window.chartInstances.volumeChart = new Chart(ctx, {
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
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: 'Volume (weight × reps)'
                        }
                    }
                },
                plugins: {
                    tooltip: {
                        callbacks: {
                            title: function(context) {
                                return context[0].raw.x ? moment(context[0].raw.x).format('MMMM D, YYYY') : '';
                            },
                            label: function(context) {
                                return `Volume: ${Number(context.raw.y).toLocaleString()}`;
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
        
        if (spinner) spinner.style.display = 'none';
        if (ctx) ctx.style.display = 'block';
        
    } catch (error) {
        console.error('Failed to create volume chart:', error);
        
        if (spinner) spinner.style.display = 'none';
        
        // Show error message instead of leaving spinner spinning
        const errorMsg = document.createElement('div');
        errorMsg.className = 'alert alert-danger';
        errorMsg.textContent = 'Unable to load volume chart data. Please try with a shorter time period or try again later.';
        
        if (container) {
            // Remove any existing error messages
            const existingErrors = container.querySelectorAll('.alert.alert-danger');
            existingErrors.forEach(el => el.remove());
            
            const cardBody = container.querySelector('.card-body');
            if (cardBody) cardBody.appendChild(errorMsg);
        }
        
        // Show canvas with empty chart
        if (ctx) {
            ctx.style.display = 'block';
            
            // Create empty chart
            window.chartInstances.volumeChart = new Chart(ctx, {
                type: 'line',
                data: {
                    datasets: [{
                        label: 'No Data Available',
                        data: [
                            { x: moment().subtract(30, 'days').format('YYYY-MM-DD'), y: 0 },
                            { x: moment().format('YYYY-MM-DD'), y: 0 }
                        ],
                        borderColor: '#cccccc',
                        backgroundColor: '#cccccc33',
                        borderDash: [5, 5]
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        x: {
                            type: 'time',
                            time: { unit: 'day' }
                        },
                        y: { beginAtZero: true }
                    }
                }
            });
        }
    }
}

// Optimize calories data loading with timeout handling
async function loadCaloriesChartData(period) {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), 15000); // 15-second timeout
    
    try {
        console.log(`Loading calories data for period: ${period} days`);
        const response = await fetch(`/api/ReportsApi/dashboard-metrics?days=${period}`, {
            signal: controller.signal
        });
        clearTimeout(timeoutId);
        
        if (!response.ok) {
            throw new Error(`Failed to fetch calories data: ${response.status} ${response.statusText}`);
        }
        
        console.log('Calories data fetched successfully');
        return await response.json();
    } catch (error) {
        clearTimeout(timeoutId);
        console.error('Error loading calories data:', error);
        
        if (error.name === 'AbortError') {
            console.log('Calories data request timed out, trying with smaller period');
            // Try with a smaller time period if the original request timed out
            if (period > 90) {
                console.log('Retrying with 90 day period');
                return await loadCaloriesChartData(90);
            } else if (period > 60) {
                console.log('Retrying with 60 day period');
                return await loadCaloriesChartData(60);
            } else if (period > 30) {
                console.log('Retrying with 30 day period');
                return await loadCaloriesChartData(30);
            }
        }
        
        throw error;
    }
}

// Improved calories chart creation function with better error handling
async function createCaloriesChart() {
    const ctx = document.getElementById('caloriesChart');
    if (!ctx) return;
    
    const container = ctx.closest('.card');
    const spinner = container?.querySelector('.loading-spinner');
    
    // If chart instance already exists, destroy it
    if (window.chartInstances?.caloriesChart) {
        window.chartInstances.caloriesChart.destroy();
        window.chartInstances.caloriesChart = null;
    }
    
    if (spinner) spinner.style.display = 'block';
    if (ctx) ctx.style.display = 'none';

    try {
        const period = document.getElementById('period')?.value || 90;
        
        // Fetch data with timeout and retry handling
        const data = await loadCaloriesChartData(period);
        console.log('Processing calories data');
        
        // Create a stacked area chart showing calories burned over time
        const caloriesData = {
            labels: [], // Will be filled with dates
            datasets: [{
                label: 'Calories Burned',
                data: [], 
                backgroundColor: 'rgba(54, 162, 235, 0.5)',
                borderColor: 'rgba(54, 162, 235, 1)',
                fill: true
            }]
        };
        
        // Create dates array for the last period days
        const startDate = moment().subtract(period, 'days');
        const dates = [];
        const values = [];
        
        // Display total calories if available
        if (data && data.totalVolume) {
            // If we have any data, just show the total as one point for simplicity
            dates.push(startDate.format('YYYY-MM-DD'));
            dates.push(moment().format('YYYY-MM-DD'));
            
            values.push(0); // Start at zero
            values.push(data.totalVolume / 10); // Rough estimate of calories
        } else {
            // If no data, just show empty chart with placeholder dates
            dates.push(startDate.format('YYYY-MM-DD'));
            dates.push(moment().format('YYYY-MM-DD'));
            values.push(0);
            values.push(0);
        }
        
        caloriesData.labels = dates;
        caloriesData.datasets[0].data = values;
        
        window.chartInstances.caloriesChart = new Chart(ctx, {
            type: 'line',
            data: caloriesData,
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: { duration: 0 },
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
                        beginAtZero: true,
                        title: {
                            display: true,
                            text: 'Calories Burned'
                        }
                    }
                },
                plugins: {
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                if (context.raw === 0) return 'No data available';
                                return `Calories: ${Math.round(context.raw).toLocaleString()}`;
                            }
                        }
                    }
                }
            }
        });
        
        if (spinner) spinner.style.display = 'none';
        if (ctx) ctx.style.display = 'block';
        
        // Also create the calorie pie chart
        createCaloriesPieChart(data);
        
    } catch (error) {
        console.error('Failed to create calories chart:', error);
        
        if (spinner) spinner.style.display = 'none';
        
        // Show error message instead of leaving spinner spinning
        const errorMsg = document.createElement('div');
        errorMsg.className = 'alert alert-danger';
        errorMsg.textContent = 'Unable to load calorie chart data. Please try with a shorter time period.';
        
        if (container) {
            // Remove any existing error messages
            const existingErrors = container.querySelectorAll('.alert.alert-danger');
            existingErrors.forEach(el => el.remove());
            
            const cardBody = container.querySelector('.card-body');
            if (cardBody) cardBody.appendChild(errorMsg);
        }
        
        // Show canvas with empty chart
        if (ctx) {
            ctx.style.display = 'block';
            
            // Create empty chart
            window.chartInstances.caloriesChart = new Chart(ctx, {
                type: 'line',
                data: {
                    datasets: [{
                        label: 'No Data Available',
                        data: [
                            { x: moment().subtract(period, 'days').format('YYYY-MM-DD'), y: 0 },
                            { x: moment().format('YYYY-MM-DD'), y: 0 }
                        ],
                        borderColor: '#cccccc',
                        backgroundColor: '#cccccc33',
                        borderDash: [5, 5]
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        x: {
                            type: 'time',
                            time: { unit: 'day' }
                        },
                        y: { beginAtZero: true }
                    }
                }
            });
        }
    }
}

// Create the calorie breakdown pie chart
function createCaloriesPieChart(data) {
    const ctx = document.getElementById('caloriesPieChart');
    if (!ctx) return;
    
    // If chart instance already exists, destroy it
    if (window.chartInstances?.caloriesPieChart) {
        window.chartInstances.caloriesPieChart.destroy();
        window.chartInstances.caloriesPieChart = null;
    }
    
    try {
        // Simplified pie chart with just one value for now
        // In a real implementation, we'd break down calories by exercise type
        const chartData = {
            labels: ['Total Calories'],
            datasets: [{
                data: [data && data.totalVolume ? data.totalVolume / 10 : 0],
                backgroundColor: [
                    'rgba(54, 162, 235, 0.7)',
                    'rgba(255, 99, 132, 0.7)',
                    'rgba(75, 192, 192, 0.7)',
                    'rgba(255, 206, 86, 0.7)',
                    'rgba(153, 102, 255, 0.7)'
                ]
            }]
        };
        
        window.chartInstances.caloriesPieChart = new Chart(ctx, {
            type: 'pie',
            data: chartData,
            options: {
                responsive: true,
                maintainAspectRatio: false,
                animation: { duration: 0 },
                plugins: {
                    legend: {
                        position: 'right',
                        labels: {
                            boxWidth: 12,
                            usePointStyle: true
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function(context) {
                                const value = context.raw;
                                if (value === 0) return 'No data available';
                                return `${context.label}: ${Math.round(value).toLocaleString()} calories`;
                            }
                        }
                    }
                }
            }
        });
    } catch (error) {
        console.error('Failed to create calories pie chart:', error);
        
        // Create empty chart
        window.chartInstances.caloriesPieChart = new Chart(ctx, {
            type: 'pie',
            data: {
                labels: ['No Data Available'],
                datasets: [{
                    data: [1],
                    backgroundColor: ['#cccccc']
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false
            }
        });
    }
}