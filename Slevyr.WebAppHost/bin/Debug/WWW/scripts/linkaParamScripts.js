var uri = 'api/slevyr';
var isTimerEnabled = false;
var timerRefreshPeriod = 10000;
var refreshTimer;
var addr = null;

$(document).ready(function () {
        readRunConfig();

        $("#AddrIdDropDown").change(onAddrIdChange);
       
        $("#SaveParams").click(saveUnitConfig);
        $("#LoadParams").click(readUnitConfig);

        $("#NastavCileSmen").click(nastavCileSmen);
        $("#NastavPrestavky").click(nastavPrestavkySmen);
        $("#NastavDefektivitu").click(nastavDefektivitu);
        $("#NastavStatus").click(nastavStatus);

        $("#SyncTime").click(nastavAktualniCas);
    });

    function readRunConfig() {
        //alert('readConfig');
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

                $.each(data.UnitAddrs, function (key, value) {
                    $('#AddrIdDropDown')
                        .append($("<option></option>")
                                   .attr("value", value)
                                   .text(value));
                });

                onAddrIdChange();

            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("refreshStatus - error");
            });
    }    

    function onAddrIdChange() {
        addr = $("#AddrIdDropDown option:selected").val();
        readUnitConfig();
    }

    function readUnitConfig() {
        //alert('readUnitConfig'+addr);
        //var addr = $('#addrId').val();
        if (typeof addr != 'undefined' && addr != null && addr.length > 1)
        $.getJSON(uri + '/LoadUnitConfig', {
            addr: addr
            })
            .done(function (data) {

                $('#TypSmennosti').val(data.TypSmennosti);
                $('#LinkaName').text(data.UnitName);
                $('#LinkaNameEdit').val(data.UnitName);                
                $('#Cil1Smeny').val(data.Cil1Smeny);
                $('#Cil2Smeny').val(data.Cil2Smeny);
                $('#Cil3Smeny').val(data.Cil3Smeny);
                $('#Def1Smeny').val(data.Def1Smeny);
                $('#Def2Smeny').val(data.Def2Smeny);
                $('#Def3Smeny').val(data.Def3Smeny);

                $('#Prestavka1Smeny').val(data.Prestavka1Smeny);
                $('#Prestavka2Smeny').val(data.Prestavka2Smeny);
                $('#Prestavka3Smeny').val(data.Prestavka3Smeny);

                $('#Zacatek1Smeny').val(data.Zacatek1Smeny);
                $('#Zacatek2Smeny').val(data.Zacatek2Smeny);
                $('#Zacatek3Smeny').val(data.Zacatek3Smeny);

                $('#WriteProtectEEprom').val(data.WriteProtectEEprom);

                $('#MinOk').val(data.MinOk);

                $('#MinNg').val(data.MinNG);

                $('#BootloaderOn').val(data.BootloaderOn);
                $('#rozliseniCidelTeploty').val(data.RozliseniCidel);
                $('#PracovniJasLed').val(data.PracovniJasLed);
                $('#addrParovanyLED').val(data.ParovanyLED);

            })
            .fail(function (jqXHR, textStatus, err) {
                $('#stav').text('Error: ' + err);
                alert("readUnitConfig - error");
            });
    }

    function saveUnitConfig() {
        //alert('saveUnitConfig');

        var model = {
            Addr: addr,
            UnitName: $('#LinkaNameEdit').val(),
            TypSmennosti: $('#TypSmennosti').val(),
            Cil1Smeny: $('#Cil1Smeny').val(),
            Cil2Smeny: $('#Cil2Smeny').val(),
            Cil3Smeny: $('#Cil3Smeny').val(),
            Def1Smeny: $('#Def1Smeny').val(),
            Def2Smeny: $('#Def2Smeny').val(),
            Def3Smeny: $('#Def3Smeny').val(),
            Prestavka1Smeny: $('#Prestavka1Smeny').val(),
            Prestavka2Smeny: $('#Prestavka2Smeny').val(),
            Prestavka3Smeny: $('#Prestavka3Smeny').val(),
            Zacatek1Smeny: $('#Zacatek1Smeny').val(),
            Zacatek2Smeny: $('#Zacatek2Smeny').val(),
            Zacatek3Smeny: $('#Zacatek3Smeny').val(),
            WriteProtectEEprom: $('#WriteProtectEEprom').val(),
            MinOK: $('#MinOk').val(),
            MinNG: $('#MinNg').val(),
            BootloaderOn: $('#BootloaderOn').val(),
            ParovanyLED: $('#addrParovanyLED').val(),
            RozliseniCidel: $('#rozliseniCidel').val(),
            PracovniJasLed: $('#PracovniJasLed').val()
        };

        $.ajax({
            type: "POST",
            data: JSON.stringify(model),
            url: uri + "/saveUnitConfig",
            contentType: "application/json"
        }).done(function (res) {
            $('#stav').text('');
            $('#LinkaName').text(model.UnitName);
            console.log('res', res);
            // Do something with the result :)
        }).fail(function (jqXHR, textStatus, err) {
            $('#stav').text('Error: ' + err);
            alert("saveUnitConfig - error");
        });       
    }

    function nastavCileSmen() {
        //alert('nastavCileSmen');
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
        //alert('nastavPrestavkySmen');
        var varianta = $('#typSmennosti').val();
        var p1Smeny = $('#Prestavka1Smeny').val();
        var p2Smeny = $('#Prestavka2Smeny').val();
        var p3Smeny = $('#Prestavka3Smeny').val();
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
        //alert('nastavCitaceOkNg');
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
                addr: addr,
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
                addr: addr
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

   
 

