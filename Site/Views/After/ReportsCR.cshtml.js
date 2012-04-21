/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7.1.js" />
/// <reference path="../../Scripts/highcharts.js" />

var ReportsPage = function () {
    var local = {
    };

    var preparePage = function () {
    };

    var getReport = function (code) {
        ShowStatusDisplay('Getting data...', 0);
        local.reportHolder.fadeOut();
        CallAjaxHandler(publicInterface.controllerUrl + '/GetReportData', { code: code }, showInfo, code);
    };

    var showInfo = function (info, code) {
        ResetStatusDisplay();

        local.reportInfo = info;

        if (info.Status != 'ok') {
            $('#Status').text(info.Status).show();
            local.reportHolder.hide();
            return;
        }

        $('#Status').hide();

        var reportDef = local.templatesRaw.find('#' + code);

        var rows = expandRows(info, reportDef.find('.row2').html());

        var data = {
            info: info,
            rows: reportDef.find('.row').html().filledWithEach(rows)
        };

        var body = reportDef.find('.body').html().filledWith(data);

        local.reportHolder.removeClass().addClass('Report' + code).fadeIn().html(body);

        if (info.ElectionStatus != 'Report') {
            local.reportHolder.prepend('<div class="status">Report may not be complete (Status: {ElectionStatusText})</div>'.filledWith(info));
        }
    };

    var expandRows = function (info, row2Template) {
        $.each(info.Rows, function () {
            if (row2Template) {
                this.rows2 = row2Template.filledWithEach(this.Rows2, ';&nbsp; ');
            }
        });
        return info.Rows;
    };

    var publicInterface = {
        controllerUrl: '',
        getReportInfo: function () {
            return local.reportInfo;
        },
        PreparePage: preparePage
    };

    return publicInterface;
};

var reportsPage = ReportsPage();

$(function () {
    reportsPage.PreparePage();
});