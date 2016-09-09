var uri = 'api/slevyr';
var isTimerEnabled = false;
var timerRefreshPeriod = 10000;
var refreshTimer;
var addr = null;

    $(document).ready(function () {
        readRunConfig();

        //readUnitConfig();
       
        $("#RefreshStatus").click(refreshStatus);

        $("#GetStatus").click(getStatus);

        $("#AddrIdDropDown").change(onAddrIdChange);

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
                    refreshTimer = window.setInterval(getStatus, timerRefreshPeriod * 1000);
                }
                else {
                    if (refreshTimer) {
                        //alert('clear timer');
                        window.clearInterval(refreshTimer);
                    }
                }

                //if (data.IsMockupMode) {
                //    closePort();
                //    $("#isMockupMode").text(" [mockup]");
                //} else {
                //    openPort();
                //    $("#isMockupMode").text("");
                //}

                if (isTimerEnabled) {
                    $("#isTimerOn").text(" [timer on]");

                } else {
                    $("#isTimerOn").text("");
                }

                $.each(data.UnitAddrs, function (key,value) {
                    $('#AddrIdDropDown')
                        .append($("<option></option>")
                                   .attr("value", value)
                                   .text(value));
                });

                onAddrIdChange();

                $('#stav').text('');

            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("getConfig - error");
            });
    }

    function readUnitConfig() {
        clearStatus();
        if (typeof addr != 'undefined' &&  addr != null && addr.length > 1)
        $.getJSON(uri + '/LoadUnitConfig?', {addr: addr})
            .done(function (data) {
                $('#LinkaName').text(data.UnitName);
                $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("loadParams - error");
            });
    }   

    function onAddrIdChange() {
        addr = $("#AddrIdDropDown option:selected").val();
        readUnitConfig();
    }

    function clearStatus() {
        $('#stav').text('Pracuji...');
        //$('#okNumValue').text("-");
        //$('#ngNumValue').text("-");
        //$('#okNgRefreshTime').text("-");
        //$('#casOkValue').text("-");
        //$('#casNgValue').text("-");

        //$('#cilTabule').text("-");
        //$('#rozdilTabule').text("-");
        //$('#cilDefTabule').text("-");
    }

    function refreshStatus() {
        clearStatus();
        //alert('status, Addr=' + addr);
        $.getJSON(uri + '/refreshStatus?Addr=' + addr)
            .done(function (data) {
                //alert('status,okNumValue =' + data.Ok);
                $('#okNumValue').text(data.Ok);
                $('#ngNumValue').text(data.Ng);
                //$('#okNgRefreshTime').text(new Date().toLocaleTimeString());
                $('#okNgRefreshTime').text(data.TimeStr);
                $('#casOkValue').text(data.CasOk);
                $('#casNgValue').text(data.CasNg);

                $('#cilTabule').text(data.CilKusuTabule);
                $('#rozdilTabule').text(data.RozdilTabule);
                $('#cilDefTabule').text(data.CilDefectTabule + "%");
                $('#aktualniDefTabule').text(data.AktualDefectTabule);

                $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("refreshStatus - error");
            });
    }

    function getStatus() {
        clearStatus();
        //alert('status, Addr=' + addr);
        $.getJSON(uri + '/getStatus?Addr=' + addr)
            .done(function (data) {
                //alert('status,okNumValue =' + data.Ok);
                $('#okNumValue').text(data.Ok);
                $('#ngNumValue').text(data.Ng);
                $('#okNgRefreshTime').text(new Date().toLocaleTimeString());
                $('#casOkValue').text(data.CasOk);
                $('#casNgValue').text(data.CasNg);

                $('#cilTabule').text(data.CilKusuTabule);
                $('#rozdilTabule').text(data.RozdilTabule);
                $('#cilDefTabule').text(data.CilDefectTabule + "%");
                $('#aktualniDefTabule').text(data.AktualDefectTabule);

                $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("getStatus - error");
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
