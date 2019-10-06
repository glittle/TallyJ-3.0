﻿var HomeIndexPage = function () {
  var local = {
    reconnectHubTimeout: null,
    hubReconnectionTime: 95000,
    warmupDone: false
  };

  var preparePage = function () {
    $('#btnJoin').on('click', btnJoinClick);
    $('#btnRefresh').on('click', refreshElectionList);
    $('#txtPasscode').on('keypress', function (ev) {
      if (ev.which === 13) {
        btnJoinClick();
      }
    });
    $('#btnChooseJoin').click(startJoinClick);
    $('#btnChooseLogin').click(startJoinClick);
    $('#btnChooseVoter').click(startJoinClick);
    $(document).keydown(function (ev) {
      if (ev.which === 27) {
        cancelStart();
      }
    });
    $('img.closer').click(cancelStart);

    clearElectionRelatedStorageItems();

    warnIfCompatibilityMode();

    connectToPublicHub();

    if ($('.VoterLoginError').length) {
      startJoinClick(null, 'btnChooseVoter');
    }

    // refreshElectionList();
    $('form').on('submit', function () {
      logoffSignalR();
    });
  };

  var connectToPublicHub = function () {
    var hub = $.connection.publicHubCore;

    hub.client.electionsListUpdated = function (listing) {
      console.log('signalR: electionsListUpdated');

      $('#ddlElections').html(listing);
      selectDefaultElection();
    };

    startSignalR(function () {
      console.log('Joining public hub');
      CallAjaxHandler(publicInterface.controllerUrl + 'PublicHub', { connId: site.signalrConnectionId }, function (info) {
        showElections(info);
      });
    });
  };

  var showElections = function (info) {
    $('#ddlElections').html(info.html);
    selectDefaultElection();
  };

  var refreshElectionList = function () {
    connectToPublicHub();
    //CallAjaxHandler(publicInterface.controllerUrl + 'OpenElections', null, function (info) {
    //  showElections(info);
    //});
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
    if (children.length === 1 && children.eq(0).val() !== 0) {
      children.eq(0).prop('selected', true);
    }
  };



  function startJoinClick(dummy, btnIdRequested) {
    $('.CenterPanel').addClass('chosen');
    $('.JoinPanel').hide();
    $('.LoginPanel').hide();
    $('.VoterPanel').hide();

    var btnId = btnIdRequested || $(this).attr('id');

    if (btnId === 'btnChooseJoin') {
      $('.JoinPanel').fadeIn();
    }
    else if (btnId === 'btnChooseVoter') {
      $('.VoterPanel').fadeIn();
      warmupServer();
    }
    else {
      $('.LoginPanel').fadeIn();
      warmupServer();
    }
    $('input:visible').eq(0).focus();
  };

  function warmupServer() {
    if (local.warmupDone) {
      return;
    }
    local.warmupDone = true;
    CallAjaxHandler(publicInterface.controllerUrl + 'Warmup');
  }

  function cancelStart() {
    $('.CenterPanel').removeClass('chosen');
    $('.JoinPanel').hide();
    $('.LoginPanel').hide();
    $('.VoterPanel').hide();
  }

  var btnJoinClick = function () {
    var statusSpan = $('#joinStatus').removeClass('error');

    var electionGuid = $('#ddlElections').val();
    if (!electionGuid || electionGuid === '0') {
      statusSpan.addClass('error').html('Please select an election');
      return false;
    }

    var passCode = $('#txtPasscode').val();
    if (!passCode) {
      statusSpan.addClass('error').html('Please type in the access code');
      return false;

    }
    statusSpan.addClass('active').removeClass('error').text('Checking...');

    var form = {
      electionGuid: electionGuid,
      pc: passCode,
      oldCompGuid: GetFromStorage('compcode_' + electionGuid, null)
    };

    CallAjaxHandler(publicInterface.controllerUrl + 'TellerJoin', form, function (info) {
      if (info.LoggedIn) {
        SetInStorage('compcode_' + electionGuid, info.CompGuid);
        statusSpan.addClass('success').removeClass('active').html('Success! &nbsp; Going to the Dashboard now...');
        location.href = publicInterface.dashBoardUrl;
        return;
      }

      refreshElectionList();
      statusSpan.addClass('error').removeClass('active').html(info.Error);
    });
    return false;
  };

  var publicInterface = {
    PreparePage: preparePage,
    controllerUrl: '',
    dashBoardUrl: '',
    local: local
  };

  return publicInterface;
};

var homeIndexPage = HomeIndexPage();

$(function () {
  homeIndexPage.PreparePage();
});