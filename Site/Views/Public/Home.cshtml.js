var HomeIndexPage = function () {
  var local = {
    publicHubConnectionId: null,
    publicHub: null,
    reconnectHubTimeout: null,
    hubReconnectionTime: 95000
  };

  var preparePage = function () {


    $('#btnJoin').on('click', null, null, btnJoinClick);
    $('#btnRefresh').on('click', null, refreshElectionList);
    $('#btnChooseJoin').click(startJoinClick);
    $('#btnChooseLogin').click(startJoinClick);

    warnIfCompatibilityMode();

    // temp until signalr is working
//    setTimeout(function () {
//      refreshElectionList();
//    }, 60000);

    setTimeout(delayedPreparePage, 200);
  };
  var delayedPreparePage = function() {
    refreshElectionList();

    local.publicHub = $.connection.publicHubCore;

    // Declare a local function so the server can invoke it          
    local.publicHub.client.electionsListUpdated = function (listing) {
      LogMessage('signalR: electionsListUpdated');
      $('#ddlElections').html(listing);
      LogMessage(listing);
      selectDefaultElection();
    };

    // Start the connection
    local.publicHub.connection
      .start(function () {
        local.publicHubConnectionId = this.id;
      }).done(function () {
        updateHubConnection();
      });
  };

  var updateHubConnection = function () {
    clearTimeout(local.reconnectHubTimeout);
    CallAjaxHandler(publicInterface.controllerUrl + 'PublicHub', { connId: local.publicHubConnectionId }, function (info) {
      local.reconnectHubTimeout = setTimeout(updateHubConnection, local.hubReconnectionTime);
    });

  };

  var refreshElectionList = function () {
    CallAjaxHandler(publicInterface.controllerUrl + 'OpenElections', null, function (info) {
      $('#ddlElections').html(info.html);
      selectDefaultElection();
    });
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