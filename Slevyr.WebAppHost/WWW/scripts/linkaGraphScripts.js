var uriGra = 'api/graph';
var uriSys = 'api/sys';
var uriSlevyr = 'api/slevyr';

var isTimerEnabled = false;
var refreshTimer;
var addr = null;
var measure = null;
var measure2 = null;
var startAddr = null;
var lineChart = null;
var lineChartStavLinky = null;
var maxhour = 8;
var smena = -1;
var currentSmena = -1;
var smenaStarHour = 0;
var ctxMeasuresLinky;
var ctxStavLinky;

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
        $("#SmenaDropDown").change(onSmenaChange);

        jQuery.ajaxSetup({ cache: false });

        Chart.defaults.global.animation.duration = 0;
        ctxMeasuresLinky = $("#lineChart");
        ctxStavLinky = $("#lineChartStavLinky");
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

    function onSmenaChange() {
        smena = $("#SmenaDropDown option:selected").val();
        measure2 = $("#Measure2DropDown option:selected").val();
        removeChart();
        getGraphData();
    }
    
    function clearStatus() {
        $('#stav').text('...');
    }

    function getGraphHistoryData() {
        if (!addr || !measure) return;

        $.getJSON(uriSlevyr + '/GetSmenaStartHour?', { addr: addr, smenaIndex: smena})
            .done(function (data) {
                smenaStarHour = data;
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'GetSmenaStartHour - error: ' + err);
            });

        $.getJSON(uriGra + '/getPast',
                {
                    addr: addr,
                    measureName: measure,
                    smena:smena
                })
                .done(function (data) {
                    //dataserie = data;
                    updateGraph(data,0);
                    //$('#stav').text('');
                })
                .fail(function (jqXHR, textStatus, err) {
                    window.slVyr.addNotification('error', 'get graph data - error: ' + err);
                    //$('#stav').text('Error: ' + err);
                });

        if (!measure2 || measure2 === '-') return;
        $.getJSON(uriGra + '/getPast',
            {
                addr: addr,
                measureName: measure2,
                smena: smena
            })
            .done(function (data) {
                //dataserie = data;
                updateGraph(data,1);
                //$('#stav').text('');
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'get graph data - error: ' + err);
                //$('#stav').text('Error: ' + err);
            });

        $.getJSON(uriGra + '/getPast',
               {
                   addr: addr,
                   measureName: 'StavLinky',
                   smena: smena
               })
               .done(function (data) {
                   updateGraphStavLinky(data,0);
                   $('#stav').text('');
               })
               .fail(function (jqXHR, textStatus, err) {
                   window.slVyr.addNotification('error', 'get graph data - error: ' + err);
                   $('#stav').text('Error: ' + err);
               });

        $.getJSON(uriGra + '/getPast',
              {
                  addr: addr,
                  measureName: 'Prestavka',
                  smena: smena
              })
              .done(function (data) {
                  updateGraphStavLinky(data, 1);
                  $('#stav').text('');
              })
              .fail(function (jqXHR, textStatus, err) {
                  window.slVyr.addNotification('error', 'get graph data - error: ' + err);
                  $('#stav').text('Error: ' + err);
              });
    }

    function getGraphData() {
        if (!addr || !measure) return;

        if (smena > -1) {
            getGraphHistoryData();
            return;
        }

        $.getJSON(uriSlevyr + '/GetCurrentSmena?', { addr: addr })
            .done(function (data) {
                currentSmena = data.Smena;
                smenaStarHour = data.StarHour;
            })
            .fail(function (jqXHR, textStatus, err) {
                window.slVyr.addNotification('error', 'GetCurrentSmena - error: ' + err);
            });

        $.getJSON(uriGra + '/get',
                {
                    addr: addr,
                    measureName: measure
                })
                .done(function (data) {
                    //dataserie = data;
                    updateGraph(data,0);
                    $('#stav').text('');
                })
                .fail(function (jqXHR, textStatus, err) {
                    window.slVyr.addNotification('error', 'get graph data - error: ' + err);
                    $('#stav').text('Error: ' + err);
                });

        if (!measure2 || measure2 === '-') return;

        $.getJSON(uriGra + '/get',
                {
                    addr: addr,
                    measureName: measure2
                })
                .done(function (data) {
                    //dataserie = data;
                    updateGraph(data,1);
                    $('#stav').text('');
                })
                .fail(function (jqXHR, textStatus, err) {
                    window.slVyr.addNotification('error', 'get graph data - error: ' + err);
                    $('#stav').text('Error: ' + err);
                });

        $.getJSON(uriGra + '/get',
                {
                    addr: addr,
                    measureName: 'StavLinky'
                })
                .done(function (data) {
                    updateGraphStavLinky(data,0);
                    $('#stav').text('');
                })
                .fail(function (jqXHR, textStatus, err) {
                    window.slVyr.addNotification('error', 'get graph data - error: ' + err);
                    $('#stav').text('Error: ' + err);
                });

        $.getJSON(uriGra + '/get',
                {
                    addr: addr,
                    measureName: 'Prestavka'
                })
                .done(function (data) {
                    updateGraphStavLinky(data,1);
                    $('#stav').text('');
                })
                .fail(function (jqXHR, textStatus, err) {
                    window.slVyr.addNotification('error', 'get graph data - error: ' + err);
                    $('#stav').text('Error: ' + err);
                });
    }

    function updateGraphStavLinky(dataserie, index) {
        if (lineChartStavLinky) {
            lineChartStavLinky.data.datasets[index].data = dataserie;
            lineChartStavLinky.update();
        } else {
            var gdata;

            gdata = {
                datasets: [
                    {
                        label: 'StavLinky',
                        backgroundColor: "rgba(100,100,100,0.3)",
                        borderColor: "rgba(100,100,100,1)",
                        steppedLine: true,
                        pointRadius: 2,
                        yAxisID: "y-axis-0",
                        data: dataserie
                    },
                    {
                        label: 'Prestavka',
                        backgroundColor: "rgba(0,250,0,0.4)",
                        borderColor: "#3ADF00",
                        steppedLine: true,
                        yAxisID: "y-axis-1",
                        pointRadius: 0
                        //data: dataserie
                    }
                ]
            };

            lineChartStavLinky = new Chart(ctxStavLinky, {
                type: 'line',
                data: gdata,
                options: {
                    tooltips: {
                        enabled: true,
                        callbacks: {
                            label: function (tooltipItem, data) {
                                var i = tooltipItem.index;
                                var dsi = tooltipItem.datasetIndex;
                                return formatStavToDescription(data.datasets[dsi].data[i].y)+
                                ' - čas ' + formatToHHMMSS(smenaStarHour + data.datasets[dsi].data[i].x);
                            },
                            title: function (tooltipItem, data) {
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
                        yAxes: [
                            {                                
                                //display: false
                                position: "left",
                                ticks: {
                                    min: 0,
                                    max: 5.0,
                                    stepSize: 1,
                                    suggestedMax: 5.0,
                                    suggestedMin: 0,
                                    padding:14
                                },
                                id: "y-axis-0"
                            },
                            {
                                //display: false
                                position: "right",
                                ticks: {
                                    min: 0,
                                    max: 1.0,
                                    stepSize: 1,
                                    suggestedMax: 1.0,
                                    suggestedMin: 0,
                                    padding: 8
                                },
                                id: "y-axis-1"
                            }

                        ]
                    }
                }
            });
        }
    }

   function updateGraph(dataserie, index) {

        if (lineChart) {
            //pouzit push ?
            //https://github.com/chartjs/Chart.js/issues/1997
            //zrejme funguje prirazeni cele serie, to je jednodussi
            lineChart.data.datasets[index].data = dataserie;
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
                            //data: dataserie
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

            //if (hasMeasure2) {
            //    gyAxes = [
            //        {
            //            stacked: true,
            //            position: "left",                        
            //            id: "y-axis-0"
            //        }, {
            //            stacked: true,
            //            position: "right",
            //            id: "y-axis-1"
            //        }
            //    ];
            //} else {
            //    gyAxes = [
            //        {
            //            position: "left",
            //            id: "y-axis-0"
            //        }
            //    ];
            //}

            lineChart = new Chart(ctxMeasuresLinky, {
                type: 'line',
                data: gdata,
                options: {
                    tooltips: {
                        enabled: true,
                        callbacks: {
                            label: function (tooltipItem, data) {
                                var i = tooltipItem.index;
                                var dsi = tooltipItem.datasetIndex;
                                return data.datasets[dsi].label + ' - čas '
                                    + formatToHHMMSS(smenaStarHour + data.datasets[dsi].data[i].x);
                            },
                            title: function (tooltipItem, data) {
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
                        yAxes: [
                            {
                                stacked: true,
                                position: "left",
                                id: "y-axis-0"
                            }, {
                                stacked: true,
                                position: "right",
                                id: "y-axis-1"
                            }
                        ]
                    }                    
                }
            });

            lineChart.data.datasets[index].data = dataserie;

        }
    }

   function removeChart() {
        if (lineChart) {
            lineChart.clear();
            lineChart.destroy();
            lineChart = null;
        }
        if (lineChartStavLinky) {
            lineChartStavLinky.clear();
            lineChartStavLinky.destroy();
            lineChartStavLinky = null;
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

    function formatStavToDescription(stav) {
        switch (stav) {
            case 0:
                return 'Výroba';
                break;
            case 1:
                return 'Přerušení výroby';
                break;
            case 2:
                return 'Stop stroje';
                break;
            case 3:
                return 'Změna modelu';
                break;
            case 4:
                return 'Porucha';
                break;
            case 5:
                return 'Servis';
                break;
            default:
                return '-';
        }

    }
