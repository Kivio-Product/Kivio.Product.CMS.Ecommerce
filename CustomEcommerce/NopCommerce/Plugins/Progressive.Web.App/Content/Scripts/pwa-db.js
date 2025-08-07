var AddToCartProductsDb = (function() {
        
    localforage.config({
        name: 'nop-offline-add-to-cart'
    });
        
    function ready() {
        return localforage.ready();
    }

    function get(key) {
        return localforage.getItem(key);
    }
        
    function getAllkeys(){
        return localforage.keys();
    }
    
    function add(key, value) {

        localforage.setItem(key, value)
            .then(function (value) {
                ServiceWorkerSite.registerAddToCartSync(value[`addtocart_${key}.EnteredQuantity`]);
            })
            .catch(function (err) {
                console.log(err);
            });  
    }
        
    function remove(key) {

        localforage.removeItem(key)
            .catch(function (err) {
                console.log(err);
            }); 
    }
        
    return {
        get: get,
        add: add,
        remove:remove,
        ready:ready,
        getAllkeys: getAllkeys
    }
})();