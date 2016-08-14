var uri = 'api/slevy';
var isTimerEnabled = false;
var timerRefreshPeriod = 10000;
var refreshTimer;

$(document).ready(function () {
        readRunConfig();
        loadParamsFromFile();
       
        $("#SaveParams").click(saveParamsToFile);

        $("#NastavCileSmen").click(nastavCileSmen);
        $("#NastavPrestavky").click(nastavPrestavkySmen);
        $("#NastavDefektivitu").click(nastavDefektivitu);
        $("#NastavStatus").click(nastavStatus);

        $("#SyncTime").click(nastavAktualniCas);
    });

    function readRunConfig() {
        alert('readConfig');
        $('#stav').text('');
        $.getJSON(uri + '/getConfig?')
            .done(function (data) {

                if (data.IsMockupMode) {
                    closePort();
                    $("#isMockupMode").text(" [mockup]");
                } else {
                    openPort();
                    $("#isMockupMode").text("");
                }
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("refreshStatus - error");
            });
    }

    function readLinkaParams() {
        alert('readLinkaParamsX');
        var addr = $('#addrId').val();
        $.getJSON(uri + '/getLinkaParams?', {
            addr: addr
            })
            .done(function (data) {

                $('#TypSmennosti').text(data.typSmennosti);
                $('#Cil1Smeny').text(data.cil1Smeny);
                $('#Cil2Smeny').text(data.cil2Smeny);
                $('#Cil3Smeny').text(data.cil3Smeny);
                $('#Def1Smeny').text(data.def1Smeny);
                $('#Def2Smeny').text(data.def2Smeny);
                $('#Def3Smeny').text(data.def3Smeny);

                $('#Prestavka1Smeny').text(data.prestavka1Smeny);
                $('#Prestavka2Smeny').text(data.prestavka2Smeny);
                $('#Prestavka3Smeny').text(data.prestavka3Smeny);

                $('#WriteProtectEEprom').text(data.writeProtectEEprom);

                $('#MinOk').text(data.minOK);

                $('#MinNg').text(data.minNG);

                $('#BootloaderOn').text(data.bootloaderOn);
                $('#rozliseniCidelTeploty').text(data.rozliseniCidelTeploty);
                $('#PracovniJasLed').text(data.pracovniJasLed);
                $('#addrParovanyLED').text(data.addrParovanyLED);

            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("readLinkaParams - error");
            });
    }

    function saveParamsToFile() {
        alert('saveParamsToFile');
        $.getJSON(uri + '/saveLinkaParams',
            {
                addr:  $('#addrId').val(),
                typSmennosti: $('#TypSmennosti').val(),
                cil1Smeny: $('#Cil1Smeny').val(),
                cil2Smeny: $('#Cil2Smeny').val(),
                cil3Smeny: $('#Cil3Smeny').val(),
                def1Smeny: $('#Def1Smeny').val(),
                def2Smeny: $('#Def2Smeny').val(),
                def3Smeny: $('#Def3Smeny').val(),
                prestavka1Smeny: $('#Prestavka1Smeny').val(),
                prestavka2Smeny: $('#Prestavka2Smeny').val(),
                prestavka3Smeny: $('#Prestavka3Smeny').val(),

                writeProtectEEprom: $('#WriteProtectEEprom').val(),
                minOK: $('#MinOk').val(),
                minNG: $('#MinNg').val(),
                bootloaderOn: $('#BootloaderOn').val(),
                parovanyLED: $('#addrParovanyLED').val(),
                rozliseniCidel: $('#rozliseniCidelTeploty').val(),
                pracovniJasLed: $('#PracovniJasLed').val()
            })
            .done(function (data) {
                $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("nastavStatus - error");
            });
    }

    function nastavCileSmen() {
        alert('nastavCileSmen');
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

    function nastavPrestavkySmen() {
        alert('nastavPrestavkySmen');
        var addr = $('#addrId').val();
        var varianta = $('#typSmennosti').val();
        var p1Smeny = $('#Prest1Smeny').val();
        var p2Smeny = $('#Prest2Smeny').val();
        var p3Smeny = $('#Prest3Smeny').val();
        $.getJSON(uri + '/NastavPrestavkySmen',
            {
                addr: addr,
                varianta: varianta,
                prest1: p1Smeny,
                prest2: p2Smeny,
                prest3: p3Smeny
            })
            .done(function (data) {
                $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("nastavPrestavkySmen - error");
            });
    }

    function nastavCitaceOkNg() {
        alert('nastavCitaceOkNg');
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
        alert('nastavDefektivitu');
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
        alert('nastavStatus');
        $.getJSON(uri + '/nastavStatus',
            {
                addr: $('#addrId').val(),
                writeProtectEEprom: $('#WriteProtectEEprom').val(),
                minOK: $('#MinOk').val(),
                minNG: $('#MinNg').val(),
                bootloaderOn: $('#BootloaderOn').val(),
                parovanyLED: $('#addrParovanyLED').val(),
                rozliseniCidel: $('#rozliseniCidelTeploty').val(),
                pracovniJasLed: $('#PracovniJasLed').val()
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
        alert('nastavAktualniCas');
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

    function loadParamsFromFile() {
    }

 

