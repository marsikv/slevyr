var uri = 'api/slevyr';
var uriBase = 'api/slevyr';
var uriSys = 'api/sys';

var tabule = $('#tabule');
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

    data.forEach(function (unitTabule) {
        var html;

        if (unitTabule.MachineStatus == 90)
            html = '<div class="tabuleNedef">';
        else if (unitTabule.MachineStatus <= 1)
            html = '<div class="tabuleGood">';
        else
            html = '<div class="tabuleBad">';

        var rozdilClass = (unitTabule.RozdilTabule < 0) ? 'class="num--bad"' : '';
        var actDefectClass = (unitTabule.AktualDefectTabule > unitTabule.CilDefectTabule) ? 'class="num--bad"' : '';

        html = html +
            '<div class="tabule--Name">' + '<a href="linka-tabule.html#'+unitTabule.Addr+'">'+unitTabule.LinkaName+'</a>' + '</div>' +
            '<div class="tabule--row" data-title="Cíl">' + unitTabule.CilKusuTabule + '</div>' +
            '<div class="tabule--row" data-title="Rozdíl"><span ' + rozdilClass + '>' + unitTabule.RozdilTabuleTxt + '</span></div>' +
            '<div class="tabule--row" data-title="Cíl defektivita">' + unitTabule.CilDefectTabule + '</div>' +
            '<div class="tabule--row" data-title="Aktualni defektivita"><span ' + actDefectClass + '>' + unitTabule.AktualDefectTabuleTxt + '</span></div>';
       
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