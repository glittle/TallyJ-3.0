/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />

var MonitorPage = function () {
    var settings = {
        rowTemplateMain: '',
        rowTemplateExtra: '',
        refreshTimeout: null
    };

    var preparePage = function () {
        var tableBody = $('#mainBody');
        settings.rowTemplateMain = '<tr class="{ClassName}">' + tableBody.children().eq(0).html() + '</tr>';
        settings.rowTemplateExtra = '<tr class="Extra {ClassName}">' + tableBody.children().eq(1).html() + '</tr>';

        showInfo(publicInterface.LocationInfos, true);

        var desiredTime = GetFromStorage(storageKey.MonitorRefresh, 60);

        $('#ddlElectionStatus').on('change', function () {
            ShowStatusDisplay('Updating...', 0);
            CallAjaxHandler(publicInterface.controllerUrl + '/UpdateElectionStatus', {
                status: $(this).val()
            }, function () {
                ShowStatusDisplay('Updated', 0, 3000, false, true);
            });
        });

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
        clearInterval(settings.autoMinutesTimeout);

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
        table.html(expandWithTemplates(info));

        $('#lastRefresh').html(new Date().toLocaleTimeString());

        startAutoMinutes();
        setAutoRefresh();
    };
    var startAutoMinutes = function () {
        var startTime = new Date();
        $('.minutesOld').each(function () {
            var span = $(this);
            span.data('startTime', startTime);
        });
        settings.autoMinutesTimeout = setInterval(function () {
            updateAutoMinutes();
        }, 15 * 1000);
    };

    var updateAutoMinutes = function () {
        $('.minutesOld').each(function () {
            var span = $(this);
            var start = +span.data('start');
            if (start) {
                var startTime = span.data('startTime');
                var now = new Date();
                var ms = now.getTime() - startTime.getTime();
                span.text(Math.round(ms / 1000 / 6 + start * 10) / 10);
            }
        });
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

    var expandWithTemplates = function (info) {
        var lastName = '';
        var count = 0;
        var rows = -1;
        var last = null;
        var html = [];

        $.each(info.Locations, function () {
            rows++;

            this.Btn = '<a class=ZoomIn title=View href="../Ballots?l={LocationId}&b={BallotId}"><span class="ui-icon ui-icon-zoomin"></span></a>'.filledWith(this);

            if (this.Name != lastName) {
                if (last != null) {
                    last.rows = rows;
                    rows = 0;
                }

                count++;
                this.ClassName = count % 2 == 0 ? 'Even' : 'Odd';
                lastName = this.Name;
                last = this;
            } else {
                this.Extra = true;
                this.ClassName = last.ClassName;
                //joinBtn.push('<span title="View" class="ui-icon ui-icon-zoomin" data-code="{0}"></span>'.filledWith(this.Code));

            }
        });

        if (last != null) {
            last.rows = rows + 1;
        }

        $.each(info.Locations, function () {
            if (this.Extra) {
                html.push(settings.rowTemplateExtra.filledWith(this));
            }
            else {
                html.push(settings.rowTemplateMain.filledWith(this));
            }
        });

        return html.join('');
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