// Import and configure the Firebase SDK
importScripts('https://www.gstatic.com/firebasejs/9.22.0/firebase-app-compat.js');
importScripts('https://www.gstatic.com/firebasejs/9.22.0/firebase-messaging-compat.js');

const firebaseConfig = {
    apiKey: "AIzaSyDRvO2WOzXJ_FgorqeamA-mgnO5-iTuVig",
    authDomain: "merkko.firebaseapp.com",
    projectId: "merkko",
    storageBucket: "merkko.firebasestorage.app",
    messagingSenderId: "604905130862",
    appId: "1:604905130862:web:1f0f4dcd533d208d128f7f",
    measurementId: "G-8LKH7V9B6T"
};

firebase.initializeApp(firebaseConfig);

const messaging = firebase.messaging();

messaging.onBackgroundMessage(function(payload) {
  console.log('[firebase-messaging-sw.js] Received background message ', payload);
  // Customize notification here
  const notificationTitle = payload.notification.title;
  const notificationOptions = {
    body: payload.notification.body,
    icon: '/Plugins/Misc.PushNotifications/logo.jpg'
  };

  self.registration.showNotification(notificationTitle, notificationOptions);
});
