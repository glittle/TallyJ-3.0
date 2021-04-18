var HomeIndexPage = function () {
  var local = {
    reconnectHubTimeout: null,
    hubReconnectionTime: 95000,
    warmupDone: false,
    isBad: false
  };

  var isBadBrowser = function () {
    if (window.safari) {
      // odd layout issues?
      return 'Safari';
    }
    // catch ie11 and some old mobile chromes
    if (typeof Symbol === "undefined") return 'Symbol';

    var msg = '';
    try {
      eval('let x = 1');
    } catch (e) {
      return 'Old browser';
    }
    return '';
  };

  var preparePage = function () {
    var isBad = local.isBad = isBadBrowser();
    if (isBad) {
      $('.badBrowser .detail').text(isBad + ', ' + navigator.userAgent).show();
      $('.badBrowser').show();
      return;
    }

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

    connectToPublicHub();

    //    if ($('.VoterLoginError').length) {
    //      startJoinClick(null, 'btnChooseVoter');
    //    }

    // refreshElectionList();
    $('form').on('submit', function () {
      logoffSignalR();
    });

    if (window.location.search.indexOf('v=voter') !== -1) {
      $('#btnChooseVoter').click();
    }
  };

  var connectToPublicHub = function () {
    var hub = $.connection.publicHubCore;

    hub.client.electionsListUpdated = function (listing) {
      console.log('signalR: electionsListUpdated');

      showElections(listing);
    };

    startSignalR(function () {
      console.log('Joining public hub');
      CallAjaxHandler(publicInterface.controllerUrl + 'PublicHub', { connId: site.signalrConnectionId }, function (info) {
        showElections(info.html);
      });
    });
  };

  function showElections(html) {
    var select = $('#ddlElections');
    if (!select.length) {
      return;
    }
    select.html(html);
    select.attr('size', select[0].children.length + 2);
    selectDefaultElection();
  };

  var refreshElectionList = function () {
    connectToPublicHub();
  };

  function selectDefaultElection() {
    var children = $('#ddlElections').children();
    if (children.length === 1 && children.eq(0).val() !== 0) {
      children.eq(0).prop('selected', true);
    }
  };



  function startJoinClick(dummy, btnIdRequested) {
    var btnId = btnIdRequested || $(this).attr('id');


    if (btnId === 'btnChooseJoin') {
      $('.CenterPanel').addClass('chosen');
      $('.LoginPanel').hide();
      $('.VoterPanel').hide();
      $('.JoinPanel').fadeIn();
    }
    else if (btnId === 'btnChooseVoter') {
      //location.href = GetRootUrl() + 'VoterAccount/Login';
      $('.CenterPanel').addClass('chosen');
      $('.JoinPanel').hide();
      $('.LoginPanel').hide();
      $('.VoterPanel').fadeIn();
      warmupServer();
    }
    else {
      $('.CenterPanel').addClass('chosen');
      $('.VoterPanel').hide();
      $('.JoinPanel').hide();
      $('.LoginPanel').fadeIn();
      warmupServer();
    }
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