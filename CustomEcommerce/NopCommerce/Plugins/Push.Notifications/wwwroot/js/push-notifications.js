document.addEventListener('DOMContentLoaded', function () {
    if ('serviceWorker' in navigator && 'PushManager' in window) {
        navigator.serviceWorker.register('/js/service-worker.js')
            .then(function (swReg) {
                console.log('Service Worker is registered', swReg);
                swReg.pushManager.getSubscription().then(function (subscription) {
                    if (subscription === null) {
                        // New subscription
                        swReg.pushManager.subscribe({
                            userVisibleOnly: true,
                            applicationServerKey: urlBase64ToUint8Array(vapidPublicKey) 
                        }).then(function (newSubscription) {
                            saveSubscription(newSubscription);
                        });
                    } else {
                        // Already subscribed
                        saveSubscription(subscription);
                    }
                });
            })
            .catch(function (error) {
                console.error('Service Worker Error', error);
            });
    }
});

function saveSubscription(subscription) {
    const token = JSON.stringify(subscription);
    fetch('/PushNotificationsPublic/RegisterDevice', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ token: token })
    });
}

function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding)
        .replace(/\-/g, '+')
        .replace(/_/g, '/');

    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
}
