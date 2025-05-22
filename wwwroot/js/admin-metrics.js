// Charts for system metrics dashboard

// Initialize all charts with default data
function initializeCharts() {
    // System metrics charts
    initializeDbConnectionChart();
    initializeRedisCacheChart();
    initializeHangfireQueueChart();

    // User metrics charts
    initializeUserActivityChart();
    initializeLoginRateChart();
    initializeRetentionChart();

    // Workout metrics charts
    initializeWorkoutActivityChart();
    initializeExercisePopularityChart();
    initializeWorkoutDurationChart();
    initializeWorkoutTimeOfDayChart();

    // Performance metrics charts
    initializeHttpDurationChart();
    initializeDbPerformanceChart();
    initializeErrorRateChart();

    // Health metrics charts
    initializeCircuitBreakerChart();
    initializeHealthCheckTimesChart();
    initializeUptimeHistoryChart();
}

// Database connection pool chart
function initializeDbConnectionChart() {
    const ctx = document.getElementById('dbConnectionChart').getContext('2d');
    window.dbConnectionChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: getTimeLabels(12),
            datasets: [{
                label: 'Active Connections',
                backgroundColor: 'rgba(54, 162, 235, 0.2)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 2,
                data: generateRandomData(12, 5, 20),
                tension: 0.4,
                fill: true
            }, {
                label: 'Max Pool Size',
                backgroundColor: 'rgba(255, 99, 132, 0.1)',
                borderColor: 'rgba(255, 99, 132, 1)',
                borderWidth: 2,
                borderDash: [5, 5],
                data: Array(12).fill(100),
                tension: 0.4,
                fill: false
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                title: {
                    display: false
                },
                tooltip: {
                    mode: 'index',
                    intersect: false
                },
                legend: {
                    position: 'top',
                    align: 'end'
                }
            },
            scales: {
                y: {
                    min: 0,
                    max: 110,
                    title: {
                        display: true,
                        text: 'Connections'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Time'
                    }
                }
            }
        }
    });
}

// Redis cache chart
function initializeRedisCacheChart() {
    const ctx = document.getElementById('redisCacheChart').getContext('2d');
    window.redisCacheChart = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Hits', 'Misses'],
            datasets: [{
                data: [75, 25],
                backgroundColor: [
                    'rgba(75, 192, 192, 0.7)',
                    'rgba(255, 99, 132, 0.7)'
                ],
                borderColor: [
                    'rgba(75, 192, 192, 1)',
                    'rgba(255, 99, 132, 1)'
                ],
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom'
                },
                title: {
                    display: false
                }
            }
        }
    });
}

// Hangfire job queue chart
function initializeHangfireQueueChart() {
    const ctx = document.getElementById('hangfireQueueChart').getContext('2d');
    window.hangfireQueueChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: ['Enqueued', 'Processing', 'Succeeded', 'Failed'],
            datasets: [{
                label: 'Jobs',
                data: [5, 2, 120, 3],
                backgroundColor: [
                    'rgba(54, 162, 235, 0.7)',
                    'rgba(255, 205, 86, 0.7)',
                    'rgba(75, 192, 192, 0.7)',
                    'rgba(255, 99, 132, 0.7)'
                ],
                borderColor: [
                    'rgba(54, 162, 235, 1)',
                    'rgba(255, 205, 86, 1)',
                    'rgba(75, 192, 192, 1)',
                    'rgba(255, 99, 132, 1)'
                ],
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    type: 'logarithmic'
                }
            },
            plugins: {
                legend: {
                    display: false
                }
            }
        }
    });
}

// User activity chart
function initializeUserActivityChart() {
    const ctx = document.getElementById('userActivityChart').getContext('2d');
    window.userActivityChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: getDateLabels(30),
            datasets: [{
                label: 'Daily Active Users',
                data: generateRandomData(30, 10, 50),
                backgroundColor: 'rgba(54, 162, 235, 0.2)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 2,
                tension: 0.4,
                fill: true
            }, {
                label: 'New Registrations',
                data: generateRandomData(30, 0, 10),
                backgroundColor: 'rgba(75, 192, 192, 0.2)',
                borderColor: 'rgba(75, 192, 192, 1)',
                borderWidth: 2,
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                tooltip: {
                    mode: 'index',
                    intersect: false
                },
                legend: {
                    position: 'top'
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Users'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Date'
                    }
                }
            }
        }
    });
}

// Login success/failure chart
function initializeLoginRateChart() {
    const ctx = document.getElementById('loginRateChart').getContext('2d');
    window.loginRateChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: getTimeLabels(24),
            datasets: [{
                label: 'Successful Logins',
                data: generateRandomData(24, 10, 50),
                backgroundColor: 'rgba(75, 192, 192, 0.2)',
                borderColor: 'rgba(75, 192, 192, 1)',
                borderWidth: 2,
                tension: 0.4,
                fill: true
            }, {
                label: 'Failed Logins',
                data: generateRandomData(24, 0, 10),
                backgroundColor: 'rgba(255, 99, 132, 0.2)',
                borderColor: 'rgba(255, 99, 132, 1)',
                borderWidth: 2,
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                tooltip: {
                    mode: 'index',
                    intersect: false
                },
                legend: {
                    position: 'top'
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Count'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Hour'
                    }
                }
            }
        }
    });
}

// User retention chart
function initializeRetentionChart() {
    const ctx = document.getElementById('retentionChart').getContext('2d');
    window.retentionChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: ['Week 1', 'Week 2', 'Week 3', 'Week 4', 'Month 2', 'Month 3'],
            datasets: [{
                label: 'Retention Rate',
                data: [100, 85, 75, 65, 55, 45],
                backgroundColor: [
                    'rgba(75, 192, 192, 0.7)',
                    'rgba(75, 192, 192, 0.6)',
                    'rgba(75, 192, 192, 0.5)',
                    'rgba(75, 192, 192, 0.4)',
                    'rgba(75, 192, 192, 0.3)',
                    'rgba(75, 192, 192, 0.2)'
                ],
                borderColor: [
                    'rgba(75, 192, 192, 1)',
                    'rgba(75, 192, 192, 1)',
                    'rgba(75, 192, 192, 1)',
                    'rgba(75, 192, 192, 1)',
                    'rgba(75, 192, 192, 1)',
                    'rgba(75, 192, 192, 1)'
                ],
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true,
                    max: 100,
                    title: {
                        display: true,
                        text: 'Retention %'
                    }
                }
            },
            plugins: {
                legend: {
                    display: false
                }
            }
        }
    });
}

// Workout activity chart
function initializeWorkoutActivityChart() {
    const ctx = document.getElementById('workoutActivityChart').getContext('2d');
    window.workoutActivityChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: getDateLabels(14),
            datasets: [{
                label: 'Workout Sessions',
                data: generateRandomData(14, 5, 25),
                backgroundColor: 'rgba(153, 102, 255, 0.2)',
                borderColor: 'rgba(153, 102, 255, 1)',
                borderWidth: 2,
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                tooltip: {
                    mode: 'index',
                    intersect: false
                },
                legend: {
                    position: 'top'
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Sessions'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Date'
                    }
                }
            }
        }
    });
}

// Exercise popularity chart
function initializeExercisePopularityChart() {
    const ctx = document.getElementById('exercisePopularityChart').getContext('2d');
    window.exercisePopularityChart = new Chart(ctx, {
        type: 'pie',
        data: {
            labels: ['Bench Press', 'Squat', 'Deadlift', 'Shoulder Press', 'Pull-up', 'Other'],
            datasets: [{
                data: [30, 25, 20, 15, 5, 5],
                backgroundColor: [
                    'rgba(255, 99, 132, 0.7)',
                    'rgba(54, 162, 235, 0.7)',
                    'rgba(255, 206, 86, 0.7)',
                    'rgba(75, 192, 192, 0.7)',
                    'rgba(153, 102, 255, 0.7)',
                    'rgba(255, 159, 64, 0.7)'
                ],
                borderColor: [
                    'rgba(255, 99, 132, 1)',
                    'rgba(54, 162, 235, 1)',
                    'rgba(255, 206, 86, 1)',
                    'rgba(75, 192, 192, 1)',
                    'rgba(153, 102, 255, 1)',
                    'rgba(255, 159, 64, 1)'
                ],
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'right'
                }
            }
        }
    });
}

// Workout duration chart
function initializeWorkoutDurationChart() {
    const ctx = document.getElementById('workoutDurationChart').getContext('2d');
    window.workoutDurationChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: getDateLabels(10),
            datasets: [{
                label: 'Avg. Duration (minutes)',
                data: generateRandomData(10, 30, 90),
                backgroundColor: 'rgba(255, 159, 64, 0.2)',
                borderColor: 'rgba(255, 159, 64, 1)',
                borderWidth: 2,
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                tooltip: {
                    mode: 'index',
                    intersect: false
                },
                legend: {
                    display: false
                }
            },
            scales: {
                y: {
                    beginAtZero: false,
                    title: {
                        display: true,
                        text: 'Minutes'
                    }
                }
            }
        }
    });
}

// Workout time of day chart
function initializeWorkoutTimeOfDayChart() {
    const ctx = document.getElementById('workoutTimeOfDayChart').getContext('2d');
    window.workoutTimeOfDayChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: ['Morning', 'Afternoon', 'Evening', 'Night'],
            datasets: [{
                label: 'Workouts',
                data: [40, 25, 30, 5],
                backgroundColor: [
                    'rgba(255, 206, 86, 0.7)',
                    'rgba(75, 192, 192, 0.7)',
                    'rgba(153, 102, 255, 0.7)',
                    'rgba(54, 162, 235, 0.7)'
                ],
                borderColor: [
                    'rgba(255, 206, 86, 1)',
                    'rgba(75, 192, 192, 1)',
                    'rgba(153, 102, 255, 1)',
                    'rgba(54, 162, 235, 1)'
                ],
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
                        text: 'Count (%)'
                    }
                }
            },
            plugins: {
                legend: {
                    display: false
                }
            }
        }
    });
}

// HTTP request duration chart
function initializeHttpDurationChart() {
    const ctx = document.getElementById('httpDurationChart').getContext('2d');
    window.httpDurationChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: ['/Home', '/Sessions', '/Reports', '/Account', '/Admin'],
            datasets: [{
                label: 'Avg. Response Time (ms)',
                data: [45, 125, 200, 80, 150],
                backgroundColor: 'rgba(75, 192, 192, 0.7)',
                borderColor: 'rgba(75, 192, 192, 1)',
                borderWidth: 1
            }, {
                label: 'P95 Response Time (ms)',
                data: [120, 300, 450, 200, 380],
                backgroundColor: 'rgba(255, 99, 132, 0.7)',
                borderColor: 'rgba(255, 99, 132, 1)',
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                tooltip: {
                    mode: 'index',
                    intersect: false
                },
                legend: {
                    position: 'top'
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Response Time (ms)'
                    }
                }
            }
        }
    });
}

// Database performance chart
function initializeDbPerformanceChart() {
    const ctx = document.getElementById('dbPerformanceChart').getContext('2d');
    window.dbPerformanceChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: getTimeLabels(12),
            datasets: [{
                label: 'Query Time (ms)',
                data: generateRandomData(12, 5, 25),
                backgroundColor: 'rgba(54, 162, 235, 0.2)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 2,
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                tooltip: {
                    mode: 'index',
                    intersect: false
                },
                legend: {
                    display: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Time (ms)'
                    }
                }
            }
        }
    });
}

// Error rate chart
function initializeErrorRateChart() {
    const ctx = document.getElementById('errorRateChart').getContext('2d');
    window.errorRateChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: getTimeLabels(24),
            datasets: [{
                label: 'Errors/Hour',
                data: generateRandomData(24, 0, 5),
                backgroundColor: 'rgba(255, 99, 132, 0.2)',
                borderColor: 'rgba(255, 99, 132, 1)',
                borderWidth: 2,
                tension: 0.4,
                fill: true
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                tooltip: {
                    mode: 'index',
                    intersect: false
                },
                legend: {
                    display: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Error Count'
                    }
                }
            }
        }
    });
}

// Circuit breaker chart
function initializeCircuitBreakerChart() {
    const ctx = document.getElementById('circuitBreakerChart').getContext('2d');
    window.circuitBreakerChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: getTimeLabels(12),
            datasets: [{
                label: 'Opens',
                data: generateRandomData(12, 0, 2),
                backgroundColor: 'rgba(255, 99, 132, 0.7)',
                borderColor: 'rgba(255, 99, 132, 1)',
                borderWidth: 1
            }, {
                label: 'Half-opens',
                data: generateRandomData(12, 0, 3),
                backgroundColor: 'rgba(255, 206, 86, 0.7)',
                borderColor: 'rgba(255, 206, 86, 1)',
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
                        text: 'Events'
                    }
                }
            },
            plugins: {
                legend: {
                    position: 'top'
                }
            }
        }
    });
}

// Health check response times chart
function initializeHealthCheckTimesChart() {
    const ctx = document.getElementById('healthCheckTimesChart').getContext('2d');
    window.healthCheckTimesChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: getTimeLabels(12),
            datasets: [{
                label: 'Database',
                data: generateRandomData(12, 10, 50),
                backgroundColor: 'rgba(255, 99, 132, 0.1)',
                borderColor: 'rgba(255, 99, 132, 1)',
                borderWidth: 2,
                tension: 0.4
            }, {
                label: 'Redis',
                data: generateRandomData(12, 5, 30),
                backgroundColor: 'rgba(54, 162, 235, 0.1)',
                borderColor: 'rgba(54, 162, 235, 1)',
                borderWidth: 2,
                tension: 0.4
            }, {
                label: 'API',
                data: generateRandomData(12, 20, 80),
                backgroundColor: 'rgba(255, 206, 86, 0.1)',
                borderColor: 'rgba(255, 206, 86, 1)',
                borderWidth: 2,
                tension: 0.4
            }, {
                label: 'Email',
                data: generateRandomData(12, 50, 150),
                backgroundColor: 'rgba(75, 192, 192, 0.1)',
                borderColor: 'rgba(75, 192, 192, 1)',
                borderWidth: 2,
                tension: 0.4
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                tooltip: {
                    mode: 'index',
                    intersect: false
                },
                legend: {
                    position: 'top'
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    title: {
                        display: true,
                        text: 'Response Time (ms)'
                    }
                }
            }
        }
    });
}

// System uptime history chart
function initializeUptimeHistoryChart() {
    const ctx = document.getElementById('uptimeHistoryChart').getContext('2d');
    
    // Generate 30 days of uptime data with a few outages
    const uptimeData = Array(30).fill(100);
    uptimeData[5] = 99.5;  // Small outage
    uptimeData[12] = 98.2; // Larger outage
    uptimeData[25] = 99.8; // Small outage
    
    window.uptimeHistoryChart = new Chart(ctx, {
        type: 'line',
        data: {
            labels: getDateLabels(30),
            datasets: [{
                label: 'Daily Uptime %',
                data: uptimeData,
                backgroundColor: 'rgba(75, 192, 192, 0.2)',
                borderColor: 'rgba(75, 192, 192, 1)',
                borderWidth: 2,
                tension: 0.4,
                fill: true,
                pointBackgroundColor: function(context) {
                    const value = context.dataset.data[context.dataIndex];
                    return value < 99.9 ? 'rgba(255, 99, 132, 1)' : 'rgba(75, 192, 192, 1)';
                },
                pointRadius: function(context) {
                    const value = context.dataset.data[context.dataIndex];
                    return value < 99.9 ? 5 : 3;
                }
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            const value = context.dataset.data[context.dataIndex];
                            const downtime = 24 * 60 * (100 - value) / 100;
                            const downtimeMinutes = Math.round(downtime);
                            return [`Uptime: ${value}%`, `Downtime: ${downtimeMinutes} minutes`];
                        }
                    }
                },
                legend: {
                    display: false
                }
            },
            scales: {
                y: {
                    min: 95,
                    max: 100.5,
                    title: {
                        display: true,
                        text: 'Uptime %'
                    }
                }
            }
        }
    });
    
    // Handle period change
    document.getElementById('uptimePeriod').addEventListener('change', function() {
        const period = parseInt(this.value);
        const labels = getDateLabels(period);
        
        // Generate new uptime data based on period
        const newUptimeData = Array(period).fill(100);
        
        // Add some random outages
        const outageCount = Math.max(1, Math.floor(period / 10));
        for (let i = 0; i < outageCount; i++) {
            const day = Math.floor(Math.random() * period);
            newUptimeData[day] = 99 + Math.random();
        }
        
        window.uptimeHistoryChart.data.labels = labels;
        window.uptimeHistoryChart.data.datasets[0].data = newUptimeData;
        window.uptimeHistoryChart.update();
    });
}

// Helper function to generate date labels (past N days)
function getDateLabels(days) {
    const labels = [];
    const today = new Date();
    
    for (let i = days - 1; i >= 0; i--) {
        const date = new Date(today);
        date.setDate(today.getDate() - i);
        labels.push(date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' }));
    }
    
    return labels;
}

// Helper function to generate time labels (past N hours)
function getTimeLabels(hours) {
    const labels = [];
    const now = new Date();
    
    for (let i = hours - 1; i >= 0; i--) {
        const time = new Date(now);
        time.setHours(now.getHours() - i);
        labels.push(time.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' }));
    }
    
    return labels;
}

// Helper function to generate random data points
function generateRandomData(count, min, max) {
    return Array.from({ length: count }, () => Math.floor(Math.random() * (max - min + 1)) + min);
}

// Function to refresh all charts with new data from the server
function refreshChartData() {
    // This would normally fetch data from the server with an AJAX call
    // For now, we just update with random data
    
    // Update system charts
    updateDbConnectionChart();
    updateRedisCacheChart();
    updateHangfireQueueChart();
    
    // Other chart updates would be implemented here
}

// Update database connection chart with new data
function updateDbConnectionChart() {
    if (!window.dbConnectionChart) return;
    
    const chart = window.dbConnectionChart;
    
    // Shift labels and add a new time
    chart.data.labels.shift();
    const now = new Date();
    chart.data.labels.push(now.toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' }));
    
    // Shift data and add new values
    chart.data.datasets[0].data.shift();
    chart.data.datasets[0].data.push(Math.floor(Math.random() * 15) + 5);
    
    chart.update();
}

function updateRedisCacheChart() {
    if (!window.redisCacheChart) return;
    
    const chart = window.redisCacheChart;
    
    // Update hit/miss ratio
    const hitRate = Math.floor(Math.random() * 30) + 70;
    chart.data.datasets[0].data = [hitRate, 100 - hitRate];
    
    chart.update();
}

function updateHangfireQueueChart() {
    if (!window.hangfireQueueChart) return;
    
    const chart = window.hangfireQueueChart;
    
    // Update job counts
    chart.data.datasets[0].data = [
        Math.floor(Math.random() * 10),
        Math.floor(Math.random() * 5),
        chart.data.datasets[0].data[2] + Math.floor(Math.random() * 10),
        chart.data.datasets[0].data[3] + (Math.random() > 0.8 ? 1 : 0)
    ];
    
    chart.update();
}