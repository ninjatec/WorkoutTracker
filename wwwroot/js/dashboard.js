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

    // Period selector logic (match Reports page)
    $('#dashboardPeriod').val('30'); // Default to 30 days
    $('#dashboardPeriod').on('change', function() {
        const period = $(this).val();
        // Optionally update the URL/query string for server-side reload
        // For now, just reload chart data via AJAX
        loadChartDataByPeriod(period);
    });

    function loadChartDataByPeriod(period) {
        let days = parseInt(period);
        if (days === 2147483647) days = 3650; // All Time: 10 years fallback
        const endDate = moment().format('YYYY-MM-DD');
        const startDate = moment().subtract(days, 'days').format('YYYY-MM-DD');
        loadChartData(startDate, endDate);
    }

    // On page load, trigger initial chart load for default period
    loadChartDataByPeriod($('#dashboardPeriod').val());

    // Ensure charts are cleared if no data is returned
    function clearChartsAndTable() {
        volumeChart.data.labels = [];
        volumeChart.data.datasets[0].data = [];
        volumeChart.update();
        distributionChart.data.labels = [];
        distributionChart.data.datasets[0].data = [];
        distributionChart.update();
        const table = $('#personalBestsTable').DataTable();
        table.clear().draw();
    }

    // Function to load chart data
    function loadChartData(startDate, endDate) {
        $.get(`?handler=ChartData&startDate=${startDate}&endDate=${endDate}`)
            .done(function(data) {
                // Update Volume Progress Chart
                if (data.volumeProgress && data.volumeProgress.length > 0) {
                    volumeChart.data.labels = data.volumeProgress.map(d => d.date);
                    volumeChart.data.datasets[0].data = data.volumeProgress.map(d => d.value);
                } else {
                    volumeChart.data.labels = [];
                    volumeChart.data.datasets[0].data = [];
                }
                volumeChart.update();

                // Update Exercise Distribution Chart using volumeByExercise from backend
                if (data.volumeByExercise && Object.keys(data.volumeByExercise).length > 0) {
                    const volumeByExercise = Object.entries(data.volumeByExercise);
                    distributionChart.data.labels = volumeByExercise.map(([label]) => label);
                    distributionChart.data.datasets[0].data = volumeByExercise.map(([, value]) => value);
                } else {
                    distributionChart.data.labels = [];
                    distributionChart.data.datasets[0].data = [];
                }
                distributionChart.update();

                // Update Personal Bests Table
                const table = $('#personalBestsTable').DataTable();
                table.clear();
                if (data.personalBests && data.personalBests.length > 0) {
                    data.personalBests.forEach(pb => {
                        table.row.add([
                            pb.exerciseName,
                            pb.weight + ' kg',
                            pb.reps,
                            pb.estimatedOneRM ? pb.estimatedOneRM.toFixed(1) + ' kg' : '',
                            pb.achievedDate ? new Date(pb.achievedDate).toLocaleDateString() : ''
                        ]);
                    });
                }
                table.draw();
            })
            .fail(function(jqXHR, textStatus, errorThrown) {
                clearChartsAndTable();
                console.error('Error loading chart data:', errorThrown);
                toastr.error('Failed to load chart data. Please try again.');
            });
    }

    // Export handlers
    $('#exportCsv').click(function() {
        const period = $('#dashboardPeriod').val();
        let days = parseInt(period);
        if (days === 2147483647) days = 3650;
        const endDate = moment().format('YYYY-MM-DD');
        const startDate = moment().subtract(days, 'days').format('YYYY-MM-DD');
        window.location.href = `/api/dashboard/export/csv?startDate=${startDate}&endDate=${endDate}`;
    });

    $('#exportPdf').click(function() {
        const period = $('#dashboardPeriod').val();
        let days = parseInt(period);
        if (days === 2147483647) days = 3650;
        const endDate = moment().format('YYYY-MM-DD');
        const startDate = moment().subtract(days, 'days').format('YYYY-MM-DD');
        window.location.href = `/api/dashboard/export/pdf?startDate=${startDate}&endDate=${endDate}`;
    });

    // Reset filters
    $('#resetFilters').click(function() {
        $('#dashboardPeriod').val('30').trigger('change');
        $('#exerciseType').val(null).trigger('change');
        $('#metricType').val(['volume', 'calories', 'frequency']).trigger('change');
    });
});
