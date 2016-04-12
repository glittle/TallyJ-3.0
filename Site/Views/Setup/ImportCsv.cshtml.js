var ImportCsvPage = function () {
  var local = {
    uploadListBody: null,
    uploadListTemplate: '',
    uploader: null,
    uploadList: [],
    activeFileRowId: 0,
    statusFieldName: 'IneligibleReasonGuid'
  };

  var staticSetup = function () {
    $('#importResults').css('display', 'inline-block').hide();

    local.uploadListBody = $('#uploadListBody');
    local.uploadListTemplate = local.uploadListBody.html();

    showStatusReasons();

    $('#listOfStatusReasons').on('click', 'dd', function () {
      var node = this;
      var txt = '';
      if (document.selection) {
        var range = document.body.createTextRange();
        range.moveToElementText(node);
        range.select();
        txt = range.toString();
      } else if (window.getSelection) {
        var range = document.createRange();
        range.selectNodeContents(node);
        window.getSelection().removeAllRanges();
        window.getSelection().addRange(range);
        txt = range.toString();
      }
      window.prompt("To copy to the clipboard, press Ctrl+C", txt);
    });
    $('#btnPrepareFields').live('click', function () {
      if (!local.activeFileRowId) {
        alert('Please select a file from the list first.');
        return;
      }
      getFieldsInfo();
    });

    $('.MakeActive').live('click', function () {
      var rowId = +$(this).parents('tr').data('rowid');
      setActiveUploadRowId(rowId, true);
    });

    $('.CopyMap').live('click', function () {
      if (!local.activeFileRowId) {
        alert('Please select a file from the list first.');
      }
      var rowId = +$(this).parents('tr').data('rowid');

      ShowStatusDisplay('Saving...');
      CallAjaxHandler(publicInterface.controllerUrl + '/CopyMap', { from: rowId, to: local.activeFileRowId }, function (info) {
        ShowStatusSuccess('Saved');
        showFields(info);
      });

    });
    $('#btnClearAll').live('click', function () {
      if (!confirm('Are you sure you want to permanently delete all the People records in this election?')) {
        return;
      }
      ShowStatusDisplay('Deleting...');

      CallAjaxHandler(publicInterface.controllerUrl + '/DeleteAllPeople', null, function (info) {
        ShowStatusSuccess('Deleted');
        $('#importResults').html(info.Results).show();
      });
    });

    $('button.deleteFile').live('click', function () {
      if (!confirm('Are you sure you want to permanently remove this file from the server?')) {
        return;
      }
      ShowStatusDisplay('Deleting...');

      var parentRow = $(this).parents('tr');
      parentRow.css('background-color', 'red');
      var rowId = parentRow.data('rowid');
      CallAjaxHandler(publicInterface.controllerUrl + '/DeleteFile', { id: rowId }, function (info) {
        if (info.previousFiles) {
          showUploads(info);
        }
        if (rowId == local.activeFileRowId) {
          local.activeFileRowId = 0;
        }
        ShowStatusSuccess('Deleted');
      });
    });
    $('#uploadListBody select').live('change', function () {
      var select = $(this);
      ShowStatusDisplay('Saving...');
      CallAjaxHandler(publicInterface.controllerUrl + '/FileCodePage', { id: select.parents('tr').data('rowid'), cp: select.val() }, function (info) {
        if (info.Message) {
          ShowStatusFailed(info.Message);
        }
        else {
          ShowStatusSuccess('Saved');
        }
      });
    });

    $('#btnImport').live('click', function () {
      if (!local.activeFileRowId) {
        alert('Please select a file from the list first.');
        return;
      }
      ShowStatusDisplay('Processing');
      $('#importResults').hide();

      CallAjaxHandler(publicInterface.controllerUrl + '/Import', { id: local.activeFileRowId }, function (info) {
        if (info.result) {
          $('#importResults').html(info.result.join('<br>')).show().toggleClass('failed', info.failed === true);
        }
        ResetStatusDisplay();
      });
    });

    $('#fieldSelector select').live('change', fieldMapChanged);

    $('button.download').live('click', function () {
      top.location.href = '{0}/Download?id={1}'.filledWith(publicInterface.controllerUrl, $(this).parents('tr').data('rowid'));
    });

    $('#upload_target').load(function (ev) {
      ResetStatusDisplay();
    });

    local.uploader = new qq.FileUploader({
      element: $('#file-uploader')[0],
      action: publicInterface.controllerUrl + '/Upload',
      allowedExtensions: ['CSV'],
      onSubmit: function (id, fileName) {
        ShowStatusDisplay('Uploading...');
        if (fileName.length > 50) {
          alert('Please shorten the name of the file to less than 50 characters long. This one was ' + fileName.length + '.');
          return false;
        }
      },
      onProgress: function (id, fileName, loaded, total) {
      },
      onComplete: function (id, fileName, info) {
        ResetStatusDisplay();
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
  var getUploadsList = function () {
    CallAjaxHandler(publicInterface.controllerUrl + '/GetUploadList', null, function (info) {
      if (info.previousFiles) {
        showUploads(info);
      }
    });
  };
  var getFieldsInfo = function () {
    ShowStatusDisplay('Reading columns...');
    CallAjaxHandler(publicInterface.controllerUrl + '/ReadFields', { id: local.activeFileRowId }, function (info) {
      showFields(info);
      ResetStatusDisplay();
    });

  };
  var showStatusReasons = function () {
    var html = [];
    var group = '';
    for (var i = 0; i < importCsvPage.statusNames.length; i++) {
      var item = importCsvPage.statusNames[i];
      if (group != item.Group) {
        group = item.Group;
        html.push('<dt>{0}</dt>'.filledWith(group))
      }
      html.push('<dd>{0}</dd>'.filledWith(item.Description))
    }
    $('#listOfStatusReasons').html('<dl>{^0}</dl>'.filledWith(html.join('')));
  }

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

  var fieldMapChanged = function () {
    var mappings = [];

    var selectChanged = $(this);
    selectChanged.toggleClass('Mapped', selectChanged.val() !== '');
    var selectNumChanged = selectChanged.data('num');
    var mapped = {};
    var dups = 0;

    $('#fieldSelector').children().each(function () {
      var div = $(this);
      var select = div.find('select');
      if (select.length == 0) {
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
        mappings.push(from + '->' + value);
      }
    });

    showSelectorsStatus();

    var $err = $('#mappingError');
    if (dups) {
      $err.text('Duplicate mappings found. Each TallyJ field can only be mapped to one data column.');
      return;
    }
    $err.text('');

    ShowStatusDisplay('Saving...');
    CallAjaxHandler(publicInterface.controllerUrl + '/SaveMapping', { id: local.activeFileRowId, mapping: mappings }, function (info) {
      if (info.Message) {
        ShowStatusFailed(info.Message);
      }
      else {
        ShowStatusSuccess('Saved');
      }
      if (info.Status) {
        activeUploadFileRow().children().eq(1).text(info.Status);
      }
    });
  };
  var showFields = function (info) {
    var host = $('#fieldSelector').html('<div class=ImportTips><span class="ui-icon ui-icon-info" id="qTipImportHead"></span><span class="ui-icon ui-icon-info" id="qTipImportFoot"></span></div>');
    var options = '<option value="{value}">{text}</option>'.filledWithEach($.map(info.possible, function (f) {
      if (f === local.statusFieldName) {
        return { value: f, text: 'Eligiblity Status' };
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

    site.qTips.push({ selector: '#qTipImportHead', title: 'Headers', text: 'These are the headers as found in the first line of the CSV file.  One column is shown for each column found in the CSV file.  All columns are shown, but may not need to be imported.' });
    site.qTips.push({ selector: '#qTipImportFoot', title: 'TallyJ Fields', text: 'For each column shown above, select the TallyJ field that is the best match for the information in the column.' });
    ActivateTips();
    showSelectorsStatus();
  };

  var showUploads = function (info) {
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
  var extendUploadList = function (list) {
    $.each(list, function () {
      this.UploadTimeExt = FormatDate(this.UploadTime, null, null, true);
      this.RowClass = this.C_RowId == local.activeFileRowId ? 'Active' : 'NotActive';
      this.ProcessingStatusAndSteps = this.ProcessingStatus;
    });
    return list;
  };
  var setActiveUploadRowId = function (rowId, highlightInList) {
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
  var getFieldsInfoIfNeeded = function () {
    //if (activeUploadFileRow().children().eq(1).text().trim() == 'Uploaded') {
    getFieldsInfo();
    //}
  };
  var showActiveFileName = function () {
    var row = activeUploadFileRow();
    $('#activeFileName').text(row.length == 0 ? 'the CSV file' : '"' + row.children().eq(2).text().trim() + '"');
  };
  var activeUploadFileRow = function () {
    return local.uploadListBody.find('tr[data-rowid={0}]'.filledWith(local.activeFileRowId));
  };
  var preparePage = function () {
    staticSetup();
    local.activeFileRowId = GetFromStorage('ActiveUploadRowId', 0);

    connectToImportHub();

    showUploads(publicInterface);

    if (activeUploadFileRow().length == 0) {
      local.activeFileRowId = 0;
      SetInStorage('ActiveUploadRowId', 0);
    } else {
      getFieldsInfoIfNeeded();
    }
  };

  var connectToImportHub = function () {
    var hub = $.connection.importHubCore;

    hub.client.importInfo = function (lines, people) {
      ResetStatusDisplay();
      var msg = 'Processed {0} lines.<br>{1} people added'.filledWith(comma(lines), comma(people));
      ShowStatusDisplay(msg, 0, 9999999);
      $('#importResults').html(msg).show();
    };

    activateHub(hub, function () {
      LogMessage('Join import Hub');
      CallAjaxHandler(publicInterface.importHubUrl, { connId: site.signalrConnectionId }, function (info) {

      });
    });
  };



  var publicInterface = {
    controllerUrl: '',
    PreparePage: preparePage,
    previousFiles: []
  };
  return publicInterface;
};

var importCsvPage = ImportCsvPage();

$(function () {
  importCsvPage.PreparePage();
});


