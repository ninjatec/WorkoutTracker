// JavaScript for the interactive dashboard
$(document).ready(function () {
    // Initialize date range picker
    $('#dateRange').daterangepicker({
        startDate: moment().subtract(30, 'days'),
        endDate: moment(),
        ranges: {
            'Last 7 Days': [moment().subtract(6, 'days'), moment()],
            'Last 30 Days': [moment().subtract(29, 'days'), moment()],
            'This Month': [moment().startOf('month'), moment().endOf('month')],
            'Last Month': [moment().subtract(1, 'month').startOf('month'), moment().subtract(1, 'month').endOf('month')],
            'Last 3 Months': [moment().subtract(3, 'months'), moment()],
            'Last 6 Months': [moment().subtract(6, 'months'), moment()],
            'This Year': [moment().startOf('year'), moment()]
        }
    }, function(start, end) {
        loadChartData(start.format('YYYY-MM-DD'), end.format('YYYY-MM-DD'));
    });

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

    // Load initial chart data
    loadChartData(
        moment().subtract(30, 'days').format('YYYY-MM-DD'),
        moment().format('YYYY-MM-DD')
    );

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
        const startDate = $('#dateRange').data('daterangepicker').startDate.format('YYYY-MM-DD');
        const endDate = $('#dateRange').data('daterangepicker').endDate.format('YYYY-MM-DD');
        window.location.href = `/api/dashboard/export/csv?startDate=${startDate}&endDate=${endDate}`;
    });

    $('#exportPdf').click(function() {
        const startDate = $('#dateRange').data('daterangepicker').startDate.format('YYYY-MM-DD');
        const endDate = $('#dateRange').data('daterangepicker').endDate.format('YYYY-MM-DD');
        window.location.href = `/api/dashboard/export/pdf?startDate=${startDate}&endDate=${endDate}`;
    });

    // Reset filters
    $('#resetFilters').click(function() {
        $('#dateRange').data('daterangepicker').setStartDate(moment().subtract(30, 'days'));
        $('#dateRange').data('daterangepicker').setEndDate(moment());
        $('#exerciseType').val(null).trigger('change');
        $('#metricType').val(['volume', 'calories', 'frequency']).trigger('change');
        loadChartData(
            moment().subtract(30, 'days').format('YYYY-MM-DD'),
            moment().format('YYYY-MM-DD')
        );
    });
});
