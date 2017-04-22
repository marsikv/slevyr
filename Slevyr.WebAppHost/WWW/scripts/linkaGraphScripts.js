var uriGra = 'api/graph';
var uriSys = 'api/sys';

var isTimerEnabled = false;
var refreshTimer;
var addr = null;
var measure = null;
var startAddr = null;
var lineChart = null;
//var dataserie = null;

(function () {
    var hash = location.hash.substr(1);
    startAddr = hash;
    //document.getElementById('user').innerHTML = hash;
})();


    $(document).ready(function () {
        readRunConfig();

        //$("#GetStatus").click(getStatus);

        $("#AddrIdDropDown").change(onAddrIdChange);
        $("#MeasureDropDown").change(onMeasureChange);

        jQuery.ajaxSetup({ cache: false });
 
    });

    function readRunConfig() {
        $.getJSON(uriSys + '/getRunConfig?')
            .done(function (data) {
                isTimerEnabled = data.IsRefreshTimerOn;
                var timerRefreshPeriod = data.RefreshTimerPeriod * 2;  //perioda pro refresh grafu bude mensi nez zjisteni stavu

                if (isTimerEnabled && (timerRefreshPeriod > 0)) {
                    if (refreshTimer) window.clearInterval(refreshTimer);
                    refreshTimer = window.setInterval(getGraphData(), timerRefreshPeriod);
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

                window.slVyr.addNotification('success', 'Sucessfully read config..');

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
        onMeasureChange();
    }

    function onMeasureChange() {
        measure = $("#MeasureDropDown option:selected").val();
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
    }

    function updateGraph(dataserie) {

        var ctx = $("#lineChart");

        if (lineChart) {
            lineChart.clear();
            lineChart.destroy();
            lineChart = null;
        }

        Chart.defaults.global.animation.duration = 0;

        //else {
            var gdata = {
                datasets: [{
                    label: measure,
                    backgroundColor: "rgba(75,192,192,0.4)",
                    borderColor: "rgba(75,192,192,1)",
                    data: dataserie
                }]
            };

            lineChart = new Chart(ctx, {
                type: 'line',
                data: gdata,
                options: {
                    tooltips: {
                        enabled: true,
                        callbacks: {
                            label: function (tooltipItem, data) {
                                var indice = tooltipItem.index;
                                return data.datasets[0].label + ' - čas ' + formatToHHMMSS(data.datasets[0].data[indice].x);
                            },
                            title: function (tooltipItem, data) {
                                //var indice = tooltipItem.index;
                                return tooltipItem[0].yLabel;
                            }
                        }
                    },
                    scales: {
                        xAxes: [{
                            type: 'linear',
                            position: 'bottom',
                            ticks: {
                                min: 0,
                                max: 8
                            }
                        }]
                    }
                }
            });

            lineChart.update();
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
