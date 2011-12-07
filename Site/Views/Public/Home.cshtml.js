/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />

var HomeIndexPage = function () {
    var localSettings = {
    };

    var preparePage = function () {

        $('#btnJoin').on('click', null, null, btnJoinClick);
        $('#txtCode').on('keypress', null, null, function () {
            $('#ddlElections').prop('selectedIndex', -1);
        });
        $('#ddlElections').on('change', null, null, function () {
            $('#txtCode').val('');
        });

        $('.CenterPanel').on('click', '.StartJoin', startJoinClick);
    };

    var startJoinClick = function () {
        var src = $(this);
        $('.CenterPanel').css({ 'float': 'left', margin: 5 });
        if (src.attr('id') == 'startJoin') {
            $('.JoinPanel').fadeIn();
            $('.LoginPanel').hide();
        }
        else {
            $('.LoginPanel').fadeIn();
            $('.JoinPanel').hide();
        }
    };

    var btnJoinClick = function () {
        var statusSpan = $('#joinStatus').removeClass('error');

        var electionCode = $('#txtCode').val() || $('#ddlElections').val();
        if (!electionCode) {
            statusSpan.addClass('error').html('Choose election first.');
            return;
        }
        var passCode = $('#txtPasscode').val();
        if (!passCode) {
            statusSpan.addClass('error').html('Secret Code?');
            return;
        }
        statusSpan.addClass('error').text('Attempting to join...');

        var form = {
            election: electionCode,
            pc: passCode
        };

        CallAjaxHandler(publicInterface.controllerUrl + 'TellerJoin', form, function (info) {
            if (info.LoggedIn) {
                location.href = 'Dashboard';
                return;
            }

            statusSpan.addClass('error').html(info.Error);
        });
    };

    var publicInterface = {
        PreparePage: preparePage,
        controllerUrl: ''
    };

    return publicInterface;
};

var homeIndexPage = HomeIndexPage();

$(function () {
  homeIndexPage.PreparePage();
});