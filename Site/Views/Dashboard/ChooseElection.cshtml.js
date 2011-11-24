/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />

var HomeIndexPage = function () {
    var localSettings = {
    };

    var preparePage = function () {
        $(document).on('click', '.btnSelectElection', null, selectElection);
        $(document).on('click', '.btnCopyElection', null, copyElection);
        $(document).on('click', '.btnSelectLocation', null, selectLocation);
    };

    var selectElection = function () {
        var btn = $(this);
        var row = btn.parents('.Election');
        var form =
        {
            guid: row.data('guid')
        };

        CallAjaxHandler(publicInterface.electionsUrl + '/SelectElection', form, afterSelectElection, row);
    };

    var afterSelectElection = function (info, row) {
        if (info.Pulse) {
            ProcessPulseResult(info.Pulse);
        }

        if (info.Selected) {
            $('.Election.True').removeClass('True');
            row.addClass('True');

            $('.CurrentElectionName').text(info.ElectionName);
            $('.CurrentLocationName').text('[No location selected]');

            showLocations(info.Locations, row);

            site.heartbeatActive = true;
            ActivateHeartbeat(true);
        }

    };

    var showLocations = function (list, row) {
        var host = row.find('.Locations');
        var template = site.templates.LocationSelectItem;
        host.html(template.filledWithEach(list));
    };

    var selectLocation = function () {
        var btn = $(this);
        var form =
        {
            id: btn.data('id')
        };

        CallAjaxHandler(publicInterface.electionsUrl + '/SelectLocation', form, afterSelectLocation);
    };

    var afterSelectLocation = function (info) {
        if (info.Selected) {
            location.href = site.rootUrl + 'Dashboard';
            return;
        }
    };

    var copyElection = function () {

        var btn = $(this);
        var form =
        {
            guid: btn.parents('.Election').data('guid')
        };

        if (!confirm('Are you sure you want to make a new election based on this one?')) {
            return;
        }

        CallAjaxHandler(publicInterface.electionsUrl + '/CopyElection', form, function (info) {

            if (info.Success) {
                location.href = '.';
                return;
            }

            alert(info.Message);

            site.heartbeatActive = true;
            ActivateHeartbeat(true);
        });
    };
    var publicInterface = {
        elections: [],
        electionsUrl: '',
        PreparePage: preparePage
    };

    return publicInterface;
};

var chooseElectionPage = HomeIndexPage();

$(function () {
    chooseElectionPage.PreparePage();
});