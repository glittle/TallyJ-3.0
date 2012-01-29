/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />
/// <reference path="../../Scripts/fileuploader.js" />

var ImportExportPage = function () {
    var local = {
        uploadListBody: null,
        uploadListTemplate: '',
        uploader: null
    };

    var staticSetup = function () {
        local.uploadListBody = $('#uploadListBody');
        local.uploadListTemplate = local.uploadListBody.html();

        $('#chooseFile').live('change', function () {
            var isEmpty = this.value == '';
            $('#btnStartUpload')
                .prop('disabled', isEmpty)
                .attr('title', isEmpty ? 'Choose a file before uploading it' : 'Click to upload:\n\n' + this.value);
            if (this.value.search(/\.csv$/) == -1) {
                alert('Please note:\n\nOnly files containing comma-separated values (CSV) are acceptable for upload.');
            }
        });
        $('button.deleteFile').live('click', function () {
            if (!confirm('Are you sure you want to permanently remove this file from the server?')) {
                return;
            }
            // ajax call
            CallAjaxHandler(publicInterface.controllerUrl + '/DeleteFile', { id: $(this).parents('tr').data('rowid') }, function (info) {
                if (info.previousFiles) {
                    showUploads(info);
                }
            });
        });
        $('button.download').live('click', function () {
            // ajax call
            top.location.href = '{0}/Download?id={1}'.filledWith(publicInterface.controllerUrl, $(this).parents('tr').data('rowid'));
        });

        $('#btnResetList').click(function () {
            ShowStatusDisplay('Resetting...');
            CallAjaxHandler(publicInterface.controllerUrl + '/ResetAll', null, function (info) {
                ResetStatusDisplay();
            });
        });
        $('#upload_target').load(function (ev) {
            ResetStatusDisplay();
        });

        local.uploader = new qq.FileUploader({
            element: $('#file-uploader')[0],
            action: publicInterface.controllerUrl + '/Upload',
            allowedExtensions: ['CSV'],
            onSubmit: function (id, fileName) {
                ShowStatusDisplay('Uploading...', 0);
            },
            onProgress: function (id, fileName, loaded, total) {
            },
            onComplete: function (id, fileName, responseJson) {
                ResetStatusDisplay();
                getUploadsList();
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
    var showUploads = function (info) {
        var list = info.previousFiles;
        var timeOffset = info.serverTime.parseJsonDate() - new Date();
        local.uploadListBody.html(local.uploadListTemplate.filledWithEach(extendUploadList(list, timeOffset)));
    };
    var extendUploadList = function (list, timeOffset) {
        $.each(list, function () {
            var serverTime = this.UploadTime.parseJsonDate();
            var clientTime = new Date(serverTime.getTime() + timeOffset);
            this.UploadTimeExt = FormatDate(clientTime, null, null, true);
            //            this.DeleteLink = '<button type=button class=deleteFile title="Permanently delete this from the server"><span class="ui-icon ui-icon-trash"></span></button>';
            //            this.DownloadLink = '<button type=button class=download title="Download a copy of this file"><span class="ui-icon ui-icon-arrowreturn-1-s"></span></button>';
        });
        return list;
    };
    var preparePage = function () {
        staticSetup();

        showUploads(publicInterface);
    };
    var publicInterface = {
        controllerUrl: '',
        PreparePage: preparePage,
        previousFiles: [],
        uploadStarted: function () {
            ShowStatusDisplay('Uploading...', 0, 30000, false, false);
        },
        uploadFinished: function (rowId) {
            ResetStatusDisplay();
        }
    };
    return publicInterface;
};

var importExportPage = ImportExportPage();

$(function () {
    importExportPage.PreparePage();
});


