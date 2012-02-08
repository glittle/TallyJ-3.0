/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7.1.js" />
/// <reference path="../../Scripts/highcharts.js" />

var ReportsPage = function () {
    var local = {
        refreshTimeout: null,
        templatesRaw: null,
        reportHolder: null
    };

    var preparePage = function () {
        local.templatesRaw = $(site.templates.ReportTemplates);
        local.reportHolder = $('#report');

        $('.chooser').on('click', 'a', function () {
            setTimeout(function () {
                getReport(location.hash.substr(1));
            }, 0);
        });

        var list = [];
        local.templatesRaw.children().each(function () {
            var reportDef = $(this);
            list.push({ code: reportDef.attr('id'), name: reportDef.data('name') });
        });

        $('#chooser').html("<li><a href='#{code}'>{name}</a></li>".filledWithEach(list));
        local.reportHolder.hide();
    };

    var getReport = function (code) {
        ShowStatusDisplay('Getting data...', 0);
        local.reportHolder.fadeOut();
        CallAjaxHandler(publicInterface.controllerUrl + '/GetReportData', { code: code }, showInfo, code);
    };

    var showInfo = function (info, code) {
        ResetStatusDisplay();

        if (info.Status != 'ok') {
            $('#Status').text(info.Status).show();
            local.reportHolder.hide();
            return;
        }

        $('#Status').hide();

        var reportDef = local.templatesRaw.find('#' + code);

        var data = {
            info: info,
            rows: reportDef.find('.row').html().filledWithEach(info.Rows)
        };
        var body = reportDef.find('.body').html().filledWith(data);

        local.reportHolder.removeClass().addClass('Report' + code).fadeIn().html(body);

        if (info.ElectionStatus != 'Report') {
            local.reportHolder.prepend('<div class="status">Report may not be complete (Status: {ElectionStatusText})</div>'.filledWith(info));
        }
    };

    var publicInterface = {
        controllerUrl: '',
        PreparePage: preparePage
    };

    return publicInterface;
};

var reportsPage = ReportsPage();

$(function () {
    reportsPage.PreparePage();
});