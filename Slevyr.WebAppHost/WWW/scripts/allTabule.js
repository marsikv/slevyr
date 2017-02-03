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


    //<a href="index.html">Home</a>

    data.forEach(function (unitTabule) {
        var html = '<div class="tabule">' +
            //'<div class="tabule--Name">' + unitTabule.LinkaName + '</div>' +
            '<div class="tabule--Name">' + '<a href="linka-tabule.html#'+unitTabule.Addr+'">'+unitTabule.LinkaName+'</a>' + '</div>' +
            '<div class="tabule--row" data-title="Cíl">' + unitTabule.CilKusuTabule + '</div>' +
            '<div class="tabule--row" data-title="Rozdíl">' + unitTabule.RozdilTabuleTxt + '</div>' +
            '<div class="tabule--row" data-title="Cíl defektivita">' + unitTabule.CilDefectTabule + '</div>' +
            '<div class="tabule--row" data-title="Aktualni defektivita">' + unitTabule.AktualDefectTabuleTxt + '</div>';
       
        if (unitTabule.IsPrestavkaTabule) {
            html = html + '<div class="tabule--rowCenterEnhanced"> Přestávka </div>';
        } else {
            html = html + '<div class="tabule--rowCenter">' + unitTabule.MachineStatusTxt + '</div>';
        }

        html = html + '</div>';
        
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