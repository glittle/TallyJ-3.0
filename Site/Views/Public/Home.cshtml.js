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
        $('#btnRefresh').on('click', null, function () {
            CallAjaxHandler(publicInterface.controllerUrl + 'OpenElections', null, function (info) {
                $('#ddlElections').html(info.html);
                selectDefaultElection();
            });

        });
        $('#btnChooseJoin').click(startJoinClick);
        $('#btnChooseLogin').click(startJoinClick);

        warnIfCompatibilityMode();

        selectDefaultElection();
    };

    var warnIfCompatibilityMode = function () {
        var $div = $('.browser.ie');
        if ($div.length) {
            if (document.documentMode < 9) {
                $div.append('<div>When using Internet Explorer, ensure that you are NOT using compatability mode!</div>');
            }
        }
    };

    var selectDefaultElection = function () {
        var children = $('#ddlElections').children();
        if (children.length == 1 && children.eq(0).val() != 0) {
            children.eq(0).prop('selected', true);
        }
    };

    var startJoinClick = function () {
        var src = $(this);
        $('.CenterPanel').addClass('chosen');

        if (src.attr('id') == 'btnChooseJoin') {
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
        if (!electionCode || electionCode === '0') {
            statusSpan.addClass('error').html('Please select an election');
            return false;
        }
        LogMessage('x' + electionCode + 'x');

        var passCode = $('#txtPasscode').val();
        if (!passCode) {
            statusSpan.addClass('error').html('Please type in the access code');
            return false;

        }
        statusSpan.addClass('active').removeClass('error').text('Checking...');

        var form = {
            election: electionCode,
            pc: passCode
        };

        CallAjaxHandler(publicInterface.controllerUrl + 'TellerJoin', form, function (info) {
            if (info.LoggedIn) {
                statusSpan.html('Success! &nbsp; Going to the Dashboard now...');
                location.href = publicInterface.dashBoardUrl;
                return;
            }

            statusSpan.addClass('error').removeClass('active').html(info.Error);
        });
        return false;
    };

    var publicInterface = {
        PreparePage: preparePage,
        controllerUrl: '',
        dashBoardUrl: ''
    };

    return publicInterface;
};

var homeIndexPage = HomeIndexPage();

$(function () {
    homeIndexPage.PreparePage();
});