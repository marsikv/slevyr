
var uri = 'api/slevyr';

$(document).ready(function () {
    readRunConfig();

    $("#exportInterval").submit(exportInterval);
    $("#ExportToCsv").click(exportInterval);
    $("#ExportCsv1").click(exportIntervalPredef);
    $("#ExportCsv2").click(exportIntervalPredef);
    $("#ExportCsv3").click(exportIntervalPredef);

    $("#selectedFile").change(function () {
        //alert('changed!');
        var pom = $('#selectedFile').val();
        alert('changed to'+pom);
        $('#exportFilename').val($('#selectedFile').val());
    });

});

function readRunConfig() {
    $.getJSON(uri + '/getConfig?')
        .done(function (data) {
            $('#exportFilename').val(data.DefaultExportFileName);
        })
        .fail(function (jqXHR, textStatus, err) {
            window.slVyr.addNotification('error', 'Load Configuration Error: ' + err);
        });
}

function exportInterval(event) {
    event.preventDefault();    
    //alert('exportInterval');

    var model = {
        FileName: $('#exportFilename').val(),
        TimeFromStr: $('#exportTimeFrom').val(),
        TimeToStr: $('#exportTimeTo').val(),
        UnitId: $('#linkaId').val(),
        ExportAll: $('#exportAll').prop('checked'),
        ExportAllSeparated: $('#exportAllSeparated').prop('checked')
    };

    $.ajax({
        type: 'POST',
        url: uri + '/exportInterval/',
        data: JSON.stringify(model),
        contentType: "application/json",
        success: function (response) {
            alert(response);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            alert(xhr.status);
            alert(thrownError);
        }
    });
}

function exportIntervalPredef(event) {
    var targetid = event.target.id;
    var expVar = 0;
    event.preventDefault();
    //alert('exportInterval id:' + tid);

    if (targetid == "ExportCsv1")
        expVar = 1;
    else if (targetid == "ExportCsv2")
        expVar = 2;
    else if (targetid == "ExportCsv3")
        expVar = 3;

    $.getJSON(uri + '/ExportIntervalPredef',
        {
            fileName: $('#exportFilename').val(),
            unitId: $('#linkaId').val(),
            expAll: $('#exportAll').prop('checked'),
            expAllSepar: $('#exportAllSeparated').prop('checked'),
            exportVariant: expVar
        })
        .done(function (data) {
            //window.slVyr.addNotification('success', 'Sucessfully set.');
            alert(data);
            // $('#stav').text('');
        })
        .fail(function (jqXHR, textStatus, err) {
            window.slVyr.addNotification('error', 'NastavCileSmen - error: ' + err);
            // $('#stav').text('Error: ' + err);
            // alert("nastavCileSmen - error");
        });   
}