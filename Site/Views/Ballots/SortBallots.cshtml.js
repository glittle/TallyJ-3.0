var ReconcilePageFunc = function () {
  var local = {
    ballotListTemplate: '<div id=B{Id}>{Code} - <span id=BallotStatus{Id}>{StatusCode}</span></div>',
    ballots: {},
    currentLocation: -1,
    frontDeskHub: null,
    hubReconnectionTime: 95000,
    showingNames: false,
    ballotMethods: [],
    recentUpdates: [],
    sortSelector: '<select class=sortSelector><option value=Name>Sort by Name</option><option value=Env>Sort by Envelope Number</option></select>'
  };

  var preparePage = function () {
    connectToFrontDeskHub();

    changeLocation($('#locations'), false);

    $('#btnShowNames').click(function () {
      $(this).hide();
      $('.Names, #lists').fadeIn();
      local.showingNames = true;
    });
    $('#locations').change(function () {
      changeLocation($(this));
    });
    $('#btnRefresh').click(function () {
      local.currentLocation = -1;
      changeLocation($('#locations'), true);
    });

    $('#lists').on('change', '.sortSelector', sortSection);
  };

  var connectToFrontDeskHub = function () {
    var hub = $.connection.frontDeskHubCore;

    hub.client.updatePeople = function (changes) {
      LogMessage('signalR: updatePeople');
      var personLines = changes.PersonLines;
      for (var i = 0; i < personLines.length; i++) {
        local.recentUpdates.push({
          when: new Date(),
          who: personLines[i].PersonId,
          active: false
        });
      }

      local.currentLocation = '';
      changeLocation($('#locations'), true);
    };

    activateHub(hub, function () {
      LogMessage('Join frontDesk Hub');
      CallAjaxHandler(publicInterface.beforeUrl + '/JoinFrontDeskHub', { connId: site.signalrConnectionId });
    });
  };

  var changeLocation = function (ddlLocation, highlight) {
    var newLocation = ddlLocation.val();
    if (newLocation != local.currentLocation && newLocation) {
      ShowStatusDisplay('Loading ballot information');
      CallAjaxHandler(publicInterface.controllerUrl + '/BallotsForLocation', { id: newLocation }, function (info) {
        local.currentLocation = newLocation;
        processBallots(info.Ballots);
        sortSection();
        highlightRecentChanges();

        ActivateTips(true);
        ResetStatusDisplay();
      });
    }
  };

  var highlightRecentChanges = function () {
    var i = 0;
    var minutes = .2;
    var maxAge = minutes * 60 * 1000;
    var now = new Date();
    while (true) {
      if (i >= local.recentUpdates.length) {
        break;
      }
      var info = local.recentUpdates[i];
      var age = now - info.when;
      if (age < maxAge) {
        if (!info.active) {
          var div = $('#B_' + info.who);
          // each call cancels any others currently in effect - can't change it :(
          div.effect({ effect: 'highlight', duration: maxAge - age });
          info.active = true;
        }
        i++;
      } else {
        local.recentUpdates.splice(i, 1);
      }
    }
  }

  var extend2 = function (ballot) {
    ballot.TellerIcon = ballot.Tellers == '?' ? '' : '<span title="{Tellers}" class=\'ui-icon ui-icon-person\'></span>'.filledWith(ballot);
    ballot.EnvInfo = '<b data-num="{EnvNum}">{EnvNum}</b>'.filledWith(ballot);
    //var time = new Date(parseInt(ballot.RegistrationTime.substr(6)));
    //ballot.FullTime = time.toString();
    //ballot.SortTime = time.getTime();
    return ballot;
  }

  var processBallots = function (ballots) {
    local.ballots = [];
    var ThresholdFor3Columns = 20;

    for (var i = 0; i < ballots.length; i++) {
      var ballot = ballots[i];
      var method = ballot.VotingMethod;
      if (method == 'P') {
        continue;
      }

      local.ballots.push(extend2(ballot));
    }

    var host = $('#lists');
    host.html('');

    var ballotList = '<div id="B_{PersonId}">{^EnvInfo}<span>{C_FullName}</span><span class=When>{^TellerIcon}</span></div>'.filledWithEach(local.ballots);
    host.append('<div>{^0}<h3>Envelopes: {1}</h3><div class=Names>{^2}</div></div>'.filledWith(
        local.sortSelector, local.ballots.length, ballotList));

    $('#lists .Names').toggleClass('Col3', local.ballots.length > ThresholdFor3Columns)

    if (local.showingNames) {
      $('.Names').fadeIn();
    }
  };

  var sortSection = function () {
    var select = $('.sortSelector');
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
          value = $(a).find('span').eq(0).text();
          break;
        case 'Env':
          value = +($(a).find('b').data('num') || 0);
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
    ballots: []
  };

  return publicInterface;
};

var sortBallotsPage = ReconcilePageFunc();

$(function () {
  sortBallotsPage.preparePage();
});