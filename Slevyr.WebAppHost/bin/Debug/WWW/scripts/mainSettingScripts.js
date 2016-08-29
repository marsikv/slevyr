var uri = 'api/slevyr';

    $(document).ready(function () {
        readRunConfig();
        $("#Apply").click(applySettings);
    });

    function readRunConfig() {
        //alert('readConfig');
        $.getJSON(uri + '/getConfig?')
            .done(function (data) {
                //$('#isMockupMode').prop('checked',data.IsMockupMode);
                $('#isTimerOn').prop('checked',data.IsRefreshTimerOn);
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
                RelaxTime: $('#relaxTime').val(),
                PortReadTimeout: $('#portReadTimeout').val(),
            })
            .done(function (data) {
                $('#error').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#error').text('Error: ' + err);
                alert("setConfig - error");
            });
 
    }