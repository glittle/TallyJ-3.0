/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />
/// <reference path="../../Scripts/jquery-1.7.1.js" />
/// <reference path="../../Scripts/fileuploader.js" />

var ImportV1Page = function () {
    var local = {
        uploadListBody: null,
        uploadListTemplate: '',
        uploader: null,
        uploadList: [],
        activeFileRowId: 0
    };

    var staticSetup = function () {
        local.uploadListBody = $('#uploadListBody');
        local.uploadListTemplate = local.uploadListBody.html();

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

        $('#btnClearAll').live('click', function () {
            if (!confirm('Are you sure you want to permanently delete all the people and ballots in this election?')) {
                return;
            }
            ShowStatusDisplay('Deleting...');

            CallAjaxHandler(publicInterface.controllerUrl + '/DeleteAllPeopleAndBallots', null, function (info) {
                ShowStatusSuccess('Deleted');
                $('#importResults').html(info.Results);
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
            ShowStatusDisplay('Processing...');
            $('#importResults').html('');

            CallAjaxHandler(publicInterface.controllerUrl + '/ImportXml', { id: local.activeFileRowId }, function (info) {
                if (info.importReport) {
                    $('#importResults').html(info.importReport);
                    ShowStatusSuccess(info.importReport);
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
            action: publicInterface.controllerUrl + '/UploadXml',
            allowedExtensions: ['xml'],
            onSubmit: function (id, fileName) {
                ShowStatusDisplay('Uploading...');
            },
            onProgress: function (id, fileName, loaded, total) {
            },
            onComplete: function (id, fileName, info) {
                ResetStatusDisplay();
                if (info.success) {
                    getUploadsList();
                    if (info.rowId) {
                        setActiveUploadRowId(+info.rowId);
                    }
                }
                else {
                    ShowStatusFailed(info.messages);
                }
            },
            onCancel: function (id, fileName) {
                ResetStatusDisplay();
            },
            showMessage: function (message) { ShowStatusFailed(message); }
        });
    };
    var getUploadsList = function () {
        CallAjaxHandler(publicInterface.controllerUrl + '/GetUploadListXml', null, function (info) {
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
    var fieldMapChanged = function () {
        var mappings = [];

        var selectChanged = $(this);
        var selectNumChanged = selectChanged.data('num');
        var newValue = selectChanged.val();

        $('#fieldSelector').children().each(function () {
            var div = $(this);
            var select = div.find('select');

            // if other has same value, reset the other
            if (select.data('num') != selectNumChanged) {
                if (select.val() == newValue) {
                    select.val('');
                }
            }

            var to = select.val();
            if (to) {
                var from = div.find('h3').text();
                mappings.push(from + '->' + to);
            }
        });
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
        var host = $('#fieldSelector').html('');
        var options = '<option value="{0}">{#ExpandName("{0}")}</option>'.filledWithEach(info.possible);
        var template1 = '<div><h3>{field}</h3><select data-num={num}><option class=Ignore value="">-</option>' + options + '</select><div>{^sampleDivs}</div></div>';
        var count = 1;
        $.each(info.csvFields, function () {
            this.sampleDivs = '<div>{0}&nbsp;</div>'.filledWithEach(this.sample);
            this.num = count++;
            host.append(template1.filledWith(this));
            host.find('select').last().val(this.map);
        });
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
        });
        showActiveFileName();
    };
    var extendUploadList = function (list) {
        $.each(list, function () {
            this.UploadTimeExt = FormatDate(this.UploadTime, null, null, true);
            this.RowClass = this.C_RowId == local.activeFileRowId ? 'Active' : 'NotActive';
            this.ProcessingStatusAndSteps = this.ProcessingStatus;
            switch (this.FileType) {
                case 'V1_Comm':
                    this.TypeDisplay = 'Community';
                    break;
                case 'V1_Elect':
                    this.TypeDisplay = 'Election';
                    break;
                default:
            }
        });
        return list;
    };
    var setActiveUploadRowId = function (rowId, highlightInList) {
        SetInStorage('ActiveUploadRowId', rowId);
        local.activeFileRowId = rowId;
        if (highlightInList) {
            $.each(local.uploadList, function () {
                this.RowClass = this.C_RowId == rowId ? 'Active' : 'NotActive';
            });
            showUploads();
            getFieldsInfoIfNeeded();
        }
        showActiveFileName();
    };
    var getFieldsInfoIfNeeded = function () {
        if (activeUploadFileRow().children().eq(1).text().trim() == 'Uploaded') {
            getFieldsInfo();
        }
    };
    var showActiveFileName = function () {
        var row = activeUploadFileRow();
        $('#activeFileName').text(row.length == 0 ? 'the XML file' : '"' + row.children().eq(2).text().trim() + '"');
    };
    var activeUploadFileRow = function () {
        return local.uploadListBody.find('tr[data-rowid={0}]'.filledWith(local.activeFileRowId));
    };
    var preparePage = function () {
        staticSetup();
        local.activeFileRowId = GetFromStorage('ActiveUploadRowId', 0);

        showUploads(publicInterface);

        if (activeUploadFileRow().length == 0) {
            local.activeFileRowId = 0;
            SetInStorage('ActiveUploadRowId', 0);
        } else {
            getFieldsInfoIfNeeded();
        }
    };
    var publicInterface = {
        controllerUrl: '',
        PreparePage: preparePage,
        previousFiles: []
    };
    return publicInterface;
};

var importV1Page = ImportV1Page();

$(function () {
    importV1Page.PreparePage();
});


