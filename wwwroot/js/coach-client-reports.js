// Chart.js configurations for detailed client reports

function initializeCharts(data) {
    initializeTrendChart(data);

    // Update trend chart when metric changes
    document.getElementById('trendMetric').addEventListener('change', function() {
        updateTrendChart(data, this.value);
    });
}

function initializeTrendChart(data) {
    const ctx = document.getElementById('trendChart').getContext('2d');
    window.trendChart = new Chart(ctx, {
        type: 'line',
        data: createTrendChartData(data, 'sessions'),
        options: createTrendChartOptions('sessions')
    });
}

function updateTrendChart(data, metric) {
    window.trendChart.data = createTrendChartData(data, metric);
    window.trendChart.options = createTrendChartOptions(metric);
    window.trendChart.update();
}

function createTrendChartData(data, metric) {
    const sessionData = data.sessions;
    
    switch (metric) {
        case 'sessions':
            return {
                labels: sessionData.labels,
                datasets: [{
                    label: 'Sessions per Day',
                    data: sessionData.counts,
                    backgroundColor: 'rgba(54, 162, 235, 0.1)',
                    borderColor: 'rgba(54, 162, 235, 1)',
                    fill: true,
                    tension: 0.4
                }]
            };
        
        case 'volume':
            return {
                labels: sessionData.labels,
                datasets: [{
                    label: 'Total Volume',
                    data: sessionData.volumes,
                    backgroundColor: 'rgba(75, 192, 192, 0.1)',
                    borderColor: 'rgba(75, 192, 192, 1)',
                    fill: true,
                    tension: 0.4
                }]
            };
            
        case 'intensity':
            // Calculate average weight per rep for intensity
            const intensity = sessionData.volumes.map((v, i) => 
                sessionData.counts[i] > 0 ? v / sessionData.counts[i] : 0
            );
            
            return {
                labels: sessionData.labels,
                datasets: [{
                    label: 'Average Weight per Rep',
                    data: intensity,
                    backgroundColor: 'rgba(153, 102, 255, 0.1)',
                    borderColor: 'rgba(153, 102, 255, 1)',
                    fill: true,
                    tension: 0.4
                }]
            };
    }
}

function createTrendChartOptions(metric) {
    const baseOptions = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: {
                position: 'bottom'
            },
            tooltip: {
                mode: 'index',
                intersect: false
            }
        },
        scales: {
            y: {
                beginAtZero: true,
                title: {
                    display: true,
                    text: getMetricLabel(metric)
                }
            },
            x: {
                title: {
                    display: true,
                    text: 'Date'
                }
            }
        }
    };

    // Add metric-specific options
    switch (metric) {
        case 'sessions':
            baseOptions.scales.y.ticks = { precision: 0 };
            break;
        case 'volume':
            baseOptions.scales.y.ticks = { callback: value => `${value.toLocaleString()}kg` };
            break;
        case 'intensity':
            baseOptions.scales.y.ticks = { callback: value => `${value.toFixed(1)}kg` };
            break;
    }

    return baseOptions;
}

function getMetricLabel(metric) {
    switch (metric) {
        case 'sessions': return 'Number of Sessions';
        case 'volume': return 'Total Volume (kg)';
        case 'intensity': return 'Average Weight per Rep (kg)';
        default: return '';
    }
}

// Chart.js configurations for client-specific reports

function initializeClientCharts(data) {
    initializeWorkoutHistoryChart(data.workoutHistory);
    initializeStrengthProgressChart(data.strengthProgress);
    initializeWorkoutVolumeChart(data.volume);
    initializeGoalCategoriesChart(data.goalCategories);
}

function initializeWorkoutHistoryChart(data) {
    const ctx = document.getElementById('workoutHistoryChart').getContext('2d');
    new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.labels,
            datasets: [{
                label: 'Workouts',
                data: data.data,
                backgroundColor: 'rgba(54, 162, 235, 0.1)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 2,
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    ticks: {
                        precision: 0
                    },
                    title: {
                        display: true,
                        text: 'Number of Workouts'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Date'
                    }
                }
            },
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    mode: 'index',
                    intersect: false
                }
            }
        }
    });
}

function initializeStrengthProgressChart(data) {
    const ctx = document.getElementById('strengthProgressChart').getContext('2d');
    new Chart(ctx, {
        type: 'line',
        data: {
            datasets: data.map((exercise, index) => ({
                label: exercise.exercise,
                data: exercise.weights,
                borderColor: getColorForIndex(index),
                backgroundColor: 'transparent',
                tension: 0.4
            })),
            labels: data[0]?.dates || []
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Weight (kg)'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Date'
                    }
                }
            },
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        boxWidth: 12
                    }
                },
                tooltip: {
                    mode: 'index',
                    intersect: false
                }
            },
            interaction: {
                mode: 'nearest',
                axis: 'x',
                intersect: false
            }
        }
    });
}

function initializeWorkoutVolumeChart(data) {
    const ctx = document.getElementById('workoutVolumeChart').getContext('2d');
    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: data.labels,
            datasets: [{
                label: 'Volume (kg)',
                data: data.data,
                backgroundColor: 'rgba(75, 192, 192, 0.7)',
                borderColor: 'rgba(75, 192, 192, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Total Volume (kg)'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Date'
                    }
                }
            },
            plugins: {
                legend: {
                    display: false
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return `Volume: ${context.parsed.y.toLocaleString()}kg`;
                        }
                    }
                }
            }
        }
    });
}

function initializeGoalCategoriesChart(data) {
    const ctx = document.getElementById('goalCategoriesChart').getContext('2d');
    new Chart(ctx, {
        type: 'pie',
        data: {
            labels: data.labels,
            datasets: [{
                data: data.data,
                backgroundColor: data.labels.map((_, index) => getColorForIndex(index, 0.7)),
                borderColor: data.labels.map((_, index) => getColorForIndex(index)),
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        boxWidth: 12
                    }
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const total = context.dataset.data.reduce((a, b) => a + b, 0);
                            const percentage = ((context.parsed / total) * 100).toFixed(1);
                            return `${context.label}: ${context.parsed} (${percentage}%)`;
                        }
                    }
                }
            }
        }
    });
}

function initializeGoalFeedbackModal() {
    const modal = document.getElementById('goalFeedbackModal');
    if (modal) {
        modal.addEventListener('show.bs.modal', function(event) {
            const button = event.relatedTarget;
            const goalId = button.getAttribute('data-goal-id');
            document.getElementById('goalIdInput').value = goalId;
        });
    }
}

// Helper function to get consistent colors for charts
function getColorForIndex(index, alpha = 1) {
    const colors = [
        `rgba(54, 162, 235, ${alpha})`,   // Blue
        `rgba(75, 192, 192, ${alpha})`,   // Green
        `rgba(153, 102, 255, ${alpha})`,  // Purple
        `rgba(255, 159, 64, ${alpha})`,   // Orange
        `rgba(255, 99, 132, ${alpha})`,   // Red
        `rgba(255, 205, 86, ${alpha})`,   // Yellow
        `rgba(201, 203, 207, ${alpha})`,  // Grey
    ];
    return colors[index % colors.length];
}