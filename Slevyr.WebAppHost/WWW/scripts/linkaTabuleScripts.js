var uri = 'api/slevyr';
var uriSys = 'api/sys';
var uriGra = 'api/graph';

var isTimerEnabled = false;
var refreshTimer;
var addr = null;
var startAddr = null;
var chart = null;
var lineChart = null;
var ctxLineChart;
var ctxPie;
var maxhour = null;

(function () {
    var hash = location.hash.substr(1);
    startAddr = hash;
    //document.getElementById('user').innerHTML = hash;
})();


    $(document).ready(function () {
        readRunConfig();

        $("#GetStatus").click(getStatus);

        $("#AddrIdDropDown").change(onAddrIdChange);

        jQuery.ajaxSetup({ cache: false });

        Chart.defaults.global.animation.duration = 0;

        ctxLineChart = $("#lineChart");

        ctxPie = $("#pieChart");
    });

    function readRunConfig() {
        $.getJSON(uriSys + '/getRunConfig?')
            .done(function (data) {
                isTimerEnabled = data.IsRefreshTimerOn;
                var timerRefreshPeriod = data.RefreshTimerPeriod;

                if (isTimerEnabled && (timerRefreshPeriod > 0)) {
                    if (refreshTimer) window.clearInterval(refreshTimer);
                    refreshTimer = window.setInterval(getStatus, timerRefreshPeriod);
                }
                else {
                    if (refreshTimer) {
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

                //window.slVyr.addNotification('success', 'Sucessfully read config..');
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
                if (data.IsTypSmennostiA) {
                    maxhour = 8;
                } else {
                    maxhour = 12;
                }
                //window.slVyr.addNotification('success', 'Sucessfully read unit config.');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'LoadParams - error: ' + err);
            });
    }

    function onAddrIdChange() {
        addr = $("#AddrIdDropDown option:selected").val();
        removeChart();
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
                updatePieChart(data);
                if (data.IsTypSmennostiA) {
                    updateLastSmenaTable('#lastSmena1', 'Ranní', data.PastSmenaResults[0]);
                    updateLastSmenaTable('#lastSmena2', 'Odpolední', data.PastSmenaResults[1]);
                    updateLastSmenaTable('#lastSmena3', 'Noční', data.PastSmenaResults[2]);
                } else {
                    updateLastSmenaTable('#lastSmena1', 'Ranní', data.PastSmenaResults[0]);
                    updateLastSmenaTable('#lastSmena2', 'Noční', data.PastSmenaResults[2]);
                    $('#lastSmena3').empty();
                }
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'GetStatus - error: ' + err);
            });

            getLineGraphData();
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
            var prumCyklusStr = "-";
            if (!isNaN(data.PrumCyklusOk)) {
                prumCyklusStr = Math.round(data.PrumCyklusOk * 100) / 100;
            }
            var defektivitaStr = "-";
            if (!isNaN(data.Defektivita)) {
                defektivitaStr = Math.round(data.Defektivita * 100) / 100;
            }

            s = '<td>' + smenaNum + '</td>' + '<td>' + data.Ok + '</td>' + '<td>' + data.Ng + '</td>'
                + '<td>' + prumCyklusStr + '</td>'
                + '<td>' + data.RozdilKusu + '</td>'
                + '<td>' + defektivitaStr + '</td>'
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
            window.slVyr.addNotification('error', 'Neplatná data jednotky');   //lepsi hlasku
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

    function updateAkumulovaneCasyElements(cdata) {
        if (cdata.IsOkNg && cdata.IsDurationKnown) {
            $('#casZmenyModelu').text(cdata.ZmenaModeluDurationTxt);
            $('#casPoruchy').text(cdata.PoruchaDurationTxt);
            $('#casServisu').text(cdata.ServisDurationTxt);
        } else {
            $('#casZmenyModelu').text('-');
            $('#casPoruchy').text('-');
            $('#casServisu').text('-');
        }
    }

    function updatePieChart(cdata) {
        
        if (chart) {
            chart.data.datasets[0].data[0].value = cdata.VyrobaDurationSec;
            chart.data.datasets[0].data[1].value = cdata.ZmenaModeluDurationSec;
            chart.data.datasets[0].data[2].value = cdata.PoruchaDurationSec;
            chart.data.datasets[0].data[3].value = cdata.ServisDurationSec;
            chart.data.datasets[0].data[4].value = cdata.OtherStopDurationSec;
            chart.update();
        } else {
            var pdata = {
                labels: ["Výroba", "Změna modelu", "Porucha", "Servis", "Ostatni"],
                datasets: [
                    {
                        data: [cdata.VyrobaDurationSec, cdata.ZmenaModeluDurationSec, cdata.PoruchaDurationSec,
                               cdata.ServisDurationSec, cdata.OtherStopDurationSec],
                        backgroundColor: [
                            "#006600",
                            "#0000ff",
                            "#ff0000",
                            "#ff3300",
                            "#8c8c8c"
                        ],
                        hoverBackgroundColor: [
                            "#2db92d",
                            "#3333ff",
                            "#ff1a1a",
                            "#ff5c33",
                            "#b3b3b3"
                        ]
                    }]
            };

            chart = new Chart(ctxPie, {
                type: 'doughnut',
                data: pdata,
                options: {
                    legend: {
                        position: 'top',
                        boxWidth: 10
                    },
                    tooltips: {
                        enabled: true,
                        callbacks: {
                            label: function (tooltipItem, data) {
                                var indice = tooltipItem.index;
                                return data.labels[indice] + ': ' + formatToHHMMSS(data.datasets[0].data[indice]) ;
                            }
                        }
                    },
                    hover: {
                        animationDuration: 400
                    }
                }
            });
        }
    }


    function getLineGraphData() {
        if (!addr) return;

        //return;

        $.getJSON(uriGra + '/get',
            {
                addr: addr,
                measureName: "OK"
            })
            .done(function (data) {
                //dataserie = data;
                updateLineGraph(data,0);
                //$('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'get graph data - error: ' + err);
                //$('#stav').text('Error: ' + err);
            });


        $.getJSON(uriGra + '/get',
                {
                    addr: addr,
                    measureName: "Rozdil"
                })
                .done(function (data) {
                    //dataserie = data;
                    updateLineGraph(data,1);
                    //$('#stav').text('');
                })
                .fail(function (jqXHR, textStatus, err) {
                    window.slVyr.addNotification('error', 'get graph data - error: ' + err);
                    //$('#stav').text('Error: ' + err);
                });
    }


    function updateLineGraph(dataserie,index) {
        if (lineChart) {
            lineChart.data.datasets[index].data = dataserie;
            lineChart.update();
        } else {
            var gdata = {
                    datasets: [
                        {
                            label: "OK",
                            backgroundColor: "rgba(75,192,192,0.3)",
                            borderColor: "rgba(75,192,192,1)",
                            steppedLine: true,
                            pointRadius: 0,
                            yAxisID: "y-axis-0",
                            data: dataserie
                        },
                        {
                            label: "Rozdíl",
                            backgroundColor: "rgba(153, 102, 255, 0.2)",
                            borderColor: "rgba(153, 102, 255, 1)",
                            steppedLine: true,
                            yAxisID: "y-axis-1",
                            pointRadius: 0
                            //data: dataserie
                        }
                    ]
                };

            lineChart = new Chart(ctxLineChart, {
                type: 'line',
                data: gdata,
                options: {                    
                    scales: {
                        xAxes: [
                            {
                                type: 'linear',
                                position: 'bottom',
                                ticks: {
                                    min: 0,
                                    max: maxhour
                                }
                            }
                        ],
                        yAxes: [
                            {
                                //display: false
                                position: "left",
                                id: "y-axis-0"
                            },
                            {
                                 //display: false
                                position: "right",
                                id: "y-axis-1"
                            }

                        ]
                    }
                }
            });
        }
    }

    function removeChart() {
        if (chart) {
            chart.clear();
            chart.destroy();
            chart = null;
        }
        if (lineChart) {
            lineChart.clear();
            lineChart.destroy();
            lineChart = null;
        }
    }

    function formatToHHMMSS(sec_num) {
        var hours = Math.floor(sec_num / 3600);
        var minutes = Math.floor((sec_num - (hours * 3600)) / 60);
        var seconds = Math.floor(sec_num) - (hours * 3600) - (minutes * 60);

        if (hours < 10) { hours = "0" + hours; }
        if (minutes < 10) { minutes = "0" + minutes; }
        if (seconds < 10) { seconds = "0" + seconds; }
        return hours + ':' + minutes + ':' + seconds;
    }


