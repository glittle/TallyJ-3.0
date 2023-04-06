var ImportCsvPage = function () {
  var local = {
    uploadListBody: null,
    uploadListTemplate: '',
    uploader: null,
    uploadList: [],
    activeFileRowId: 0,
    statusFieldName: 'IneligibleReasonGuid',
    timeTemplate: ''
  };

  var publicInterface = {
    controllerUrl: '',
    PreparePage: preparePage,
    previousFiles: []
  };

  function copyReason(ev) {
    var node = ev.target;
    var txt = '';
    var range;
    if (document.selection) {
      range = document.body.createTextRange();
      range.moveToElementText(node);
      range.select();
      txt = range.toString();
    } else if (window.getSelection) {
      range = document.createRange();
      range.selectNodeContents(node);
      window.getSelection().removeAllRanges();
      window.getSelection().addRange(range);
      txt = range.toString();
    }
    window.prompt("Copy this text and use it in the CSV data file!", txt);
  }

  function staticSetup() {
    $('#importResults').hide();

    local.timeTemplate = importCsvPage.T24 ? 'YYYY MMM D, H:mm' : 'YYYY MMM D, h:mm a';
    local.uploadListBody = $('#uploadListBody');
    local.uploadListTemplate = local.uploadListBody.html();

    showStatusReasons();

    $('#listOfStatusReasons').on('click', 'dd', copyReason);
    $('.reasonSamples').on('click', 'strong', copyReason);
    $('#btnPrepareFields').on('click', function () {
      if (!local.activeFileRowId) {
        alert('Please select a file from the list first.');
        return;
      }
      getFieldsInfo();
    });

    $('#uploadListBody')
      .on('click', '.MakeActive', function () {
        var rowId = +$(this).parents('tr').data('rowid');
        setActiveUploadRowId(rowId, true);
      })
      .on('click', '.CopyMap', function () {
        if (!local.activeFileRowId) {
          alert('Please select a file from the list first.');
        }
        var rowId = +$(this).parents('tr').data('rowid');

        CallAjax2(publicInterface.controllerUrl + '/CopyMap', { from: rowId, to: local.activeFileRowId },
          {
            busy: 'Saving',
            done: 'Saved'
          },
          function (info) {
            showFields(info);
          });
      })
      .on('click', 'button.deleteFile', function () {
        if (!confirm('Are you sure you want to permanently remove this file from the server?')) {
          return;
        }

        var parentRow = $(this).parents('tr');
        parentRow.css('background-color', 'red');
        var rowId = parentRow.data('rowid');
        CallAjax2(publicInterface.controllerUrl + '/DeleteFile', { id: rowId },
          {
            busy: 'Deleting'
          },
          function (info) {
            $('#fieldSelector').hide();
            if (info.previousFiles) {
              showUploads(info);
            }
            if (rowId == local.activeFileRowId) {
              local.activeFileRowId = 0;
            }
          });
      })
      .on('click', 'button.download', function () {
        top.location.href = '{0}/Download?id={1}'.filledWith(publicInterface.controllerUrl, $(this).parents('tr').data('rowid'));
      });


    ;

    $('#btnClearAll').on('click', function () {
      if (!confirm('Are you sure you want to permanently delete all the People records in this election?')) {
        return;
      }

      CallAjax2(publicInterface.controllerUrl + '/DeleteAllPeople', null,
        {
          busy: 'Deleting',
        },
        function (info) {
          if (info.Success) {
            $('#importResults').html(info.Results).show();
            $('.DbCount span').text(comma(info.count));
          } else {
            ShowStatusFailed(info.Results);
          }
        });
    });

    $('#uploadListBody').on('change', 'select.codePage', function () {
      var select = $(this);
      CallAjax2(publicInterface.controllerUrl + '/FileCodePage',
        {
          id: select.parents('tr').data('rowid'), cp: select.val()
        },
        {
          busy: 'Saving',
          done: 'Saved'
        }, function (info) {
          if (info.Message) {
            ShowStatusFailed(info.Message);
          }
          else {
            getFieldsInfo();
          }
        });
    });

    $('#uploadListBody').on('change', 'select.dataRow', function () {
      var select = $(this);
      CallAjax2(publicInterface.controllerUrl + '/FileDataRow',
        {
          id: select.parents('tr').data('rowid'),
          firstDataRow: select.val()
        },
        {
          busy: 'Saving',
          done: 'Saved'
        }, function (info) {
          if (info.Message) {
            ShowStatusFailed(info.Message);
          }
          else {
            getFieldsInfo();
          }
        });
    });

    $('#btnImport').on('click', function () {
      if (!local.activeFileRowId) {
        alert('Please select a file from the list first.');
        return;
      }

      var resultDiv = $('#importResults');
      var currentHeight = resultDiv.outerHeight();
      resultDiv.css('min-height', currentHeight + 'px').html('Starting').removeClass('failed').show();

      CallAjax2(publicInterface.controllerUrl + '/Import', { id: local.activeFileRowId },
        {
          busy: 'Starting'
        },
        function (info) {
          if (info.result) {
            for (var i = 0; i < info.result.length; i++) {
              var r = info.result[i];
              var prefix = r.substr(0, 3);

              switch (prefix) {
                case '~E ':
                case '~W ':
                case '~I ':
                  r = r.substr(3);
              }
              switch (prefix) {
                case '~E ':
                  r = `<div class=E>${r}</div>`;
                  break;
                case '~W ':
                  r = `<div class=W>${r}</div>`;
                  break;
                case '~I ':
                  r = `<div class=I>${r}</div>`;
                  break;
                default:
                  r = `<div>${r}</div>`;
                  break;
              }

              info.result[i] = r;
            };

            resultDiv.html(info.result.join('')).show().css('min-height', '0')
              .toggleClass('failed', info.failed === true);
            if (!info.failed) {
            $('.DbCount span').text(comma(info.count));
          }
          }
        });
    });

    $('#fieldSelector').on('change', 'select', fieldMapChanged);


    //$('#upload_target').load(function (ev) {
    //  ResetStatusDisplay();
    //});
    var msg = null;

    local.uploader = new qq.FileUploader({
      element: $('#file-uploader')[0],
      action: publicInterface.controllerUrl + '/Upload',
      allowedExtensions: ['CSV'],
      onSubmit: function (id, fileName) {
        if (fileName.length > 50) {
          alert('Please shorten the name of the file to less than 50 characters long. This one was ' + fileName.length + '.');
          return false;
        }
        msg = ShowStatusBusy('Uploading...');
      },
      onProgress: function (id, fileName, loaded, total) {
      },
      onComplete: function (id, fileName, info) {
        ResetStatusDisplay();
        ResetStatusDisplay(msg); // in case it hasn't been displayed yet
        getUploadsList();
        if (info.rowId) {
          setActiveUploadRowId(+info.rowId);
          getFieldsInfo();
        }
      },
      onCancel: function (id, fileName) {
        ResetStatusDisplay();
      },
      showMessage: function (message) { ShowStatusFailed(message); }
    });
  };
  function getUploadsList() {
    CallAjaxHandler(publicInterface.controllerUrl + '/GetUploadList', null, function (info) {
      if (info.previousFiles) {
        showUploads(info);
      }
    });
  };
  function getFieldsInfo() {
    CallAjax2(publicInterface.controllerUrl + '/ReadFields', { id: local.activeFileRowId },
      {
        busy: 'Reading columns'
      },
      function (info) {
        if (info.Success) {
          showFields(info);
        } else {
          $('#fieldSelector').hide();
          ShowStatusFailed(info.Message);
        }
      });

  };
  function showStatusReasons() {
    var html = [];
    html.push('<dt>Can Vote and be Voted For</dt>');
    html.push('<dd>Eligible</dd>');

    var group = '';
    for (var i = 0; i < importCsvPage.statusNames.length; i++) {
      var item = importCsvPage.statusNames[i];
      if (group !== item.Group) {
        group = item.Group;
        html.push('<dt>{0}</dt>'.filledWith(group));
      }
      html.push('<dd>{0}</dd>'.filledWith(item.Description));
    }
    $('#listOfStatusReasons').html('<dl>{^0}</dl>'.filledWith(html.join('')));
  };

  function showSelectorsStatus() {
    $('#fieldSelector').children().each(function () {
      var div = $(this);
      var select = div.find('select');
      if (select.length === 0) {
        return;
      }
      div.toggleClass('mapped', !!select.val());
    });
  }

  function fieldMapChanged() {
    var mappings = [];

    var selectChanged = $(this);
    selectChanged.toggleClass('Mapped', selectChanged.val() !== '');
    var selectNumChanged = selectChanged.data('num');
    var mapped = {};
    var dups = 0;

    $('#fieldSelector').children().each(function () {
      var div = $(this);
      var select = div.find('select');
      if (select.length === 0) {
        return;
      }
      //            // if other has same value, reset the other
      //            if (otherSelect.data('num') != selectNumChanged) {
      //                if (otherSelect.val() == newValue) {
      //                    // otherSelect.val('');
      //                    dups = true;
      //                }
      //            }

      var value = select.val();
      if (value) {
        mapped[value] = (mapped[value] || 0) + 1;
        if (mapped[value] > 1) {
          dups++;
        }
        var from = div.find('h5').text();
        mappings.push(from + String.fromCharCode(29) + value); // a " cannot be in the CSV
      }
    });

    showSelectorsStatus();

    var $err = $('#mappingError');
    if (dups) {
      $err.text('Duplicate mappings found. Each TallyJ field can only be mapped to one data column.');
      return;
    }
    $err.text('');

    CallAjax2(publicInterface.controllerUrl + '/SaveMapping', { id: local.activeFileRowId, mapping: mappings },
      {
        busy: 'Saving',
        done: 'Saved'
      }, function (info) {
        if (info.Message) {
          ShowStatusFailed(info.Message);
        }
        if (info.Status) {
          activeUploadFileRow().children().eq(1).text(info.Status);
        }
      });
  };
  function showFields(info) {
    var host = $('#fieldSelector')
      .html('<div class=ImportTips><div>CSV<span class="ui-icon ui-icon-info" id="qTipImportHead"></span></div><div>TallyJ <span class="ui-icon ui-icon-info" id="qTipImportFoot"></span></div></div>')
      .show();
    var options = '<option value="{value}">{text}</option>'.filledWithEach($.map(info.possible, function (f) {
      switch (f) {
        case local.statusFieldName:
        return { value: f, text: 'Eligibility Status' };
        case 'UnitName':
          return { value: f, text: 'Electoral Unit Name' };
      }
      return { value: f, text: ExpandName(f) };
    }));
    var template1 = '<div class="Col{extra}">' +
      '<h5>{field}</h5>' +
      '<select data-num={num}><option class=Ignore value="">(ignore)</option>' + options + '</select>' +
      '<div>{^sampleDivs}</div>' +
      '</div>';
    var count = 1;
    $.each(info.csvFields, function () {
      this.sampleDivs = '<div>{0}&nbsp;</div>'.filledWithEach(this.sample);
      if (count === 1) {
        this.extra = " FirstCol";
      }
      this.num = count++;
      host.append(template1.filledWith(this));
      var select = host.find('select').last();
      select.val(this.map);
      select.toggleClass('Mapped', select.val() !== '');
    });
    //console.log(info.csvFields);
    $('#numColumns').text(count - 1);

    site.qTips.push({ selector: '#qTipImportHead', title: 'Headers', text: 'These are the headers found in the header line of the CSV file.  One column is shown for each column found in the CSV file.  All columns are shown, but may not need to be imported.' });
    site.qTips.push({ selector: '#qTipImportFoot', title: 'TallyJ Fields', text: 'For each column shown here, select the TallyJ field that is the best match for the information in the column.' });
    ActivateTips();
    showSelectorsStatus();
  };

  function showUploads(info) {
    var list;
    if (typeof info !== 'undefined') {
      list = extendUploadList(info.previousFiles);
      local.uploadList = list;
    } else {
      list = local.uploadList;
    }
    local.uploadListBody.html(local.uploadListTemplate.filledWithEach(list));
    $('div.uploadList').toggle(local.uploadListBody.children().length != 0);
    local.uploadListBody.find('select').each(function () {
      var select = $(this);
      select.val(select.data('value'));
      if (this.selectedIndex == -1) {
        this.selectedIndex = 0;
      }
    });

    if (list.length == 1) {
      setActiveUploadRowId(list[0].C_RowId, true);
    }
    showActiveFileName();
  };
  function extendUploadList(list) {
    $.each(list, function () {
      this.UploadTimeExt = this.UploadTime ? moment(this.UploadTime).format(local.timeTemplate) : '';
      this.RowClass = this.C_RowId === local.activeFileRowId ? 'Active' : 'NotActive';
      this.ProcessingStatusAndSteps = this.ProcessingStatus;
    });
    return list;
  };
  function setActiveUploadRowId(rowId, highlightInList) {
    SetInStorage('ActiveUploadRowId', rowId);
    local.activeFileRowId = rowId;
    if (highlightInList) {
      $('tr[data-rowid]').removeClass('Active').addClass('NotActive');

      $.each(local.uploadList, function () {
        if (this.C_RowId == rowId) {
          this.RowClass = 'Active';
          $('tr[data-rowid="{0}"]'.filledWith(rowId)).addClass('Active').removeClass('NotActive');
        } else {
          this.RowClass = 'NotActive';
        }
      });
      // showUploads();
      getFieldsInfoIfNeeded();
    }
    showActiveFileName();
  };
  function getFieldsInfoIfNeeded() {
    //if (activeUploadFileRow().children().eq(1).text().trim() == 'Uploaded') {
    getFieldsInfo();
    //}
  };
  function showActiveFileName() {
    var row = activeUploadFileRow();
    $('#activeFileName').text(row.length == 0 ? 'the CSV file' : '"' + row.children().eq(2).text().trim() + '"');
  };
  function activeUploadFileRow() {
    return local.uploadListBody.find('tr[data-rowid={0}]'.filledWith(local.activeFileRowId));
  };
  function preparePage() {
    staticSetup();
    local.activeFileRowId = GetFromStorage('ActiveUploadRowId', 0);

    connectToImportHub();

    showUploads(publicInterface);

    if (!activeUploadFileRow().length) {
      local.activeFileRowId = 0;
      SetInStorage('ActiveUploadRowId', 0);
    } else {
      getFieldsInfoIfNeeded();
    }
  };

  function connectToImportHub() {
    var hub = $.connection.importHubCore;

    hub.client.importInfo = function (lines, people) {
      ResetStatusDisplay();
      var msg = 'Processed {0} lines<br>{1} people added'.filledWith(comma(lines), comma(people));
      ShowStatusDone(msg);
      $('#importResults').html(msg).show();
    };

    startSignalR(function () {
      console.log('Joining import hub');
      CallAjaxHandler(publicInterface.importHubUrl, { connId: site.signalrConnectionId }, function (info) {

      });
    });
  };


  return publicInterface;
};

var importCsvPage = ImportCsvPage();

$(function () {
  importCsvPage.PreparePage();
});


