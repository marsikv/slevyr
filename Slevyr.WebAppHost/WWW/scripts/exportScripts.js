
var uriBase = 'api/slevyr';
var uriExport = 'api/export';

$(document).ready(function () {
    readRunConfig();

    $("#exportInterval").submit(exportInterval);
    $("#ExportToCsv").click(exportInterval);
    $("#ExportCsv1").click(exportPredef);
    $("#ExportCsv2").click(exportPredef);
    $("#ExportCsv3").click(exportPredef);

    $('input[type=radio][name=transferType]').change(function () {
        if (this.value == 'download') {
            //alert("download");
            $("#saveParams").hide();
        }
        else  {
            //alert("save");
            $("#saveParams").show();
        }
    });

    $("#selectedFile").change(function () {
        //alert('changed!');
        var pom = $('#selectedFile').val();
        alert('changed to'+pom);
        $('#exportFilename').val($('#selectedFile').val());
    });

});

function readRunConfig() {
    $.getJSON(uriBase + '/getConfig?')
        .done(function (data) {
            $('#exportFilename').val(data.DefaultExportFileName);
        })
        .fail(function (jqXHR, textStatus, err) {
            window.slVyr.addNotification('error', 'Load Configuration Error: ' + err);
        });
}


function exportInterval(event) {
    event.preventDefault();
    if ($("#radioDownloadId:radio:checked").length > 0) {
        exportIntervalDownload();
    }
    else {
        exportIntervalSave();
    }
}

function exportPredef(event) {
    event.preventDefault();
    if ($("#radioDownloadId:radio:checked").length > 0) {
        exportPredefDownload();
    }
    else {
        exportPredefSave();
    }
}


function exportIntervalSave() {
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
        url: uriExport + '/exportInterval/',
        data: JSON.stringify(model),
        contentType: "application/json",
        success: function (response) {
            Download('http://localhost:5000/api/export/GetExportFile');
            alert(response);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            alert(xhr.status);
            alert(thrownError);
        }
    });
}

function exportIntervalDownload() {
    var fileName = $('#exportFilename').val();
    var timeFrom = $('#exportTimeFrom').val();
    var timeTo = $('#exportTimeTo').val();
    var unitId = $('#linkaId').val();
    var expAll = $('#exportAll').prop('checked');
    var expAllSepar = $('#exportAllSeparated').prop('checked');

    Download('api/export/ExportIntervalDownload?fileName=' + fileName + '&timeFrom=' + timeFrom +'&timeTo=' + timeFrom+'&timeTo=' + timeTo +
        '&unitId=' + unitId + '&expAll=' + expAll + '&expAllSepar=' + expAllSepar);
}

function exportPredefSave() {
    var targetid = event.target.id;
    var expVar = 0;
    //alert('exportInterval id:' + tid);

    if (targetid == "ExportCsv1")
        expVar = 1;
    else if (targetid == "ExportCsv2")
        expVar = 2;
    else if (targetid == "ExportCsv3")
        expVar = 3;

    $.getJSON(uriExport + '/ExportPredef',
        {
            fileName: $('#exportFilename').val(),
            unitId: $('#linkaId').val(),
            expAll: $('#exportAll').prop('checked'),
            expAllSepar: $('#exportAllSeparated').prop('checked'),
            exportVariant: expVar
        })
        .done(function (data) {
            window.slVyr.addNotification('success', 'Sucessfully set.');
            alert(data);
            // $('#stav').text('');
        })
        .fail(function (jqXHR, textStatus, err) {
            window.slVyr.addNotification('error', 'NastavCileSmen - error: ' + err);
            // $('#stav').text('Error: ' + err);
            // alert("nastavCileSmen - error");
        });   
}

function exportPredefDownload() {
    var targetid = event.target.id;
    var expVar = 0;
    //alert('exportInterval id:' + tid);

    if (targetid == "ExportCsv1")
        expVar = 1;
    else if (targetid == "ExportCsv2")
        expVar = 2;
    else if (targetid == "ExportCsv3")
        expVar = 3;

    Download('api/export/ExportPredefDownload?unitId=' + $('#linkaId').val() + '&exportVariant=' + expVar);
}


function Download(url) {
    document.getElementById('my_iframe').src = url;
};

