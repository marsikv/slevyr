$(document).ready(function () {
    var $tabule = $('#tabule'),
        uri = 'api/slevyr';

    var get = function () {
        $.getJSON(uri + '/GetAllTabule')
            .done(function (data) {
                add(data);
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'Load All Tabule Error: ' + err);
            });
    };

    var add = function (data) {
        data.forEach(function (device) {
            var html = '<div class="tabule">' +
                '<div class="tabule--name">' + device.LinkaName + '</div>' +
                '<div class="tabule--row" data-title="Cíl">' + device.CilKusuTabule + '</div>' +
                '<div class="tabule--row" data-title="Rozdíl">' + device.RozdilTabule + '</div>' +
                '<div class="tabule--row" data-title="Cíl defektivita">' + device.CilDefectTabule + '</div>' +
                '<div class="tabule--row" data-title="Aktualni defektivita">' + device.AktualDefectTabule + '</div>' +
            '</div>';

            $tabule.append(html);
        });
    };

    get();

});