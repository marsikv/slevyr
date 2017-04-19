var uriGra = 'api/graph';
var uriSys = 'api/sys';

var isTimerEnabled = false;
var refreshTimer;
var addr = null;
var measure = null;
var startAddr = null;
var lineChart = null;

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
                timerRefreshPeriod = data.RefreshTimerPeriod;

                if (isTimerEnabled && (timerRefreshPeriod > 0)) {
                    if (refreshTimer) window.clearInterval(refreshTimer);
                    //refreshTimer = window.setInterval(getStatus, timerRefreshPeriod);
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
        //getStatus();
    }

    function onMeasureChange() {
        measure = $("#MeasureDropDown option:selected").val();
        //getGraphData();
        updateGraph();
    }

    function clearStatus() {
        $('#stav').text('...');
    }

    function getGraphData() {
        $.getJSON(uriGra + '/get',
            {
                addr: addr,
                measureName:measure
            })
            .done(function (data) {
                //window.slVyr.addNotification('success', 'Sucessfully set PocetOkNg');
                $('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'get graph data - error: ' + err);
                $('#stav').text('Error: ' + err);
                alert("get graph data - error");
            });
    }

    function updateGraph() {
        var ctx = $("#lineChart");

        if (lineChart) {
            //myDoughnutChart.data.datasets[0].data[0].value = 100;
        } else {
            var gdata = {
                labels: ["January", "February", "March", "April", "May", "June", "July"],
                datasets: [
                    {
                        label: "X",
                        fill: false,
                        lineTension: 0.1,
                        backgroundColor: "rgba(75,192,192,0.4)",
                        borderColor: "rgba(75,192,192,1)",
                        borderCapStyle: 'butt',
                        borderDash: [],
                        borderDashOffset: 0.0,
                        borderJoinStyle: 'miter',
                        pointBorderColor: "rgba(75,192,192,1)",
                        pointBackgroundColor: "#fff",
                        pointBorderWidth: 1,
                        pointHoverRadius: 5,
                        pointHoverBackgroundColor: "rgba(75,192,192,1)",
                        pointHoverBorderColor: "rgba(220,220,220,1)",
                        pointHoverBorderWidth: 2,
                        pointRadius: 1,
                        pointHitRadius: 10,
                        data: [65, 59, 80, 81, 56, 55, 40],
                        spanGaps: false,
                    }
                ]
            };

            lineChart = new Chart(ctx, {
                type: 'line',
                data: gdata,
                //options: options
            });
        }
    }

