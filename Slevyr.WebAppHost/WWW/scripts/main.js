(function(){
    var slVyr = {
        jQ: {
            notification: $('#notification'),
            menu: $('#menu')
        },

        addNotification: function(type, ntf) {
            var txt = '';
            switch (type) {
                case 'error':
                    txt += '<div class="notification notification-error">';
                    break;
                case 'success':
                    txt += '<div class="notification notification-success">';
                    break;
            }

            txt += ntf;
            txt += '<span class="notification--close"></span></div>';

            // this.jQ.notification.prepend(txt);
            this.jQ.notification.html(txt);
        },

        toggleMenu: function (boolean) {
            this.jQ.menu.toggleClass('menu-visible', boolean);
        }
    };

    $('#notification').on('click', '.notification--close', function () {
        $(this).parent().remove();
    });

    $('#menuOpener').on('click', function () {
        window.slVyr.toggleMenu(true);
    });

    $('#menuCloser').on('click', function () {
        window.slVyr.toggleMenu(false);
    });

    window.slVyr = slVyr;
})();