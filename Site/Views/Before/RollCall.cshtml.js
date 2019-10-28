﻿var RollCallPage = function () {
  var local = {
    currentNameNum: 1,
    currentVoterDiv: null,
    currentLocation: -1,
    nameDivs: [],
    hasLocations: false,
    settingPrefix: 'rollCall_',
    rollCallLineTemplate: ''
  };

  function preparePage() {

    var ddlLocations = $('#locations');
    local.hasLocations = ddlLocations.find('option').length > 0;

    local.rollCallLineTemplate = $('#rollCallLine').text();
    local.currentLocation = rollCallPage.location;
    var currentLocOption = ddlLocations.find('option[value="{0}"]'.filledWith(local.currentLocation));
    currentLocOption.html(currentLocOption.html() + ' *');

    recallSetting('locations');
    if (local.hasLocations && !ddlLocations.val()) {
      ddlLocations.val(local.currentLocation);
      saveSetting(ddlLocations[0]);
    }

    recallSetting('includeMethod');
    recallSetting('showOthers');
    recallSetting('showLocations');
    recallSetting('showEnvReason');
    var main = $('#voterList');

    $.each(rollCallPage.voters, function (i, v) {
      addInfo(v);
    });

    main.html(local.rollCallLineTemplate.filledWithEach(rollCallPage.voters));
    local.nameDivs = main.children('div.Voter');
    updateVisibility();

    //<span class="ui-icon ui-icon-info" id="qTipWhyMask"></span>  
    //site.qTips.push({ selector: '#qTipWhyMask', title: 'Show Ballot Delivery Methods', text: 'If only one or two people have used a voting method, consider not showing the delivery methods.' });

    //scrollToMe(local.nameDivs[1]);

    connectToRollCallHub();

    attachHandlers();
  }

  function attachHandlers() {

    $('#btnStart').click(function () {
      $('#voterList').focus();
      local.currentNameNum = 0;
      scrollToMe(local.nameDivs[0]);
    });

    //$('#hideNav').click(function () {
    //  $('.Nav').removeClass('Show');
    //  $(document).focus();
    //});

    //$('#showNav').click(function (ev) {
    //  $('.Nav').addClass('Show').filter(':input').focus();
    //  ev.stopPropagation();
    //});

    $('#locations').change(function () {
      updateVisibility();
      saveSetting(this);
    });
    $('#includeMethod').change(function () {
      updateVisibility();
      SetInStorage('rollCall_includeMethod', $(this).val());
    });
    $('#showOthers').change(function () {
      updateVisibility();
      SetInStorage('rollCall_showOthers', $(this).val());
    });
    $('#showLocations').change(function () {
      updateVisibility();
      SetInStorage('rollCall_showLocations', $(this).val());
    });
    $('#showEnvReason').change(function () {
      updateVisibility();
      SetInStorage('rollCall_showEnvReason', $(this).prop('checked') ? 'Y' : 'N');
    });

    $(document).keydown(keyDown);

    $('#voterList').click(function (ev) {
      ev.which = 32;
      keyDown(ev);
    });

    //$('#btnReturn').click(function () {
    //  var isShowing = $('header').is(':visible');
    //  $('header').toggle(!isShowing);

    //  isShowing = !isShowing;
    //  $(this).text(isShowing ? 'Hide Menus' : 'Show Menus');
    //  $('.Nav').toggleClass('Show', isShowing);
    //  window.scrollTo(0, 0);
    //  return false;
    //});
  };

  function recallSetting(id, key) {
    var notSet = 'NOTSET';
    if (typeof key === 'undefined') {
      key = id;
    }
    var value = GetFromStorage(local.settingPrefix + key, notSet);
    if (value === notSet) {
      return;
    }
    var input = $('#' + id);
    if (input.attr('type') === 'checkbox') {
      input.prop('checked', value === 'Y');
      return;
    }

    input.val(value);
  };

  function addInfo(v) {
    //var currentDisplayLocation = $('#locations').val();
    //if (v.Loc != currentDisplayLocation) {
    v.Location = rollCallPage.hasLocations ? rollCallPage.locations[v.Loc] : '';
    //}

    if (v.VM !== 'P' && v.VM) {
      var vm = rollCallPage.methods[v.VM];
      if (vm) {
        v.VotingMethod = vm;
      }
      v.EnvInfo = v.Env;
      v.VotingInfo = '{VotingMethod}'.filledWith(v);
    }
  };
  function saveSetting(dom) {
    SetInStorage(local.settingPrefix + dom.id, $(dom).val());
  };
  function updateVisibility() {
    var locToShow = $('#locations').val() || 0; // may not exist; 0 means ALL
    var methodToShow = $('#includeMethod').val() || '';
    var othersHidden = $('#showOthers').val() == 'hidden';

    var showingOthers = !!(locToShow || methodToShow);
    $('#askOthers').toggle(showingOthers);
    if (!showingOthers) {
      othersHidden = false;
    }

    $.each(local.nameDivs, function (i, d) {
      var div = $(d);
      var thisLocation = locToShow ? div.hasClass('Loc_' + locToShow) : true;
      var thisMethod = methodToShow ? div.hasClass('VM_' + methodToShow) : true;
      var blank = d.id.search('-') != -1;

      div.toggleClass('Other', !(thisLocation && thisMethod) && !blank);
      div.toggleClass('NotLocal', !thisLocation);
      div.toggleClass('Present', thisLocation && div.hasClass('VM_P') && methodToShow !== 'P');
    });

    var value = $('#showOthers').val();
    $('body').toggleClass('OthersDim', value === 'dim');
    $('body').toggleClass('OthersHidden', value === 'hidden');

    value = $('#showLocations').val();
    $('body').removeClass('ShowLocations_em');
    $('body').removeClass('ShowLocations_i');
    $('body').addClass('ShowLocations_' + value);

    value = $('#showEnvReason').prop('checked');
    $('body').toggleClass('ShowEnvReason', value);
  };
  function connectToRollCallHub() {
    var hub = $.connection.rollCallHubCore;

    hub.client.updatePeople = function (info) {
      console.log('signalR: updatePeople');
      //console.log(info);
      updatePeople(info);
    };

    startSignalR(function () {
      console.log('Joining rollCall hub');
      CallAjaxHandler(publicInterface.controllerUrl + '/JoinRollCallHub', { connId: site.signalrConnectionId });
    });
  };

  //  function refreshHubConnection = () {
  //    function resetHubConnectionTimer () {
  //      clearTimeout(local.reconnectHubTimeout);
  //      local.reconnectHubTimeout = setTimeout(refreshHubConnection, local.hubReconnectionTime);
  //    };
  //
  //    console.log('Joining rollCallHub');
  //    clearTimeout(local.reconnectHubTimeout);
  //    CallAjaxHandler(publicInterface.controllerUrl + '/JoinRollCallHub', { connId: site.signalrConnectionId }, function () {
  //      resetHubConnectionTimer();
  //    });
  //  };

  function changeLocation(ddlLocation) {
    var newLocation = ddlLocation.val();
    if (newLocation != local.currentLocation && newLocation) {
      console.log('Change location');
    }
  };

  function updatePeople(info) {
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
        addInfo(item);
        var itemLine = $('#P' + item.PersonId);
        var html = local.rollCallLineTemplate.filledWith(item);

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
  //  function processPulse (info) {
  //    var people = info.MorePeople;
  //    if (people) {
  //      var firstBlankAtEnd = $('div.Voter#P-100');
  //      firstBlankAtEnd.before(people);
  //      local.nameDivs = $('.Main').children('div.Voter');
  //    }
  //  };

  function keyDown(ev) {
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
      case 13: // enter
      case 40: // down
        delta = 1;
        ev.preventDefault();
        break;
      //case 36: // home
      //  delta = 1 - local.currentNameNum;
      //  ev.preventDefault();
      //  break;

      //case 35: // end
      //  delta = local.nameDivs.length - local.currentNameNum - 1;
      //  ev.preventDefault();
      //  break;

      case 34: // page down
        delta = 4;
        ev.preventDefault();
        break;

      default:
        console.log(ev.which);
        return;
    }
    if ($(ev.target).closest('.Nav, header').length) {
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

  function scrollToMe(nameDiv) {
    var voter = $(nameDiv);

    if (local.currentVoterDiv) {
      //local.currentVoterDiv.switchClass('Current', 'NotCurrent', time, 'linear');
      $('#voterList .Current').removeClass('Current').addClass('NotCurrent');
    }

    var showAtTop = voter.prev().length ? voter.prev() : voter;

    var top = showAtTop.offset().top;
    var fudge = -10;//-83;
    var time = 500;

    $('html,body').animate({
      scrollTop: top + fudge
    },
      {
        duration: time,
        queue: false
      });

    //voter.switchClass('NotCurrent', 'Current', time, 'linear');
    voter.removeClass('NotCurrent').addClass('Current');

    local.currentVoterDiv = voter;
  };

  function returnToTop() {
    $('html,body').scrollTop(0);
  }

  //    function goFullScreen (div) {
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