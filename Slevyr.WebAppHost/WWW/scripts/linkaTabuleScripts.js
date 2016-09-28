var uri = 'api/slevyr';
var isTimerEnabled = false;
var refreshTimer;
var addr = null;

    $(document).ready(function () {
        readRunConfig();

        //readUnitConfig();
       
        $("#RefreshStatus").click(refreshStatus);

        $("#GetStatus").click(getStatus);

        $("#AddrIdDropDown").change(onAddrIdChange);

        jQuery.ajaxSetup({ cache: false });

    });

    function readRunConfig() {
        //alert('readConfig')
        $.getJSON(uri + '/getConfig?')
            .done(function (data) {
                isTimerEnabled = data.IsRefreshTimerOn;
                timerRefreshPeriod = data.RefreshTimerPeriod;

                if (isTimerEnabled && (timerRefreshPeriod > 0)) {
                    //alert('set timer to ' + timerRefreshPeriod);
                    if (refreshTimer) window.clearInterval(refreshTimer);                    
                    refreshTimer = window.setInterval(getStatus, timerRefreshPeriod);
                }
                else {
                    if (refreshTimer) {
                        //alert('clear timer');
                        window.clearInterval(refreshTimer);
                    }
                }

                if (isTimerEnabled) {
                    $("#isTimerOn").text(" [timer on]");
                    $("#RefreshStatus").prop('disabled', true);

                } else {
                    $("#isTimerOn").text("");
                    $("#RefreshStatus").prop('disabled', false);
                }

                if (data.IsReadOkNgTime) {
                    $('#casOkValue').show();
                    $('#casOkLabel').show();                    
                    $('#casNgValue').show();
                    $('#casNgLabel').show();
                } else {
                    $('#casOkValue').hide();
                    $('#casNgValue').hide();
                    $('#casOkLabel').hide();
                    $('#casNgLabel').hide();

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
        $.getJSON(uri + '/GetUnitConfig?', {addr: addr})
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
        getStatus();
    }

    function clearStatus() {
        $('#stav').text('pracuji..');
    }

    function refreshStatus() {
        clearStatus();
        //alert('status, Addr=' + addr);
        $.getJSON(uri + '/refreshStatus?Addr=' + addr)
            .done(function (data) {
                updateTableElements(data);
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                //alert("refreshStatus - error");
            });
    }

    function getStatus() {
        //clearStatus();
        //alert('status, Addr=' + addr);
        $.getJSON(uri + '/getStatus?Addr=' + addr)
            .done(function (data) {
                //alert('status,okNumValue =' + data.Ok);
                updateTableElements(data);
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                //alert("getStatus - error");
            });
    }

    function updateTableElements(data) {
        if (data.IsOkNg) {
            $('#okNumValue').text(data.Ok);
            $('#ngNumValue').text(data.Ng);
            $('#okNgRefreshTime').text(data.OkNgTimeTxt);
            $('#chybaJednotky').text('');
        } else {
            $('#chybaJednotky').text('Chyba jednotky - ' + data.ErrorTimeTxt);
        }

        if (data.IsPrestavkaTabule) {
            $('#isPrestavkaTabule').show();
        } else {
            $('#isPrestavkaTabule').hide();
        }

        $('#casOkValue').text(Number((data.CasOk).toFixed(1)) + 's');
        $('#casNgValue').text(Number((data.CasNg).toFixed(1)) + 's');
        $('#checkTime').text(data.LastCheckTimeTxt);

        $('#cilTabule').text(data.CilKusuTabule);
        $('#rozdilTabule').text(data.RozdilTabuleTxt);
        $('#cilDefTabule').text(data.CilDefectTabule + "%");
        $('#aktualniDefTabule').text(data.AktualDefectTabuleTxt + "%");

        //$('#stav').text('');
        if (data.MachineStatus == 0)
            $('#stav').text('');
        else
            $('#stav').text(data.MachineStatusTxt);
    }

