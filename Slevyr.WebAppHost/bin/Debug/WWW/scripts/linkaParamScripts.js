var uri = 'api/slevyr';
var uriExport = 'api/export';
var uriSys = 'api/sys';

var isTimerEnabled = false;
var refreshTimer;
var addr = null;
var typSmennostiIsA = true;

$(document).ready(function () {
        readRunConfig();

        $("#AddrIdDropDown").change(onAddrIdChange);

        $("#SaveParams").click(saveUnitConfig);
        $("#LoadParams").click(readUnitConfig);

        $("#NastavCileSmen").click(nastavCileSmen);
        $("#NastavPocOkNg").click(nastavPocOkNg);
        $("#NastavPrestavkyA").click(nastavPrestavkySmenA);
        $("#NastavPrestavkyB").click(nastavPrestavkySmenB);
        $("#NastavDefektivitu").click(nastavDefektivitu);
        $("#NastavJednotku").click(nastavJednotku);
        $("#NastavTypSmennosti").click(nastavVariantuSmeny);

        $("#SyncTime").click(nastavAktualniCas);

        jQuery.ajaxSetup({ cache: false });
});


function updateElementsForTypSmennosti(typSmennostiIsA) {
    if (typSmennostiIsA) {
        $('#Cil3SmenyDiv').show();
        $('#Def3SmenyDiv').show();
        $('#Zacatek3SmenyDiv').show();
        $('#PrestavkyA').show();
        $('#PrestavkyB').hide();
    } else {
        $('#Cil3SmenyDiv').hide();
        $('#Def3SmenyDiv').hide();
        $('#Zacatek3SmenyDiv').hide();
        $('#PrestavkyA').hide();
        $('#PrestavkyB').show();
    }
}

function readRunConfig() {
        //alert('readConfig');
        $('#stav').text('');
        $.getJSON(uriSys + '/getRunConfig?')
            .done(function (data) {

                if (data.IsMockupMode) {
                    //closePort();
                    $("#isMockupMode").text(" [mockup]");
                } else {
                    //openPort();
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
                window.slVyr.addNotification('error', 'RefreshStatus - error: ' + err);
                // $('#stav').text('Error: ' + err);
                // alert("refreshStatus - error");
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
        $.getJSON(uriSys + '/GetUnitConfig', {
            addr: addr
            })
            .done(function (data) {
                typSmennostiIsA = data.TypSmennosti === 'A';
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

                $('#WriteProtectEEprom').prop('checked', data.WriteProtectEEprom);

                $('#MinOK').val(data.MinOK);

                $('#MinNG').val(data.MinNG);

                $('#BootloaderOn').prop('checked', data.BootloaderOn);
                $('#RozliseniCidelTeploty').val(data.RozliseniCidel);
                $('#PracovniJasLed').val(data.PracovniJasLed);
                $('#AddrParovanyLed').val(data.ParovanyLED);

                $('#Prestavka1Smeny1').val(data.Prestavka1Smeny1);
                $('#Prestavka1Smeny2').val(data.Prestavka1Smeny2);
                $('#Prestavka2Po').val(data.Prestavka2Po);

                updateElementsForTypSmennosti(typSmennostiIsA);

                window.slVyr.addNotification('success', 'ReadUnitConfig Successufully done.');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'ReadUnitConfig - error: ' + err);
                // $('#stav').text('Error: ' + err);
                // alert("readUnitConfig - error");
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
            WriteProtectEEprom:  $('#WriteProtectEEprom').prop('checked'),
            MinOK: $('#MinOK').val(),
            MinNG: $('#MinNG').val(),
            BootloaderOn: $('#BootloaderOn').prop('checked'),
            ParovanyLED: $('#AddrParovanyLed').val(),
            RozliseniCidel: $('#RozliseniCidelTeploty').val(),
            PracovniJasLed: $('#PracovniJasLed').val(),
            Prestavka1Smeny1: $('#Prestavka1Smeny1').val(),
            Prestavka1Smeny2: $('#Prestavka1Smeny2').val(),
            Prestavka2Po: $('#Prestavka2Po').val()
        };

        typSmennostiIsA = model.TypSmennosti === 'A';

        updateElementsForTypSmennosti(typSmennostiIsA);

        $.ajax({
            type: "POST",
            data: JSON.stringify(model),
            url: uriSys + "/saveUnitConfig",
            contentType: "application/json"
        }).done(function (res) {
            $('#stav').text('');
            $('#LinkaName').text(model.UnitName);
            console.log('res', res);
            // Do something with the result :)
        }).fail(function (jqXHR, textStatus, err) {
            window.slVyr.addNotification('error', 'SaveUnitConfig - error: ' + err);
            // $('#stav').text('Error: ' + err);
        });
    }

    function nastavPocOkNg() {
        var pocOk = $('#PocetOk').val();
        var pocNg = $('#PocetNg').val();
        $.getJSON(uri + '/nastavOkNg',
            {
                addr: addr,
                ok: pocOk,
                ng: pocNg,
            })
            .done(function (data) {
                window.slVyr.addNotification('success', 'Sucessfully set PocetOkNg');
                 $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'nastavPocOkNg - error: ' + err);
                $('#stav').text('Error: ' + err);
                alert("nastavPocOkNg - error");
            });
    }

    function nastavVariantuSmeny() {
        //alert('NastavVariantuSmeny');
        var typSmennosti = $('#TypSmennosti').val();
        $.getJSON(uri + '/nastavVariantuSmeny',
            {
                addr: addr,
                varianta: typSmennosti
            })
            .done(function (data) {
                window.slVyr.addNotification('success', 'Sucessfully set.');
                typSmennostiIsA = typSmennosti === 'A';
                updateElementsForTypSmennosti(typSmennostiIsA);
                // $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'nastavVariantuSmeny - error: ' + err);
                // $('#stav').text('Error: ' + err);
                // alert("nastavCileSmen - error");
            });
    }

    function nastavCileSmen() {
        //alert('nastavCileSmen');
        var typSmennosti  = $('#TypSmennosti').val();
        var cil1Smeny = $('#Cil1Smeny').val();
        var cil2Smeny = $('#Cil2Smeny').val();
        var cil3Smeny = $('#Cil3Smeny').val();
        $.getJSON(uri + '/nastavCileSmen',
            {
                addr: addr,
                varianta: typSmennosti,
                cil1: cil1Smeny,
                cil2: cil2Smeny,
                cil3: cil3Smeny
            })
            .done(function (data) {
                window.slVyr.addNotification('success', 'Sucessfully set.');
                typSmennostiIsA = typSmennosti === 'A';

                updateElementsForTypSmennosti(typSmennostiIsA);
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'NastavCileSmen - error: ' + err);
                // $('#stav').text('Error: ' + err);
                // alert("nastavCileSmen - error");
            });
    }

    function nastavPrestavkySmenA() {
        //alert('nastavPrestavkySmen');
        var typSmennosti = $('#TypSmennosti').val();
        var p1Smeny = $('#Prestavka1Smeny').val();
        var p2Smeny = $('#Prestavka2Smeny').val();
        var p3Smeny = $('#Prestavka3Smeny').val();
        $.getJSON(uri + '/nastavPrestavkySmenA',
            {
                addr: addr,
                prest1: p1Smeny,
                prest2: p2Smeny,
                prest3: p3Smeny
            })
            .done(function (data) {
                window.slVyr.addNotification('success', 'Sucessfully set.');
                typSmennostiIsA = typSmennosti === 'A';

                updateElementsForTypSmennosti(typSmennostiIsA);
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'nastavPrestavkySmenA - error: ' + err);
                // $('#stav').text('Error: ' + err);
                // alert("nastavPrestavkySmen - error");
            });
    }

    function nastavPrestavkySmenB() {
        //alert('nastavPrestavkySmen');
        var typSmennosti = $('#TypSmennosti').val();
        var p1s1 = $('#Prestavka1Smeny1').val();
        var p1s2 = $('#Prestavka1Smeny2').val();
        var p2po = $('#Prestavka2Po').val();
        $.getJSON(uri + '/nastavPrestavkySmenB',
            {
                addr: addr,
                prest1S1: p1s1,
                prest1S2: p1s2,
                prest2Po: p2po
            })
            .done(function (data) {
                window.slVyr.addNotification('success', 'Sucessfully set.');
                typSmennostiIsA = typSmennosti === 'A';

                updateElementsForTypSmennosti(typSmennostiIsA);
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'nastavPrestavkySmenB - error: ' + err);
                // $('#stav').text('Error: ' + err);
                // alert("nastavPrestavkySmen - error");
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
                window.slVyr.addNotification('success', 'Sucessfully set.');
                // $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'NastavCitaceOkNg - error: ' + err);
                // $('#stav').text('Error: ' + err);
                // alert("nastavCitaceOkNg - error");
            });
    }

    function nastavDefektivitu() {
        //alert('nastavDefektivitu');
        var varianta = $('#TypSmennosti').val();
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
                window.slVyr.addNotification('success', 'Sucessfully set.');
                // $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'NastavDefektivitu - error: ' + err);
                // $('#stav').text('Error: ' + err);
                // alert("nastavDefektivitu - error");
            });
    }

    function nastavJednotku() {
        var writeProtectEEprom = $('#WriteProtectEEprom').prop('checked');
        var minOk = $('#MinOK').val();
        var minNg = $('#MinNG').val();
        var bootloaderOn = $('#BootloaderOn').prop('checked');
        var parovanyLed = $('#AddrParovanyLed').val();
        var rozliseniCidel = $('#RozliseniCidelTeploty').val();
        var pracovniJasLed = $('#PracovniJasLed').val();
        $.getJSON(uri + '/nastavJednotku',
            {
                addr: addr,
                writeProtectEEprom: writeProtectEEprom,
                minOK: minOk,
                minNG: minNg,
                bootloaderOn: bootloaderOn,
                parovanyLED: parovanyLed,
                rozliseniCidel: rozliseniCidel,
                pracovniJasLed: pracovniJasLed
            })
            .done(function (data) {
                window.slVyr.addNotification('success', 'Sucessfully set.');
                // $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'NastavJednotku - error: ' + err);
                // $('#stav').text('Error: ' + err);
                // alert("nastavJednotku - error");
            });
    }

    function nastavAktualniCas() {
        //alert('nastavAktualniCas');
        $.getJSON(uri + '/nastavAktualniCas',
            {
                addr: addr
            })
            .done(function (data) {
                window.slVyr.addNotification('success', 'Sucessfully set.');
                // $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'NastavAktualniCas - error: ' + err);
                // $('#stav').text('Error: ' + err);
                // alert("'); - error");
            });
    }

    




