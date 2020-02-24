﻿// Copyright Glen Little, TallyJ
var site = {
  onload: [],
  languageCode: 'EN',
  templates: {},
  computerActive: true,
  context: '', // controller/action
  passcode: null,
  lastVersionNum: 0,
  infoForHeartbeat: {},
  heartbeatActive: true,
  heartbeatSeconds: 60, // default seconds
  heartbeatTimeout: null,
  timeOffsetKnown: false,
  hoverQuickLinkRevertDelay: null,
  hoverQuickLinkShowDelay: null,
  firstPulse: null,
  electionGuid: null,
  electionState: null,
  signalrConnectionId: null,
  signalrConnecting: false,
  signalrDelayedCallbacks: [],
  signalrDelay: null,
  signalrReconnecting: false,
  menuShowingDefault: true,
  menuShowDelay: 1000,
  menuResetDelay: 2000,
  qTips: [],
  broadcastCode: {
    electionStatusChanged: 'electionStatusChanged',
    locationChanged: 'locationChanged',
    startNewPerson: 'startNewPerson',
    personSaved: 'personSaved',
    personNameChanging: 'personNameChanging',
    pulse: 'pulse',
  },
  broadcast: function (broadcastCode, data) {
    console.log('broadcast', broadcastCode);
    $(document).triggerHandler(broadcastCode, data);
  },
  onbroadcast: function (broadcastCode, fn, eventData) {
    $(document).on(broadcastCode, null, eventData, fn);
  },
  timeOffset: 0,
  rootUrl: '/'
};
var storageKey = {
  MonitorRefresh: 'MonitorRefresh',
  MonitorRefreshOn: 'MonitorRefreshOn'
};
var MyResources = {};

$(function () {
  Onload();
});

function Onload() {
  if (site.onload.length !== 0) {
    eval(site.onload.join(';'));
  }
  PrepareStatusDisplay();

  // site.timeOffset = site.serverTime.parseJsonDate() - new Date();

  if (site.electionGuid) {
    connectToElectionHub();
  }

  showMenu(site.electionState, true);

  CheckTimeOffset();

  AttachHelp();

  PrepareMainMenu();
  HighlightActiveLink();

  AttachHandlers();

  PrepareTopLocationAndTellers();

  PrepareQTips();

  showElectionInfo();
}

function showElectionInfo() {
  // after qtips are ready
  $('.passcodeOkay').toggleClass('show', site.passcode !== '');
  $('.passcodeLocked').toggleClass('show', site.passcode === '');

  $('.passcode').on('click',
    function () {
      location.href = site.rootUrl + 'After/monitor';
    });
}

function updatePasscodeDisplay(okay, passcode) {
  //console.log(okay, passcode);
  if (typeof passcode === 'string') {
    site.passcode = passcode;
    $('.passcodeText').text(site.passcode);
  }
  if (okay === null) {
    okay = false;
  }
  $('.passcodeOkay').toggleClass('show', okay && !!site.passcode);
  $('.passcodeLocked').toggleClass('show', !okay || !site.passcode);
}

function HighlightActiveLink() {
  var url = location.href;
  var found = false; // only do first... the Finalized menu repeats some
  var breadCrumbDiv = $('.showBreadCrumb');
  breadCrumbDiv.hide();

  $('#quickLinkItems a').each(function () {
    var matched = url === this.href;
    var a = $(this);
    if (matched) {
      a.addClass('Active');

      if (!found && !a.is(':visible')) {
        var id = a.parent().attr('id').replace('menu', '');
        var parent = $('#electionState .' + id);
        breadCrumbDiv
          .html(`Viewing: <span class="${parent.attr('class')}">${parent.text()} / <span>${a.text()}</span></span>`)
          .show();
      }

      found = true;

    } else {
      a.removeClass('Active');
    }
  });
}

function clearElectionRelatedStorageItems() {
  for (var key in localStorage) {
    if (localStorage.hasOwnProperty(key)) {
      if (key.substr(0, 5) === 'name_') {
        localStorage.removeItem(key);
      } else if (key === 'ActiveUploadRowId') {
        localStorage.removeItem(key);
      }
    }
  }
  localStorage.removeItem('locationKnown');

};


function scrollIntoView(element, blockWhere) {
  if (!element) return;
  if (element.jquery) {
    if (!element.length) {
      return;
    } else {
      element = element[0];
    }
  }

  console.log('scroll', element, blockWhere);
  element.scrollIntoView({
    block: blockWhere || 'center'
  });
}

var connectToElectionHub = function () {
  var hub = $.connection.mainHubCore;

  hub.client.statusChanged = function (info) {
    console.log('signalR: electionStatusChanged');
    site.broadcast(site.broadcastCode.electionStatusChanged, info);
  };

  var closing = false;
  hub.client.electionClosed = function () {
    console.log('signalR: electionClosed');
    var msg = 'This election has been closed. Thank you for your participation!';
    //ShowStatusFailed(msg);
    if (closing) return;
    logoffSignalR();
    location.href = site.rootUrl + 'Account/Logoff';
    closing = true;
    alert(msg);
  };

  startSignalR(function () {
    console.log('Joining main hub');
    CallAjaxHandler(site.rootUrl + 'Public/JoinMainHub',
      { connId: site.signalrConnectionId, electionGuid: site.electionGuid });
  });
};

var startSignalR = function (callBack, showReconnectMsg) {
  if ($.connection.hub.id) {
    console.log('WARNING: already connected');
    callBack();
    return;
  }

  site.signalrDelayedCallbacks.push(callBack);

  clearTimeout(site.signalrDelay);

  site.signalrDelay = setTimeout(function () {
    $.connection.hub.error(function (error) {
      var msg = error.toString();
      //console.log('error', error);
      if (msg.indexOf('The client has been inactive since') !== -1) {
        ShowStatusFailed("We've been disconnected from the server for too long." +
          "<br>Please refresh this page (press F5) to reconnect and continue.");
      } else if (msg.indexOf('WebSocket closed')) {
        ShowStatusFailed("Disconnected from the server.");
      } else {
        ShowStatusFailed(msg);
      }
    });

    $.connection.hub
      .start()
      .done(function () {
        if (showReconnectMsg) {
          ShowStatusDisplay('Reconnected!', 0, 9000, false, true);
        }
        // console.log('signalR client connected', $.connection.hub.id);
        site.signalrConnectionId = $.connection.hub.id;
        for (var i = 0; i < site.signalrDelayedCallbacks.length; i++) {
          var fn = site.signalrDelayedCallbacks[i];
          if (fn) {
            fn();
          }
        }
      });

    site.signalrReconnecting = false;

    $.connection.hub.connectionSlow(function () {
      console.log('slow');
      ShowStatusDisplay('The connection to the server is slow... please wait...', null, null, true);
    });
    $.connection.hub.reconnecting(function () {
      console.log('reconnecting');
      ShowStatusDisplay('Attempting to reconnect to the server...', null, null, true);
      site.signalrReconnecting = true;
    });
    $.connection.hub.reconnected(function () {
      console.log('connected');
      ShowStatusDisplay('Reconnected!', 0, 9000, false, true);
      site.signalrReconnecting = false;
    });
    $.connection.hub.disconnected(function () {
      console.log('disconnected');
      if (site.signalrReconnecting) {
        ShowStatusFailed("We've been disconnected from the server for too long." +
          "<br>Please refresh this page (press F5) to reconnect and continue.");
        setTimeout(function() {
//          ResetStatusDisplay();
          console.log('starting signalR again');
          startSignalR(null, true);
        }, 1000);
      }
    });
  },
    0); // delay before calling server
};

function logoffSignalR() {
  console.log('closing signalr');
  $.connection.hub.stop();
}

function PrepareQTips(doNow) {
  if (!doNow) {
    setTimeout(function () {
      PrepareQTips(true);
    },
      500);
    return;
  }

  // global tips
  site.qTips.push({
    selector: '#qTipQuickLinks',
    title: 'Relevant Pages',
    text:
      'Shows the pages relevant to the current state of the election. All pages are still available by hovering over other State buttons.'
  });
  site.qTips.push({
    selector: '#qTipElectionStatus',
    title: 'Election State',
    text:
      'An election proceeds through various states. The head teller should actively change the state as appropriate.'
  });
  site.qTips.push({
    selector: '#qTipTeller',
    title: 'Tellers',
    text:
      'Please ensure that your name shows here when using this computer. If your name is not in the list, add it! This can help later when reviewing ballots.'
  });
  site.qTips.push({
    selector: '#qTipTopLocation',
    title: 'Location',
    text: 'Please ensure that this is your location!'
  });
  site.qTips.push({
    selector: '#qTipPasscode',
    title: 'Election Open for Tellers',
    text:
      'Tellers can join this election using the access code&nbsp; <b class=passcodeText></b> &nbsp;on the Home Page.',
    events: {
      render: function () {
        // only runs on first render
        $('.passcodeText').text(site.passcode);
      }
    }
  });
  site.qTips.push({
    selector: '#qTipPasscodeLocked',
    title: 'Election Closed',
    text: 'This election is not visible on the home page.'
  });

  if ($('body').hasClass('AuthKnown')) {
    site.qTips.push({
      selector: '#qTipFinalized',
      title: 'Finalized State',
      text:
        'Set the election to this state using the buttons on the Analyze page. When "Finalized", no further inputs or changes are permitted.'
    });
  } else {
    site.qTips.push({
      selector: '#qTipFinalized',
      title: 'Finalized State',
      text: 'Set by the head teller. When "Finalized", no further inputs or changes are permitted.'
    });
  }
  // add some tips for pages without dedicated js
  if ($('#qTipReg1').length) {
    site.qTips.push({
      selector: '#qTipReg1',
      title: 'Admin Login ID',
      text:
        'This is your administrator login ID, and should be relatively short.  You will use it when logging in each time you use TallyJ to create or administer elections.'
    });
    site.qTips.push({
      selector: '#qTipReg2',
      title: 'Email Address',
      text:
        'Please use a valid address where you can be notified when there is important news regarding TallyJ. It will not be given to anyone else or used for other purposes.'
    });
    site.qTips.push({
      selector: '#qTipReg3',
      title: 'Password',
      text:
        'Needs to be at least 6 characters long. It will be encrypted when stored, so cannot be viewed by anyone.'
    });
  }

  ActivateTips();
}

function ActivateTips(forceRecreate) {
  var baseOption = {
    position: {
      my: 'bottom left',
      at: 'top center',
      viewport: true
    },
    content: {
      text: '',
      title: {
        text: ''
      }
    },
    style: {
      classes: 'qtip-tallyj qtip-rounded qtip-shadow'
    }
  };

  $('.qTip').qtip(baseOption);

  $.each(site.qTips,
    function () {
      if (!(forceRecreate || false) && $(this).data('done')) return;

      var opt = $.extend(true, {}, baseOption, this);
      if (this.text) {
        opt.content.text = this.text;
        if (this.title) {
          opt.content.title.text = this.title;
          //$.extend(true, opt, { content: { text: this.text, title: { text: this.title } } });
          //        } else {
          //$.extend(true, opt, { content: { text: this.text } });
          //        }
        }
        $(this).data('done', true);
        $(this.selector).qtip(opt);
      }
    }
  );
}

function AttachHandlers() {
  site.onbroadcast(site.broadcastCode.electionStatusChanged, updateElectionStatus);

  var dropDownTimeout = null;
  var closeDropDown = function () {
    $('#quickLinkItems span.HighlightMenu').removeClass('HighlightMenu');
    $('#quickLinkItems span.DropDown').removeClass('DropDown');
    $('.QuickDash').fadeOut('fast');
  };
  $('body.AuthKnown #electionState span.Finalized').on('click',
    function () {
      $('#qTipElectionStatus').trigger('click');
    });

  //  $('body.AuthKnown #electionState span.state').not('.General, .Finalized').on('click',
  $('body.AuthKnown').on('click', '.SetThis',
    function () {
      var setThis = $(this);
      var form = {
        state: setThis.data('state')
      };

      if (form.state === site.electionState) {
        return;
      }

      ShowStatusDisplay('Saving...');
      CallAjaxHandler(site.rootUrl + 'Elections/UpdateElectionStatus',
        form,
        function (info) {
          if (info.Message) {
            ShowStatusFailed(info.Message);
          } else {
            ResetStatusDisplay();
          }
          HighlightActiveLink();
        });
    });

  $('#electionState')
    .on('mouseover', '#AllPages', function () {
      clearTimeout(dropDownTimeout);
      closeDropDown();
      showAllPages(this);
    })
    .on('mouseout', '#AllPages', function () {
      clearTimeout(dropDownTimeout);
      dropDownTimeout = setTimeout(closeDropDown, 200);
    });

  //  $('.SetThis').on('click', function (ev) {
  //    var item = $(ev.target);
  //    let parent = item.parent();
  //    console.log(parent)
  //    parent.click();
  //  });

  $('#electionState')
    .on('mouseover', 'span.state', function (ev) {
      clearTimeout(dropDownTimeout);
      var item = $(ev.target);
      var state = item.data('state');
      var menu = $('#menu' + state);
      closeDropDown();
      if (menu.is(':visible')) {
        menu.addClass('HighlightMenu');
        dropDownTimeout = setTimeout(closeDropDown, 700);
        return;
      }
      menu.addClass('DropDown')
        .css({
          left: getFullOffsetLeft(item) + 'px',
          top: (item.offset().top + item.height() - 2) + 'px'
        });
    })
    .on('mouseout', 'span.state', function (ev) {
      clearTimeout(dropDownTimeout);
      dropDownTimeout = setTimeout(closeDropDown, 200);
    });

  $('body').on('mouseover', '.DropDown,.QuickDash',
    function () {
      clearTimeout(dropDownTimeout);
    }).on('mouseout',
      '.DropDown,.QuickDash',
      function () {
        clearTimeout(dropDownTimeout);
        dropDownTimeout = setTimeout(closeDropDown, 200);
      });


  $('body').on('click',
    'a[href="/Account/Logoff"]',
    function () {
      logoffSignalR();
    });

  $('#statusDisplay').on('click', '.closeStatus', function () {
    ResetStatusDisplay();
  });
}

function getFullOffsetLeft(item) {
  if (item.hasClass('content-wrapper')) {
    return 0 - item.offset().left;
  }
  return item.offset().left + getFullOffsetLeft(item.offsetParent());
}

function updateElectionStatus(ev, info) {
  site.electionState = info.StateName;

  updatePasscodeDisplay(info.Listed, info.Passcode);

  let isClosed = !info.Online;
  $('body').toggleClass('OnlineOpen', !isClosed);
  $('body').toggleClass('OnlineClosed', isClosed);

  showMenu(info.StateName, true);
}

function showAllPages(btnRaw) {
  var btn = $(btnRaw);
  var quickDash = $('span.QuickDash');

  quickDash.css({
    //left: Math.max(btnOffset.left - quickDash.width(), 0),
    left: getFullOffsetLeft(btn) - 15, //- $('.TopInfo').offset().left - 5 + btn.width() - quickDash.width(),
    right: 'auto',
    top: btn.offset().top + btn.height() - 2
  }).show();

  $('.ElectionState .General').addClass('GeneralActive');
  setTimeout(function () {
    $(document).on('click', quickDashCloser);
  },
    0);
}

var quickDashCloser = function (ev) {
  console.log($(ev.srcElement).closest('.QuickDash'));
  if ($(ev.srcElement).closest('.QuickDash').length === 0) {
    $('.QuickDash').fadeOut('fast');
    $('.ElectionState .General').removeClass('GeneralActive');
    $(document).off('click', quickDashCloser);
  }
};

function showMenu(state, permanent, slow) {
  var target = $('#electionState');
  //  var temp = target.data('temp') || target.data('state');
  // console.log('changed from {0} to {1} ({2})'.filledWith(temp, state, site.electionState));
  //  if (state != temp) {
  $('#quickLinkItems span:visible').addClass('Hidden').removeClass('DropDown');
  $('#menu' + state).removeClass('Hidden').removeClass('DropDown');
  //  }

  //$('#QuickLinks2').toggleClass('temp', site.electionState != state);

  //  target.data('temp', state);

  var mainItem = target.find('span.state[data-state={0}]'.filledWith(state));

  $('#qmenuTitle').text(mainItem.text()).removeClass().addClass(state);
  $('#QuickLinks2').removeClass().addClass(state);

  site.menuShowingDefault = permanent;

  //  if (permanent) {
  site.electionState = state;
  target.data('state', state);
  target.find('span.state').removeClass('Active_True Active_Temp').addClass('Active_False');
  mainItem.removeClass('Active_False Active_Temp').addClass('Active_True');
  //  } else {
  //    target.find('li').removeClass('Active_Temp');
  //    if (state != site.electionState) {
  //      mainItem.addClass('Active_Temp');
  //    }
  //  }
}

function HoverQuickLink(ev, showNow) {
  //  var reentered = function () {
  //    clearTimeout(site.hoverQuickLinkRevertDelay);
  //    $('.TopInfo').on('mouseleave', mouseLeavingTopInfo);
  //  };
  //  var mouseLeavingTopInfo = function () {
  //    $('.TopInfo').off('mouseleave', mouseLeavingTopInfo);
  //    $('.TopInfo').on('mouseenter', reentered);
  //
  //    clearTimeout(site.hoverQuickLinkRevertDelay);
  //    site.hoverQuickLinkRevertDelay = setTimeout(function () {
  //      showMenu(site.electionState, true, true);
  //      $('.TopInfo').off('mouseenter', reentered);
  //    }, site.menuResetDelay);
  //  };
  //
  //  clearTimeout(site.hoverQuickLinkRevertDelay);

  //  if (!showNow) { // && site.menuShowingDefault) {
  //    clearTimeout(site.hoverQuickLinkShowDelay);
  //
  //    $('.TopInfo').on('mouseleave', function () {
  //      clearTimeout(site.hoverQuickLinkShowDelay);
  //    });
  //
  //    site.hoverQuickLinkShowDelay = setTimeout(function () {
  //      HoverQuickLink(ev, true);
  //    }, site.menuShowDelay);
  //    return;
  //  }

  //  var state = $(ev.currentTarget).data('state');

  //  showMenu(state, false);

  //  $('.TopInfo').on('mouseleave', mouseLeavingTopInfo);
}

function CheckTimeOffset() {
  if (site.timeOffsetKnown) return;
  var now = new Date();
  var form = {
    now: now.getTime() - now.getTimezoneOffset() * 60 * 1000
  };
  CallAjaxHandler(GetRootUrl() + 'Public/GetTimeOffset',
    form,
    function (info) {
      site.timeOffset = info.timeOffset;
      site.timeOffsetKnown = true;
    });
}

function topLocationChanged(ev) {
  ShowStatusDisplay('Saving...');
  var ddl = $(ev.currentTarget);
  var form = {
    id: ddl.val()
  };
  ddl.find('option[value="-1"]').remove();

  if (form.id === '-2') {
    // some pages add -2 for [All Locations] -- but we don't store it
    site.broadcast(site.broadcastCode.locationChanged);
    setTopInfo();
    return;
  }

  CallAjaxHandler(GetRootUrl() + 'Dashboard/ChooseLocation',
    form,
    function () {
      ShowStatusSuccess('Saved');
      site.broadcast(site.broadcastCode.locationChanged);
      setTopInfo();
    });
}

function tellerChanged(ev) {
  var ddl = $(ev.currentTarget);
  var choice = +ddl.val();
  var form = {
    num: ev.currentTarget.id.substr(-1),
    teller: choice
  };
  if (choice === -1) {
    var text = 'Please enter a short name for this teller:';
    do {
      form.newName = prompt(text);
      if (!form.newName) {
        ddl.val(ddl.data('current'));
        ResetStatusDisplay();
        return;
      }
      text = 'Please enter a short name (up to 25 letters) for this teller:';
    } while (form.newName.length > 25);
  }
  ShowStatusDisplay('Saving...');

  CallAjaxHandler(GetRootUrl() + 'Dashboard/ChooseTeller',
    form,
    function (info) {
      ShowStatusSuccess('Saved');

      ddl.data('current', ddl.val());

      if (info.TellerList) {
        ddl.html(info.TellerList);

        var otherDll = $('.TopTeller').not(ddl);
        var otherValue = otherDll.val();
        otherDll.html(info.TellerList);
        otherDll.val(otherValue);
      }

      setTopInfo();
    });
}

function PrepareTopLocationAndTellers() {
  $('#ddlTopLocation').change(topLocationChanged);

  $('.TopTeller').change(tellerChanged).each(function () {
    var ddl = $(this);
    ddl.data('current', ddl.val());
  });

  setTopInfo();
}

var setTopInfo = function () {
  var ddlLocation = $('#ddlTopLocation');
  var location = +ddlLocation.val();
  var locationNeeded = ddlLocation.is(':visible') && location === -1;

  var ddlTeller1 = $('#ddlTopTeller1');
  var teller1Needed = ddlTeller1.is(':visible') && ddlTeller1.val() <= 0;

  $('.CurrentInfo').toggleClass('NotSet', locationNeeded || teller1Needed);
  ddlLocation.toggleClass('NotSet', locationNeeded);
  ddlTeller1.toggleClass('NotSet', teller1Needed);

  $('#ddlTopTeller2').toggleClass('NotSet', +$('#ddlTopTeller2').val() <= 0);
}

function PrepareMainMenu() {
  $('.QuickLinks').supersubs().superfish();
}

function AttachHelp() {
  var pihList = $('.PullInstructionsHandle');

  pihList.each(function (i, el) {
    var pih = $(el);
    pih[0].accessKey = "I";
    var title = pih.text() || 'Instructions & Tips';
    //pih.html('<span class="ui-icon ui-icon-info IfClosed qTip" title="Click to show more instructions"></span><span class=IfOpen>Hide</span><span class=IfClosed>Show</span> <span>{0}</span>'.filledWith(title));
    pih.html('<span class=IfOpen>Hide</span> <span>{0}</span>'.filledWith(title));
  });

  var showHelp = function (handle, show, fast) {
    var next = handle.next();
    if (fast) {
      next.toggle(show);
    } else {
      if (show) {
        next.slideDown({
          easing: 'linear',
          duration: 'fast'
        });
      } else {
        next.slideUp({
          easing: 'linear',
          duration: 'fast'
        });
      }
      //next.slideToggle(show);
    }
    handle.toggleClass('Closed', !show);
    var key = 'HidePI_' + location.pathname + handle[0].id;
    if (show) {
      SetInStorage(key, null);
    } else {
      SetInStorage(key, 'hide');
    }
  };

  $(document).on('click',
    '.PullInstructionsHandle',
    function (ev) {
      var handle = $(ev.currentTarget);
      //console.log(handle, handle.data());
      showHelp(handle, !handle.next().is(':visible'), false);
    });

  $('.PullInstructionsHandle').each(function (i, el) {
    var handle = $(el);
    var instance = i + 1; // don't want 0
    el.id = 'pi' + instance;
    showHelp(handle, GetFromStorage('HidePI_' + location.pathname + el.id, 'show') !== 'hide', true);
  });

}

//var UpdateActiveInfo = function () {
//  var election = GetFromStorage(lsName.Election);
//  if (election) {
//    var electionDisplay = $('.CurrentElectionName');
//    electionDisplay.text(election.Name);
//    electionDisplay.effect('highlight', { mode: 'slow' });
//    site.heartbeatActive = true;
//    // SetInStorage(lsName.Election, election);
//    ActivateHeartbeat(true);
//  }
//};

//var lastVersionNum = function () {
//  return {
//    get: function (defaultValue) {
//      return GetFromStorage(lsName.LastVersionNum) || defaultValue;
//    },
//    set: function (value) {
//      SetInStorage(lsName.LastVersionNum, value);
//      return value;
//    }
//  };
//};

// Called after AJAX server calls

function HasErrors(data, jqXhr) {
  if (data && data.length) {

    if (data[0] === '{' && data[data.length-1] === '}') {
      // must be JSON text
      return false;
    }

    // PrepareNextKeepAlive(); 
    // --> would like to update KeepAlive after each AJAX call, but session is not extended on the server for AJAX calls!

    //  if (data.search(/login/i) !== -1) {
    //    var now = new Date();
    //    alert('{0}\n\nYou are no longer logged in.\n\nYou must login again to continue.\n\nThis happened at...  {1}'.filledWith(document.title, now.toLocaleTimeString()));
    //    top.location.href = GetRootUrl() + 'login';
    //    return true;
    //  }
    if (/\<h2\>Object moved to/.test(data)) {
      top.location.href = new RegExp('href\=\"(.*)"\>').exec(data)[1];
      return true;
    }

    if (/\<\!DOCTYPE html\>/.test(data)) {
      // seem to have a complete web page!
      console.log('Error - ajax call got full page', data);
      //    top.location.reload();
      return true;
    }

    if (/Internal Server Error/.test(data)) {
      ShowStatusFailed('Server Error.');
      return true;
    }
    if (/Anonymous access denied/.test(data)) {
      top.location.reload();
      return true;
    }
    if (/Server Error/.test(data)) {
      ShowStatusFailed(data.replace('/*', '').replace('*/', ''));
      return true;
    }
    if (/^Exception:/.test(data)) {
      ShowStatusFailed('Server Error: ' + data.substr(0, 60) + '...');
      console.log(data);
      return true;
    }
    if (/Error\:/.test(data)) {
      ShowStatusFailed(
        'An error occurred on the server. The Technical Support Team has been provided with the error details.');
      return true;
    }
  }

  var jsonHeader = jqXhr.getResponseHeader('x-responded-json');
  if (jsonHeader) {
    var result = JsonParse(jsonHeader);
    if (result.status >= 400) {
      if (result.headers.location) {
        ShowStatusFailed('Reloading...');
        top.location.href = result.headers.location;
        return true;
      }

      ShowStatusFailed('Unknown error');
      console.log(result);
      return true;
    }
  }

  return false;
}

function GetResource(resourceKey) {
  return MyResources[resourceKey];
}

function ActivateHeartbeat(makeActive, delaySeconds) {
  if (makeActive) {
    if (delaySeconds) {
      site.heartbeatSeconds = +delaySeconds;
    }
    clearTimeout(site.heartbeatTimeout);
    site.heartbeatTimeout = setTimeout(SendHeartbeat, 1000 * site.heartbeatSeconds);
  } else {
    clearTimeout(site.heartbeatTimeout);
  }
}

function SendHeartbeat() {
  if (!site.heartbeatActive) return;
  var form = {
    Status: $('#electionState').data('state'),
    Context: site.context,
    Stamp: site.lastVersionNum
  };
  CallAjaxHandler(GetRootUrl() + 'Public/Heartbeat', form, ProcessPulseResult);
}

function ProcessPulseResult(info) {
  ActivateHeartbeat(site.heartbeatActive);
  if (!info) {
    return;
  }
  //if (info === false) {
  //  // logged out
  //  top.location.href = GetRootUrl();
  //  return;
  //}
  if (info.NewStamp) {
    site.lastVersionNum = info.NewStamp;
  }

  site.computerActive = info.Active;
  if (info.Active) {
    $('.Heartbeat').removeClass('Frozen').text('').effect('highlight', 'slow');
  } else {
    $('.Heartbeat').addClass('Frozen').text('Not Connected');
  }

  //  if (info.NewStatus) {
  //    site.broadcast(site.broadcastCode.electionStatusChanged, info.NewStatus);
  //  }

  if (info.PulseSeconds) {
    site.heartbeatSeconds = info.PulseSeconds;
  }

  site.broadcast(site.broadcastCode.pulse, info);
}

// function ShowQaPanel(url) {
// setTimeout(function () {
// $('body').append('<iframe src="' + url + '" id=QaPanelFrame frameborder=0></iframe>');
// }, 1000);
// }


function CallAjaxHandler(handlerUrl,
  form,
  callbackWithInfo,
  optionalExtraObjectForCallbackFunction,
  callbackOnFailed,
  waitForResponse) {
  /// <summary>Do a POST to the named handler. If form is not needed, pass null. Query and Form are objects with named properties.</summary>
  var options = {
    type: 'POST',
    url: handlerUrl,
    traditional: true,
    success: function (data, textStatus, jqXhr) {
      if (HasErrors(data, jqXhr)) return;

      ResetStatusDisplay();

      if (typeof callbackWithInfo !== 'undefined') {
        callbackWithInfo(JsonParse(data), optionalExtraObjectForCallbackFunction);
      }
    },
    error: function (jqXhr, textStatus) {
      if (typeof callbackOnFailed !== 'undefined') {
        callbackOnFailed(jqXhr);
      } else {
        ShowStatusFailed(jqXhr);
      }
    }
  };

  if (form) {
    options.data = form;
    // options.contentType = "application/x-www-form-urlencoded";
  }
  if (waitForResponse) {
    options.async = false;
  }
  return $.ajax(options);
}


String.prototype.parseJsonDate = function () {
  if (this == '') return null;
  var num = /\((.+)\)/.exec(this)[1];
  return new Date(+num);

  //Date(1072940400000)/
  //Date(1654149600000)/
  //Date(165414960000)/
  //Date(-1566496800000)/
};

String.prototype.parseJsonDateForInput = function () {
  if (this == '') return '';
  var d = this.parseJsonDate();
  // counteract UTC time...
  d.setTime(d.getTime() + d.getTimezoneOffset() * 60 * 1000);

  var day = ("0" + d.getDate()).slice(-2);
  var month = ("0" + (d.getMonth() + 1)).slice(-2);
  var date = d.getFullYear() + "-" + (month) + "-" + (day);
  return date;
};

function JsonParse(json) {
  if (json == '') return null;
  if (typeof (JSON) != 'undefined' && JSON) {
    //if (!!window.chrome) json = json.replace('\\', '\\\\');
    try {
      return JSON.parse(json); // if not pure JSON, may get parse error
    } catch (e) {
      console.log(e);
      console.log(json);
      ShowStatusFailed(e.message);
    }
  }
  try {
    return eval('(' + json + ')');
  } catch (e2) {
    console.log(e2);
    console.log(json);
  }
  return null;
}


// Root Url ////////////////////////////////////////////////////////////////////////////

function GetRootUrl() {
  return site.rootUrl;
}

//  Status Display //////////////////////////////////////
var statusDisplay = {
  minDisplayTimeBeforeStatusReset: 0,
  resetTimer: null,
  delayedShowStatusArray: []
};

function PrepareStatusDisplay() {
  //if ($('body').hasClass('Public Index')) {
  //  var target = $('body').hasClass('Public Index') ? 'body' : '#body';

  $('body').prepend('<div class="StatusOuter"><div class="StatusMiddle"><div class="StatusInner">' +
    '<div id="statusDisplay" class="StatusActive" style="display: none;"></div>' +
    '</div></div></div>');
  //} else {
  //    $('#body').prepend('<div class="StatusOuter2 content-wrapper"><span id="statusDisplay2" class="StatusActive" style="display: none;"></span></div>');
  //}
}

function ShowStatusDisplay(msg, delayBeforeShowing, timeBeforeStatusReset, showErrorIcon, showNoIcon) {
  statusDisplay.minDisplayTimeBeforeStatusReset = timeBeforeStatusReset =
    (typeof timeBeforeStatusReset === 'number') ? timeBeforeStatusReset : 600 * 1000;
  if (statusDisplay.minDisplayTimeBeforeStatusReset) {
    clearTimeout(statusDisplay.resetTimer);
    statusDisplay.resetTimer = setTimeout(ResetStatusDisplay, statusDisplay.minDisplayTimeBeforeStatusReset);
    statusDisplay.minDisplayTimeBeforeStatusReset = 0;
  }

  if (typeof delayBeforeShowing !== 'number') {
    delayBeforeShowing = 0;
  }

  if (delayBeforeShowing > 0) {
    statusDisplay.delayedShowStatusArray[statusDisplay.delayedShowStatusArray.length] = setTimeout(function () {
      ShowStatusDisplay(msg, 0, timeBeforeStatusReset, showErrorIcon);
    },
      delayBeforeShowing);
    return;
  }
  var target = $('#statusDisplay2, #statusDisplay');
  if (target.length === 0) {
    // ??? on a page without a Status display
  }
  var loaderPath = '<img class=ajaxIcon src="' + GetRootUrl() + 'images/ajax-loader.gif"> ';
  var imageHtml = showErrorIcon ? '<span class="ui-icon ui-icon-alert"></span>' : showNoIcon ? '' : loaderPath;
  target.html(imageHtml + msg).show();
  if (showErrorIcon) {
    target.addClass('error');
    $('body').addClass('errorStatus');
  } else {
    target.removeClass('error');
    $('body').removeClass('errorStatus');
  }
  // idea: hold errors until click ok?    <button onclick="ClearDisplay()" type=button>Ok</button>
}

function ShowStatusSuccess(msg) {
  ShowStatusDisplay(msg, 0, 3000, false, true);
}

function ShowStatusFailed(msg, keepTime) {
  ResetStatusDisplay();
  var delayBeforeShow = 0;
  var msgShown = false;

  if (typeof keepTime == 'undefined') keepTime = 600 * 1000; // 10 minutes

  var text;
  if (typeof msg === 'string') {
    text = msg;
  } else if (typeof msg.statusText === 'string') {
    if (msg.status === 200 || msg.status === 406) {
      text = msg.responseText;
    } else if (msg.status === 0 && msg.statusText === 'error') {
      text = 'Please wait...';
      ShowStatusDisplay(text, 0, keepTime, false, false);
      msgShown = true;
    } else if (msg.status === 503) {
      top.location.href = top.location.href;
      return '';
    } else {
      text = '(' + msg.status + ') ' + msg.statusText + ': ';
      if (msg.responseText) {
        var matches = msg.responseText.match(/\<title\>(.*?)\<\/title\>/i);
        if (matches !== null) {
          text = text + matches[1];
        } else {
          text = text + msg.responseText;
        }
      }
    }
  } else {
    text = 'Error';
  }

  if (!msgShown) {
    ResetStatusDisplay();
    ShowStatusDisplay(text + '<span class=closeStatus>x</span>', delayBeforeShow, keepTime, true);
  }

  return text;
}

function ResetStatusDisplay() {
  clearTimeout(statusDisplay.resetTimer);
  $('body').removeClass('errorStatus');

  for (; statusDisplay.delayedShowStatusArray.length;) {
    clearTimeout(statusDisplay.delayedShowStatusArray[statusDisplay.delayedShowStatusArray.length - 1]);
    statusDisplay.delayedShowStatusArray.length--;
  }

  if (statusDisplay.minDisplayTimeBeforeStatusReset !== 0) {
    statusDisplay.resetTimer = setTimeout(ResetStatusDisplay, statusDisplay.minDisplayTimeBeforeStatusReset);
    statusDisplay.minDisplayTimeBeforeStatusReset = 0;
    return;
  }

  HideStatusDisplay();
}

function HideStatusDisplay() {
  $('#statusDisplay2, #statusDisplay').hide();
}


function alerts(arg1, arg2, arg3, etc) {
  // alert('{0}\n'.filledWithEach(arguments));
  // show the contents of a list of parameters
  var msgList = [];
  for (var i = 0; i < arguments.length; i++) {
    var arg = arguments[i];
    try {
      var msg = arg === null ? 'null' : typeof arg === 'undefined' ? 'undefined' : arguments[i].toString();
      msgList[msgList.length] = (i + 1) + ': ' + msg;
    } catch (e) {
      msgList[msgList.length] = (i + 1) + ': ' + e.name + ' - ' + e.message;
    }
  }
  alert(msgList.join('\n'));
}


function comma(number, iDecimals, type, zeroText) { // works on positive numbers under 100 trillion
  // modified from version at irt.org - not very efficient!?
  if (number === 0 && typeof (zeroText) != 'undefined' && zeroText !== null && zeroText !== '') {
    return zeroText;
  }
  var bNegative = (number < 0); //work with the positive number and add -'ve at end if needed
  number = Math.abs(number);
  number = number - 0; // convert to number
  if (isNaN(number)) return 'Num?';
  var bFrench = site.languageCode === 'FR'; //if idecimals is -1 then only return decimals if there are some
  if (iDecimals === -1) {
    if (Math.floor(number) === number)
      iDecimals = 0;
    else
      iDecimals = 2;
  }
  // round to correct decimals
  if (iDecimals == null) iDecimals = 0;
  if (iDecimals >= 0) {
    number = number * Math.pow(10, iDecimals);
    number = Math.round(number);
    number = number / Math.pow(10, iDecimals);
  }

  // chop result in parts
  var whole = Math.floor(number);
  var decimal = number - whole;
  whole = '' + whole; // convert to text
  var output, i;
  if (whole.length > 3) {
    var mod = whole.length % 3; // leftover after groups of 3 removed
    var sections = Math.floor(whole.length / 3);
    output = (mod > 0 ? (whole.substring(0, mod)) : '');
    for (i = 0; i < sections; i++) {
      if ((mod === 0) && (i === 0))
        output = output + '' + whole.substring(mod + 3 * i, mod + 3 * i + 3);
      else
        output = output + (bFrench ? ' ' : ',') + whole.substring(mod + 3 * i, mod + 3 * i + 3);
    }
  } else
    output = whole;
  var sDecimalChar = (bFrench ? ',' : '.');
  if (decimal !== '' && iDecimals !== 0) {
    output += sDecimalChar +
      (Math.round(decimal * Math.pow(10, iDecimals)) / Math.pow(10, iDecimals)).toString().substr(2);
  }

  //make sure that the specified number of decimals is returned
  if (iDecimals !== 0) {
    var nPosition = output.indexOf(sDecimalChar);
    var nLength = output.length;
    if (nPosition === -1) //no decimal point
    {
      nPosition = output.length - 1;
      output += sDecimalChar;
    }
    var nRequired = Math.abs(nLength - nPosition - 1 - iDecimals);
    for (i = 0; i < nRequired; i++) {
      output += 0;
    }
  }

  if (bNegative)
    output = '-' + output;
  if (type === "D") {
    if (bFrench)
      output = output + ' $';
    else
      output = '$' + output;
  }

  if (type === "P") {
    if (bFrench)
      output = output + ' %';
    else
      output = output + '%';
  }

  return output;

}

function BrowserRemovesAttrQuotes() {
  //http://stackoverflow.com/questions/1231770/innerhtml-removes-attribute-quotes-in-internet-explorer
  //see this website for possible solution to ie removal of quotes
  return site.browserModelDetail === 'IE8';
}

function getTemplate(selector, replacements) {
  /// <summary>Return the numeric value of a css measure (without 'px')
  /// </summary>
  /// <param name="selector">JQuery selector to get one DOM object</param>
  /// <param name="replacements">(Optional) An object with named properties. The HTML will be searched for each "name" and replaced with its "value".</param>
  var target = $(selector);
  var rawHtml = target.html();
  if (!rawHtml) return '';

  var html = BrowserRemovesAttrQuotes() ? ieInnerHTML(target) : rawHtml;

  var internalReplacements = {
    // Firefox encodes these
    '%7B': '{',
    '%7D': '}'
  };

  $.extend(internalReplacements, replacements);

  for (var replacement in internalReplacements) {
    var regex = new RegExp(replacement, 'g');
    html = html.replace(regex, internalReplacements[replacement]);
  }

  return html;
}


function ieInnerHTML(obj, convertToLowerCase) {
  var zz = obj.html(), z = zz.match(/<\/?\w+((\s+\w+(\s*=\s*(?:".*?"|'.*?'|[^'">\s]+))?)+\s*|\s*)\/?>/g);

  if (z) {
    for (var i = 0; i < z.length; i++) {
      var zSaved = z[i], attrReg = /\=[a-zA-Z\.\:\[\]_\(\)\{\}\&\$\%#\@\!0-9]+[?\s+|?>]/g;
      z[i] = z[i]
        .replace(/(<?\w+)|(<\/?\w+)\s/, function (a) { return a.toLowerCase(); });
      var y = z[i].match(attrReg); //deze match

      if (y) {
        var j = 0, len = y.length;
        while (j < len) {
          var replaceReg = /(\=)([a-zA-Z\.\:\[\]_\(\{\}\)\&\$\%#\@\!0-9]+)?([\s+|?>])/g,
            replacer = function () {
              var args = Array.prototype.slice.call(arguments);
              return '="' + (convertToLowerCase ? args[2].toLowerCase() : args[2]) + '"' + args[3];
            };
          z[i] = z[i].replace(y[j], y[j].replace(replaceReg, replacer));
          j++;
        }
      }
      zz = zz.replace(zSaved, z[i]);
    }
  }
  return zz;
}

function CountOf(s, re) { // match regular expression to string. If find more than X of re, return false
  var match = s.match(re);
  if (match) {
    return match.length;
  } else
    return 0;
}

function GetValue(sNum) {

  if (site.languageCode === 'FR') {
    sNum = sNum.replace(/\./g, '');
    sNum = sNum.replace(/,/g, '.');
  } else {
    sNum = sNum.replace(/\,/g, '');
  }
  // ensure no more than one .
  if (CountOf(sNum, /\./g) > 1) return NaN;

  sNum = sNum.replace(/\$/g, '');

  sNum = sNum.replace(/\%/g, '');

  sNum = sNum.replace(/[ \xA0]/g, '');

  var nNum = Number(sNum);
  return nNum;
}

function FormatDate(dateObj, format, forDisplayOnly, includeHrMin, doNotAdjustForServerTimeOffset) {
  // MMM = JAN
  // MMMM = JANUARY
  if (('' + dateObj).substring(0, 5) === '/Date') {
    dateObj = dateObj.parseJsonDate();
  }
  if (dateObj == null || isNaN(dateObj) || dateObj === 'NaN' || dateObj === '01/01/0001' || dateObj === '') {
    return '';
  }

  var date = new Date(dateObj);
  if (isNaN(date)) {
    return '[Invalid Date]';
  }
  if (!doNotAdjustForServerTimeOffset) {
    console.log('time offset {0}'.filledWith(site.timeOffset));
    console.log('  - original: {0}'.filledWith(date.toString()));
    date = new Date(date.getTime() + site.timeOffset);
    console.log('  - after   : {0}'.filledWith(date.toString()));
  }

  if (!format) {
    format = 'YYYY-MM-DD';
  }

  var months = 'January February March April May June July August September October November December'.split(' ');
  var days = 'Sun Mon Tue Wed Thu Fri Sat'.split(' ');

  var dayValue = date.getDate();
  var monthValue = date.getMonth();
  var yearValue = date.getFullYear();
  var hourValue = date.getHours();
  var minuteValue = date.getMinutes();
  var result = '';

  switch (format) {
    case 'MMM D, YYYY':
      result = months[monthValue].substring(0, 3) + ' ' + dayValue + ', ' + yearValue;
      break;

    case 'D MMM YYYY':
      var returnVal = dayValue + ' ' + months[monthValue].substring(0, 3) + ' ' + yearValue;
      if (site.languageCode === 'FR' && forDisplayOnly && +dayValue === 1) {
        returnVal = returnVal.replace('1', '1<sup>{0}</sup>'.filledWith('er'));
      }
      result = returnVal;
      break;

    case 'DDD, D MMM YYYY':
      result = days[date.getDay()] + ', ' + dayValue + ' ' + months[monthValue].substring(0, 3) + ' ' + yearValue;
      break;

    case 'YYYY-MM-DD':
      var monthNum = monthValue + 1;
      result = yearValue + '-' + (monthNum < 10 ? '0' : '') + monthNum + '-' + (dayValue < 10 ? '0' : '') + dayValue;
      break;

    case 'MMM YYYY':
      result = months[monthValue].substring(0, 3) + ' ' + yearValue;
      break;

    case 'MMMM YYYY':
      result = months[monthValue] + ' ' + yearValue;
      break;

    case 'MM/DD/YYYY':
      var monthVal = monthValue + 1;
      var monthStr = ('0' + monthVal).slice(-2);
      var dayStr = ('0' + dayValue).slice(-2);
      result = monthStr + '/' + dayStr + '/' + yearValue;
      break;

    default:
      result = '';
      break;
  }

  if (includeHrMin) {
    result += ' {0}:{1}'.filledWith(hourValue.as2digitString(), minuteValue.as2digitString());
  }

  return result;
}

Number.prototype.as2digitString = function () {
  return ('00' + this).substr(-2);
};
String.prototype.filledWith = function () {
  /// <summary>Similar to C# String.Format...  in two modes:
  /// 1) Replaces {0},{1},{2}... in the string with values from the list of arguments. 
  /// 2) If the first and only parameter is an object, replaces {xyz}... (only names allowed) in the string with the properties of that object. 
  /// </summary>
  var values = typeof arguments[0] === 'object' && arguments.length === 1 ? arguments[0] : arguments;
  if (arguments.length === 0 && arguments[0].length) {
    // use values in array, substituting {0}, {1}, etc.
    values = {};
    $.each(arguments[0],
      function (i, value) {
        values[i] = value;
      });
  }

  var testForFunc = /^#/; // simple test for "#"
  var testForNoEscape = /^\^/; // simple test for "^"
  var extractTokens = /{([^{]+?)}/g; // greedy

  var replaceTokens = function (input) {
    return input.replace(extractTokens,
      function () {
        var token = arguments[1];
        var value = undefined;
        try {
          if (values === null) {
            value = '';
          } else if (testForFunc.test(token)) {
            value = eval(token.substring(1));
          } else if (testForNoEscape.test(token)) {
            value = values[token.substring(1)];
          } else {
            var toEscape = values[token];
            value = typeof toEscape == 'undefined' || toEscape === null
              ? ''
              : ('' + toEscape).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;').replace(/{/g, '&#123;');
          }
        } catch (err) {
          console.log('filledWithError:\n' +
            err +
            '\ntoken:' +
            token +
            '\nvalue:' +
            value +
            '\ntemplate:' +
            input);
          console.log(values);
          throw 'Error in filledWith';
        }
        return (typeof value == 'undefined' || value == null ? '' : ('' + value));
      });
  };

  var result = replaceTokens(this);

  var lastResult = '';
  while (lastResult !== result) {
    lastResult = result;
    result = replaceTokens(result);
  }

  return result.replace(/&#123;/g, '{');
};

String.prototype.filledWithEach = function (arr, sep) {
  /// <summary>Similar to 'filledWith', but repeats the fill for each item in the array. Returns a single string with the results.
  /// </summary>
  if (arr === undefined || arr === null) return '';
  if (sep === undefined) sep = '';
  var result = [];
  for (var i = 0, max = arr.length; i < max; i++) {
    result[result.length] = this.filledWith(arr[i]);
  }
  return result.join(sep);
};

// Turn { a:1, b:2 } into   a=1&b=2

//function JoinProperties(obj, sep1, sep2, sepInner1, sepInner2) {
//    var toJoin = [];
//    sep1 = sep1 || '=';
//    sep2 = sep2 || '&';
//    sepInner1 = sepInner1 || ('\\' + sep1);
//    sepInner2 = sepInner2 || ('\\' + sep2);
//    for (var i in obj) {
//        var prop = obj[i];
//        if (typeof prop !== 'function' && obj.hasOwnProperty(i)) {
//            if (prop === null) {
//                toJoin.push(i + sep1);
//            } else if (typeof prop === 'object' && prop.jquery) {
//                toJoin.push(i + sep1 + encodeURIComponent(prop.val()));
//            } else if ($.isArray(prop)) {
//                toJoin.push(i + sep1 + encodeURIComponent(prop.join()));
//            } else if (typeof prop === 'object') {
//                toJoin.push(i + sep1 + JoinProperties(prop.jquery ? encodeURIComponent(prop.val()) : prop, sepInner1, sepInner2));
//            } else {
//                toJoin.push(i + sep1 + encodeURIComponent(prop));
//            }
//        }
//    }
//    return toJoin.join(sep2);
//}


function StringifyObject(obj) {
  return JSON.stringify(obj);

  //  var toJoin = [];

  //  for (var i in obj) {
  //    var prop = obj[i];
  //    if (typeof prop !== 'function' && obj.hasOwnProperty(i)) {
  //      if (typeof prop === 'object') {
  //        if (prop === null) {
  //          toJoin.push('"' + i + '":null');
  //        } else {
  //          toJoin.push('"' + i + '":' + StringifyObject(prop));
  //        } 
  //      } else {
  //        toJoin.push('"' + i + '":' + JSON.stringify(prop));
  //      }

  //    }
  //  }
  //  return ('{' + toJoin.join() + '}');
}

function Plural(num, plural, single, zero) {
  if (num === 1) return single || '';
  if (num === 0) return zero || plural || 's';
  return plural || 's';
}

//following for performance testing

function startTimer() {
  site.stTime = new Date().getTime();
}

function endTimer(msg) {
  if (typeof site.stTime != 'undefined' && site.stTime) {
    console.log(msg + " " + (new Date().getTime() - site.stTime));
  }

}

function OptionsFromResourceList(resourceList, defaultValue) {
  var items = [];
  $.each(resourceList,
    function (key, text) {
      items.push({ Key: key, Text: text, Selected: defaultValue === key ? ' selected' : '' });
    });

  return '<option value="{Key}"{Selected}>{Text}</option>'
    .filledWithEach(items);
}

//  Storage  //////////////////////////////////////////////////
var ObjectConstant = '$@$';

function GetFromStorage(key, defaultValue) {
  var checkForObject = function (obj) {

    if (obj.substring(0, ObjectConstant.length) === ObjectConstant) {
      obj = JSON.parse(obj.substring(ObjectConstant.length));
    }
    return obj;
  };

  var value = localStorage[key];
  if (typeof value !== 'undefined' && value !== null) return checkForObject(value);
  return defaultValue;
}

function SetInStorage(key, value) {
  if (value === null) {
    localStorage.removeItem(key);
    return null;
  }
  if (typeof value === 'object' || typeof value === 'boolean') {
    var strObj = StringifyObject(value);
    value = ObjectConstant + strObj;
  }
  localStorage[key] = value;
  return value;
}
//
//var adjustElection = function (election) {
//  return election;
//  //  election.DateOfElection = FormatDate(
//  //    !isNaN(election) ? election : election.DateOfElection ? election.DateOfElection.parseJsonDate() : new Date());
//  //  return election;
//};

function ExpandName(s) {
  var result = [];
  for (var i = 0, len = s.length; i < len; i++) {
    var ch = s[i];
    if (ch >= 'A' && ch <= 'Z') {
      result.push(' ');
    }
    result.push(ch);
  }
  return result.join('');
}


// https://tc39.github.io/ecma262/#sec-array.prototype.findIndex
if (!Array.prototype.findIndex) {
  Object.defineProperty(Array.prototype,
    'findIndex',
    {
      value: function (predicate) {
        // 1. Let O be ? ToObject(this value).
        if (this == null) {
          throw new TypeError('"this" is null or not defined');
        }

        var o = Object(this);

        // 2. Let len be ? ToLength(? Get(O, "length")).
        var len = o.length >>> 0;

        // 3. If IsCallable(predicate) is false, throw a TypeError exception.
        if (typeof predicate !== 'function') {
          throw new TypeError('predicate must be a function');
        }

        // 4. If thisArg was supplied, let T be thisArg; else let T be undefined.
        var thisArg = arguments[1];

        // 5. Let k be 0.
        var k = 0;

        // 6. Repeat, while k < len
        while (k < len) {
          // a. Let Pk be ! ToString(k).
          // b. Let kValue be ? Get(O, Pk).
          // c. Let testResult be ToBoolean(? Call(predicate, T, « kValue, k, O »)).
          // d. If testResult is true, return k.
          var kValue = o[k];
          if (predicate.call(thisArg, kValue, k, o)) {
            return k;
          }
          // e. Increase k by 1.
          k++;
        }

        // 7. Return -1.
        return -1;
      }
    });
}