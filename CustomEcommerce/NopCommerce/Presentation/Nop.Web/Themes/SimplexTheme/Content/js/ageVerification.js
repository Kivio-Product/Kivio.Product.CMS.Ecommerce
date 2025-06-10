document.addEventListener('DOMContentLoaded', function () {
        const modal = document.getElementById('ageConfirmationModal');
        const btnYes = document.getElementById('btnYesAdult');
        const btnNo = document.getElementById('btnNoAdult');

        // Function to set a cookie
        function setCookie(name, value, days) {
            let expires = "";
            if (days) {
                const date = new Date();
                date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
                expires = "; expires=" + date.toUTCString();
            }
            document.cookie = name + "=" + (value || "") + expires + "; path=/";
        }

        // Function to get a cookie
        function getCookie(name) {
            const nameEQ = name + "=";
            const ca = document.cookie.split(';');
            for (let i = 0; i < ca.length; i++) {
                let c = ca[i];
                while (c.charAt(0) === ' ') c = c.substring(1, c.length);
                if (c.indexOf(nameEQ) === 0) return c.substring(nameEQ.length, c.length);
            }
            return null;
        }

        // Check if the age has been confirmed via cookie
        if (!getCookie('ageConfirmed')) {
            modal.style.display = 'flex'; // Show the modal
        }

        // "Yes" button click event
        btnYes.addEventListener('click', function () {
            setCookie('ageConfirmed', 'true', 30); // Set cookie for 30 days
            modal.style.display = 'none'; // Hide the modal
        });

        // "No" button click event
        btnNo.addEventListener('click', function () {
            window.location.href = 'https://www.google.com';
        });
    });