var uri = 'api/slevy';
var isTimerEnabled = false;
var timerRefreshPeriod = 10000;
var refreshTimer;

    $(document).ready(function () {
        readRunConfig();
       
        $("#RefreshStatus").click(refreshStatus);
        $("#NastavCileSmen").click(nastavCileSmen);
        $("#NastavDefektivitu").click(nastavDefektivitu);
        $("#NastavStatus").click(nastavStatus);
        $("#SyncTime").click(nastavAktualniCas);
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
        //alert('status, addr=' + addr);
        $.getJSON(uri + '/refreshStatus?addr=' + addr)
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

    function nastavCileSmen() {
        //alert('nastavCileSmen');
        var addr = $('#addrId').val();
        var varianta  = $('#typSmennosti').val();
        var cil1Smeny = $('#Cil1Smeny').val();
        var cil2Smeny = $('#Cil2Smeny').val();
        var cil3Smeny = $('#Cil3Smeny').val();
        $.getJSON(uri + '/nastavCileSmen',
            {
                addr: addr,
                varianta: varianta,
                cil1: cil1Smeny,
                cil2: cil2Smeny,
                cil3: cil3Smeny
            })
            .done(function (data) {
                $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("nastavCileSmen - error");
            });
    }

    function nastavCitaceOkNg() {
        //alert('nastavCitaceOkNg');
        var addr = $('#addrId').val();
        var ok = $('#ok').val();
        var ng = $('#ng').val();
        $.getJSON(uri + '/NastavOkNg',
            {
                addr: addr,
                ok: ok,
                ng: ng
            })
            .done(function (data) {
                $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("nastavCitaceOkNg - error");
            });
    }

    function nastavDefektivitu() {
        //alert('nastavDefektivitu');
        var addr = $('#addrId').val();
        var varianta = $('#typSmennosti').val();
        var def1Smeny = $('#Def1Smeny').val();
        var def2Smeny = $('#Def2Smeny').val();
        var def3Smeny = $('#Def3Smeny').val();
        $.getJSON(uri + '/nastavDefektivitu',
            {
                addr: addr,
                varianta: varianta,
                def1: def1Smeny,
                def2: def2Smeny,
                def3: def3Smeny
            })
            .done(function (data) {
                $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("nastavDefektivitu - error");
            });
    }

    function nastavStatus() {
        //alert('nastavStatus');
        $.getJSON(uri + '/nastavStatus',
            {
                addr: $('#addrId').val(),
                writeProtectEEprom: $('#writeProtectEEprom').val(),
                minOK: $('#minOK').val(),
                minNG: $('#minNG').val(),
                bootloaderOn: $('#bootloaderOn').val(),
                parovanyLED: $('#addrParovanyLED').val(),
                rozliseniCidel: $('#rozliseniCidelTeploty').val(),
                pracovniJasLed: $('#pracovniJasLed').val()
            })
            .done(function (data) {
                $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("nastavStatus - error");
            });
    }

    function nastavAktualniCas() {
        //alert('nastavAktualniCas');
        $.getJSON(uri + '/nastavAktualniCas',
            {
                addr: $('#addrId').val()
            })
            .done(function (data) {
                $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("'); - error");
            });
    }


    function openPort() {
        //alert("openPort");

        $.getJSON(uri + '/openPort',
            {                
            })
            .done(function (data) {
                $('#stav').text('serial port open');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("'); - error");
            });
    }

    function closePort() {
        //alert("closePort");

        $.getJSON(uri + '/closePort',
            {
            })
            .done(function (data) {
                $('#stav').text('serial port closed');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("'); - error");
            });
    }