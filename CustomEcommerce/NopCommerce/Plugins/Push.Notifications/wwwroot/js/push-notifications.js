import { initializeApp } from 'https://www.gstatic.com/firebasejs/9.23.0/firebase-app.js';
import { getMessaging, getToken, onMessage } from 'https://www.gstatic.com/firebasejs/9.23.0/firebase-messaging.js';

// Global variables that should be set by the server
const shouldUseWebPush = detectIOSOrSafari();
let messaging = null;

// Initialize firebase only if not using Web Push
if (!shouldUseWebPush && typeof firebaseConfig !== 'undefined') {
    const app = initializeApp(firebaseConfig);
    messaging = getMessaging(app);
}

document.addEventListener('DOMContentLoaded', (event) => {
    if ('serviceWorker' in navigator) {
        const swPath = shouldUseWebPush ? '/webpush-sw.js' : '/firebase-messaging-sw.js';
        navigator.serviceWorker.register(swPath)
            .then((registration) => {
                console.log('Service Worker registered for:', shouldUseWebPush ? 'Web Push' : 'Firebase');
                requestPermissionAndToken();
            }).catch((err) => {
                console.error('Service Worker registration failed:', err);
            });
    } else {
        requestPermissionAndToken();
    }
});

function detectIOSOrSafari() {
    const userAgent = navigator.userAgent || navigator.vendor || window.opera;
    
    // Detect iOS devices
    if (/iPad|iPhone|iPod/.test(userAgent) && !window.MSStream) {
        return true;
    }
    
    // Detect Safari on macOS
    if (/^((?!chrome|android).)*safari/i.test(userAgent) && /macintosh/i.test(userAgent)) {
        return true;
    }
    
    return false;
}

function requestPermissionAndToken() {
    console.log('Requesting permission...');
    Notification.requestPermission().then((permission) => {
        if (permission === 'granted') {
            console.log('Notification permission granted.');
            if (shouldUseWebPush) {
                setupWebPush();
            } else {
                getTokenFCM();
            }
        } else {
            console.log('Unable to get permission to notify.');
        }
    });
}

function getTokenFCM() {
    if (!messaging) {
        console.error('Firebase messaging not initialized');
        return;
    }
    
    getToken(messaging, { vapidKey: vapidPublicKey })
    .then((currentToken) => {
        if (currentToken) {
            console.log('Token FCM obtenido:', currentToken);
            saveTokenToServer({
                token: currentToken,
                type: "FCM", // FCM
                userAgent: navigator.userAgent
            });
        } else {
            console.log('No se pudo obtener el token. Es necesario solicitar permiso primero.');
        }
    })
    .catch((err) => {
        console.log('Ocurrió un error al obtener el token.', err);
    });
}

function setupWebPush() {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
        console.error('Web Push not supported');
        return;
    }
    
    navigator.serviceWorker.ready.then(registration => {
        return registration.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: urlBase64ToUint8Array(vapidPublicKey)
        });
    }).then(subscription => {
        console.log('Web Push subscription:', subscription);
        
        const subscriptionObject = subscription.toJSON();
        saveTokenToServer({
            token: subscription.endpoint,
            type: "WebPush", // WebPush
            userAgent: navigator.userAgent,
            endpoint: subscription.endpoint,
            p256dh: subscriptionObject.keys.p256dh,
            auth: subscriptionObject.keys.auth
        });
    }).catch(err => {
        console.error('Failed to subscribe to Web Push:', err);
    });
}

function saveTokenToServer(subscriptionData) {
    fetch('/PushNotificationsPublic/RegisterDevice', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(subscriptionData)
    }).then(response => response.json())
    .then(data => {
        console.log('Subscription saved:', data);
    }).catch(err => {
        console.error('Failed to save subscription:', err);
    });
}

function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding)
        .replace(/-/g, '+')
        .replace(/_/g, '/');

    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);

    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
}

// Handle Firebase messages if using FCM
if (messaging) {
    onMessage(messaging, (payload) => {
        console.log('[firebase-messaging-sw.js] Received foreground message ', payload);
        
        // Display notification manually for foreground messages
        if (payload.data) {
            showNotification(payload.data.title, payload.data.body, payload.data.icon, payload.data.urlToOpen);
        }
    });
}

function showNotification(title, body, icon, url) {
    if ('serviceWorker' in navigator && 'showNotification' in ServiceWorkerRegistration.prototype) {
        navigator.serviceWorker.ready.then(registration => {
            registration.showNotification(title, {
                body: body,
                icon: icon,
                data: { url: url },
                tag: 'push-notification'
            });
        });
    } else {
        // Fallback for browsers that don't support service worker notifications
        new Notification(title, {
            body: body,
            icon: icon
        });
    }
}