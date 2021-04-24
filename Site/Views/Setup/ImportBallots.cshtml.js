var ImportBallotsPage = function () {
  var local = {
    uploadListBody: null,
    uploadListTemplate: '',
    uploader: null,
    uploadList: [],
    vue: null
  };

  function staticSetup() {
    setupVue();

    $('#importResults').hide();

    local.uploadListBody = $('#uploadListBody');
    local.uploadListTemplate = local.uploadListBody.html();

    $('#uploadListBody')
      .on('click',
        '.MakeActive',
        function () {
          var rowId = +$(this).parents('tr').data('rowid');
          setActiveUploadRowId(rowId, true);
        })
      .on('click',
        'button.deleteFile',
        function () {
          if (!confirm('Are you sure you want to permanently remove this file from the server?')) {
            return;
          }

          var parentRow = $(this).parents('tr');
          parentRow.css('background-color', 'red');
          var rowId = parentRow.data('rowid');
          CallAjax2(publicInterface.controllerUrl + '/DeleteBallotsFile',
            { id: rowId },
            {
              busy: 'Deleting'
            },
            function (info) {
              if (info.previousFiles) {
                showUploads(info);
              }
              if (rowId == local.activeFileRowId) {
                local.activeFileRowId = 0;
                local.vue.activeFileRowId = local.activeFileRowId;
              }
            });
        })
      .on('click',
        'button.download',
        function () {
          top.location.href =
            '{0}/Download?id={1}'.filledWith(publicInterface.controllerUrl, $(this).parents('tr').data('rowid'));
        });

    //    $('#uploadListBody').on('change', 'select.codePage', function () {
    //      var select = $(this);
    //      CallAjax2(publicInterface.controllerUrl + '/FileCodePage',
    //        {
    //          id: select.parents('tr').data('rowid'), cp: select.val()
    //        },
    //        {
    //          busy: 'Saving',
    //          done: 'Saved'
    //        }, function (info) {
    //          if (info.Message) {
    //            ShowStatusFailed(info.Message);
    //          }
    //          else {
    //            getPreviewInfo();
    //          }
    //        });
    //    });

    $('#btnImport').on('click', importNow);

    //$('#upload_target').load(function (ev) {
    //  ResetStatusDisplay();
    //});
    var uploadMsg;

    local.uploader = new qq.FileUploader({
      element: $('#file-uploader')[0],
      action: publicInterface.controllerUrl + '/UploadBallots',
      allowedExtensions: ['XML'],
      onSubmit: function (id, fileName) {
        if (fileName.length > 50) {
          alert('Please shorten the name of the file to less than 50 characters long. This one was ' + fileName.length + '.');
          return false;
        }
        uploadMsg = ShowStatusBusy('Uploading...');
      },
      onProgress: function (id, fileName, loaded, total) {
      },
      onComplete: function (id, fileName, info) {
        ResetStatusDisplay(uploadMsg);
        getUploadsList();
        if (info.rowId) {
          setActiveUploadRowId(+info.rowId);
          getPreviewInfo();
        }
      },
      onCancel: function (id, fileName) {
        ResetStatusDisplay(uploadMsg);
      },
      showMessage: function (message) {
        ResetStatusDisplay(uploadMsg);
        ShowStatusFailed(message);
      }
    });
  };

  function importNow() {
    if (!local.activeFileRowId) {
      alert('Please select a file from the list first.');
      return;
    }

    local.vue.importJustDone = false;

    ResetStatusDisplay();

    $('#loadingLog').show();
    $('#log').html('');

    //var resultDiv = $('#importResults');
    //var currentHeight = resultDiv.outerHeight();
    //resultDiv.css('min-height', currentHeight + 'px').html('Starting').removeClass('failed').show();

    CallAjax2(publicInterface.controllerUrl + '/LoadBallotsFile', { id: local.activeFileRowId },
      {
        busy: 'Importing'
      },
      function (info) {
        local.vue.importing = false;

        if (info.Success) {
          ShowStatusDone('Imported');

          local.vue.importJustDone = true;
          getPreviewInfo();

        } else {
          ShowStatusFailedMessage('Import failed. See details below.');
          getPreviewInfo();
        }
      });
  }

  function removeImportedInfo() {
    $('#log').html('');
    ResetStatusDisplay();

    CallAjax2(publicInterface.controllerUrl + '/RemoveImportedInfo', null,
      {
        busy: 'Removing ballots'
      },
      function (info) {
        local.vue.enableRemove = false;
        local.vue.removing = false;

        if (info.Success) {
          ShowStatusDone(info.Message);
          getPreviewInfo();
        } else {
          ShowStatusFailed(info.Message);
        }
      });
  }

  function getUploadsList() {
    CallAjaxHandler(publicInterface.controllerUrl + '/GetBallotUploadlist', null, function (info) {
      if (info.previousFiles) {
        showUploads(info);
      }
    });
  };
  function getPreviewInfo(forceRefreshCache) {
    CallAjax2(publicInterface.controllerUrl + '/GetBallotsPreviewInfo',
      {
        id: local.activeFileRowId,
        forceRefreshCache: forceRefreshCache || false
      },
      {
        busy: 'Reading the ballots file'
      },
      function (info) {
        local.vue.previewInfo = info;
      });

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
      this.UploadTimeExt = FormatDate(this.UploadTime, null, null, true);
      this.RowClass = this.C_RowId == local.activeFileRowId ? 'Active' : 'NotActive';
      this.ProcessingStatusAndSteps = this.ProcessingStatus;
    });
    return list;
  };
  function setActiveUploadRowId(rowId, highlightInList) {
    SetInStorage('ActiveUploadRowId', rowId);
    local.activeFileRowId = rowId;
    local.vue.activeFileRowId = local.activeFileRowId;
    local.vue.importJustDone = false;

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
      getPreviewInfo();
    }
    showActiveFileName();
  };
  function showActiveFileName() {
    var row = activeUploadFileRow();
    $('#activeFileName').text(row.length == 0 ? 'the XML file' : '"' + row.children().eq(2).text().trim() + '"');
  };
  function activeUploadFileRow() {
    return local.uploadListBody.find('tr[data-rowid={0}]'.filledWith(local.activeFileRowId));
  };
  function preparePage() {
    staticSetup();
    local.activeFileRowId = GetFromStorage('ActiveUploadRowId', 0);
    local.vue.activeFileRowId = local.activeFileRowId;

    connectToImportHub();

    showUploads(publicInterface);

    if (!activeUploadFileRow().length) {
      local.activeFileRowId = 0;
      local.vue.activeFileRowId = local.activeFileRowId;
      SetInStorage('ActiveUploadRowId', 0);
    } else {
      getPreviewInfo();
    }
  };


  function connectToImportHub() {
    var hub = $.connection.ballotImportHubCore;

    hub.client.StatusUpdate = function (msg, isTemp) {
      var mainLogDiv = $('#log');
      mainLogDiv.show();

      local.vue.statusUpdated = true;

      var tempLogDiv = $('#tempLog');
      if (isTemp) {
        tempLogDiv.html(msg);
      } else {
        tempLogDiv.html('');
        mainLogDiv.append('<div>' + msg + '</div>');
      }
    };

    startSignalR(function () {
      console.log('Joining ballot import hub');
      CallAjaxHandler(publicInterface.ballotImportHubUrl, { connId: site.signalrConnectionId }, function (info) {

      });
    });
  };

  function setupVue() {
    local.vue = new Vue({
      el: '#main',
      components: {
      },
      data: {
        sourceSystem: 'Cdn',
        previewInfo: {},
        activeFileRowId: 0,
        enableRemove: false,
        importing: false,
        removing: false,
        importJustDone: false,
        statusUpdated: false,
        dummy: 1
      },
      computed: {
      },
      watch: {
        sourceSystem: function (a, b) {
          if (!$('.PullInstructions.sourceTips:visible').length) {
            $('.PullInstructionsHandle.sourceTips').click();
          }
        }
      },
      created: function () {
      },
      mounted: function () {

      },
      methods: {
        plural: function (s, a, b, c) {
          return Plural(s, a, b, c);
        },
        getPreviewInfo: function () {
          getPreviewInfo(true);
        },
        importNow: function () {
          this.statusUpdated = false;
          this.importing = true;
          importNow();
        },
        removeImportedInfo: function () {
          this.removing = true;
          removeImportedInfo();
        }
      }
    });

  }

  var publicInterface = {
    controllerUrl: '',
    PreparePage: preparePage,
    previousFiles: []
  };
  return publicInterface;
};

var importBallotsPage = ImportBallotsPage();

$(function () {
  importBallotsPage.PreparePage();
});


