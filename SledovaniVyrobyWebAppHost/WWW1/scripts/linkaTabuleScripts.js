var uri = 'api/slevy';
var isTimerEnabled = false;
var timerRefreshPeriod = 10000;
var refreshTimer;

    $(document).ready(function () {
        readRunConfig();
       
        $("#RefreshStatus").click(refreshStatus);
        
    });

    function readRunConfig() {
        //alert('readConfig');
        $.getJSON(uri + '/getConfig?')
            .done(function (data) {
                isTimerEnabled = data.IsRefreshTimerOn;
                timerRefreshPeriod = data.RefreshTimerPeriod;

                if (isTimerEnabled && (timerRefreshPeriod > 0)) {
                    //alert('set timer to ' + timerRefreshPeriod);
                    if (refreshTimer) window.clearInterval(refreshTimer);                    
                    refreshTimer = window.setInterval(refreshStatus, timerRefreshPeriod * 1000);
                }
                else {
                    if (refreshTimer) {
                        //alert('clear timer');
                        window.clearInterval(refreshTimer);
                    }
                }

                if (data.IsMockupMode) {
                    closePort();
                    $("#isMockupMode").text(" [mockup]");
                } else {
                    openPort();
                    $("#isMockupMode").text("");
                }
                if (isTimerEnabled) {
                    $("#isTimerOn").text(" [timer on]");

                } else {
                    $("#isTimerOn").text("");
                }

            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("refreshStatus - error");
            });
    }

    function refreshStatus() {
        var addr = $('#addrId').val();
        alert('status, Addr=' + addr);
        $.getJSON(uri + '/refreshStatus?Addr=' + addr)
            .done(function (data) {
                //alert('status,okNumValue =' + data.Ok);
                $('#okNumValue').text(data.Ok);
                $('#ngNumValue').text(data.Ng);
                $('#okNgRefreshTime').text(new Date().toLocaleTimeString());
                $('#casOkValue').text(data.CasOk);
                $('#casNgValue').text(data.CasNg);

                $('#cilTabule').text(data.CilTabule);
                $('#rozdilTabule').text(data.RozdilTabule);
                $('#cilDefTabule').text(data.DefectTabule + "%");
                //$('#aktualniDefTabule').text(data.ActDefectTabule);

                $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("refreshStatus - error");
            });
    }


  

    //function openPort() {
    //    //alert("openPort");

    //    $.getJSON(uri + '/openPort',
    //        {                
    //        })
    //        .done(function (data) {
    //            $('#stav').text('serial port open');
    //        })
    //        .fail(function (jqXHR, textStatus, err) {
    //            $('#stav').text('Error: ' + err);
    //            alert("'); - error");
    //        });
    //}

    //function closePort() {
    //    //alert("closePort");

    //    $.getJSON(uri + '/closePort',
    //        {
    //        })
    //        .done(function (data) {
    //            $('#stav').text('serial port closed');
    //        })
    //        .fail(function (jqXHR, textStatus, err) {
    //            $('#stav').text('Error: ' + err);
    //            alert("'); - error");
    //        });
    //}
