var FrontDeskPage = function () {
  var local = {
    currentSearch: '',
    currentTop: 0,
    lastSearch: 0,
    timer: null,
    frontDeskHub: null,
    hubReconnectionTime: 95000,
    matches: [],
    focusedOnMatches: false,
    headerSpace: 0
  };
  var preparePage = function () {
    $('#Main')
      .on('click', '.Btn', function (ev) {
        voteBtnClicked(ev.target);
      })
      .on('click', '.Voter', function (ev) {
        setSelection($(ev.target).closest('.Voter'), false);
      });

    $(document).keydown(processKey);

    connectToFrontDeskHub();

    local.headerSpace = $('header').outerHeight();

    $('body').animate({ scrollTop: 0 }, 0);

  };

  var connectToFrontDeskHub = function () {
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
      case 27: // esc
        if (inSelectionMode()) {
          $('#Main').removeClass('InSelection');
        }
        else {
          resetSearch();
        }
        ev.preventDefault();
        return;

      default:
        letter = String.fromCharCode(key);
        break;
    }

    var doSearch = false;

    if (inSelectionMode()) {
      $('.Voter.Selection div.Btn:visible').each(function (i, el) {
        if (letter == el.innerText.substr(0, 1)) {
          voteBtnClicked(el);
        }
      });
    }
    else if (/[\w\'\-]/.test(letter)) {
      if (!local.focusedOnMatches) {
        local.currentSearch = local.currentSearch + letter.toLowerCase();
        doSearch = true;
        clearTimeout(local.timer);
      } else {
        handleKeyWhileFocused(ev);
      }
    }
    //    LogMessage('main ' + key);
    switch (key) {
      case 13: // enter
        if (inSelectionMode()) {
          $('#Main').removeClass('InSelection');
        } else {
          activateSelection();
        }
        ev.preventDefault();
        break;

      case 38: // up
        moveSelector(-1);
        ev.preventDefault();
        break;
      case 40: // down
        moveSelector(1);
        ev.preventDefault();
        break;

      case 8: //backspace
        if (!inSelectionMode()) {
          local.currentSearch = local.currentSearch.substr(0, local.currentSearch.length - 1);
          doSearch = true;
        }
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

  var inSelectionMode = function () {
    return $('#Main').hasClass('InSelection');
  }

  var activateSelection = function () {
    var current = $('.Voter.Selection');
    if (!current.length) {
      return;
    }
    LogMessage(current.offset().top);
    LogMessage(local.headerSpace)
    $('#selectorTip').css('top', current.offset().top - local.headerSpace - 40);
    $('#Main').addClass('InSelection');
  }
  var moveSelector = function (delta) {
    if (inSelectionMode()) {
      return;
    }
    var current = $('.Voter.Selection');
    if (current.length) {
      var moveTo = [];
      switch (delta) {
        case -1:
          moveTo = current.prev('.Voter');
          break;
        case 1:
          moveTo = current.next('.Voter');
          break;
      }
      if (moveTo.length) {
        setSelection(moveTo, true);
      }
    }
  }
  var setSelection = function (el, move) {
    $('.Voter.Selection').removeClass('Selection');
    if (move) {
      scrollToMe(el, function () {
        el.addClass('Selection');
      });
    } else {
      el.addClass('Selection');
    }
  }

  var scrollToMe = function (el, after) {

    var top = el.offset().top;
    var gap = 40;
    var time = 100;

    $('body').scrollTop(top - gap - local.headerSpace);
    after();
    //return;


    //$('body').animate({
    //  scrollTop: top + gap - local.headerSpace
    //}, {
    //  duration: time,
    //  queue: false,
    //  easing: 'easeInOutCubic',
    //  complete: after
    //});
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
    $('#search').fadeIn().html('Last name: <span>' + local.currentSearch + '</span> (Esc to clear)');
    local.matches = $('.Voter[data-name^="{0}"]'.filledWith(local.currentSearch.toLowerCase()));
    focusOnMatches();
  };
  var focusOnMatches = function () {
    if (!local.matches.length) {
      local.focusedOnMatches = false;
      return;
    }
    var desired = local.matches.offset().top - 100;

    local.currentTop = desired;

    $('.Voter').removeClass('KeyMatch Focused Selection');
    local.matches.addClass('KeyMatch'); //$(this).switchClass('KeyMatch', 'AfterMatch', 5000, 'linear');
    var num = local.matches.length;
    if (local.focusedOnMatches) {
      local.matches.addClass('Focused');
    }
    setSelection(local.matches.eq(Math.floor((num - 1) / 2)).first(), true);
  };
  var voteBtnClicked = function (target, overrideConfirm) {
    var btn = $(target);
    
    if (!overrideConfirm && btn.hasClass('True')) {
      // already on
      if (!confirm('Are you sure you want to de-select this person?')) {
        return;
      }
      //$("#dialog-confirm").dialog({
      //  resizable: false,
      //  modal: true,
      //  buttons: {
      //    "Yes": function () {
      //      voteBtnClicked(target, true);
      //      $(this).dialog("close");
      //    },
      //    Cancel: function () {
      //      $(this).dialog("close");
      //    }
      //  }
      //});
      //return;
    }

    btn.addClass('clicked');

    var row = btn.closest('.Voter');
    setSelection(row, false);

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
    var current = $('.Voter.Selection').attr('id');
    if (info) {
      if (info.PersonLines) {
        $.each(info.PersonLines, function () {
          var selector = '#P' + this.PersonId;
          $(selector).replaceWith(site.templates.FrontDeskLine.filledWith(this));
          if (this.PersonId != pid) {
            $(selector).effect('highlight', {}, 5000);
          }
          $('.KeyMatch').removeClass('KeyMatch');
          if (selector === '#' + current) {
            setSelection($(selector), false);
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