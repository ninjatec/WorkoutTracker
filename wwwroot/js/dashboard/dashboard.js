// JavaScript for the interactive dashboard
$(document).ready(function() {
    // Chart color scheme
    const colors = {
        primary: '#4e73df',
        success: '#1cc88a',
        info: '#36b9cc',
        warning: '#f6c23e',
        danger: '#e74a3b'
    };

    // Use dashboard period selector instead of date range picker
    $('#dashboardPeriod').on('change', function() {
        const period = $(this).val();
        // Reload chart data based on period
        loadChartDataByPeriod(period);
    });

    // Initialize volume progress chart
    const volumeCtx = document.getElementById('volumeChart').getContext('2d');
    const volumeChart = new Chart(volumeCtx, {
        type: 'line',
        data: {
            datasets: [{
                label: 'Total Volume (kg)',
                borderColor: colors.primary,
                pointBackgroundColor: colors.primary,
                borderWidth: 2,
                fill: false
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    type: 'time',
                    time: {
                        unit: 'day',
                        tooltipFormat: 'll'
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
                legend: {
                    position: 'bottom'
                },
                tooltip: {
                    mode: 'index',
                    intersect: false
                }
            }
        }
    });

    // Initialize workout distribution chart
    const distributionCtx = document.getElementById('exerciseDistributionChart').getContext('2d');
    const distributionChart = new Chart(distributionCtx, {
        type: 'doughnut',
        data: {
            datasets: [{
                backgroundColor: Object.values(colors),
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    position: 'bottom'
                }
            }
        }
    });

    // Initialize frequency heatmap
    const frequencyCtx = document.getElementById('frequencyChart').getContext('2d');
    const frequencyChart = new Chart(frequencyCtx, {
        type: 'bar',
        data: {
            datasets: [{
                label: 'Workouts',
                backgroundColor: colors.info,
                borderColor: colors.info,
                borderWidth: 1
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    type: 'time',
                    time: {
                        unit: 'day',
                        tooltipFormat: 'll'
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
                        text: 'Workouts'
                    },
                    ticks: {
                        stepSize: 1
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

    // Initialize DataTable for personal bests
    const personalBestsTable = $('#personalBestsTable').DataTable({
        pageLength: 5,
        order: [[4, 'desc']],
        responsive: true,
        dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>rtip'
    });

    // Function to load chart data
    function loadChartData(startDate, endDate) {
        $.get(`?handler=ChartData&startDate=${startDate}&endDate=${endDate}`)
            .done(function(data) {
                // Update volume progress chart
                volumeChart.data.labels = data.volumeProgress.map(d => d.date);
                volumeChart.data.datasets[0].data = data.volumeProgress.map(d => d.value);
                volumeChart.update();

                // Update workout frequency chart
                frequencyChart.data.labels = data.workoutFrequency.map(d => d.date);
                frequencyChart.data.datasets[0].data = data.workoutFrequency.map(d => d.value);
                frequencyChart.update();

                // Update exercise distribution chart
                if (data.volumeByExercise && Object.keys(data.volumeByExercise).length > 0) {
                    const volumeByExercise = Object.entries(data.volumeByExercise);
                    distributionChart.data.labels = volumeByExercise.map(([label]) => label);
                    distributionChart.data.datasets[0].data = volumeByExercise.map(([, value]) => value);
                } else {
                    // Fallback to calculating from volume progress data
                    const volumeByExercise = Object.entries(data.volumeProgress.reduce((acc, curr) => {
                        if (!acc[curr.label]) acc[curr.label] = 0;
                        acc[curr.label] += curr.value;
                        return acc;
                    }, {}));
                    distributionChart.data.labels = volumeByExercise.map(([label]) => label);
                    distributionChart.data.datasets[0].data = volumeByExercise.map(([, value]) => value);
                }
                distributionChart.update();
            })
            .fail(function(jqXHR, textStatus, errorThrown) {
                console.error('Error loading chart data:', errorThrown);
                toastr.error('Failed to load chart data. Please try again.');
            });
    }

    // Handle export functionality
    $('#exportCsv').click(function() {
        const period = $('#dashboardPeriod').val();
        let days = parseInt(period);
        if (days === 2147483647) days = 3650; // All Time: 10 years fallback
        const endDate = moment().format('YYYY-MM-DD');
        const startDate = moment().subtract(days, 'days').format('YYYY-MM-DD');
        
        const url = new URL('/api/dashboard/export/csv', window.location.origin);
        url.searchParams.append('startDate', startDate);
        url.searchParams.append('endDate', endDate);
        window.location.href = url.toString();
    });

    $('#exportPdf').click(function() {
        const period = $('#dashboardPeriod').val();
        let days = parseInt(period);
        if (days === 2147483647) days = 3650; // All Time: 10 years fallback
        const endDate = moment().format('YYYY-MM-DD');
        const startDate = moment().subtract(days, 'days').format('YYYY-MM-DD');
        
        const url = new URL('/api/dashboard/export/pdf', window.location.origin);
        url.searchParams.append('startDate', startDate);
        url.searchParams.append('endDate', endDate);
        window.location.href = url.toString();
    });
    
    // Function to load chart data by period
    function loadChartDataByPeriod(period) {
        let days = parseInt(period);
        if (days === 2147483647) days = 3650; // All Time: 10 years fallback
        const endDate = moment().format('YYYY-MM-DD');
        const startDate = moment().subtract(days, 'days').format('YYYY-MM-DD');
        loadChartData(startDate, endDate);
    }

    // Load initial chart data
    loadChartDataByPeriod($('#dashboardPeriod').val());
});
