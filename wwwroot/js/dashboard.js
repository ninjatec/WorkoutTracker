// JavaScript for the interactive dashboard
$(document).ready(function () {
    // Initialize select2 for multiple select
    $('#exerciseType, #metricType').select2({
        theme: 'bootstrap-5'
    });

    // Initialize DataTable for personal bests
    $('#personalBestsTable').DataTable({
        order: [[4, 'desc']], // Sort by date achieved by default
        responsive: true
    });

    // Volume Progress Chart
    const volumeCtx = document.getElementById('volumeChart');
    const volumeChart = new Chart(volumeCtx, {
        type: 'line',
        data: {
            datasets: [{
                label: 'Total Volume',
                borderColor: '#4e73df',
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
                        unit: 'day'
                    },
                    title: {
                        display: true,
                        text: 'Date'
                    }
                },
                y: {
                    title: {
                        display: true,
                        text: 'Volume (kg)'
                    }
                }
            }
        }
    });

    // Exercise Distribution Chart
    const distributionCtx = document.getElementById('exerciseDistributionChart');
    const distributionChart = new Chart(distributionCtx, {
        type: 'doughnut',
        data: {
            datasets: [{
                backgroundColor: [
                    '#4e73df',
                    '#1cc88a',
                    '#36b9cc',
                    '#f6c23e',
                    '#e74a3b'
                ]
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

    // Helper to get current date picker values
    function getDateRange() {
        return {
            start: $('#dateStart').val() || moment().subtract(30, 'days').format('YYYY-MM-DD'),
            end: $('#dateEnd').val() || moment().format('YYYY-MM-DD')
        };
    }

    // Load initial chart data
    const initialRange = getDateRange();
    loadChartData(initialRange.start, initialRange.end);

    // Listen for date picker changes
    $('#dateStart, #dateEnd').on('change', function() {
        const range = getDateRange();
        loadChartData(range.start, range.end);
    });

    // Function to load chart data
    function loadChartData(startDate, endDate) {
        $.get(`?handler=ChartData&startDate=${startDate}&endDate=${endDate}`)
            .done(function(data) {
                // Update Volume Progress Chart
                volumeChart.data.labels = data.volumeProgress.map(d => d.date);
                volumeChart.data.datasets[0].data = data.volumeProgress.map(d => d.value);
                volumeChart.update();

                // Update Exercise Distribution Chart
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

    // Export handlers
    $('#exportCsv').click(function() {
        const range = getDateRange();
        window.location.href = `/api/dashboard/export/csv?startDate=${range.start}&endDate=${range.end}`;
    });

    $('#exportPdf').click(function() {
        const range = getDateRange();
        window.location.href = `/api/dashboard/export/pdf?startDate=${range.start}&endDate=${range.end}`;
    });

    // Reset filters
    $('#resetFilters').click(function() {
        $('#dateStart').val(moment().subtract(30, 'days').format('YYYY-MM-DD'));
        $('#dateEnd').val(moment().format('YYYY-MM-DD'));
        $('#exerciseType').val(null).trigger('change');
        $('#metricType').val(['volume', 'calories', 'frequency']).trigger('change');
        const range = getDateRange();
        loadChartData(range.start, range.end);
    });
});
