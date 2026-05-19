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
