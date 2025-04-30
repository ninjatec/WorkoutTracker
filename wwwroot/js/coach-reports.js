// Chart.js configurations for coach reports

function initializeCharts(data) {
    initializeClientProgressChart(data.clientProgress);
    initializeGoalAnalyticsChart(data.goals);
    initializeSessionTrendsChart(data.sessions);
}

function initializeClientProgressChart(data) {
    const ctx = document.getElementById('clientProgressChart').getContext('2d');
    new Chart(ctx, {
        type: 'bar',
        data: {
            labels: data.labels,
            datasets: data.datasets.map((ds, index) => ({
                ...ds,
                backgroundColor: index === 0 ? 'rgba(75, 192, 192, 0.7)' : 'rgba(54, 162, 235, 0.7)',
                borderColor: index === 0 ? 'rgba(75, 192, 192, 1)' : 'rgba(54, 162, 235, 1)',
                borderWidth: 1
            }))
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
                        text: 'Percentage'
                    }
                },
                x: {
                    title: {
                        display: true,
                        text: 'Clients'
                    }
                }
            },
            plugins: {
                legend: {
                    position: 'bottom'
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            return `${context.dataset.label}: ${context.parsed.y.toFixed(1)}%`;
                        }
                    }
                }
            }
        }
    });
}

function initializeGoalAnalyticsChart(data) {
    const ctx = document.getElementById('goalAnalyticsChart').getContext('2d');
    new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Completed', 'In Progress', 'Pending'],
            datasets: [{
                data: [data.completedGoals, data.inProgressGoals, data.pendingGoals],
                backgroundColor: [
                    'rgba(75, 192, 192, 0.7)',
                    'rgba(54, 162, 235, 0.7)',
                    'rgba(255, 206, 86, 0.7)'
                ],
                borderColor: [
                    'rgba(75, 192, 192, 1)',
                    'rgba(54, 162, 235, 1)',
                    'rgba(255, 206, 86, 1)'
                ],
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            layout: {
                padding: {
                    bottom: 10
                }
            },
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        boxWidth: 12,
                        padding: 10,
                        font: {
                            size: 11
                        }
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

function initializeSessionTrendsChart(data) {
    const ctx = document.getElementById('sessionTrendsChart').getContext('2d');
    new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.labels,
            datasets: [{
                label: 'Sessions',
                data: data.counts,
                backgroundColor: 'rgba(153, 102, 255, 0.1)',
                borderColor: 'rgba(153, 102, 255, 1)',
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
                        precision: 0,
                        maxTicksLimit: 5
                    },
                    title: {
                        display: true,
                        text: 'Sessions',
                        padding: {
                            top: 0,
                            bottom: 0
                        }
                    }
                },
                x: {
                    grid: {
                        display: false
                    },
                    ticks: {
                        maxRotation: 0,
                        autoSkip: true,
                        maxTicksLimit: 7
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