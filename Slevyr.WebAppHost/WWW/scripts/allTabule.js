var tabule = $('#tabule');
var uri = 'api/slevyr';
var isTimerEnabled = false;
var refreshTimer;

$(document).ready(function () {
    
    readRunConfig();

    jQuery.ajaxSetup({ cache: false });
  
    getAllTabule();

});

function getAllTabule() {
    $.getJSON(uri + '/GetAllTabule')
              .done(function (data) {
                  formatTable(data);
              })
              .fail(function (jqXHR, textStatus, err) {
                  window.slVyr.addNotification('error', 'Load All Tabule Error: ' + err);
              });
}

function formatTable(data) {
    $('#tabule').empty();

    data.forEach(function (device) {
        var html = '<div class="tabule">' +
            '<div class="tabule--Name">' + device.LinkaName + '</div>' +
            '<div class="tabule--row" data-title="Cíl">' + device.CilKusuTabule + '</div>' +
            '<div class="tabule--row" data-title="Rozdíl">' + device.RozdilTabuleTxt + '</div>' +
            '<div class="tabule--row" data-title="Cíl defektivita">' + device.CilDefectTabule + '</div>' +
            '<div class="tabule--row" data-title="Aktualni defektivita">' + device.AktualDefectTabuleTxt + '</div>' +
            '<div class="tabule--row">' +device.MachineStatusTxt + '</div>' +
        '</div>';
        
        $('#tabule').append(html);
    });
}

function readRunConfig() {
    $.getJSON(uri + '/getConfig?')
        .done(function (data) {
            isTimerEnabled = data.IsRefreshTimerOn;
            var timerRefreshPeriod = data.RefreshTimerPeriod;

            if (isTimerEnabled && (timerRefreshPeriod > 0)) {
                if (refreshTimer) window.clearInterval(refreshTimer);
                refreshTimer = window.setInterval(getAllTabule, timerRefreshPeriod);
            }
            else {
                if (refreshTimer) {
                    window.clearInterval(refreshTimer);
                }
            }

            //window.slVyr.addNotification('success', 'Sucessfully read config..');
        })
        .fail(function (jqXHR, textStatus, err) {
            window.slVyr.addNotification('error', 'ReadRunConfig - error: ' + err);
        });
}