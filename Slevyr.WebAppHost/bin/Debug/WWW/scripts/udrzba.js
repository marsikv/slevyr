var uriUdrzba = 'api/udrzba';
var uriSlevyr = 'api/slevyr';
var uriSys = 'api/sys';

var tabule = $('#udrzba');
var isTimerEnabled = false;
var refreshTimer;

$(document).ready(function () {
    
    readRunConfig();

    jQuery.ajaxSetup({ cache: false });
  
    getAllTabule();

});

function getAllTabule() {
    $.getJSON(uriUdrzba + '/GetAllUdrzba')
              .done(function (data) {
                  formatTable(data);
              })
              .fail(function (jqXHR, textStatus, err) {
                  window.slVyr.addNotification('error', 'Load udrzba Error: ' + err);
              });
}

function formatTable(data) {
    $('#udrzba').empty();

    if (data.length <= 0) {
        var html = '<div class="tabule">' +
            //'<div class="tabule--NameBig">Žádná porucha nebo servis stroje</div>' +
            '<div> <h1>Žádná porucha nebo servis stroje</h1></div>' +
            '<div class=""> <img src="images/steam.png" title="Žádná porucha nebo servis" alt=""></div>' +
        '</div>';

        $('#udrzba').append(html);
        return;
    }

    data.forEach(function (device) {
        var html = '<div class="tabule">' +
            '<div class="tabule--Name">' + device.LinkaName + '</div>' +
            '<div class="tabule--row" data-title="Stav">' + device.MachineStatusTxt + '</div>' +
            '<div class="tabule--row" data-title="Čas odkdy">' + device.MachineStopTimeTxt + '</div>' +
            '<div class="tabule--row" data-title="Doba trvání">' + device.MachineStopDuration + ' sec.</div>' +
        '</div>';
        
        $('#udrzba').append(html);
    });
}

function readRunConfig() {
    $.getJSON(uriSys + '/getRunConfig?')
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