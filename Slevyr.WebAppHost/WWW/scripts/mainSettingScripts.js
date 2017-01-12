var uri = 'api/slevyr';

    $(document).ready(function () {
        readRunConfig();
        $("#Apply").click(applySettings);
        $("#SyncTime").click(nastavAktualniCas);
        jQuery.ajaxSetup({ cache: false });
    });

    function readRunConfig() {
        //alert('readConfig');
        $.getJSON(uri + '/getConfig?')
            .done(function (data) {
                //$('#isMockupMode').prop('checked',data.IsMockupMode);
                $('#isTimerOn').prop('checked', data.IsRefreshTimerOn);
                $('#isReadOkNgTime').prop('checked', data.IsReadOkNgTime);
                $('#timerPeriod').val(data.RefreshTimerPeriod);
                $('#relaxTime').val(data.RelaxTime);
                $('#portReadTimeout').val(data.PortReadTimeout);

                window.slVyr.addNotification('success', 'Successufully Load Configuration.');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'Load Configuration Error: ' + err);
                //$('#error').text('Error: ' + err);
                //alert("getConfig - error");
            });
    }


    function applySettings() {
        //alert('applySettings');
        $.getJSON(uri + '/setConfig',
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
                window.slVyr.addNotification('success', 'Set Configuration Successufully Done.');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'Set Configuration Error: ' + err);
                // $('#error').text('Error: ' + err);
                // alert("setConfig - error");
            });
    }

    function nastavAktualniCas() {
        //alert('nastavAktualniCas pro vsechny jednotky');
        $.getJSON(uri + '/NastavAktualniCasAllUnits',
            {
            })
            .done(function (data) {
                window.slVyr.addNotification('success', 'Successufully Synchronized.');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'Synchronization Error: ' + err);
                // $('#stav').text('Error: ' + err);
                // alert("'); - error");
            });
    }