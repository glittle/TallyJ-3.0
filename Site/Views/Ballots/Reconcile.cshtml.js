var ReconcilePageFunc = function () {
  var local = {
    ballotListTemplate: '<div id=B{Id}>{Code} - <span id=BallotStatus{Id}>{StatusCode}</span></div>',
    sortedBallots: {},
    currentLocation: -1,
    frontDeskHub: null,
    hubReconnectionTime: 95000,
    showingNames: false,
    ballotMethods: []
  };

  var preparePage = function () {
    connectToFrontDeskHub();

    $('#btnShowNames').click(function () {
      $(this).hide();
      $('.Names, #lists').fadeIn();
      local.showingNames = true;
    });
    $('#locations').change(function() {
      changeLocation(this);
    });

    site.qTips.push({ selector: '#qTipUn', title: 'Un-used', text: 'If a person is registered on the Front Desk, then later "un-registered", they show here.' });

    processBallots(publicInterface.ballots);
    showOld(publicInterface.oldEnvelopes);
  };

  var connectToFrontDeskHub = function () {
    var hub = $.connection.frontDeskHubCore;

    hub.client.updatePeople = function () {
      LogMessage('signalR: updatePeople');

      local.currentLocation = '';
      changeLocation($('#locations'), true);
    };

    activateHub(hub, function () {
      LogMessage('Join frontDesk Hub');
      CallAjaxHandler(publicInterface.beforeUrl + '/JoinFrontDeskHub', { connId: site.signalrConnectionId });
    });
  };

//  var refreshHubConnection = function () {
//    var resetHubConnectionTimer = function () {
//      clearTimeout(local.reconnectHubTimeout);
//      local.reconnectHubTimeout = setTimeout(refreshHubConnection, local.hubReconnectionTime);
//    };
//    LogMessage('Join frontDeskHub');
//    clearTimeout(local.reconnectHubTimeout);
//    CallAjaxHandler(publicInterface.beforeUrl + '/JoinFrontDeskHub', { connId: site.signalrConnectionId }, function (info) {
//      resetHubConnectionTimer();
//    });
//  };

  var showOld = function (list) {
    if (!list.length) return;
    var ballotList = '<div title="{Tellers}"><span>{C_FullName}</span><span class=When>{When}{#("{Tellers}"==""?"":" <span class=\'ui-icon ui-icon-person\'></span>")}</span>{#("{EnvNum}"=="") ? "" : "<span class=EnvNum>#{EnvNum} &nbsp; {Method}</span>"}</div>'.filledWithEach(extend(list));
    $('#lists').append('<div><h3>Un-used Envelopes & Un-registered: {0} <span class="ui-icon ui-icon-info" id="qTipUn"></span></h3><div class="Names oldEnv">{^1}</div></div>'.filledWith(
        list.length, ballotList));

    ActivateTips();
  };

  var changeLocation = function (ddlLocation, highlight) {
    var newLocation = ddlLocation.val();
    if (newLocation != local.currentLocation && newLocation) {
      ShowStatusDisplay('Loading ballot information');
      CallAjaxHandler(publicInterface.controllerUrl + '/BallotsForLocation', { id: newLocation }, function (info) {
        if (highlight) {
          $('#Totals').effect('highlight', {}, 5000);
        }
        local.currentLocation = newLocation;
        processBallots(info.Ballots);
        if (newLocation == -1) {
          showOld(publicInterface.oldEnvelopes);
        }
        ResetStatusDisplay();
      });
    }
  };

  var processBallots = function (ballots) {
    local.sortedBallots = {};
    local.ballotMethods = [];

    for (var i = 0; i < ballots.length; i++) {
      var ballot = ballots[i];
      var method = ballot.VotingMethod;

      var list = local.sortedBallots[method];
      if (!list) {
        list = local.sortedBallots[method] = [];
        local.ballotMethods.push(method);
      }

      list.push(ballot);
    }

    var methodInfos = {
      P: { name: 'In Person', count: 0 },
      D: { name: 'Dropped Off', count: 0 },
      M: { name: 'Mailed In', count: 0 },
      C: { name: 'Called In', count: 0 }
    };
    var methodList = ['P', 'D', 'M', 'C'];

    var host = $('#lists');
    host.html('');

    for (var j = 0; j < methodList.length; j++) {
      method = methodList[j];
      var methodInfo = methodInfos[method] || { name: '???', count: 0 };

      var methodName = methodInfo.name;

      var sortedBallots = local.sortedBallots[method] || null;
      if (sortedBallots) {
        var ballotList = '<div title="{Tellers}"><span>{C_FullName}</span><span class=When>{When}{#("{Tellers}"==""?"":" <span class=\'ui-icon ui-icon-person\'></span>")}</span>{#("{EnvNum}"=="") ? "" : "<span class=EnvNum>#{EnvNum}</span>"}</div>'.filledWithEach(sortedBallots);
        host.append('<div data-method={0}><h3>{1}: {2}</h3><div class=Names>{^3}</div></div>'.filledWith(
            method, methodName, sortedBallots.length, ballotList));

        methodInfo.count = sortedBallots.length;
      }
    }

    // show totals
    //        var html = [];
    //        var template = '<tr class="{className}"><td>{name}</td><td>{count}</td></tr>';
    //        html.push(template.filledWith(methodInfos['M']));
    //        html.push(template.filledWith(methodInfos['D']));
    //        html.push(template.filledWith(methodInfos['C']));

    var totals = {
      absent: methodInfos['M'].count + methodInfos['D'].count + methodInfos['C'].count,
      inPerson: methodInfos['P'].count
    };
    totals.total = totals.inPerson + totals.absent;

    $('#Totals').html([
            'Total: {total}'.filledWith(totals),
            (methodInfos.P.name + ': {0}'.filledWith(methodInfos.P.count)).bold(),
            //'Absent: {absent}'.filledWith(totals),
            (methodInfos.D.name + ': {0}'.filledWith(methodInfos.D.count)).bold(),
            (methodInfos.M.name + ': {0}'.filledWith(methodInfos.M.count)).bold(),
            (methodInfos.C.count > 0 ? (methodInfos.C.name + ': {0}'.filledWith(methodInfos.C.count)) : '').bold()
    ].join(' &nbsp; &nbsp; '));

    //        html.push(template.filledWith({ className: 'SubTotal', name: 'Absentee Ballots', count: subTotal }));

    //        html.push(template.filledWith(methodInfos['P']));

    //        html.push(template.filledWith({ className: 'Total', name: 'Total', count: subTotal + methodInfos['P'].count }));

    //        $('#Totals').html('<table>{^0}</table>'.filledWith(html.join('')));


    if (local.showingNames) {
      $('.Names').fadeIn();
    }
  };

  var extend = function (ballots) {
    if (!ballots) return null;
    $.each(ballots, function () {
      //this.WhenText = FormatDate(this.When, ' ', true, true);
      this.Method = this.VotingMethod == 'P' ? 'In Person' : this.VotingMethod;
    });
    return ballots;
  };

  var publicInterface = {
    controllerUrl: '',
    preparePage: preparePage,
    ballots: []
  };

  return publicInterface;
};

var reconcilePage = ReconcilePageFunc();

$(function () {
  reconcilePage.preparePage();
});