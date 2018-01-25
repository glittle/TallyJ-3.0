var ReconcilePageFunc = function () {
  var local = {
    groupedBallots: {},
    currentLocation: -1,
    frontDeskHub: null,
    hubReconnectionTime: 95000,
    showingNames: false,
    hasLocations: false,
    ballotMethods: [],
    sortSelector: '',
    envelopeTemplate: ''
  };

  var preparePage = function () {
    connectToFrontDeskHub();

    var ddlLocations = $('#ddlTopLocation');
    local.hasLocations = ddlLocations.is(':visible');

    local.envelopeTemplate = $('#envelopeTemplate').text();
    local.sortSelector = '<select class=sortSelector><option value=Time>Sort by Time</option>'
      + (reconcilePage.envNumMode == 'None' ? '' : '<option value=Env>Sort by Envelope Number</option>')
      + '<option value=Name>Sort by Name</option></select>'

    if (local.hasLocations) {
      var firstOption = ddlLocations.find('option[value="-1"]');
      var htmlAllOptions = '<option value="-2">[All Locations]</option>';
      if (firstOption.length) {
        firstOption.after(htmlAllOptions);
      } else {
        ddlLocations.prepend(htmlAllOptions);
      }
    }

    changeLocation(false);

    $('#btnShowNames').click(function () {
      $(this).hide();
      $('.Names, #lists').fadeIn();
      local.showingNames = true;
    });
    site.onbroadcast(site.broadcastCode.locationChanged, function () {
      changeLocation(false);
    });
    $('#btnRefresh').click(function () {
      local.currentLocation = 0;
      changeLocation(true);
    });

    $('#lists').on('change', '.sortSelector', sortSection);

    site.qTips.push({ selector: '#qTipUn', title: 'Un-used', text: 'If a person is registered on the Front Desk, then later "un-registered", they show here.' });

    //processBallots(publicInterface.ballots);
    //showDeselected(publicInterface.oldEnvelopes);


  };

  function connectToFrontDeskHub() {
    var hub = $.connection.frontDeskHubCore;

    hub.client.updatePeople = function () {
      console.log('signalR: updatePeople');

      local.currentLocation = 0;
      changeLocation(true);
    };

    startSignalR(function () {
      console.log('Joining frontDesk hub');
      CallAjaxHandler(publicInterface.beforeUrl + '/JoinFrontDeskHub', { connId: site.signalrConnectionId });
    });
  };

  //  var refreshHubConnection = function () {
  //    var resetHubConnectionTimer = function () {
  //      clearTimeout(local.reconnectHubTimeout);
  //      local.reconnectHubTimeout = setTimeout(refreshHubConnection, local.hubReconnectionTime);
  //    };
  //    console.log('Joining frontDeskHub');
  //    clearTimeout(local.reconnectHubTimeout);
  //    CallAjaxHandler(publicInterface.beforeUrl + '/JoinFrontDeskHub', { connId: site.signalrConnectionId }, function (info) {
  //      resetHubConnectionTimer();
  //    });
  //  };

  var showDeselected = function (list) {
    if (!list.length) {
      return;
    }
    //var ballotList = ('<div title="{Tellers}" data-time="{RegistrationTime}"><span><span>{C_FullName}</span>'
    //  + '<span class=When>{#("{Method}"=="") ? "" : " --> "}{Method} {When}</span>'
    //  + '</span>'
    //  + '{#("{Tellers}"==""?"":" <span class=\'ui-icon ui-icon-person EnvNum\'></span>")}'
    //  + '{#("{EnvNum}"=="") ? "" : "<span class=EnvNum>#{EnvNum}</span>"}'
    //  + '</div>').filledWithEach(extend(list));
    var ballotList = local.envelopeTemplate.filledWithEach(extend(list));
    $('#lists').append(

      // <h3>{1}: {2}</h3>{^4}</div>

      '<div class=removedBallots><div class=VmgHead><h3>De-selected{0}: {1}'.filledWith(local.hasLocations ? ' for all Locations' : '', list.length) +
      '<span class="ui-icon ui-icon-info" id="qTipUn"></span></h3>{^1}</div><div class="Names oldEnv">{^0}</div></div>'.filledWith(ballotList, local.sortSelector));

    ActivateTips();
  };

  function changeLocation(highlight) {
    var newLocation = local.hasLocations ? +$('#ddlTopLocation').val() : -2;
    if (newLocation !== local.currentLocation && newLocation) {
      ShowStatusDisplay('Loading ballot information');
      CallAjaxHandler(publicInterface.controllerUrl + '/BallotsForLocation', { id: newLocation }, function (info) {
        if (highlight) {
          $('#Totals').effect('highlight', {}, 5000);
        }
        local.currentLocation = newLocation;
        processBallots(info.Ballots);
        publicInterface.deselected = info.Deselected;

        //        if (newLocation === -1) {
        showDeselected(publicInterface.deselected);
        //        }

        ActivateTips(true);
        ResetStatusDisplay();
      });
    }
  };


  function extend2(ballot) {
    if (ballot.RegistrationTime) {
      var time = new Date(parseInt(ballot.RegistrationTime.substr(6)));
      ballot.FullTime = time.toString();
      ballot.SortTime = time.getTime();
    }
    return ballot;
  }

  function processBallots(ballots) {
    local.groupedBallots = {};
    local.ballotMethods = [];

    for (var i = 0; i < ballots.length; i++) {
      var ballot = ballots[i];
      var method = ballot.VotingMethod;

      var list = local.groupedBallots[method];
      if (!list) {
        list = local.groupedBallots[method] = [];
        local.ballotMethods.push(method);
      }

      list.push(extend2(ballot));
    }


    var methodInfos = {
      P: { name: reconcilePage.inPersonName, count: 0 },
      D: { name: 'Dropped Off', count: 0 },
      M: { name: 'Mailed In', count: 0 },
      R: { name: 'Registered, not Received', count: 0 },
      C: { name: 'Called In', count: 0 }
    };
    var methodList = ['P', 'D', 'M', 'C', 'R'];

    var host = $('#lists');
    host.html('');

    for (var j = 0; j < methodList.length; j++) {
      method = methodList[j];
      var methodInfo = methodInfos[method] || { name: '???', count: 0 };

      var methodName = methodInfo.name;

      var groupedBallots = local.groupedBallots[method] || null;
      if (groupedBallots) {

        var ballotList = local.envelopeTemplate.filledWithEach(groupedBallots);

        // VMG = vote method group
        host.append('<div data-method={0} class=VMG-{0}><div class=VmgHead><h3>{1}: {2}</h3>{^4}</div><div class=Names>{^3}</div></div>'.filledWith(
          method, methodName, groupedBallots.length, ballotList, local.sortSelector));

        //TODO
        //if (method == 'P') {
        //  host.find('.sortSelector option[value="Env"]').remove();
        //}

        methodInfo.count = groupedBallots.length;
      }
    }

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
      (methodInfos.C.count > 0 ? (methodInfos.C.name + ': {0}'.filledWith(methodInfos.C.count)) : '').bold(),
      (methodInfos.R.count > 0 ? ('(' + methodInfos.R.name + ': {0})'.filledWith(methodInfos.R.count)) : ''),
    ].join(' &nbsp; &nbsp; '));

    if (local.showingNames) {
      $('.Names').fadeIn();
    }
  };

  function extend(ballots) {
    if (!ballots) return null;
    var newBallots = [];
    $.each(ballots, function () {
      this.Method = this.VotingMethod == 'P' ? reconcilePage.inPersonName : this.VotingMethod;
      newBallots.push(extend2(this));
    });
    return newBallots;
  };

  var sortSection = function (ev) {
    var select = $(ev.target);
    var section = select.parent().find('.Names');
    var rows = section.children();
    var sortType = select.val();

    $.each(rows, function (i, r) {
      r.sortValue = null;
    });

    var getValue = function (a) {
      var value;
      switch (sortType) {
        case 'Name':
          value = $(a).children().eq(0).text();
          break;
        case 'Time':
          value = 0 - +$(a).data('time'); // reverse sort!
          break;
        case 'Env':
          value = +($(a).find('.EnvNum').data('num') || 0);
          break;
      }
      return value;
    }

    rows.sort(function (a, b) {
      var aValue = a.sortValue || (a.sortValue = getValue(a));
      var bValue = b.sortValue || (b.sortValue = getValue(b));
      return aValue > bValue ? 1 : -1;
    });

    rows.detach().appendTo(section);
  }

  var publicInterface = {
    controllerUrl: '',
    preparePage: preparePage,
    ballots: [],
    local: local
  };

  return publicInterface;
};

var reconcilePage = ReconcilePageFunc();

$(function () {
  reconcilePage.preparePage();
});