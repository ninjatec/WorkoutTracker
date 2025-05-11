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

    // Initialize date range picker
    $('#dateRange').daterangepicker({
        startDate: moment().subtract(30, 'days'),
        endDate: moment(),
        ranges: {
            'Last 7 Days': [moment().subtract(6, 'days'), moment()],
            'Last 30 Days': [moment().subtract(29, 'days'), moment()],
            'This Month': [moment().startOf('month'), moment().endOf('month')],
            'Last Month': [moment().subtract(1, 'month').startOf('month'), moment().subtract(1, 'month').endOf('month')]
        }
    }, function(start, end) {
        loadChartData(start.format('YYYY-MM-DD'), end.format('YYYY-MM-DD'));
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
                const volumeByExercise = Object.entries(data.volumeProgress.reduce((acc, curr) => {
                    if (!acc[curr.label]) acc[curr.label] = 0;
                    acc[curr.label] += curr.value;
                    return acc;
                }, {}));

                distributionChart.data.labels = volumeByExercise.map(([label]) => label);
                distributionChart.data.datasets[0].data = volumeByExercise.map(([, value]) => value);
                distributionChart.update();
            })
            .fail(function(jqXHR, textStatus, errorThrown) {
                console.error('Error loading chart data:', errorThrown);
                toastr.error('Failed to load chart data. Please try again.');
            });
    }

    // Handle export functionality
    $('#exportCsv').click(function() {
        const dates = $('#dateRange').data('daterangepicker');
        const url = new URL('/api/dashboard/export/csv', window.location.origin);
        url.searchParams.append('startDate', dates.startDate.format('YYYY-MM-DD'));
        url.searchParams.append('endDate', dates.endDate.format('YYYY-MM-DD'));
        window.location.href = url.toString();
    });

    $('#exportPdf').click(function() {
        const dates = $('#dateRange').data('daterangepicker');
        const url = new URL('/api/dashboard/export/pdf', window.location.origin);
        url.searchParams.append('startDate', dates.startDate.format('YYYY-MM-DD'));
        url.searchParams.append('endDate', dates.endDate.format('YYYY-MM-DD'));
        window.location.href = url.toString();
    });

    // Load initial chart data
    loadChartData(
        moment().subtract(30, 'days').format('YYYY-MM-DD'),
        moment().format('YYYY-MM-DD')
    );
});
