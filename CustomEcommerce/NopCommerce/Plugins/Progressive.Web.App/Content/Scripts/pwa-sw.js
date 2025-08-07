importScripts('https://storage.googleapis.com/workbox-cdn/releases/6.2.0/workbox-sw.js');
importScripts('./pwa-db.js');

workbox.core.setCacheNameDetails({
    prefix: 'nop-site',
    suffix: 'v1',
    precache: 'precache',
    runtime: 'runtime-cache'
});

const PRECACHE_ROUTES = [
    { url: '/Plugins/Progressive.Web.App/Views/Offline.html', revision: '1' },
    { url: '/Plugins/Progressive.Web.App/Content/Images/NoConnection.jpg', revision: '1' },
    { url: '/Plugins/Progressive.Web.App/Content/Icons/safari-pinned-tab-blue.svg', revision: '1' }
];

workbox.precaching.precacheAndRoute(PRECACHE_ROUTES);

// Admin Area - Network Only
workbox.routing.registerRoute(
    new RegExp('/admin/.*'),
    new workbox.strategies.NetworkOnly()
);

// Static Content - Cache First
workbox.routing.registerRoute(
    new RegExp('/(Content|Scripts|Themes|Plugins)/.*'),
    new workbox.strategies.CacheFirst({
        cacheName: 'nop-site-static-v1',
        plugins: [
            new workbox.expiration.ExpirationPlugin({
                maxAgeSeconds: 60 * 60 * 24, // 1 Day
            }),
        ],
    })
);

// Dynamic Content - Network First
workbox.routing.registerRoute(
    ({request}) => request.destination === 'document',
    new workbox.strategies.NetworkFirst({
        networkTimeoutSeconds: 5,
        cacheName: 'nop-site-dynamic-v1',
        plugins: [
            new workbox.expiration.ExpirationPlugin({
                maxEntries: 50,
            }),
        ],
    })
);

// Offline Fallback
const offlineFallback = '/Plugins/Progressive.Web.App/Views/Offline.html';
workbox.routing.setCatchHandler(({ event }) => {
  switch (event.request.destination) {
    case 'document':
      return caches.match(offlineFallback);
    default:
      return Response.error();
  }
});

self.addEventListener('notificationclick', function (event) {
    event.notification.close();
    var payload = event.notification.data;

    switch (event.action) {
    case 'addToWishlist':
        fetch(`/addproducttocart/catalog/${payload.offer.Id}/2/1`,
                {
                    headers: { 'Content-Type': 'application/json' },
                    method: 'POST',
                    credentials: 'same-origin',
                    cache: 'no-cache'
                })
            .then(function (response) {
                if (response.ok) {
                    return response.json();
                }
            })
            .then(function (data) {
            })
            .catch(function (err) {
                console.log(err);
            });
        break;
    case 'viewOffer':
        self.clients.openWindow(`${event.target.location.origin}/${payload.offer.SeName}`);
        break;
    case 'goToCart':
        self.clients.openWindow('/cart');
        break;
    case 'later':
        event.notification.close();
        break;
    }
});

self.addEventListener('push', function(event) {
    
    var title = "";
    var options = {};
    var payload = event.data.json();

    if(typeof payload.notificationType == "undefined") 
        return false;
    
    switch (payload.notificationType) {
    case 'Offer':

        var body = payload.offer.Name;
        if (payload.offer.Price) {
            body = payload.offer.Name + ' for ' + payload.offer.Price;
        }

        title = 'New Super Offer';
        options = {
            body: body,
            icon: '/Plugins/Progressive.Web.App/Content/Icons/android-chrome-192x192.png',
            badge: '/Plugins/Progressive.Web.App/Content/Icons/android-chrome-192x192.png',
            image: payload.offer.ImageUrl,
            data: payload,
            actions: [
                { action: 'viewOffer', title: 'See the Offer', icon: '' },
                { action: 'addToWishlist', title: 'Add to wishlist', icon: '' }
            ]
        };
        break;
    case 'Cart':
        title = 'You are online, continue your shopping';
        options = {
            body: 'Your cart was updated, You may proceed to purchase',
            icon: '/Plugins/Progressive.Web.App/Content/Icons/android-chrome-192x192.png',
            badge: '/Plugins/Progressive.Web.App/Content/Icons/android-chrome-192x192.png',
            //image: ,
            //data: payload,
            actions: [
                { action: 'goToCart', title: 'Go to Cart', icon: '' },
                { action: 'later', title: 'Dismiss for later', icon: '' }
            ]
        };
        break;
    }

    event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('sync', function (event) {
    if (event.tag === 'sync-add-to-cart') {
        
        event.waitUntil( syncProducts() );
    }
});              
                
function syncProducts() {
    AddToCartProductsDb.ready().then(() => {
        AddToCartProductsDb.getAllkeys()
            .then((allkeys) => {

                return Promise.all(allkeys.map(function(key) {

                    return AddToCartProductsDb.get(key);
                }));
            })
            .then((addToCartProducts) => {

                return Promise.all(addToCartProducts.map(function(product) {

                    return processAddToCart(product);
                }));
            })
            .then((synchronized) => {

                var hasASyncProduct = false;

                synchronized.forEach((sync) => {

                    if (sync.success) {
                        hasASyncProduct = true;
                    }
                });

                if (hasASyncProduct) {

                    fetch('/WebPush/AddToCartNotification',
                            {
                                method: "GET",
                                credentials: 'include',
                                cache: 'no-cache'
                            })
                        .then(function(response) {
                            return response.json();
                        })
                        .then(function(data) {
                        });
                }
            })
            .catch(error => {
                console.log(error);
            });
    })
    .catch(error => {
        console.log(error);
    });            
}

async function processAddToCart(product){
    var response;
    
    if (product.isFrom === 'ProductPage') {

        var body = new FormData();
        for (prAttr in product) {
            body.append(prAttr, product[prAttr]);
        }

        response = await fetch(`/addproducttocart/details/${product.productId}/1`,
        {
            method: "POST",
            body: body,
            credentials: 'include',
            cache: 'no-cache'
        });

    } else {
       
        response = await fetch(`/addproducttocart/catalog/${product.productId}/1/${product[`addtocart_${product.productId}.EnteredQuantity`]}`, 
        {
            headers: { 'Content-Type': 'application/json' },
            method: 'POST',
            credentials: 'include',
            cache: 'no-cache'
        });
    }

    if (response.ok){

        var cloneResponse = response.clone();

        var data = await cloneResponse.json();

        if (data && (data.success || data.redirect)) {

           AddToCartProductsDb.remove(product.productId);

           return {
               productId: product.productId,
               success: data.success,
               redirect: data.redirect
            }
        }
    }
}