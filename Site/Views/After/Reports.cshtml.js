var ReportsPage = function() {
    var local = {
        refreshTimeout: null,
        templatesRaw: null,
        currentTitle: '',
        reportLines: [],
        reportHolder: null
    };

    var preparePage = function() {
        local.reportHolder = $('#report');

        $('.chooser').on('click', 'a', function() {
            $('.chooser a').removeClass('selected');
            local.currentTitle = $(this).addClass('selected').text();
            setTimeout(function() {
                getReport(location.hash.substr(1), local.currentTitle);
            }, 0);
        });

        var hash = location.hash;
        if (hash) {
            local.currentTitle = $('.chooser a[href="' + hash + '"]').text();
            getReport(hash.substr(1), local.currentTitle);
        }

        $('.reportPanel').on('click', '.btnDownloadCsv', downloadCsv)
    };

    var warningMsg = '<p>Warning: The election is not yet finalized. This report may be incomplete and/or showing wrong information.</p>';

    var downloadCsv = function(ev) {
        var btn = $(ev.target);
        var lines = [
            '="Election",' + csvQuoted(reportsPage.electionTitle),
            'Report,"' + local.currentTitle + '"',
            'Date,"' + new Date().toLocaleString() + '"',
            ''
        ];

        var table = $(btn.data('table'));
        var divs = $(btn.data('divs'));
        var cellSplitter = btn.data('splitter') || '';

        if (divs.length) {
            divs.each(function(i, el) {
                lines.push(csvQuoted(extractText(el), cellSplitter));
            });
        }
        if (divs.length && table.length) {
            lines.push('');
        }
        if (table.length) {
            lines.push(table.find('thead td, thead th').map(function(i, el) { return csvQuoted(el.innerText, cellSplitter); }).get().join(','))
            table.find('tbody tr').each(function(i, tr) {
                lines.push($(tr).find('td').map(function(i, el) { return csvQuoted(el.innerText, cellSplitter); }).get().join(','));
            })
        }

        var contents = 'data:text/csv;charset=utf-8,\uFEFF' + encodeURIComponent(lines.join('\n'));

        var link = document.createElement('a');
        link.setAttribute('href', contents);
        link.setAttribute('download', local.currentTitle + (btn.data('file') || '') + '.csv');
        link.click();
    }

    var extractText = function(el) {
        var html = el.outerHTML;

        if (html.indexOf('<select') !== -1) {
            // get text of select embedded in other html is difficult. SelectedIndex is not cloned in outerHTML or .clone()
            var original = $(el);

            // copy current value to an attribute before cloning
            original.find('select').each(function(i, select) { $(select).attr('text', select.options[select.selectedIndex].text); });

            var temp = original.clone();

            // replace each select with it's new "text" attribute
            temp.find('select').each(function(i, select) { return $(select).replaceWith($(select).attr('text')) });

            return temp.text();
        }

        return el.innerText;
    }

    var csvQuoted = function(s, splitter) {
        if (s && splitter) {
            return s.split(splitter).map(function(s2) { return csvQuoted(s2.trim()); }).join(',');
        }

        var prefix = s.indexOf(',') === -1 ? '=' : '';

        return prefix + '"' + s.replace(/\n/g, ' ').replace(/\s+/g, ' ').trim() + '"';

        //var quote = false;
        //if (s.indexOf(',') !== -1) quote = true;
        //if (s.indexOf('\n') !== -1) quote = true;
        //if (quote) {
        //    return '"' + s + '"';
        //}
        //return s;
    }

    var getReport = function(code, title) {
        ShowStatusDisplay('Getting report...');
        local.reportLines = [];
        local.reportHolder.html('<div class=getting>Getting report...</div>');
        $('#Status').hide();
        CallAjaxHandler(publicInterface.controllerUrl + '/GetReportData', { code: code }, showInfo, { code: code, title: title });
    };

    var showInfo = function(info, codeTitle) {
        ResetStatusDisplay();
        if (!info) {
            return;
        }
        if (info.Status != 'ok') {
            $('#Status').text(info.Status).show();
            local.reportHolder.hide();
            return;
        }

        if (info.ElectionStatus != 'Finalized') {
            local.reportHolder.prepend('<div class="status">Report may not be complete (Status: {ElectionStatusText})</div>'.filledWith(info));
        }

        local.reportHolder.removeClass().addClass('Report' + codeTitle.code).fadeIn().html(info.Html);
        console.log('warn', info.Ready, $('div.body.WarnIfNotFinalized').length);
        if (!info.Ready && $('div.body.WarnIfNotFinalized').length) {
            console.log(warningMsg);
            $('#Status').html(warningMsg).show();
        }
        $('#title').text(codeTitle.title);
    };

    var publicInterface = {
        controllerUrl: '',
        local: local,
        PreparePage: preparePage
    };

    return publicInterface;
};

var reportsPage = ReportsPage();

$(function() {
    reportsPage.PreparePage();
});