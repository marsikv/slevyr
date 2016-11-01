
var uri = 'api/slevyr';

$(document).ready(function () {
    $("#exportInterval").submit(exportInterval);
});


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