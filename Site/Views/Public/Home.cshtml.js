/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery.qtip.js" />
/// <reference path="../../Scripts/jquery-1.7.1.js" />

var HomeIndexPage = function () {
    var localSettings = {
    };

    var preparePage = function () {

        $('#btnJoin').on('click', null, null, btnJoinClick);
        //        $('#txtCode').on('keypress', null, null, function () {
        //            $('#ddlElections').prop('selectedIndex', -1);
        //        });
        //        $('#ddlElections').on('change', null, null, function () {
        //            $('#txtCode').val('');
        //        });

        $('.CenterPanel').on('click', 'p.StartJoin', startJoinClick);

        var children = $('#ddlElections').children();
        if (children.length == 1 && children.eq(0).val() != 0) {
            children.eq(0).prop('selected', true);
        }
    };

    var startJoinClick = function () {
        var src = $(this);
        $('.CenterPanel').addClass('chosen');

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

        var electionCode = $('#ddlElections').val();
        if (!electionCode) {
            statusSpan.addClass('error').html('Please select an election');
            return false;
        }
        var passCode = $('#txtPasscode').val();
        if (!passCode) {
            statusSpan.addClass('error').html('Please type in the access code');
            return false;

        }
        statusSpan.addClass('active').removeClass('error').text('Attempting to join...');

        var form = {
            election: electionCode,
            pc: passCode
        };

        CallAjaxHandler(publicInterface.controllerUrl + 'TellerJoin', form, function (info) {
            if (info.LoggedIn) {
                statusSpan.html('Success! &nbsp; Going to the Dashboard now...');
                location.href = 'Dashboard';
                return;
            }

            statusSpan.addClass('error').removeClass('active').html(info.Error);
        });
        return false;
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