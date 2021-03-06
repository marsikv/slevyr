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
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#error').text('Error: ' + err);
                alert("getConfig - error");
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
                $('#error').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#error').text('Error: ' + err);
                alert("setConfig - error");
            }); 
    }

    function nastavAktualniCas() {
        alert('nastavAktualniCas pro vsechny jednotky');
        $.getJSON(uri + '/NastavAktualniCasAllUnits',
            {
            })
            .done(function (data) {
                $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("'); - error");
            });
    }