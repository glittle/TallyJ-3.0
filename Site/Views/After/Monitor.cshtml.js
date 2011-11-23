/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />

var MonitorPage = function () {
    var settings = {
        rowTemplate: '',
        refreshTimeout: null
    };

    var preparePage = function () {
        var tableBody = $('#mainBody');
        settings.rowTemplate = tableBody.html();

        showInfo(publicInterface.LocationInfos, true);

        var desiredTime = GetFromStorage(storageKey.MonitorRefresh, 60);

        $('#ddlRefresh').val(desiredTime).change(function () {
            $('#chkAutoRefresh').prop('checked', true);
            setAutoRefresh(true);
            SetInStorage(storageKey.MonitorRefresh, $(this).val());
        });

        $('#chkAutoRefresh').click(setAutoRefresh);
        $('#btnRefesh').click(refresh);

        setAutoRefresh(false);
    };

    var showInfo = function (info, firstLoad) {
        var table = $('#mainBody');
        if (!firstLoad) {
            table.animate({
                opacity: 0.5
            }, 10, function () {
                table.animate({
                    opacity: 1
                }, 500);
            });
        }
        table.html(settings.rowTemplate.filledWithEach(expand(info)));
        
        $('#lastRefresh').html(new Date().toLocaleTimeString());

        setAutoRefresh();
    };

    var setAutoRefresh = function (ev) {
        var wantAutorefresh = $('#chkAutoRefresh').prop('checked');
        clearTimeout(settings.refreshTimeout);

        if (wantAutorefresh) {
            settings.refreshTimeout = setTimeout(function () {
                refresh();
            }, 1000 * $('#ddlRefresh').val());

            if (ev) { // called from a handler
                refresh();
            }
        }
    };

    var refresh = function () {
        CallAjaxHandler(publicInterface.controllerUrl + '/RefreshMonitor', null, showInfo);
    };

    var expand = function (locationInfos) {
        $.each(locationInfos, function (i) {
            this.ClassName = i % 2 == 0 ? 'Even' : 'Odd';
        });
        return locationInfos;
    };

    var publicInterface = {
        controllerUrl: '',
        LocationInfos: null,
        PreparePage: preparePage
    };

    return publicInterface;
};

var monitorPage = MonitorPage();

$(function () {
    monitorPage.PreparePage();
});