import { initializeApp } from 'https://www.gstatic.com/firebasejs/9.23.0/firebase-app.js';
import { getMessaging, getToken, onMessage } from 'https://www.gstatic.com/firebasejs/9.23.0/firebase-messaging.js';

// Initialize firebase
const app = initializeApp(firebaseConfig);
const messaging = getMessaging(app);

// Check for service worker

document.addEventListener('DOMContentLoaded', (event) => {
    if ('serviceWorker' in navigator) {
        navigator.serviceWorker.register('/firebase-messaging-sw.js')
            .then((registration) => {
                console.log('Firebase Messaging Service Worker registered');
            }).catch((err) => {
                console.error('Service Worker registration failed:', err);
            });
        requestPermissionAndToken();
    }
});

function requestPermissionAndToken() {
    console.log('Requesting permission...');
    Notification.requestPermission().then((permission) => {
        if (permission === 'granted') {
            console.log('Notification permission granted.');
            getTokenFCM();
        } else {
            console.log('Unable to get permission to notify.');
        }
    });
}

function getTokenFCM() {
    getToken(messaging, { vapidKey: vapidPublicKey })
    .then((currentToken) => {
        if (currentToken) {
            console.log('Token FCM obtenido:', currentToken);
            saveTokenToServer(currentToken);
        } else {
        console.log('No se pudo obtener el token. Es necesario solicitar permiso primero.');
        }
    })
    .catch((err) => {
        console.log('Ocurrió un error al obtener el token.', err);
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

onMessage(messaging, (payload) => {
    console.log('[firebase-messaging-sw.js] Received background message ', payload);
});