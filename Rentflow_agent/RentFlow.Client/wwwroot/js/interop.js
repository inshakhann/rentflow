window.getLocation = () => {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject("Geolocation is not supported by your browser");
        } else {
            navigator.geolocation.getCurrentPosition(
                (position) => {
                    resolve({
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude
                    });
                },
                (error) => {
                    reject("Unable to retrieve your location");
                }
            );
        }
    });
};

window.initMapIframe = (iframeId, lat, lng) => {
    const iframe = document.getElementById(iframeId);
    if (iframe) {
        iframe.src = `https://maps.google.com/maps?q=${lat},${lng}&z=15&output=embed`;
    }
};

// Dark Mode Toggle
window.toggleDarkMode = () => {
    const html = document.documentElement;
    const current = html.getAttribute('data-theme');
    const next = current === 'dark' ? 'light' : 'dark';
    html.setAttribute('data-theme', next);
    localStorage.setItem('rentflow-theme', next);
    return next;
};

window.getDarkMode = () => {
    return localStorage.getItem('rentflow-theme') || 'light';
};

window.applyStoredTheme = () => {
    const theme = localStorage.getItem('rentflow-theme') || 'light';
    document.documentElement.setAttribute('data-theme', theme);
    return theme;
};

// Print Receipt
window.printReceipt = () => {
    window.print();
};

// QR Code Renderer (receives base64 PNG from server)
window.renderQrImage = (containerId, base64Data) => {
    const container = document.getElementById(containerId);
    if (container) {
        container.innerHTML = `<img src="data:image/png;base64,${base64Data}" alt="QR Code" style="max-width:200px;" />`;
    }
};
