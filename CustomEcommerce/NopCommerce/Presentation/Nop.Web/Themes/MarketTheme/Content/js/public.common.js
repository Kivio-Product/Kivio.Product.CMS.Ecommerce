/*
** nopCommerce custom js functions
*/



function OpenWindow(query, w, h, scroll) {
    var l = (screen.width - w) / 2;
    var t = (screen.height - h) / 2;

    winprops = 'resizable=0, height=' + h + ',width=' + w + ',top=' + t + ',left=' + l + 'w';
    if (scroll) winprops += ',scrollbars=1';
    var f = window.open(query, "_blank", winprops);
}

function setLocation(url) {
    window.location.href = url;
}

function displayAjaxLoading(display) {
    if (display) {
        $('.ajax-loading-block-window').show();
    }
    else {
        $('.ajax-loading-block-window').hide('slow');
    }
}

function displayPopupNotification(message, messagetype, modal) {
    //types: success, error, warning
    var container;
    if (messagetype == 'success') {
        //success
        container = $('#dialog-notifications-success');
    }
    else if (messagetype == 'error') {
        //error
        container = $('#dialog-notifications-error');
    }
    else if (messagetype == 'warning') {
        //warning
        container = $('#dialog-notifications-warning');
    }
    else {
        //other
        container = $('#dialog-notifications-success');
    }

    //we do not encode displayed message
    var htmlcode = '';
    if ((typeof message) == 'string') {
        htmlcode = '<p>' + message + '</p>';
    } else {
        for (var i = 0; i < message.length; i++) {
            htmlcode = htmlcode + '<p>' + message[i] + '</p>';
        }
    }

    container.html(htmlcode);

    var isModal = (modal ? true : false);
    container.dialog({
        modal: isModal,
        width: 350
    });
}
function displayJoinedPopupNotifications(notes) {
    if (Object.keys(notes).length === 0) return;

    var container = $('#dialog-notifications-success');
    var htmlcode = document.createElement('div');

    for (var note in notes) {
        if (notes.hasOwnProperty(note)) {
            var messages = notes[note];

            for (var i = 0; i < messages.length; ++i) {
                var elem = document.createElement("div");
                elem.innerHTML = messages[i];
                elem.classList.add('popup-notification');
                elem.classList.add(note);

                htmlcode.append(elem);
            }
        }
    }

    container.html(htmlcode);
    container.dialog({
        width: 350,
        modal: true
    });
}

function displayPopupContentFromUrl(url, title, modal, width) {
    const isModal = modal !== false; 
    const targetWidth = width || 550;
    
    const stickyHeader = document.querySelector('.header.sticky');
    let originalZIndex = null;
    
    if (stickyHeader) {
        const computedStyle = window.getComputedStyle(stickyHeader);
        originalZIndex = computedStyle.zIndex;
        stickyHeader.style.zIndex = 'auto';
    }

    const overlay = document.createElement('div');
    overlay.className = 'modern-modal-overlay';
    overlay.innerHTML = `
        <div class="modern-modal-container" style="max-width: ${targetWidth}px;">
            <div class="modern-modal-header">
                <h2 class="modern-modal-title">${title || 'Información'}</h2>
                <button class="modern-modal-close" type="button">×</button>
            </div>
            <div class="modern-modal-content">
                <div class="modern-modal-loading">
                    <div class="modern-modal-spinner"></div>
                    <p>Cargando contenido...</p>
                </div>
            </div>
        </div>
    `;
    
    const closeModal = function() {
        if (stickyHeader && originalZIndex !== null) {
            if (originalZIndex === 'auto' || originalZIndex === '') {
                stickyHeader.style.zIndex = '';
            } else {
                stickyHeader.style.zIndex = originalZIndex;
            }
        }
        
        overlay.style.opacity = '0';
        overlay.querySelector('.modern-modal-container').style.transform = 'translateY(-20px) scale(0.95)';
        
        setTimeout(() => {
            if (overlay.parentNode) {
                overlay.parentNode.removeChild(overlay);
            }
            document.body.style.overflow = '';
        }, 300);
    };
    
    overlay.querySelector('.modern-modal-close').addEventListener('click', closeModal);
    
    if (isModal) {
        overlay.addEventListener('click', function(e) {
            if (e.target === overlay) {
                closeModal();
            }
        });
    }
    
    const escapeHandler = function(e) {
        if (e.key === 'Escape') {
            document.removeEventListener('keydown', escapeHandler);
            closeModal();
        }
    };
    document.addEventListener('keydown', escapeHandler);
    
    document.body.appendChild(overlay);
    document.body.style.overflow = 'hidden';
    
    if (typeof $ !== 'undefined' && $.fn.load) {
        $('<div></div>').load(url, function(response, status, xhr) {
            const contentDiv = overlay.querySelector('.modern-modal-content');
            
            if (status === 'error') {
                contentDiv.innerHTML = `
                    <div class="modern-modal-error">
                        <p>Error al cargar el contenido</p>
                        <p class="error-details">${xhr.status} ${xhr.statusText}</p>
                    </div>
                `;
            } else {
                contentDiv.innerHTML = response;
            }
        });
    } else {
        fetch(url)
            .then(response => {
                if (!response.ok) {
                    throw new Error(`${response.status} ${response.statusText}`);
                }
                return response.text();
            })
            .then(data => {
                overlay.querySelector('.modern-modal-content').innerHTML = data;
            })
            .catch(error => {
                overlay.querySelector('.modern-modal-content').innerHTML = `
                    <div class="modern-modal-error">
                        <p>Error al cargar el contenido</p>
                        <p class="error-details">${error.message}</p>
                    </div>
                `;
            });
    }
    
    return overlay;
}

function displayPopupContentFromUrlLegacy(url, title, modal, width) {
    return displayPopupContentFromUrl(url, title, modal, width);
}

function displayBarNotification(message, messagetype, timeout) {
    var notificationTimeout;

    var messages = typeof message === 'string' ? [message] : message;
    if (messages.length === 0)
        return;

    //types: success, error, warning
    var cssclass = ['success', 'error', 'warning'].indexOf(messagetype) !== -1 ? messagetype : 'success';

    $('#bar-notification').children('.generalnote-main').show();
    //remove previous CSS classes and notifications
    $('#bar-notification')
      .removeClass('success')
      .removeClass('error')
      .removeClass('warning');
    $('.bar-notification').remove();

    //add new notifications
    var htmlcode = document.createElement('div');

    //IE11 Does not support miltiple parameters for the add() & remove() methods
    htmlcode.classList.add('bar-notification', cssclass);
    htmlcode.classList.add(cssclass);

    //add close button for notification
    var close = document.createElement('span');
    close.classList.add('close');
    close.setAttribute('title', document.getElementById('bar-notification').dataset.close);

    for (var i = 0; i < messages.length; i++) {
        var content = document.createElement('p');
        content.classList.add('content');
        content.innerHTML = messages[i];

      htmlcode.appendChild(content);
    }
    
    htmlcode.appendChild(close);

    $('.generalnote-main')
        .append(htmlcode);

    $(htmlcode)
        .fadeIn('slow')
        .on('mouseenter', function() {
            clearTimeout(notificationTimeout);
        });

    //callback for notification removing
    var removeNoteItem = function () {
        $(htmlcode).remove();
    };

    $(close).on('click', function () {
        $('#bar-notification').children('.generalnote-main').hide();
        $(htmlcode).fadeOut('slow', removeNoteItem);
    });

    //timeout (if set)
    if (timeout > 0) {
        notificationTimeout = setTimeout(function () {
            $('#bar-notification').children('.generalnote-main').hide();
            $(htmlcode).fadeOut('slow', removeNoteItem);
        }, timeout);
    }
}

function htmlEncode(value) {
    return $('<div/>').text(value).html();
}

function htmlDecode(value) {
    return $('<div/>').html(value).text();
}


// CSRF (XSRF) security
function addAntiForgeryToken(data) {
    //if the object is undefined, create a new one.
    if (!data) {
        data = {};
    }
    //add token
    var tokenInput = $('input[name=__RequestVerificationToken]');
    if (tokenInput.length) {
        data.__RequestVerificationToken = tokenInput.val();
    }
    return data;
};