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

    $('img.closer').click(cancelStart);

  };

  function changeFocus(ev) {
    $('.featureBig').removeClass('open').addClass('closed');
    $(ev.target).closest('.featureBig').addClass('open').removeClass('closed');
    window.scrollTo(0, 0);
  }
  //  function startJoinClick(dummy, btnIdRequested) {
  //    $('.CenterPanel').addClass('chosen');
  //    $('.JoinPanel').hide();
  //    $('.LoginPanel').hide();
  //    $('.VoterPanel').hide();
  //
  //    var btnId = btnIdRequested || $(this).attr('id');
  //
  //    if (btnId === 'btnChooseJoin') {
  //      $('.JoinPanel').fadeIn();
  //    }
  //    else if (btnId === 'btnChooseVoter') {
  //      $('.VoterPanel').fadeIn();
  //      warmupServer();
  //    }
  //    else {
  //      $('.LoginPanel').fadeIn();
  //      warmupServer();
  //    }
  //    $('input:visible').eq(0).focus();
  //  };

  function warmupServer() {
    if (local.warmupDone) {
      return;
    }
    local.warmupDone = true;
    CallAjaxHandler(publicInterface.controllerUrl + 'Warmup');
  }

  function cancelStart() {
    location.href = '.';
    //    $('.CenterPanel').removeClass('chosen');
    //    $('.JoinPanel').hide();
    //    $('.LoginPanel').hide();
    //    $('.VoterPanel').hide();
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