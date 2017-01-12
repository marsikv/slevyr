$(document).ready(function () {
    var $tabule = $('#tabule'),
        uri = 'api/slevyr';

    var data = [
      {
        "LinkaName": "TEST nazuv liky strasne dlouheho chacha",
        "CilKusuTabule": 2000,
        "CilDefectTabule": 15,
        "CilDefectTabuleStr": "15",
        "AktualDefectTabule": "100 %",
        "AktualDefectTabuleTxt": "-",
        "AktualDefectTabuleStr": "null",
        "RozdilTabule": -1658,
        "RozdilTabuleTxt": "-1658"
      },
      {
        "LinkaName": "Linka 101",
        "CilKusuTabule": 3850,
        "CilDefectTabule": 1.5,
        "CilDefectTabuleStr": "1.5",
        "AktualDefectTabule": "NaN",
        "AktualDefectTabuleTxt": "-",
        "AktualDefectTabuleStr": "null",
        "RozdilTabule": -3192,
        "RozdilTabuleTxt": "-3192"
      },
      {
        "LinkaName": "TEST nazuv liky strasne dlouheho chacha",
        "CilKusuTabule": 2000,
        "CilDefectTabule": 15,
        "CilDefectTabuleStr": "15",
        "AktualDefectTabule": "NaN",
        "AktualDefectTabuleTxt": "-",
        "AktualDefectTabuleStr": "null",
        "RozdilTabule": -1658,
        "RozdilTabuleTxt": "-1658"
      },
      {
        "LinkaName": "Linka 101",
        "CilKusuTabule": 3850,
        "CilDefectTabule": 1.5,
        "CilDefectTabuleStr": "1.5",
        "AktualDefectTabule": "NaN",
        "AktualDefectTabuleTxt": "-",
        "AktualDefectTabuleStr": "null",
        "RozdilTabule": -3192,
        "RozdilTabuleTxt": "-3192"
      }
    ];

    var get = function () {
        console.log('aaa');

        $.getJSON(uri + '/GetAllTabule?')
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
    add(data);

});