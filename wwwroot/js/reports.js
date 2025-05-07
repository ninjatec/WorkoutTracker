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

    // Enhance mobile accordion responsiveness
    enhanceMobileAccordions();

    // Initialize charts with improved error handling
    initializeCharts();
});

// New function to enhance accordion responsiveness on mobile
function enhanceMobileAccordions() {
    // Find all accordion headers
    const accordionHeaders = document.querySelectorAll('.accordion-button, [data-bs-toggle="collapse"]');
    
    accordionHeaders.forEach(header => {
        // Add touchstart event with passive flag
        header.addEventListener('touchstart', function(e) {
            header.dataset.touchStartX = e.touches[0].clientX;
            header.dataset.touchStartY = e.touches[0].clientY;
            header.dataset.swiping = 'false';
        }, { passive: true });
        
        // Add touchmove to detect if user is swiping rather than tapping
        header.addEventListener('touchmove', function(e) {
            if (!header.dataset.touchStartX) return;
            
            const diffX = Math.abs(e.touches[0].clientX - header.dataset.touchStartX);
            const diffY = Math.abs(e.touches[0].clientY - header.dataset.touchStartY);
            
            // If finger moved more than 10px in any direction, it's a swipe
            if (diffX > 10 || diffY > 10) {
                header.dataset.swiping = 'true';
            }
        }, { passive: true });
        
        // Handle touchend to trigger click for simple taps
        header.addEventListener('touchend', function(e) {
            if (header.dataset.swiping !== 'true') {
                // This was a tap, not a swipe
                // Get the target element that should be expanded/collapsed
                const targetId = header.getAttribute('data-bs-target') || 
                                 header.getAttribute('data-target') || 
                                 header.getAttribute('href');
                
                if (targetId) {
                    // If we can find the collapse element, toggle its state
                    const targetElement = document.querySelector(targetId);
                    if (targetElement) {
                        const bsCollapse = bootstrap.Collapse.getInstance(targetElement);
                        if (bsCollapse) {
                            if (targetElement.classList.contains('show')) {
                                bsCollapse.hide();
                            } else {
                                bsCollapse.show();
                            }
                        } else {
                            // Fallback if Bootstrap instance not available
                            targetElement.classList.toggle('show');
                            header.classList.toggle('collapsed');
                            
                            const expanded = header.getAttribute('aria-expanded') === 'true';
                            header.setAttribute('aria-expanded', !expanded);
                        }
                    }
                }
            }
            
            // Reset touch tracking
            delete header.dataset.touchStartX;
            delete header.dataset.touchStartY;
            delete header.dataset.swiping;
        }, { passive: false });
    });
    
    // Also handle the accordion content areas to prevent accidental collapse
    document.querySelectorAll('.accordion-collapse').forEach(content => {
        content.addEventListener('touchstart', function(e) {
            // Prevent event bubbling to accordion header when touching inside content
            e.stopPropagation();
        }, { passive: true });
    });
}

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

// Improved chart initialization with better error handling
function initializeCharts() {
    // Initialize storage for chart instances
    window.chartInstances = {
        weightProgressChart: null,
        overallChart: null,
        exerciseChart: null,
        volumeChart: null,
        caloriesChart: null,
        caloriesPieChart: null
    };
    
    // Set default timeout for all API requests
    const defaultTimeout = 15000; // 15 seconds
    
    // Function to show error message in chart container
    function showChartError(container, message) {
        const spinner = container.querySelector('.loading-spinner');
        if (spinner) {
            spinner.style.display = 'none';
        }
        
        // Remove any existing error messages
        const existingError = container.querySelector('.chart-error');
        if (existingError) {
            existingError.remove();
        }
        
        const errorDiv = document.createElement('div');
        errorDiv.className = 'chart-error alert alert-warning';
        errorDiv.innerHTML = `
            <div class="d-flex align-items-center">
                <i class="bi bi-exclamation-triangle-fill me-2"></i>
                <div>
                    ${message}
                    <button class="btn btn-sm btn-outline-secondary ms-3 retry-button">Retry</button>
                </div>
            </div>
        `;
        
        const canvas = container.querySelector('canvas');
        if (canvas) {
            canvas.style.display = 'none';
            container.insertBefore(errorDiv, canvas);
        } else {
            container.appendChild(errorDiv);
        }
        
        // Add retry button handler
        const retryButton = errorDiv.querySelector('.retry-button');
        if (retryButton) {
            retryButton.addEventListener('click', function() {
                errorDiv.remove();
                if (canvas) {
                    canvas.style.display = 'block';
                }
                if (spinner) {
                    spinner.style.display = 'block';
                }
                
                // Determine which chart to reload based on container id
                const containerId = container.id;
                if (containerId === 'volumeChartContainer' && window.createVolumeChart) {
                    window.createVolumeChart();
                } else if (containerId === 'caloriesChartContainer' && window.createCaloriesChart) {
                    window.createCaloriesChart();
                } else if (containerId === 'caloriesBreakdownContainer' && window.createCaloriesPieChart) {
                    window.createCaloriesPieChart();
                } else if (containerId === 'weightProgressChartContainer' && window.createWeightProgressChart) {
                    window.createWeightProgressChart();
                } else if (containerId === 'exerciseChartContainer' && window.createExerciseChart) {
                    window.createExerciseChart();
                } else if (containerId === 'overallChartContainer' && window.createOverallChart) {
                    window.createOverallChart();
                }
            });
        }
    }
    
    // Enhanced API request function with timeout, retry, and caching
    async function fetchChartData(url, options = {}) {
        const cacheKey = `chart_cache_${url}`;
        
        // Check for cached data if not forced to refresh
        if (!options.forceRefresh) {
            const cachedData = sessionStorage.getItem(cacheKey);
            if (cachedData) {
                try {
                    const { data, timestamp } = JSON.parse(cachedData);
                    const cacheAge = Date.now() - timestamp;
                    
                    // Use cache if it's less than 5 minutes old
                    if (cacheAge < 5 * 60 * 1000) {
                        console.log(`Using cached data for ${url}, age: ${Math.round(cacheAge/1000)}s`);
                        return data;
                    }
                } catch (e) {
                    console.warn('Failed to parse cached data, fetching fresh data');
                }
            }
        }
        
        // Set up abort controller for timeout
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), options.timeout || defaultTimeout);
        
        try {
            const response = await fetch(url, {
                signal: controller.signal,
                headers: {
                    'Cache-Control': 'no-cache',
                    'Pragma': 'no-cache'
                }
            });
            
            clearTimeout(timeoutId);
            
            if (!response.ok) {
                throw new Error(`API request failed: ${response.status} ${response.statusText}`);
            }
            
            const data = await response.json();
            
            // Cache the successful response
            const cacheData = {
                data,
                timestamp: Date.now()
            };
            sessionStorage.setItem(cacheKey, JSON.stringify(cacheData));
            
            return data;
        } catch (error) {
            clearTimeout(timeoutId);
            
            if (error.name === 'AbortError') {
                console.error(`Request timeout for ${url}`);
                throw new Error('Request timed out. The server took too long to respond.');
            }
            
            // For other errors, throw with a better message
            console.error(`Error fetching data from ${url}:`, error);
            throw new Error(`Failed to load data: ${error.message}`);
        }
    }
    
    // Optimized volume chart loading with proper error handling
    window.createVolumeChart = async function() {
        const container = document.getElementById('volumeChartContainer');
        const ctx = document.getElementById('volumeChart');
        const spinner = container.querySelector('.loading-spinner');
        
        // If chart instance already exists, destroy it
        if (chartInstances.volumeChart) {
            chartInstances.volumeChart.destroy();
            chartInstances.volumeChart = null;
        }
        
        spinner.style.display = 'block';
        ctx.style.display = 'none';
        
        try {
            // Get period from URL or use default
            const period = getReportPeriod();
            console.log(`Loading volume chart data for period: ${period} days`);
            
            // Fetch volume data with progressive reduction strategy
            let data;
            try {
                data = await fetchChartData(`/api/ReportsApi/volume-over-time?days=${period}`);
            } catch (error) {
                // If original period failed, try with smaller periods
                if (period > 90) {
                    console.log('Retrying with 90 day period');
                    data = await fetchChartData('/api/ReportsApi/volume-over-time?days=90');
                } else if (period > 60) {
                    console.log('Retrying with 60 day period');
                    data = await fetchChartData('/api/ReportsApi/volume-over-time?days=60');
                } else if (period > 30) {
                    console.log('Retrying with 30 day period');
                    data = await fetchChartData('/api/ReportsApi/volume-over-time?days=30');
                } else {
                    throw error;
                }
            }
            
            // Process chart data
            const datasets = [];
            const colors = [
                '#4CAF50', '#2196F3', '#F44336', '#FF9800', '#9C27B0', 
                '#795548', '#607D8B', '#E91E63', '#FFEB3B', '#009688'
            ];
            
            let hasValidData = false;
            
            // Extract unique exercise names and their volumes over time
            const exerciseData = {};
            
            // Transform the data for charting
            Object.entries(data).forEach(([dateStr, volumeTotal]) => {
                const date = moment(dateStr).format('YYYY-MM-DD');
                hasValidData = true;
                
                // For now, just use total volume per date
                if (!exerciseData['Total Volume']) {
                    exerciseData['Total Volume'] = [];
                }
                
                exerciseData['Total Volume'].push({
                    x: date,
                    y: volumeTotal
                });
            });
            
            // If we have valid data, create datasets
            if (hasValidData) {
                Object.entries(exerciseData).forEach(([exercise, points], index) => {
                    const colorIndex = index % colors.length;
                    
                    // Sort points by date
                    points.sort((a, b) => moment(a.x).valueOf() - moment(b.x).valueOf());
                    
                    // If only one data point, add context points
                    if (points.length === 1) {
                        const dataPoint = points[0];
                        // Add a starting point 7 days before with value 0
                        const startDate = moment(dataPoint.x).subtract(7, 'days').format('YYYY-MM-DD');
                        points.unshift({ x: startDate, y: 0 });
                        // Add a point in the future with the same value
                        const endDate = moment(dataPoint.x).add(7, 'days').format('YYYY-MM-DD');
                        points.push({ x: endDate, y: dataPoint.y });
                    }
                    
                    datasets.push({
                        label: exercise,
                        data: points,
                        borderColor: colors[colorIndex],
                        backgroundColor: colors[colorIndex] + '33',
                        fill: true,
                        tension: 0.4
                    });
                });
            } else {
                // Add placeholder data
                datasets.push({
                    label: 'No workout data yet',
                    data: [
                        { x: moment().subtract(7, 'days').format('YYYY-MM-DD'), y: 0 },
                        { x: moment().format('YYYY-MM-DD'), y: 0 }
                    ],
                    borderColor: '#cccccc',
                    backgroundColor: '#eeeeee',
                    fill: true
                });
            }
            
            // Create the chart
            chartInstances.volumeChart = new Chart(ctx, {
                type: 'line',
                data: { datasets },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    animation: {
                        duration: 300
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
                                text: 'Volume (kg)'
                            }
                        }
                    },
                    plugins: {
                        tooltip: {
                            callbacks: {
                                label: function(context) {
                                    const label = context.dataset.label || '';
                                    const value = context.parsed.y;
                                    return `${label}: ${value.toLocaleString()} kg`;
                                }
                            }
                        },
                        legend: {
                            position: 'top',
                            labels: {
                                usePointStyle: true,
                                boxWidth: 8
                            }
                        }
                    }
                }
            });
            
            spinner.style.display = 'none';
            ctx.style.display = 'block';
        } catch (error) {
            console.error('Error creating volume chart:', error);
            showChartError(container, `Unable to load volume chart data. ${error.message}`);
        }
    };
    
    // Improved calories chart creation function with better error handling
    window.createCaloriesChart = async function() {
        const container = document.getElementById('caloriesChartContainer');
        const ctx = document.getElementById('caloriesChart');
        const spinner = container.querySelector('.loading-spinner');
        
        // If chart instance already exists, destroy it
        if (chartInstances.caloriesChart) {
            chartInstances.caloriesChart.destroy();
            chartInstances.caloriesChart = null;
        }
        
        spinner.style.display = 'block';
        ctx.style.display = 'none';
        
        try {
            // Get period from URL or use default
            const period = getReportPeriod();
            console.log(`Loading calorie chart data for period: ${period} days`);
            
            // Fetch calorie data with progressive reduction strategy
            let data;
            try {
                data = await fetchChartData(`/api/ReportsApi/dashboard-metrics?days=${period}`);
            } catch (error) {
                // If original period failed, try with smaller periods
                if (period > 90) {
                    console.log('Retrying with 90 day period');
                    data = await fetchChartData('/api/ReportsApi/dashboard-metrics?days=90');
                } else if (period > 60) {
                    console.log('Retrying with 60 day period');
                    data = await fetchChartData('/api/ReportsApi/dashboard-metrics?days=60');
                } else if (period > 30) {
                    console.log('Retrying with 30 day period');
                    data = await fetchChartData('/api/ReportsApi/dashboard-metrics?days=30');
                } else {
                    throw error;
                }
            }
            
            // Create a dataset for the chart
            const caloriesData = {
                labels: [],
                datasets: [{
                    label: 'Calories Burned',
                    data: [],
                    backgroundColor: 'rgba(54, 162, 235, 0.5)',
                    borderColor: 'rgba(54, 162, 235, 1)',
                    fill: true,
                    tension: 0.4
                }]
            };
            
            if (data && data.totalCalories > 0) {
                // Create dates array
                const startDate = moment().subtract(period, 'days');
                const endDate = moment();
                const dates = [];
                const values = [];
                
                // If we have calorie data, distribute it over the period
                // For simplicity, we'll just show a gradual increase
                dates.push(startDate.format('YYYY-MM-DD'));
                dates.push(endDate.format('YYYY-MM-DD'));
                
                values.push(0); // Start at zero
                values.push(data.totalCalories); // End at total
                
                caloriesData.labels = dates;
                caloriesData.datasets[0].data = values;
                
                chartInstances.caloriesChart = new Chart(ctx, {
                    type: 'line',
                    data: caloriesData,
                    options: {
                        responsive: true,
                        maintainAspectRatio: false,
                        animation: { duration: 300 },
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
                                        const label = context.dataset.label || '';
                                        const value = context.parsed.y;
                                        return `${label}: ${value.toLocaleString()} kcal`;
                                    }
                                }
                            }
                        }
                    }
                });
                
                spinner.style.display = 'none';
                ctx.style.display = 'block';
            } else {
                // Show empty state
                chartInstances.caloriesChart = new Chart(ctx, {
                    type: 'line',
                    data: {
                        labels: [
                            moment().subtract(period, 'days').format('YYYY-MM-DD'),
                            moment().format('YYYY-MM-DD')
                        ],
                        datasets: [{
                            label: 'No calorie data yet',
                            data: [0, 0],
                            borderColor: '#cccccc',
                            backgroundColor: '#eeeeee',
                            fill: true
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
                
                spinner.style.display = 'none';
                ctx.style.display = 'block';
            }
        } catch (error) {
            console.error('Error creating calories chart:', error);
            showChartError(container, `Unable to load calorie chart data. ${error.message}`);
        }
    };
    
    // Helper to extract period from URL
    function getReportPeriod() {
        const urlParams = new URLSearchParams(window.location.search);
        return parseInt(urlParams.get('period')) || 90; // Default to 90 days
    }
    
    // Initialize any visible charts on page load
    document.addEventListener('DOMContentLoaded', function() {
        // Attach listeners to accordion sections
        const volumeSection = document.getElementById('collapseVolume');
        if (volumeSection && volumeSection.classList.contains('show') && window.createVolumeChart) {
            window.createVolumeChart();
        }
        
        const caloriesSection = document.getElementById('collapseCalories');
        if (caloriesSection && caloriesSection.classList.contains('show')) {
            if (window.createCaloriesChart) window.createCaloriesChart();
            if (window.createCaloriesPieChart) window.createCaloriesPieChart();
        }
    });
}