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
        function() {
          var rowId = +$(this).parents('tr').data('rowid');
          setActiveUploadRowId(rowId, true);
        })
      .on('click',
        'button.deleteFile',
        function() {
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
            function(info) {
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
        function() {
          top.location.href =
            '{0}/Download?id={1}'.filledWith(publicInterface.controllerUrl, $(this).parents('tr').data('rowid'));
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
            getPreviewInfo();
          }
        });
    });

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

    var resultDiv = $('#importResults');
    var currentHeight = resultDiv.outerHeight();
    resultDiv.css('min-height', currentHeight + 'px').html('Starting').removeClass('failed').show();

    CallAjax2(publicInterface.controllerUrl + '/LoadBallotsFile', { id: local.activeFileRowId },
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

          resultDiv.html(info.result.join('')).show().css('min-height', '0').toggleClass('failed', info.failed === true);
          $('.DbCount span').text(comma(info.count));

          getPreviewInfo();

        } else if (info.ImportErrors) {
          local.vue.previewInfo = info;

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
        busy: 'Reading ballot file'
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

  function setupVue() {
    local.vue = new Vue({
      el: '#main',
      components: {
      },
      data: {
        sourceSystem: 'Cdn',
        previewInfo: {},
        activeFileRowId: 0,
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
          importNow();
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


