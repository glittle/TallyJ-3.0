var ReportsPage = function () {
    var local = {
        refreshTimeout: null,
        reportInfo: null,
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
        ShowStatusDisplay('Getting report...');
        local.reportHolder.fadeOut();
        CallAjaxHandler(publicInterface.controllerUrl + '/GetReportData', { code: code }, showInfo, code);
    };

    var showInfo = function(info, code) {
      ResetStatusDisplay();

      local.reportInfo = info;

      if (info.Status != 'ok') {
        $('#Status').text(info.Status).show();
        local.reportHolder.hide();
        return;
      }

      $('#Status').hide();

      if (info.ElectionStatus != 'Report') {
        local.reportHolder.prepend('<div class="status">Report may not be complete (Status: {ElectionStatusText})</div>'.filledWith(info));
      }

      if (info.Html) {
        local.reportHolder.removeClass().addClass('Report' + code).fadeIn().html(info.Html);
        if (!info.Ready) {
          $('#Status').html(warningMsg).show();
        }
      } else {
        processData(code, info);
      }
    };

    var processData = function (code) {
        switch (code) {
            case 'Ballots':
            case 'AllReceivingVotesByVote':
            case 'AllReceivingVotes':
                doReportListStyle1(code);

                break;
            case 'SimpleResults':
                doReportResults1(code);
                break;
            default:
        }
    };

    var warningMsg = '<p>Warning: There are unresolved issues that may affect this report.</p>'
                   + '<p>Warning: This report may be incomplete and/or showing wrong information.</p>';
    
    var doReportResults1 = function (code) {
        var info = local.reportInfo;

        var reportDef = local.templatesRaw.find('#' + code);

        var bodyInfo = $.extend({}, info.Info, info.Info.Final);

        if (!info.Ready) {
            $('#Status').html(warningMsg).show();
        }

        var rowTemplate = reportDef.find('.result1row').html();
        
        bodyInfo.result1rows = rowTemplate.filledWithEach(info.People);
        
        var bodyTemplate = reportDef.find('.body').html();
        var body = bodyTemplate.filledWith(bodyInfo);

        local.reportHolder.addClass('Report' + code).fadeIn().html(body);
    };

    var doReportListStyle1 = function (code) {
        var info = local.reportInfo;

        if (!info.Ready) {
            $('#Status').html(warningMsg).show();
        }

        var reportDef = local.templatesRaw.find('#' + code);
        var rows = expandRows(info, reportDef.find('.row2').html());

        var data = {
            info: info,
            rows: reportDef.find('.row').html().filledWithEach(rows)
        };

        var body = reportDef.find('.body').html().filledWith(data);

        local.reportHolder.removeClass().addClass('Report' + code).fadeIn().html(body);
    };

    var expandRows = function (info, row2Template) {
        $.each(info.Rows, function () {
            if (row2Template) {
                this.rows2 = row2Template.filledWithEach(this.Rows2, ';&nbsp; ');
            }
        });
        return info.Rows;
    };

//    var getPart = function (selector, source) {
//        var xmlDoc = $.parseXML(source || site.templates.ReportTemplates),
//        $xml = $(xmlDoc).find(selector + ' > *'); //we need to use the xml parser becuase ie <= 8 doesn't support xhtml (strips quotes), this way it's unmodified
//        return $xml[0].xml ? $xml[0].xml : (new XMLSerializer()).serializeToString($xml[0]);
//    };

//    var getPart = function (selector, source) {
//        return $(source || site.templates.ReportTemplates).find(selector).html();
//    };

    var publicInterface = {
        controllerUrl: '',
        getReportInfo: function () {
            return local.reportInfo;
        },
        local: local,
        PreparePage: preparePage
    };

    return publicInterface;
};

var reportsPage = ReportsPage();

$(function () {
    reportsPage.PreparePage();
});