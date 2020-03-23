var HomeIndexPage = function () {
  var local = {
    reconnectHubTimeout: null,
    hubReconnectionTime: 95000,
    warmupDone: false,
  };

  var isBadBrowser = function () {
    if (window.safari) {
      // odd layout issues?
      return 'safari';
    }
    // catch ie11 and some old mobile chromes
    if (typeof Symbol === "undefined") return 'no symbol';
    //    try {
    //      // no support for ES2015
    //      eval('var test = (x) => x');
    //    } catch (e) {
    //      return 'no arrow function';
    //    }
    return '';
  };

  var preparePage = function () {
    var isBad = isBadBrowser();
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

    warnIfCompatibilityMode();

    connectToPublicHub();

    if ($('.VoterLoginError').length) {
      startJoinClick(null, 'btnChooseVoter');
    }

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
    select.html(html);
    select.attr('size', select[0].children.length + 2);
    selectDefaultElection();
  };

  var refreshElectionList = function () {
    connectToPublicHub();
  };

  var warnIfCompatibilityMode = function () {
    var $div = $('.browser.ie');
    if ($div.length) {
      if (document.documentMode < 9) {
        $div.append('<div>When using Internet Explorer, ensure that you are NOT using compatability mode!</div>');
      }
    }
  };

  function selectDefaultElection() {
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