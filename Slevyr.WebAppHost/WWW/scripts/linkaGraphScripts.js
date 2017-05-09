var uriGra = 'api/graph';
var uriSys = 'api/sys';

var isTimerEnabled = false;
var refreshTimer;
var addr = null;
var measure = null;
var measure2 = null;
var startAddr = null;
var lineChart = null;
var maxhour = null;
var ctx;

(function () {
    var hash = location.hash.substr(1);
    startAddr = hash;
    //document.getElementById('user').innerHTML = hash;
})();


    $(document).ready(function () {
        readRunConfig();

        $("#AddrIdDropDown").change(onAddrIdChange);
        $("#MeasureDropDown").change(onMeasureChange);
        $("#Measure2DropDown").change(onMeasureChange);

        jQuery.ajaxSetup({ cache: false });

        Chart.defaults.global.animation.duration = 0;
        ctx = $("#lineChart");
    });

    function readRunConfig() {
        $.getJSON(uriSys + '/getRunConfig?')
            .done(function (data) {
                isTimerEnabled = data.IsRefreshTimerOn;
                var timerRefreshPeriod = data.RefreshTimerPeriod * 2;  //perioda pro refresh grafu bude mensi nez zjisteni stavu

                if (isTimerEnabled && (timerRefreshPeriod > 0)) {
                    if (refreshTimer) window.clearInterval(refreshTimer);
                    refreshTimer = window.setInterval(getGraphData, timerRefreshPeriod);
                }
                else {
                    if (refreshTimer) {
                        window.clearInterval(refreshTimer);
                    }
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
        onMeasureChange();
    }

    function onMeasureChange() {
        measure = $("#MeasureDropDown option:selected").val();
        measure2 = $("#Measure2DropDown option:selected").val();
        removeChart();
        getGraphData();
    }

    function clearStatus() {
        $('#stav').text('...');
    }

    function getGraphData() {
        if (!addr || !measure) return;
        $.getJSON(uriGra + '/get',
            {
                addr: addr,
                measureName:measure
            })
            .done(function (data) {
                //dataserie = data;
                updateGraph(data);
                $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'get graph data - error: ' + err);
                $('#stav').text('Error: ' + err);
                alert("get graph data - error");
            });

        if (!measure2 || measure2 === '-') return;
        $.getJSON(uriGra + '/get',
            {
                addr: addr,
                measureName: measure2
            })
            .done(function (data) {
                //dataserie = data;
                updateGraph2(data);
                $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'get graph data - error: ' + err);
                $('#stav').text('Error: ' + err);
                alert("get graph data - error");
            });
    }


    function updateGraph2(dataserie2) {
        if (lineChart) {
            lineChart.data.datasets[1].data = dataserie2;
            lineChart.update();
        }
    }

   function updateGraph(dataserie) {

    if (lineChart) {
        //pouzit push ?
        //https://github.com/chartjs/Chart.js/issues/1997
        //zrejme funguje prirazeni cele serie, to je jednodussi
        lineChart.data.datasets[0].data = dataserie;
        lineChart.update();
    } else {
        var gdata;
        var gyAxes;
        var hasMeasure2 = measure2 && measure2 !== '-';

        if (hasMeasure2) {
            gdata = {
                datasets: [
                    {
                        label: measure,
                        backgroundColor: "rgba(75,192,192,0.3)",
                        borderColor: "rgba(75,192,192,1)",
                        yAxisID: "y-axis-0",
                        steppedLine: measure === 'StavLinky',
                        data: dataserie
                    },
                    {
                        label: measure2,
                        backgroundColor: "rgba(153, 102, 255, 0.2)",
                        borderColor: "rgba(153, 102, 255, 1)",
                        steppedLine: measure2 === 'StavLinky',
                        yAxisID: "y-axis-1",
                        pointStyle: "rect"
                        //steppedLine: true
                        //data: dataserie2
                    }
                    
                ]
            };
        } else {
            gdata = {
                datasets: [
                    {
                        label: measure,
                        backgroundColor: "rgba(75,192,192,0.3)",
                        borderColor: "rgba(75,192,192,1)",
                        steppedLine: measure === 'StavLinky',
                        data: dataserie
                    }
                ]
            };
        }

        if (hasMeasure2) {
            gyAxes = [
                {
                    position: "left",
                    "id": "y-axis-0"
                }, {
                    position: "right",                    
                    "id": "y-axis-1"
                }
            ];
        } else {
            gyAxes = [
                {
                    position: "left",
                    "id": "y-axis-0"
                }
            ];
        }

        lineChart = new Chart(ctx, {
            type: 'line',
            data: gdata,
            options: {
                tooltips: {
                    enabled: true,
                    callbacks: {
                        label: function(tooltipItem, data) {
                            var i = tooltipItem.index;
                            var dsi = tooltipItem.datasetIndex;
                            return data.datasets[dsi].label + ' - čas ' + formatToHHMMSS(data.datasets[dsi].data[i].x);
                        },
                        title: function(tooltipItem, data) {
                            //var indice = tooltipItem.index;
                            return '     ' + tooltipItem[0].yLabel;
                        }
                    }
                },
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
                    yAxes: gyAxes
                }
            }
        });
    }
}

   function removeChart() {
        if (lineChart) {
            lineChart.clear();
            lineChart.destroy();
            lineChart = null;
        }
    }

    function formatToHHMMSS(hoursFrac) {
        var sec_num = hoursFrac * 3600;

        var hours = Math.floor(sec_num / 3600);
        var minutes = Math.floor((sec_num - (hours * 3600)) / 60);
        var seconds = Math.floor(sec_num) - (hours * 3600) - (minutes * 60);

        if (hours < 10) { hours = "0" + hours; }
        if (minutes < 10) { minutes = "0" + minutes; }
        if (seconds < 10) { seconds = "0" + seconds; }
        return hours + ':' + minutes + ':' + seconds;
    }
