﻿var FrontDeskPage = function () {
  var local = {
    currentSearch: '',
    currentTop: 0,
    lastSearch: 0,
    scrollTimer: null,
    frontDeskHub: null,
    hubReconnectionTime: 95000,
    matches: [],
    lastStatusFilter: '',
    focusedOnMatches: false,
    headerSpace: 0,
    lastLetter: '',
    lastLetterIndex: -1,
    pageSize: 100,
    afterList: null,
    lineTemplate: null
  };
  var preparePage = function () {
    local.lineTemplate = document.getElementById('frontDeskLineTemplate').innerText;

    connectToFrontDeskHub();
    startGettingPeople();

    $('#Main')
      .on('click', '.Btn:not(.Hasfalse)', function (ev) {
        voteBtnClicked(ev.target);
      })
      .on('click', '.Voter', function (ev) {
        setSelection($(ev.target).closest('.Voter'), false);
      });

    $('#body')
      .on('click', '.fakeEsc', function () {
        processKey({
          which: 27,
          preventDefault: function () { }
        });
      });

    $(document).keydown(processKey);
    $('#search').keyup(searchChanged).focus();


    $('.Counts').on('click', 'div', function (ev) {
      filterByStatus($(this));
    });


    local.headerSpace = $('header').outerHeight();

    $('body').animate({ scrollTop: 0 }, 0);

    local.afterList = $('#afterList');

    $(window).on('scroll', function () {
      if ($(window).scrollTop() > local.afterList.offset().top - $(window).height() - 500) {
        if (!local.currentSearch) {
          $('.Voter:not(:visible)').slice(0, local.pageSize).removeClass('hidden');
        }
      }
    }).scroll();

    resetSearch();
  };

  var connectToFrontDeskHub = function () {
    $.connection().logging = true;
    var hub = $.connection.frontDeskHubCore;

    //console.log('signalR prepare: updatePeople');
    hub.client.updatePeople = function (info) {
      console.log('signalR: updatePeople');
      updatePeople(info);
    };

    hub.client.reloadPage = function (info) {
      console.log('signalR: reloadPage');
      location.reload();
    };

    startSignalR(function () {
      console.log('Joining frontDesk hub');
      CallAjaxHandler(publicInterface.controllerUrl + '/JoinFrontDeskHub', { connId: site.signalrConnectionId });
    });

  };

  var resetFilterByStatus = function () {
    $('.Voter').removeClass('filterHidden');
    $('.Counts div').removeClass('filtering');
    local.lastStatusFilter = '';
  };

  var filterByStatus = function (btn) {
    if (btn.hasClass('filtering')) {
      resetFilterByStatus();
      return;
    }

    $('.Counts div').removeClass('filtering');

    var classWanted = btn.data('status');
    local.lastStatusFilter = classWanted;

    if (classWanted === 'Total') {
      classWanted = '';
    } else {
      classWanted = '.' + classWanted;
    }

    $('.Voter').addClass('filterHidden');
    $('.Voter .Btn.true' + classWanted).parent().removeClass('filterHidden hidden');

    btn.addClass('filtering');
  }

  //  var refreshHubConnection = function () {
  //    var resetHubConnectionTimer = function () {
  //      clearTimeout(local.reconnectHubTimeout);
  //      local.reconnectHubTimeout = setTimeout(refreshHubConnection, local.hubReconnectionTime);
  //    };
  //
  //    console.log('Joining frontDeskHub');
  //
  //    clearTimeout(local.reconnectHubTimeout);
  //    CallAjaxHandler(publicInterface.controllerUrl + '/JoinFrontDeskHub', { connId: site.signalrConnectionId }, function (info) {
  //      resetHubConnectionTimer();
  //    });
  //
  //  };

  function updateTotals() {
    var total = 0;
    var sumUp = function (name) {
      var num = $('.Voter .{0}.True, .Voter .{0}.true'.filledWith(name)).length;
      total += num;
      $('.Counts .{0} i'.filledWith(name)).text(num);
    }
    sumUp('Online');
    sumUp('Imported');
    sumUp('CalledIn');
    sumUp('MailedIn');
    sumUp('DroppedOff');
    sumUp('InPerson');
    sumUp('Registered');
    sumUp('Custom1');
    sumUp('Custom2');
    sumUp('Custom3');
    $('.Counts .Total i').text(total);
    $('.Counts .Other i').text($('.Voter.VM-').length);
  }

  var searchChanged = function (ev) {
    if (local.currentSearch === ev.target.value) {
      return;
    }

    if (inSelectionMode()) {
      ev.target.value = local.currentSearch;
      return;
    }

    local.currentSearch = ev.target.value;

    //clearTimeout(local.timer);
    applyFilter();
  }

  var processKey = function (ev) {
    var letter, key = ev.which;
    if (ev.altKey) return;
    if (ev.ctrlKey) return;
    var personIsSelected = inSelectionMode();

    switch (key) {
      case 222:
        letter = "'";
        break;
      case 116: // F5
        return;
      case 27: // esc
        if (personIsSelected) {
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

    if (personIsSelected) {
      var btnList = $('.Voter.Selection div.Btn:visible:not(.Hasfalse)').get().map(el => { return { el: el, initial: el.innerText[0] } });
      var next = local.lastLetterIndex;
      var delta = 0;

      switch (key) {
        case 13:
          var selected = btnList.find(btn => btn.el.classList.contains('selected'));
          if (selected) {
            selected.el.classList.remove('selected');
            voteBtnClicked(selected.el);
            return;
          }
          break;
        case 39:
          delta = 1;
          break;
        case 37:
          delta = -1;
          break;
      }

      var matched = null;

      if (delta) {
        next += delta;
        if (next >= btnList.length) next = 0;
        else if (next < 0) next = btnList.length - 1;

        matched = btnList[next];
      } else {
        if (local.lastLetter !== letter) {
          local.lastLetterIndex = -1;
        }

        var start = local.lastLetterIndex;
        var looped = false;
        while (true) {
          next++;
          if (looped && next >= start) {
            break;
          }

          if (next < btnList.length && btnList[next].initial === letter) {
            matched = btnList[next];
            local.lastLetter = letter;
            break;
          }

          if (next >= btnList.length) {
            next = -1;
            looped = true;
          }
        }
      }

      if (matched) {
        // clear all 'selected'
        btnList.forEach(btn => btn.el.classList.remove('selected'));
        btnList[next].el.classList.add('selected');
        local.lastLetterIndex = next;
        //        voteBtnClicked(el);
        return;
      } else {
        local.lastLetter = '';
      }
      //      btnList.each(function (i, el) {
      //        if (letter === el.innerText.substr(0, 1) && i > local.lastLetterIndex) {
      //          voteBtnClicked(el);
      //          local.lastLetterIndex = i;
      //          local.lastLetter = letter;
      //          matchFound = true;
      //          return false;
      //        }
      //      });
    }
    else if (/[\w]/.test(letter)) {
      //console.log(local.focusedOnMatches)
      if (!local.focusedOnMatches) {
        $('#search').focus();
      } else {
        handleKeyWhileFocused(ev);
      }
    }
    //else if (/[\w]/.test(letter)) {
    //  if (!local.focusedOnMatches) {
    //    local.currentSearch = local.currentSearch + letter.toLowerCase();
    //    doSearch = true;
    //    clearTimeout(local.timer);
    //  } else {
    //    handleKeyWhileFocused(ev);
    //  }
    //}
    //console.log('main ' + key);
    switch (key) {
      case 13: // enter
        if (personIsSelected) {
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

      //case 8: //backspace
      //  if (!inSelectionMode()) {
      //    local.currentSearch = local.currentSearch.substr(0, local.currentSearch.length - 1);
      //    if (!local.currentSearch) {
      //      resetSearch();
      //    } else {
      //      doSearch = true;
      //    }
      //  }
      //  ev.preventDefault();
      //  break;

      default:
        //console.log(key);
        break;
    }
    if (doSearch) {
      //clearTimeout(local.timer);
      applyFilter();
      //local.timer = setTimeout(resetSearch, 3000);
    }
  };

  function inSelectionMode() {
    return $('#Main').hasClass('InSelection');
  }

  var activateSelection = function () {
    var current = $('.Voter.Selection');
    if (!current.length) {
      return;
    }
    $('#selectorTip').css('top', current.offset().top - local.headerSpace - 44);
    $('#Main').addClass('InSelection');
  }
  var moveSelector = function (delta) {
    if (inSelectionMode() || !local.currentSearch) {
      return;
    }
    var current = $('.Voter.Selection');
    if (!current.length) {
      setSelection($('.Voter:visible').eq(0), true);
    }
    else {
      var moveTo = [];
      switch (delta) {
        case -1:
          moveTo = current.prevAll('.Voter:visible').eq(0);
          break;
        case 1:
          moveTo = current.nextAll('.Voter:visible').eq(0);
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

      clearTimeout(local.scrollTimer);

      local.scrollTimer = setTimeout(function () {
        scrollToMe(el, function () {
          el.addClass('Selection');
        });
      }, 100);
    } else {
      el.addClass('Selection');
    }
  }

  var scrollToMe = function (el, after) {

    var top = el.offset().top;
    var gap = 40;
    var time = 100;
    var where = top - gap - local.headerSpace;

    //$('body').scrollTop(where);
    $('html, body').animate({ scrollTop: where }, 200)
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
    //clearTimeout(local.timer);
    local.matches.length = 0;
    local.focusedOnMatches = false;
    //if (!local.currentSearch) {
    $('.Voter').removeClass('KeyMatch Focused Selection filterHidden');
    resetFilterByStatus();

    hideUnMatched(false);
    // press ESC twice to clear
    //}
    local.currentSearch = '';
    local.currentTop = 0;
    $('#search').val('').focus();
  };
  var handleKeyWhileFocused = function (ev) {
    if (!local.focusedOnMatches || local.matches.length === 0) return;

    ev.preventDefault();

    if (local.matches.length > 1) {
      local.matches.length = 1;
      focusOnMatches();
    }

    var key = ev.which;
    var currentId = local.matches[0].id;
    var current = $('#' + currentId);
    var moveNext;
    if (key === 40) {
      moveNext = current.next().attr('id');
    } else if (key === 38) {
      moveNext = current.prev().attr('id');
    }
    if (moveNext) {
      local.matches = $('#' + moveNext);
      focusOnMatches();
    } else {
      var btnCode;
      var keyCode = String.fromCharCode(key);
      switch (keyCode) {
        case 'P': // in person
          btnCode = 'P';
          break;
        // case 'I': // imported
        case 'O': // online
        case 'C': // called in (if used)
        case 'R': // received (if used)
        case 'M': // mailed in
        case 'D': //dropped off
        case '1': //custom
        case '2': //custom
        case '3': //custom
          btnCode = keyCode;
          break;
        default:
      }
      if (btnCode) {
        id = currentId.substr(1);
        console.log(btnCode, id);
        saveBtnClick(id, btnCode);
      }
    }
  };

  var applyFilter = function () {
    if (!local.currentSearch) {
      resetSearch();
      return;
    }
    $('#search').val(local.currentSearch);
    local.matches = $('.Voter[data-name*="{0}"]'.filledWith(local.currentSearch.toLowerCase()));
    focusOnMatches();
  };

  var focusOnMatches = function () {
    $('.Voter').removeClass('KeyMatch Focused Selection');
    if (!local.matches.length) {
      local.focusedOnMatches = false;
      return;
    }
    var desired = local.matches.offset().top - 100;

    local.currentTop = desired;

    local.matches.addClass('KeyMatch'); //$(this).switchClass('KeyMatch', 'AfterMatch', 5000, 'linear');
    var num = local.matches.length;
    if (local.focusedOnMatches) {
      local.matches.addClass('Focused');
    }
    //setSelection(local.matches.eq(Math.floor((num - 1) / 2)).first(), true);
    setSelection(local.matches.first(), true);
    setTimeout(function () {
      hideUnMatched(true);
    }, 0);
  };

  var hideUnMatched = function (hide) {
    if (hide) {
      $('.Voter').removeClass('hidden').not('.KeyMatch').addClass('hidden');
      if (!local.currentSearch) {
        resetSearch();
      }
    } else {
      $('.Voter').addClass('hidden').slice(0, local.pageSize).removeClass('hidden');
    }
  }
  function voteBtnClicked(target, forceDeselect) {
    var btn = $(target);

    var row = btn.closest('.Voter');
    var pid = row.attr('id').substr(1);

    var pidNum = +pid;
    var person = publicInterface.initial.find(function (p) { return p.PersonId === pidNum; });
    if (!person) {
      return;
    }
    
    if (person.OnlineProcessed || person.Imported) {
      // teller may have used keyboard to change (mouse is blocked)
      return;
    }

    var classes = btn[0].classList;

    if (!forceDeselect && (classes.contains('True') || classes.contains('true') || classes.contains('clicked'))) {
      // already on
      if (!confirm('Are you sure you want to de-select this person?')) {
        return;
      }
    }

    btn.addClass('clicked');

    setSelection(row, false);

    var btnType = btn.data('vm');
    //      classes.contains('InPerson') ? 'P'
    //        : classes.contains('DroppedOff') ? 'D'
    //          : classes.contains('Online') ? 'O'
    //            : classes.contains('CalledIn') ? 'C'
    //              : classes.contains('MailedIn') ? 'M'
    //                : classes.contains('Registered') ? 'R'
    //                  : classes.contains('Custom1') ? '1'
    //                    : classes.contains('Custom2') ? '2'
    //                      : classes.contains('Custom3') ? '3'
    //                        : '?';

    saveBtnClick(pid, btnType, btn, forceDeselect, person);
  };

  function saveBtnClick(pid, btnType, btn, forceDeselect, person) {
    if (!person) {
      var pidNum = +pid;
      person = publicInterface.initial.find(function (p) { return p.PersonId === pidNum; });
    }
    if (!person) {
      return;
    }
    if (person.OnlineProcessed) {
      // teller may have used keyboard to change (mouse is blocked)
      return;
    }

    var form = {
      id: pid,
      type: btnType
    };

    var loc = $('#ddlTopLocation').val();
    if (loc) {
      form.loc = loc;
    }
    if (forceDeselect) {
      form.forceDeselect = true;
    }

    CallAjaxHandler(publicInterface.controllerUrl + '/VotingMethod', form, function (info) {
      if (info.Message) {
        ShowStatusFailed(info.Message);
        if (btn) {
          btn.removeClass('clicked');
        }
      }
    });
  };

  var startGettingPeople = function () {
    CallAjax2(publicInterface.controllerUrl + '/PeopleForFrontDesk', {},
      {
        busy: 'Getting names'
      },
      function (list) {
        publicInterface.initial = list;
        fillList();
        updateTotals();
      });

  }

  var fillList = function () {
    var html = [];
    $.each(publicInterface.initial, function () {
      //      console.log(this)
      html.push(local.lineTemplate.filledWith(this));
    });
    $('#Main').prepend(html.join(''));
  }

  //  function extendPerson(p) {
  //    if (p.HasOnline) {
  //      p.extraClass = 'HasOnline';
  //    }
  //    return p;
  //  }

  var updatePeople = function (info, pid) {
    ResetStatusDisplay();
    var current = $('.Voter.Selection').attr('id');
    if (info) {
      if (info.PersonLines) {
        var someHidden = false;
        $.each(info.PersonLines, function (i, person) {
          var selector = '#P' + person.PersonId;
          var row = $(selector);
          var hidden = local.currentSearch && !row.hasClass('KeyMatch');
          var extraClasses = [];

          if (row.hasClass('KeyMatch')) {
            extraClasses.push('KeyMatch');
          }
          else if (hidden) {
            // if a search is active, start hidden
            extraClasses.push('hidden');
            someHidden = true;
          }
          //
          //          if (person.HasOnline) {
          ////          console.log(person);
          //            extraClasses.push('HasOnline');
          //          }

          person.extraClass = extraClasses.join(' ');
          person.DisplayLog = '';

          if (person.CanVote) {
            if (row.length) {
              row.replaceWith(local.lineTemplate.filledWith(person));
            } else {
              // add a new person
              insertNewPerson(person);
            }
            if (selector === '#' + current) {
              setSelection($(selector), false);
            }
          } else {
            var currentBtn = row.find('.Btn.true, .Btn.True');
            if (currentBtn.length) {
              // unclick whatever is checked
              voteBtnClicked(currentBtn, true);
            }

            row.slideUp(500, 0, function () {
              row.remove();
            });

          }
        });
        if (someHidden) {
          applyFilter();
        }
        updateTotals();
      }
      if (info.LastRowVersion) {
        publicInterface.lastRowVersion = info.LastRowVersion;
      }
    }
  };

  function insertNewPerson(person) {
    var newName = person.NameLower;
    //console.log('new', newName);
    var added = false;
    $('div.Voter').each(function (i, el) {
      var row = $(el);
      //console.log(row.data('name'));
      if (row.data('name') < newName) {
        return true;
      }
      //console.log('insert before');
      row.before(local.lineTemplate.filledWith(person));
      added = true;
      return false;
    });
    if (!added) {
      //console.log('after last');
      $('div.Voter').last().after(local.lineTemplate.filledWith(person));
    }
  }

  var publicInterface = {
    controllerUrl: '',
    lastRowVersion: 0,
    initial: [],
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