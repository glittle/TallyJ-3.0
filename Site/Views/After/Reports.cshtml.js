﻿var ReportsPage = function () {
  var local = {
    refreshTimeout: null,
    templatesRaw: null,
    currentTitle: '',
    reportLines: [],
    reportHolder: null,
    reportVue: null,
  };

  var publicInterface = {
    controllerUrl: '',
    local: local,
    vueMixin: null,
    PreparePage: preparePage
  };

  function preparePage() {
    local.reportHolder = $('#report');
    local.timeTemplate = reportsPage.T24 ? 'YYYY MMM D, H:mm' : 'YYYY MMM D, h:mm a';

    window.addEventListener("hashchange", function () {
      $('.chooser a').removeClass('selected');
      var hash1 = location.hash;
      local.currentTitle = $('.chooser a[href="' + hash1 + '"]').addClass('selected').text();
      getReport(hash1.substr(1), local.currentTitle);
    }, false);

    var hash = location.hash;
    if (hash) {
      local.currentTitle = $('.chooser a[href="' + hash + '"]').addClass('selected').text();
      getReport(hash.substr(1), local.currentTitle);
    }

    $('.reportPanel').on('click', '.btnDownloadCsv', downloadCsv);

  };

  var warningMsg = '<p><strong>Warning</strong>: The election is not Finalized in TallyJ. This report may be incomplete and/or showing wrong information.</p>';

  function downloadCsv(ev) {
    var btn = $(ev.target);
    var lines = [
      'Election,' + csvQuoted(reportsPage.electionTitle),
      'Report,"' + local.currentTitle + '"',
      'Date,"' + moment().format(local.timeTemplate) + '"',
      ''
    ];

    var table = $(btn.data('table'));
    var divs = $(btn.data('divs'));
    var cellSplitter = btn.data('splitter') || '';

    if (divs.length) {
      divs.each(function (i, el) {
        lines.push(csvQuoted(extractText(el), cellSplitter));
      });
    }
    if (divs.length && table.length) {
      lines.push('');
    }
    if (table.length) {
      lines.push(table.find('thead td, thead th')
        .map(function (i, el) { return csvQuotedTd(el, cellSplitter); })
        .get()
        .join(','));
      table.find('tbody tr:visible')
        .each(function (i, tr) {
          lines.push($(tr).find('td').map(function (i, el) { return csvQuotedTd(el, cellSplitter); })
            .get()
            .join(','));
        });
    }

    //    var contents = 'data:text/csv;charset=utf-8,\uFEFF' + encodeURIComponent(lines.join('\r\n'));
    var crlf = '\r\n';
    var contents = 'data:text/csv;charset=utf-8,' + encodeURIComponent(lines.join(crlf) + crlf);

    var link = document.createElement('a');
    link.style.display = 'none';
    link.setAttribute('href', contents);
    link.setAttribute('download', local.currentTitle + (btn.data('file') || '') + '.csv');
    link.click();
  }

  function extractText(el) {
    var html = el.outerHTML;

    if (html.indexOf('<select') !== -1) {
      // get text of select embedded in other html is difficult. SelectedIndex is not cloned in outerHTML or .clone()
      var original = $(el);

      // copy current value to an attribute before cloning
      original.find('select').each(function (i, select) { $(select).attr('text', select.options[select.selectedIndex].text); });

      var temp = original.clone();

      // replace each select with it's new "text" attribute
      temp.find('select').each(function (i, select) { return $(select).replaceWith($(select).attr('text')) });

      return temp.text();
    }

    return el.innerText;
  }

  function csvQuotedTd(td) {
    var result = [];

    addChildren(td, result);

    return csvQuoted(result.map(s => s.trim()).filter(s => s).join('|'));
  }

  function addChildren(node, result) {
    var childNodes = node.childNodes;
    for (var i = 0; i < childNodes.length; i++) {
      var child = childNodes[i];
      if (child.childNodes.length) {
        addChildren(child, result);
        continue;
      }
      switch (child.nodeName) {
        case 'BR':
          result.push('');
          //        result += ' | ';
          break;
        //      case 'DIV':
        //        result += ' | ' + node.textContent;
        //        break;
        default:
          if (child.textContent) {
            result.push(...child.textContent.split('\n'));
          }
          break;
      }
    }
  }


  function csvQuoted(s, splitter) {
    if (s && splitter) {
      return s.split(splitter).map(function (s2) { return csvQuoted(s2.trim()); }).join(',');
    }

    var value = s.trim();
    if (!value) {
      return '';
    }

    value = value.split('"').join('""'); //double any quotes

//    var isNum = (!isNaN(value) && value === (+value).toString()) // a number without any formatting
//      || value.slice(-1) === '%';

    //    return isNum ? value : '"' + value + '"';
    return '"' + value + '"';
  }

  function getReport(code, title) {
    document.title = 'TallyJ Report - ' + title;
    CallAjax2(publicInterface.controllerUrl + '/GetReportData', { code: code },
      {
        busy: 'Getting report'
      },
      showInfo, { code: code, title: title });
    local.reportLines = [];
    local.reportHolder.html('<h1 class=getting>' + title + '<span class=loading><i class="el-icon-loading"></i> Getting Report...</span></h1>');
    $('#Status').hide();
    publicInterface.vueMixin = null;
  }

  function showInfo(info, codeTitle) {
    if (!info) {
      return;
    }
    if (info.Status !== 'ok') {
      $('#Status').text(info.Status).show();
      local.reportHolder.hide();
      return;
    }

    if (info.ElectionStatus !== 'Finalized') {
      local.reportHolder.prepend('<div class="status">Report may not be complete (Status: {ElectionStatusText})</div>'.filledWith(info));
    }

    local.reportHolder
      .removeClass()
      .addClass('Report' + codeTitle.code)
      .fadeIn()
      .html(info.Html.replace(/\x01/g, '\<br>'));

    if (publicInterface.vueMixin) {
      buildWithVue();
    }

    //    console.log('warn', info.Ready, $('div.body.WarnIfNotFinalized').length);
    if (!info.Ready && $('div.body.WarnIfNotFinalized').length) {
      // server must prepare "Ready" for any report that needs it
      console.log(warningMsg);
      $('#Status').html(warningMsg).show();
    }
    $('#title').text(codeTitle.title);
    $('#titleDate').text(moment().format(local.timeTemplate));
    $(".sortable").tablesorter({
      //      widgets: ['zebra', 'columns'],
      headerTemplate: '{content}{icon}', // dropbox theme doesn't like a space between the content & icon
      usNumberFormat: false,
      sortReset: true,
      sortRestart: true
    });
  };

  function buildWithVue() {
    report.vue = new Vue({
      el: '#vueBody',
      mixins: [publicInterface.vueMixin],
      data: {
      },
      computed: {
        timeTemplate() {
          return local.timeTemplate;
        }
      },
      watch: {
      },
      created: function () {
      },
      mounted: function () {
        this.extendRows();
      },
      methods: {
        extendRows() {
          if (this.rows && this.extendRow) {
            this.rows.forEach(this.extendRow);
          }
        }
      }
    });
  }

  return publicInterface;
};

var reportsPage = ReportsPage();

$(function () {
  reportsPage.PreparePage();
});