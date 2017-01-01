var uri = 'api/slevyr';
var isTimerEnabled = false;
var refreshTimer;
var addr = null;

var $notification = $('#notification');

    $(document).ready(function () {
        readRunConfig();

        $("#RefreshStatus").click(refreshStatus);

        $("#GetStatus").click(getStatus);

        $("#AddrIdDropDown").change(onAddrIdChange);

        jQuery.ajaxSetup({ cache: false });

    });

    function readRunConfig() {
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

                $notification.prepend('<div class="notification notification-success">Sucessfully read config.</div>');
                //$('#stav').text('');

            })
            .fail(function (jqXHR, textStatus, err) {
                $notification.prepend('<div class="notification notification-error">ReadRunConfig - error: ' + err + '</div>');
            });
    }

    function readUnitConfig() {
        clearStatus();
        if (typeof addr != 'undefined' &&  addr != null && addr.length > 1)
        $.getJSON(uri + '/GetUnitConfig?', {addr: addr})
            .done(function (data) {
                $('#LinkaName').text(data.UnitName);
                $notification.prepend('<div class="notification notification-success">Sucessfully read unit config.</div>');
            })
            .fail(function (jqXHR, textStatus, err) {
                $notification.prepend('<div class="notification notification-error">LoadParams - error: ' + err + '</div>');
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

    function refreshStatus() {
        clearStatus();
        $.getJSON(uri + '/refreshStatus?Addr=' + addr)
            .done(function (data) {
                updateTableElements(data);
            })
            .fail(function (jqXHR, textStatus, err) {
                $notification.prepend('<div class="notification notification-error">RefreshStatus - error: ' + err + '</div>');
            });
    }

    function getStatus() {
        $.getJSON(uri + '/getStatus?Addr=' + addr)
            .done(function (data) {
                updateTableElements(data);
            })
            .fail(function (jqXHR, textStatus, err) {
                $notification.prepend('<div class="notification notification-error">GetStatus - error: ' + err + '</div>');
            });
    }

    function updateTableElements(data) {
        if (data.IsOkNg) {
            $('#okNumValue').text(data.Ok);
            $('#ngNumValue').text(data.Ng);
            $('#okNgRefreshTime').text(data.OkNgTimeTxt);
        } else {
            $notification.prepend('<div class="notification notification-error">Chyba jednotky - ' + data.ErrorTimeTxt + '</div>');
        }

        if (data.IsPrestavkaTabule) {
            $('#isPrestavkaTabule').addClass('linka--prestavka-visible');
        } else {
            $('#isPrestavkaTabule').removeClass('linka--prestavka-visible');
        }

        $('#casOkValue').text(Number((data.CasOk).toFixed(1)) + 's');
        $('#casNgValue').text(Number((data.CasNg).toFixed(1)) + 's');
        $('#checkTime').text(data.LastCheckTimeTxt);

        $('#cilTabule').text(data.Tabule.CilKusuTabule);
        $('#rozdilTabule').text(data.Tabule.RozdilTabuleTxt);
        $('#cilDefTabule').text(data.Tabule.CilDefectTabule);
        $('#aktualniDefTabule').text(data.Tabule.AktualDefectTabuleTxt);

        //$('#stav').text('');
        if (data.MachineStatus == 0)
            $('#stav').text('ok');
        else
            $('#stav').text(data.MachineStatusTxt);
    }

