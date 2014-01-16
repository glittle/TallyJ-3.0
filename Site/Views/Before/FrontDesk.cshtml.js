var FrontDeskPage = function () {
  var local = {
    currentSearch: '',
    currentTop: 0,
    lastSearch: 0,
    timer: null,
    frontDeskHub: null,
    hubReconnectionTime: 95000,
    matches: [],
    focusedOnMatches: false
  };
  var preparePage = function () {
    $('#Main').on('click', '.Btn', voteBtnClicked);

    $(document).keydown(processKey);

    connectToFrontDeskHub();

    $('html, body').animate({ scrollTop: 0 }, 0);

  };

  var connectToFrontDeskHub = function() {
    var hub = $.connection.frontDeskHubCore;
    hub.client.updatePeople = function (info) {
      LogMessage('signalR: updatePeople');
      updatePeople(info);
    };

    activateHub(hub, function () {
      LogMessage('Join frontDesk Hub');
      CallAjaxHandler(publicInterface.controllerUrl + '/JoinFrontDeskHub', { connId: site.signalrConnectionId });
    });

  };

//  var refreshHubConnection = function () {
//    var resetHubConnectionTimer = function () {
//      clearTimeout(local.reconnectHubTimeout);
//      local.reconnectHubTimeout = setTimeout(refreshHubConnection, local.hubReconnectionTime);
//    };
//
//    LogMessage('Join frontDeskHub');
//
//    clearTimeout(local.reconnectHubTimeout);
//    CallAjaxHandler(publicInterface.controllerUrl + '/JoinFrontDeskHub', { connId: site.signalrConnectionId }, function (info) {
//      resetHubConnectionTimer();
//    });
//
//  };

  var processKey = function (ev) {
    var letter, key = ev.which;
    if (ev.altKey) return;
    if (ev.ctrlKey) return;
    switch (key) {
      case 222:
        letter = "'";
        break;
      case 116: // F5
        return;
      default:
        letter = String.fromCharCode(key);
        break;
    }
    var doSearch = false;

    if (/[\w\'\-]/.test(letter)) {
      if (!local.focusedOnMatches) {
        local.currentSearch = local.currentSearch + letter.toLowerCase();
        doSearch = true;
        clearTimeout(local.timer);
      } else {
        handleKeyWhileFocused(ev);
      }
    }
    switch (key) {
      case 27: // esc
        resetSearch();
        ev.preventDefault();
        break;

      case 38: // up
        local.focusedOnMatches = true;
        handleKeyWhileFocused(ev);
        break;
      case 40: // down
        local.focusedOnMatches = true;
        handleKeyWhileFocused(ev);
        break;

      case 8: //backspace
        local.currentSearch = local.currentSearch.substr(0, local.currentSearch.length - 1);
        doSearch = true;
        ev.preventDefault();
        break;

      default:
        //LogMessage(key);
        break;
    }
    if (doSearch) {
      clearTimeout(local.timer);
      applyFilter();
      local.timer = setTimeout(resetSearch, 3000);
    }
  };
  var resetSearch = function () {
    clearTimeout(local.timer);
    local.matches.length = 0;
    local.focusedOnMatches = false;
    local.currentSearch = '';
    local.currentTop = 0;
    $('#search').fadeOut();
  };
  var handleKeyWhileFocused = function (ev) {
    if (!local.focusedOnMatches || local.matches.length == 0) return;

    ev.preventDefault();

    if (local.matches.length > 1) {
      local.matches.length = 1;
      focusOnMatches();
    }

    var key = ev.which;
    var currentId = local.matches[0].id;
    var current = $('#' + currentId);
    var moveNext;
    if (key == 40) {
      moveNext = current.next().attr('id');
    } else if (key == 38) {
      moveNext = current.prev().attr('id');
    }
    if (moveNext) {
      local.matches = $('#' + moveNext);
      focusOnMatches();
    } else {
      var btnCode;
      switch (String.fromCharCode(key)) {
        case 'I':
        case 'P':
          btnCode = 'P';
          break;
        case 'C':
          btnCode = 'C';
          break;
        case 'M':
          btnCode = 'M';
          break;
        case 'D':
          btnCode = 'D';
          break;
        default:
      }
      if (btnCode) {
        id = currentId.substr(1);
        saveBtnClick(id, btnCode);
      }
    }
  };
  var applyFilter = function () {
    $('#search').fadeIn().text(local.currentSearch);
    local.matches = $('.Voter[data-name^="{0}"]'.filledWith(local.currentSearch.toLowerCase()));
    focusOnMatches();
  };
  var focusOnMatches = function () {
    if (!local.matches.length) {
      local.focusedOnMatches = false;
      return;
    }
    var desired = local.matches.offset().top - 100;

    $('html, body').animate({ scrollTop: desired }, 150);

    local.currentTop = desired;

    $('.Voter').removeClass('KeyMatch Focused');
    local.matches.addClass('KeyMatch'); //$(this).switchClass('KeyMatch', 'AfterMatch', 5000, 'linear');
    if (local.focusedOnMatches) {
      local.matches.addClass('Focused');
    }
  };
  var voteBtnClicked = function (ev) {
    var btn = $(ev.target);
    var row = btn.parent();

    var btnType = btn.hasClass('InPerson') ? 'P'
        : btn.hasClass('DroppedOff') ? 'D'
        : btn.hasClass('CalledIn') ? 'C' : 'M';
    var pid = row.attr('id').substr(1);

    saveBtnClick(pid, btnType);
  };

  var saveBtnClick = function (pid, btnType) {
    var form = {
      id: pid,
      type: btnType,
      last: publicInterface.lastRowVersion || 0
    };

    ShowStatusDisplay("Saving...");
    CallAjaxHandler(publicInterface.controllerUrl + '/VotingMethod', form, function (info) {
      if (info.Message) {
        ShowStatusFailed(info.Message);
      }
    });
  };

  var updatePeople = function (info, pid) {
    ResetStatusDisplay();
    if (info) {
      if (info.PersonLines) {
        $.each(info.PersonLines, function () {
          var selector = '#P' + this.PersonId;
          $(selector).replaceWith(site.templates.FrontDeskLine.filledWith(this));
          if (this.PersonId != pid) {
            $(selector).effect('highlight', {}, 5000);
          }
        });
      }
      if (info.LastRowVersion) {
        publicInterface.lastRowVersion = info.LastRowVersion;
      }
    }
  };

  var publicInterface = {
    controllerUrl: '',
    lastRowVersion: 0,
    electionGuid: null,
    PreparePage: preparePage,
    local: local
  };
  return publicInterface;
};

var frontDeskPage = FrontDeskPage();

$(function () {
  frontDeskPage.PreparePage();
});