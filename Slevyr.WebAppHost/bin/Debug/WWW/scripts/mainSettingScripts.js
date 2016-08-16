var uri = 'api/slevyr';

    $(document).ready(function () {
        readRunConfig();
        $("#Apply").click(applySettings);
    });

    function readRunConfig() {
        //alert('readConfig');
        $.getJSON(uri + '/getConfig?')
            .done(function (data) {
                $('#isMockupMode').prop('checked',data.IsMockupMode);
                $('#isTimerOn').prop('checked',data.IsRefreshTimerOn);
                $('#timerPeriod').val(data.RefreshTimerPeriod);
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#error').text('Error: ' + err);
                alert("refreshStatus - error");
            });
    }


    function applySettings() {
        //alert('applySettings');
        $.getJSON(uri + '/setConfig',
            {
                isMockupMode: $('#isMockupMode').prop('checked'),
                isTimerOn: $('#isTimerOn').prop('checked'),
                timerPeriod: $('#timerPeriod').val()
            })
            .done(function (data) {
                $('#error').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#error').text('Error: ' + err);
                alert("setConfig - error");
            });
 
    }