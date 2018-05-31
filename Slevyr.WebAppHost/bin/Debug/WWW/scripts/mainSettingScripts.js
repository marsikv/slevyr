var uri = 'api/slevyr';
var uriSys = 'api/sys';
var uriSound = 'api/sound';

    $(document).ready(function () {
        readRunConfig();
        $("#Apply").click(applySettings);
        $("#SyncTime").click(nastavAktualniCas);
        $("#ResetRf").click(resetRf);
        $("#SaveState").click(saveUnitStatus);
        $("#RestoreState").click(restoreUnitStatus);

        $("#StartAlarm").click(startAlarm);
        $("#StopAlarm").click(stopAlarm);
        jQuery.ajaxSetup({ cache: false });
    });

    function readRunConfig() {
        //alert('readConfig');
        $.getJSON(uriSys + '/getRunConfig?')
            .done(function (data) {
                //$('#isMockupMode').prop('checked',data.IsMockupMode);
                $('#isTimerOn').prop('checked', data.IsRefreshTimerOn);
                $('#isReadOkNgTime').prop('checked', data.IsReadOkNgTime);
                $('#timerPeriod').val(data.RefreshTimerPeriod);
                $('#relaxTime').val(data.RelaxTime);
                $('#portReadTimeout').val(data.PortReadTimeout);

                //window.slVyr.addNotification('success', 'Successufully Load Configuration.');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'Load Configuration Error: ' + err);
                //$('#error').text('Error: ' + err);
            });
    }


    function applySettings() {
        //alert('applySettings');
        $.getJSON(uriSys + '/setRunConfig',
            {
                //isMockupMode: $('#isMockupMode').prop('checked'),
                isMockupMode: false,
                isTimerOn: $('#isTimerOn').prop('checked'),
                RefreshTimerPeriod: $('#timerPeriod').val(),
                ReadCasOkNg: $('#isReadOkNgTime').prop('checked')
                //RelaxTime: $('#relaxTime').val(),
                //PortReadTimeout: $('#portReadTimeout').val(),
            })
            .done(function (data) {
                window.slVyr.addNotification('success', 'Nastaveni konfigurace provedeno.');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'Set Configuration Error: ' + err);
                // $('#error').text('Error: ' + err);
            });
    }

    function nastavAktualniCas() {
        //alert('nastavAktualniCas pro vsechny jednotky');
        $.getJSON(uri + '/nastavAktualniCasAllUnits',
            {
            })
            .done(function (data) {
                window.slVyr.addNotification('success', 'Pozadavek na synchronizaci casu byl odeslan.');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'Synchronization Error: ' + err);
            });
    }

    function resetRf() {
        $.getJSON(uriSys + '/ResetRf',
            {
            })
            .done(function (data) {
                window.slVyr.addNotification('success', 'Požadavek na reset RF');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'Reset RF Error: ' + err);
            });
    }

    function saveUnitStatus() {
        $.getJSON(uriSys + '/SaveUnitStatus',
            {
            })
            .done(function (data) {
                window.slVyr.addNotification('success', 'Zpracován pozadavek na ulozeni stavu');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'Error: ' + err);
            });
    }

    function restoreUnitStatus() {
        $.getJSON(uriSys + '/RestoreUnitStatus',
            {
            })
            .done(function (data) {
                window.slVyr.addNotification('success', 'Zpracovan pozadavek na obnoveni stavu');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'Error: ' + err);
            });
    }

function startAlarm() {
    $.getJSON(uriSound + '/StartAlarm',
            {
            })
        .done(function (data) {
            window.slVyr.addNotification('success', 'Zpracovan pozadavek na spusteni poplachu, poplach se bude prehravat 15 minut');
        })
        .fail(function (jqXHR, textStatus, err) {
            window.slVyr.addNotification('error', 'Error: ' + err);
        });
    }


function stopAlarm() {
    $.getJSON(uriSound + '/StopAlarm',
            {
            })
        .done(function (data) {
            window.slVyr.addNotification('success', 'Zpracovan pozadavek na ukonceni poplachu');
        })
        .fail(function (jqXHR, textStatus, err) {
            window.slVyr.addNotification('error', 'Error: ' + err);
        });
}