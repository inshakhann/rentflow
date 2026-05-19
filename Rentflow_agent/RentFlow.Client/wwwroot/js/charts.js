window.renderBarChart = (canvasId, labels, data) => {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;
    
    // Destroy existing chart if it exists
    if (window.revenueChartInstance) {
        window.revenueChartInstance.destroy();
    }
    
    window.revenueChartInstance = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: 'Income (PKR)',
                data: data,
                backgroundColor: '#0EA5E9', // teal
                borderRadius: 4
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                y: {
                    beginAtZero: true
                }
            }
        }
    });
};

window.renderDonutChart = (canvasId, labels, data) => {
    const ctx = document.getElementById(canvasId);
    if (!ctx) return;
    
    if (window.donutChartInstance) {
        window.donutChartInstance.destroy();
    }
    
    window.donutChartInstance = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: [
                    '#EF4444', // red (Open)
                    '#0EA5E9', // teal (InProgress)
                    '#10B981'  // green (Resolved)
                ]
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false
        }
    });
};
