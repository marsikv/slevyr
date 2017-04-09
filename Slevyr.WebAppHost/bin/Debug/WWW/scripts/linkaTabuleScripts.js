var uri = 'api/slevyr';
var uriSys = 'api/sys';

var isTimerEnabled = false;
var refreshTimer;
var addr = null;
var startAddr = null;

(function () {
    var hash = location.hash.substr(1);
    startAddr = hash;
    //document.getElementById('user').innerHTML = hash;
})();


    $(document).ready(function () {
        readRunConfig();

        //$("#RefreshStatus").click(refreshStatus);     

        $("#GetStatus").click(getStatus);

        $("#AddrIdDropDown").change(onAddrIdChange);

        jQuery.ajaxSetup({ cache: false });
 
    });

    function readRunConfig() {
        $.getJSON(uriSys + '/getRunConfig?')
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

                if (startAddr) {
                    $('#AddrIdDropDown').val(startAddr);
                    startAddr = null;
                }

                onAddrIdChange();

                window.slVyr.addNotification('success', 'Sucessfully read config..');
                //$('#stav').text('');

            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'ReadRunConfig - error: ' + err);
            });
    }

    function readUnitConfig() {
        clearStatus();
        if (typeof addr != 'undefined' &&  addr != null && addr.length > 1)
        $.getJSON(uriSys + '/GetUnitConfig?', {addr: addr})
            .done(function (data) {
                $('#LinkaName').text(data.UnitName);
                window.slVyr.addNotification('success', 'Sucessfully read unit config.');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'LoadParams - error: ' + err);
            });
    }

    function onAddrIdChange() {
        addr = $("#AddrIdDropDown option:selected").val();
        readUnitConfig();
        getStatus();
    }

    function clearStatus() {
        $('#stav').text('...');
    }

    function getStatus() {
        $.getJSON(uri + '/getStatus?Addr=' + addr)
            .done(function (data) {
                updateTableElements(data);
                updateAkumulovaneCasyElements(data);
                if (data.IsTypSmennostiA) {
                    updateLastSmenaTable('#lastSmena1', 1, data.LastSmenaResults[0]);
                    updateLastSmenaTable('#lastSmena2', 2, data.LastSmenaResults[1]);
                    updateLastSmenaTable('#lastSmena3', 3, data.LastSmenaResults[2]);
                } else {
                    updateLastSmenaTable('#lastSmena1', 1, data.LastSmenaResults[0]);
                    updateLastSmenaTable('#lastSmena2', 2, data.LastSmenaResults[2]);
                    $('#lastSmena3').empty();
                }
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'GetStatus - error: ' + err);
            });
    }

    function updateLastSmenaTable(rowid, smenaNum, data) {
        $(rowid).empty();

        if (data == null) return;

        var s;

        if (data.Ok < 0 && data.Ng < 0) {
            s = '<td>' + smenaNum + '</td>' + '<td>-</td><td>-</td>'
                + '<td>-</td>'
                + '<td>-</td>'
                + '<td>-</td>'
                + '<td>-</td>';
        } else {
            s = '<td>' + smenaNum + '</td>' + '<td>' + data.Ok + '</td>' + '<td>' + data.Ng + '</td>'
                + '<td>' + Math.round(data.PrumCyklusOk * 100) / 100 + '</td>'
                + '<td>' + data.RozdilKusu + '</td>'
                + '<td>' + Math.round(data.Defektivita * 100) / 100 + '</td>'
                + '<td>' + data.StopTimeTxt + '</td>';
        }

        $(rowid).append(s);
    }

    function updateTableElements(data) {
        if (data.IsOkNg) {
            $('#okNumValue').text(data.Ok);
            $('#ngNumValue').text(data.Ng);
            $('#okNgRefreshTime').text(data.OkNgTimeTxt);
            $('#casOkValue').text(Number((data.CasOk).toFixed(1)) + 's');
            $('#casNgValue').text(Number((data.CasNg).toFixed(1)) + 's');
            $('#checkTime').text(data.LastCheckTimeTxt);
        } else {
            $('#okNumValue').text('-');
            $('#ngNumValue').text('-');
            $('#okNgRefreshTime').text('-');
            $('#casOkValue').text('-');
            $('#casNgValue').text('-');
            $('#checkTime').text('-');
            window.slVyr.addNotification('error', 'Chyba jednotky - error: ');   //lepsi hlasku
        }

        if (data.Tabule.IsPrestavkaTabule) {
            $('#isPrestavkaTabule').addClass('linka--prestavka-visible');
        } else {
            $('#isPrestavkaTabule').removeClass('linka--prestavka-visible');
        }

        $('#cilTabule').text(data.Tabule.CilKusuTabule);
        $('#rozdilTabule').text(data.Tabule.RozdilTabuleTxt);
        $('#cilDefTabule').text(data.Tabule.CilDefectTabuleTxt);
        $('#aktualniDefTabule').text(data.Tabule.AktualDefectTabuleTxt);

        $("#rozdilTabuleDiv").toggleClass('num--ok', data.Tabule.RozdilTabule >= 0);
        $("#rozdilTabuleDiv").toggleClass('num--bad', data.Tabule.RozdilTabule < 0);

        if (!isNaN(data.Tabule.AktualDefectTabule)) {
            $("#aktualniDefDiv").toggleClass('num--ok', data.Tabule.AktualDefectTabule <= data.Tabule.CilDefectTabule);
            $("#aktualniDefDiv").toggleClass('num--bad', data.Tabule.AktualDefectTabule > data.Tabule.CilDefectTabule);
        }

        if (data.MachineStatus == 0)
            $('#stav').text('Vyroba');
        else
            $('#stav').text(data.Tabule.MachineStatusTxt);
    }

    function updateAkumulovaneCasyElements(data) {
        if (data.IsOkNg) {
            $('#casZmenyModelu').attr('title', 'Akumulovaný čas zmeny modelu:' + data.ZmenaModeluDuration + ' sec');
            $('#casPoruchy').attr('title', 'Akumulovaný čas poruchy stroje:' + data.PoruchaDuration + ' sec');
            $('#casServisu').attr('title', 'Akumulovaný čas servisu stroje:' + data.ServisDuration + ' sec');
        } else {
            $('#casZmenyModelu').attr('title', '-');
            $('#casPoruchy').attr('title', '-');
            $('#casServisu').attr('title', '-');
        }

    }

