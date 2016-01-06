var RollCallPage = function () {
  var local = {
    currentNameNum: 1,
    currentVoterDiv: null,
    currentLocation: -1,
    nameDivs: []
  };

  var preparePage = function () {

    local.currentLocation = rollCallPage.location;

    var main = $('#voterList');

    main.html(site.templates.RollCallLine.filledWithEach(rollCallPage.voters));
    local.nameDivs = main.children('div.Voter');
    updateVisibility();
    scrollToMe(local.nameDivs[1]);

    connectToRollCallHub();

    // ActivateHeartbeat(true, 15); // faster

    //    site.onbroadcast(site.broadcastCode.pulse, function (ev, info) {
    //      processPulse(info);
    //    });

    setTimeout(function () {
      $('.Nav').animate({ opacity: 0 }, 1500, null, function () {
        $('.Nav').removeClass('Show').css({
          opacity: ''
        });
      });
    }, 1000);

    $('#locations').change(function () {
      updateVisibility();
    });
    $('#includeMethod').change(function () {
      updateVisibility();
    });
    $('#showOthers').change(function () {
      updateVisibility();
    });

    $(document).keydown(keyDown);

    //        main.animate({
    //            marginTop: '0%'
    //        }, 5000, 'linear', function () {
    //            // Animation complete.
    //        });

    $('#btnReturn').click(function () {
      var isShowing = $('header').is(':visible');
      $('header').toggle(!isShowing);

      isShowing = !isShowing;
      $(this).text(isShowing ? 'Hide Top Menu' : 'Show Top Menu');
      $('.Nav').toggleClass('Show', isShowing);
      window.scrollTo(0, 0);
      return false;
    });
  };

  var updateVisibility = function () {
    var locToShow = $('#locations').val() || 0; // may not exist; 0 means ALL
    var methodToShow = $('#includeMethod').val() || '';
    var othersHidden = $('#showOthers').val() == 'hidden';

    $.each(local.nameDivs, function (i, d) {
      var div = $(d);
      var thisLocation = locToShow ? div.hasClass('Loc_' + locToShow) : true;
      var thisMethod = methodToShow ? div.hasClass('VM_' + methodToShow) : true;
      var blank = d.id.search('-') != -1;

      div.toggleClass('Other', !(thisLocation && thisMethod) && !blank);
      div.toggleClass('NotLocal', !thisLocation);
      div.toggleClass('Present', thisLocation && div.hasClass('VM_P') && methodToShow!='P');
    });

    var value = $('#showOthers').val();
    $('body').toggleClass('OthersDim', value === 'dim');
    $('body').toggleClass('OthersHidden', value === 'hidden');
  }

  var connectToRollCallHub = function () {
    var hub = $.connection.rollCallHubCore;

    hub.client.updatePeople = function (info) {
      LogMessage('signalR: updatePeople');
      //LogMessage(info);
      updatePeople(info);
    };

    activateHub(hub, function () {
      LogMessage('Join rollCallHub');
      CallAjaxHandler(publicInterface.controllerUrl + '/JoinRollCallHub', { connId: site.signalrConnectionId });
    });
  };

  //  var refreshHubConnection = function () {
  //    var resetHubConnectionTimer = function () {
  //      clearTimeout(local.reconnectHubTimeout);
  //      local.reconnectHubTimeout = setTimeout(refreshHubConnection, local.hubReconnectionTime);
  //    };
  //
  //    LogMessage('Join rollCallHub');
  //    clearTimeout(local.reconnectHubTimeout);
  //    CallAjaxHandler(publicInterface.controllerUrl + '/JoinRollCallHub', { connId: site.signalrConnectionId }, function () {
  //      resetHubConnectionTimer();
  //    });
  //  };

  var changeLocation = function (ddlLocation) {
    var newLocation = ddlLocation.val();
    if (newLocation != local.currentLocation && newLocation) {
      LogMessage('Change location');
    }
  };

  var updatePeople = function (info) {
    var updated = false;
    if (info.removedId) {
      var line = $('#P' + info.removedId);
      if (line.length) {
        line.remove();
        updated = true;
      }
    }
    if (info.changed) {
      for (var i = 0; i < info.changed.length; i++) {
        var item = info.changed[i];
        var itemLine = $('#P' + item.PersonId);
        var html = site.templates.RollCallLine.filledWith(item);

        if (itemLine.length) {
          if (itemLine.data('ts') != item.TS) {
            itemLine.replaceWith(html);
          }
        } else {
          var firstBlankAtEnd = $('div.Voter#P-100');
          firstBlankAtEnd.before(html);
        }
        updated = true;
      }
    }
    if (updated) {
      local.nameDivs = $('#voterList').children('div.Voter');
      updateVisibility();
    }
    site.lastVersionNum = info.NewStamp;
  };
  //  var processPulse = function (info) {
  //    var people = info.MorePeople;
  //    if (people) {
  //      var firstBlankAtEnd = $('div.Voter#P-100');
  //      firstBlankAtEnd.before(people);
  //      local.nameDivs = $('.Main').children('div.Voter');
  //    }
  //  };

  var keyDown = function (ev) {
    var delta = 0;
    switch (ev.which) {
      case 75: // k
      case 38: // up
        delta = -1;
        ev.preventDefault();
        break;

      case 33: // page up
        delta = -4;
        ev.preventDefault();
        break;

      case 32: // space
      case 74: // j
      case 40: // down
        delta = 1;
        ev.preventDefault();
        break;
      case 36: // home
        delta = 1 - local.currentNameNum;
        ev.preventDefault();
        break;

      case 35: // end
        delta = local.nameDivs.length - local.currentNameNum - 1;
        ev.preventDefault();
        break;

      case 34: // page down
        delta = 4;
        ev.preventDefault();
        break;

      case 27: //esc
        $('.Nav').toggleClass('Show');
        ev.preventDefault();
        return;

      default:
        //LogMessage(ev.which);
        return;
    }
    var wanted = local.currentNameNum;
    while (true) {
      wanted += delta;
      if (wanted >= 0 && wanted < local.nameDivs.length) {
        var wantedDiv = $(local.nameDivs[wanted]);
        if (wantedDiv.is(':visible')) {
          local.currentNameNum = wanted;
          scrollToMe(wantedDiv);
          break;
        }
      } else {
        break;
      }
      // after jumping, proceed one by one
      delta = Math.sign(delta);
    }
  };

  var scrollToMe = function (nameDiv) {
    var voter = $(nameDiv);

    if (local.currentVoterDiv) {
      //local.currentVoterDiv.switchClass('Current', 'NotCurrent', time, 'linear');
      $('#voterList .Current').removeClass('Current').addClass('NotCurrent');
    }

    var showAtTop = voter.prev().length ? voter.prev() : voter;

    var top = showAtTop.offset().top;
    var fudge = 0;//-83;
    var time = 100;

    $('html,body').animate({
      scrollTop: top + fudge
    }, time);

    //voter.switchClass('NotCurrent', 'Current', time, 'linear');
    voter.removeClass('NotCurrent').addClass('Current');

    local.currentVoterDiv = voter;
  };


  //    var goFullScreen = function (div) {
  //        if (div.webkitRequestFullScreen) {
  //            div.webkitRequestFullScreen(Element.ALLOW_KEYBOARD_INPUT);
  //        }

  //        if (div.mozRequestFullScreen) {
  //            div.mozRequestFullScreen();
  //        }
  //    };

  var publicInterface = {
    controllerUrl: '',
    PreparePage: preparePage
  };
  return publicInterface;
};

var rollCallPage = RollCallPage();

$(function () {
  rollCallPage.PreparePage();
});