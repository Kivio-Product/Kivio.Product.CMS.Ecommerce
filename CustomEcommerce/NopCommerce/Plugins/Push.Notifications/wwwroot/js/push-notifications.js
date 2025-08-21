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
    // Check if basic APIs are available before proceeding
    if (!('Notification' in window) && !shouldUseWebPush) {
        console.log('Notifications not supported in this browser');
        return;
    }
    
    // Register service worker first without requesting permission
    if ('serviceWorker' in navigator) {
        const swPath = shouldUseWebPush ? '/webpush-sw.js' : '/firebase-messaging-sw.js';
        navigator.serviceWorker.register(swPath)
            .then((registration) => {
                console.log('Service Worker registered for:', shouldUseWebPush ? 'Web Push' : 'Firebase');
                checkNotificationPermission();
            }).catch((err) => {
                console.error('Service Worker registration failed:', err);
                checkNotificationPermission();
            });
    } else {
        checkNotificationPermission();
    }
});

function checkNotificationPermission() {
    if (!('Notification' in window)) {
        console.log('Notifications not supported in this browser');
        return;
    }
    
    if (Notification.permission === 'granted') {
        console.log('Notification permission already granted');
        if (shouldUseWebPush) {
            setupWebPush();
        } else {
            getTokenFCM();
        }
    } else if (Notification.permission === 'default') {
        if (detectIOSOrSafari()) {
            showNotificationButton();
        } else {
            requestPermissionAndToken();
        }
    } else {
        console.log('Notification permission denied');
    }
}

function showNotificationButton() {
    // Check if button already exists
    if (document.getElementById('enable-notifications-btn')) {
        return;
    }
    
    // Double check: only show for iOS and when permission is not granted
    if (!detectIOSOrSafari() || Notification.permission === 'granted') {
        return;
    }
    
    const button = document.createElement('button');
    button.id = 'enable-notifications-btn';
    button.textContent = 'Activar Notificaciones';
    button.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 9999;
        padding: 12px 20px;
        background-color: #007bff;
        color: white;
        border: none;
        border-radius: 5px;
        cursor: pointer;
        font-size: 14px;
        box-shadow: 0 2px 8px rgba(0,0,0,0.2);
        transition: background-color 0.3s ease;
    `;
    
    // Add hover effect
    button.addEventListener('mouseenter', function() {
        this.style.backgroundColor = '#0056b3';
    });
    
    button.addEventListener('mouseleave', function() {
        this.style.backgroundColor = '#007bff';
    });
    
    button.addEventListener('click', function() {
        requestPermissionAndToken();
        this.remove(); // Remove button after click
    });
    
    document.body.appendChild(button);
}

function detectIOSOrSafari() {
    const userAgent = navigator.userAgent || navigator.vendor || window.opera;
    
    // Detect iOS devices
    if (/iPad|iPhone|iPod/.test(userAgent) && !window.MSStream) {
        return true;
    }
    
    // Detect Safari on macOS (more specific check)
    if (/Safari/.test(userAgent) && /Apple Computer/.test(navigator.vendor)) {
        // Make sure it's not Chrome or other Chromium-based browsers
        if (!/Chrome|Chromium|Edge|Opera|Firefox/.test(userAgent)) {
            return true;
        }
    }
    
    // Additional check for iOS Safari that might not have been caught
    if (/iPhone|iPad|iPod|iOS/.test(userAgent) || 
        (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1)) {
        return true;
    }
    
    return false;
}

function requestPermissionAndToken() {
    console.log('Requesting permission...');
    
    // Check if Notification API is available
    if (!('Notification' in window)) {
        console.log('This browser does not support desktop notifications.');
        return;
    }
    
    // For older browsers that don't support promises
    if (Notification.requestPermission.length === 0) {
        Notification.requestPermission().then((permission) => {
            handlePermissionResult(permission);
        });
    } else {
        // Legacy callback-based approach for older Safari
        Notification.requestPermission((permission) => {
            handlePermissionResult(permission);
        });
    }
}

function handlePermissionResult(permission) {
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
}

function getTokenFCM() {
    if (!messaging) {
        console.error('Firebase messaging not initialized');
        return;
    }
    
    getToken(messaging, { vapidKey: firebaseVapidPublicKey })
    .then((currentToken) => {
        if (currentToken) {
            console.log('Token FCM obtenido:', currentToken);
            saveTokenToServer({
                token: currentToken,
                type: "FCM",
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
            applicationServerKey: urlBase64ToUint8Array(webPushVapidPublicKey)
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
    // Check if Notification API is available
    if (!('Notification' in window)) {
        console.log('This browser does not support desktop notifications.');
        return;
    }
    
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
        if (Notification.permission === 'granted') {
            new Notification(title, {
                body: body,
                icon: icon
            });
        }
    }
}
