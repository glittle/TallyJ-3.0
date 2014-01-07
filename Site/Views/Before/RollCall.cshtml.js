var RollCallPage = function () {
  var local = {
    currentNameNum: 1,
    currentVoterDiv: null,
    rollCallHub: null,
    hubReconnectionTime: 95000,
    rollCallHubConnectionId: null,
    reconnectHubTimeout: null,
    nameDivs: []
  };

  var preparePage = function () {

    var main = $('.Main');
    local.nameDivs = main.children('div.Voter');

    scrollToMe(local.nameDivs[1]);

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
      $(this).text(isShowing ? 'Hide Menu' : 'Show Menu');
      $('.Nav').toggleClass('Show', isShowing);
      window.scrollTo(0, 0);
      return false;
    });

    setTimeout(delayedPreparePage, 200);

  };

  var delayedPreparePage = function () {

    // Proxy created on the fly          
    local.rollCallHub = $.connection.rollCallHubCore;

    // Declare a local function so the server can invoke it          
    local.rollCallHub.client.updatePeople = function (info) {
      updatePeople(info);
    };

    // Start the connection
    local.rollCallHub.connection
      .start(function () {
        local.rollCallHubConnectionId = this.id;
      })
      .done(function () {
        refreshHubConnection();
      });
  };

  var refreshHubConnection = function () {
    clearTimeout(local.reconnectHubTimeout);
    CallAjaxHandler(publicInterface.controllerUrl + '/JoinRollCallHub', { connId: local.rollCallHubConnectionId }, function () {
      resetHubConnectionTimer();
    });
  };

  var resetHubConnectionTimer = function () {
    clearTimeout(local.reconnectHubTimeout);
    local.reconnectHubTimeout = setTimeout(refreshHubConnection, local.hubReconnectionTime);
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
      local.nameDivs = $('.Main').children('div.Voter');
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
        LogMessage(ev.which);
        return;
    }
    var num = local.currentNameNum;

    if (num + delta >= 0 && num + delta < local.nameDivs.length) {
      local.currentNameNum += delta;
      scrollToMe(local.nameDivs[local.currentNameNum]);

      //            if (local.currentNameNum > 0) {
      //                $(local.nameDivs[local.currentNameNum]).animate({ opacity: delta < 0 ? 0 : 100 }, 200);
      //            }
    }
  };

  var scrollToMe = function (nameDiv) {
    var voter = $(nameDiv);

    var top = voter.offset().top;
    var fudge = -83;
    var time = 800;

    $('html,body').animate({
      scrollTop: top + fudge
    }, time);

    voter.switchClass('Other', 'Current', time, 'linear');

    if (local.currentVoterDiv) {
      local.currentVoterDiv.switchClass('Current', 'Other', time, 'linear');
    }

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