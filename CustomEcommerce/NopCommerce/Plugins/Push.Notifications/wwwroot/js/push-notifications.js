document.addEventListener('DOMContentLoaded', function () {
    // Initialize Firebase
    firebase.initializeApp(firebaseConfig);
    const messaging = firebase.messaging();

    // Check for service worker
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.register('/firebase-messaging-sw.js')
            .then((registration) => {
                messaging.useServiceWorker(registration);
                console.log('Firebase Messaging Service Worker registered');
                requestPermissionAndToken(messaging);
            }).catch((err) => {
                console.error('Service Worker registration failed:', err);
            });
    }
});

function requestPermissionAndToken(messaging) {
    console.log('Requesting permission...');
    Notification.requestPermission().then((permission) => {
        if (permission === 'granted') {
            console.log('Notification permission granted.');
            // Get token
            messaging.getToken({ vapidKey: vapidPublicKey }).then((currentToken) => {
                if (currentToken) {
                    console.log('FCM Token:', currentToken);
                    saveTokenToServer(currentToken);
                } else {
                    console.log('No registration token available. Request permission to generate one.');
                }
            }).catch((err) => {
                console.log('An error occurred while retrieving token. ', err);
            });
        } else {
            console.log('Unable to get permission to notify.');
        }
    });
}

function saveTokenToServer(token) {
    fetch('/PushNotificationsPublic/RegisterDevice', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ token: token })
    });
}
