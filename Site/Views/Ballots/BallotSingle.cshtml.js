var BallotSinglePageFunc = function () {
  var publicInterface = {};
  var temp1 = '{StatusCodeText}{BallotStatusText} <span class="SpoiledCount HideZero{SpoiledCount}"> ({SpoiledCount} spoiled)<span>';
  var local = {
    People: [],
    peopleHelper: null,
    lastSearch: '',
    inputField: null,
    lastKey: null,
    nameList: null,
    searchPanel: null,
    ballotsPanel: null,
    //btnDeleteBallot: null,
    location: {},
    lastVid: 0,
    votesNeeded: 0,
    ballotStatus: '',
    ballotId: 0,
    ballots: [],
    votes: [],
    votesList: null,
    tabList: null,
    invalidReasonsHtml: null,
    invalidReasonsShortHtml: null,
    rowSelected: 0,
    searchResultTemplate: '<li id=P{Id}{^Classes}{^IneligibleData}>{^HtmlName}</li>',
    ballotListDetailTemplate: temp1,
    SingleVoteLineTemplate: null,
    lastFindPart: '',
    lastFindId: 0,
    settingNameForOnlineVote: null
  };
  var tabNum = {};

  function preparePage() {
    local.SingleVoteLineTemplate = $('#SingleVoteLine').text();

    tabNum = publicInterface.HasLocations ? {
      location: 0,
      ballotListing: 1,
      ballotEdit: 2,
    } :
      {
        ballotListing: 0,
        ballotEdit: 1,
      };

    local.tabList = $('#accordion');
    local.tabList.accordion({
      heightStyle: "content",
      collapsible: true,
      icons: { "header": "ui-icon-plus", "activeHeader": "ui-icon-minus" },
      activate: function (event, ui) {
        var activePanelId = ui.newPanel.attr('id');
        switch (activePanelId) {
          case 'tabBallots':
            local.inputField.focus().select();
            showAddToThisBtn(true);
            break;
          case 'tabNameSearch':
            local.inputField.focus().select();
            showAddToThisBtn(false);
            break;
          default:
            showAddToThisBtn(true);
            break;
        }
      }
    });

    local.inputField = $('#txtSearch').on('keyup paste', searchTextChanged);

    local.peopleHelper = new PeopleHelper(publicInterface.peopleUrl, true);
    local.peopleHelper.Prepare(function (lastVid) {
      local.lastVid = lastVid;
      local.inputField.prop('disabled', false);
      local.inputField.focus();
    });

    local.inputField.prop('disabled', true);
    local.nameList = $('#nameList');
    local.searchPanel = $('#nameSearch');
    local.ballotsPanel = $('#ballots');
    local.votesList = $('#votesList');

    local.nameList.on('click', 'li', nameClick);
    $('#ballotList').on('click', 'div', ballotClick);
    $('#btnNewBallot').on('click', newBallot);

    $('#btnAddSpoiled').on('click', addSpoiled);

    local.votesList.on('click', '.ui-icon-trash', deleteVote);
    local.votesList.on('click', '.btnClearChangeError', resaveVote);
    local.votesList.on('click', '.btnFind', function (ev) {
      findWithRawVotePart(ev, 'FL');
    });
    local.votesList.on('click', '.btnFindF', function (ev) {
      findWithRawVotePart(ev, 'F');
    });
    local.votesList.on('click', '.btnFindL', function (ev) {
      findWithRawVotePart(ev, 'L');
    });
    local.votesList.on('click', '.VoteHost.withRawtrue', function (ev) {
      if (ev.target.tagName === 'SELECT') {
        return;
      }
      if (selectRawVote(ev)) {
        showRelevantTabs();
      }
    });
    local.votesList.on('click', '.VoteHost.withRawfalse', function () {
      $('.rawTarget').removeClass('rawTarget');
      showRelevantTabs();
    });

    local.votesList.on('change', 'select.Invalid', function (ev) {
      var select = ev.target;
      if (select.size > 1) {
        return;
      }
      invalidReasonChanged(null, select);
    });
    local.votesList.on('click', 'select.Invalid option', function (ev) {
      ev.stopImmediatePropagation();
      var select = $(ev.target).closest('select');
      invalidReasonChanged(null, select);
    });
    local.votesList.on('keypress', 'select.Invalid', invalidReasonKey);

    local.votesList.on('change keyup', 'input', voteNumChange);
    local.votesList.on('click', 'input', function (ev) {
      ev.stopImmediatePropagation();
    });

    local.votesList.sortable({
      handle: '.ui-icon-arrow-2-n-s',
      items: '.VoteHost',
      axis: 'y',
      containment: 'parent',
      tolerance: 'pointer',
      stop: orderChanged
    });


    $('#btnRefreshBallotList').on('click', function () { startToRefreshBallotList(); });
    $('#btnCancelAddMissing').on('click', cancelAddMissing);

    $('#btnSortVotes').on('click', sortVotes);

    showLocation(publicInterface.Location);

    $('#ddlLocationStatus').on('change', changeLocationStatus);

    $('#txtContact').on('change', function () {
      CallAjaxHandler(publicInterface.controllerUrl + '/UpdateLocationInfo', { info: $(this).val() }, function () {
        ShowStatusSuccess('Updated');
      });
    });
    $('#txtNumCollected').on('change', function () {
      var num = Math.max(0, +$(this).val());
      ShowStatusDisplay('Saving...');
      CallAjaxHandler(publicInterface.controllerUrl + '/UpdateLocationCollected', { numCollected: num }, function (info) {
        if (info.Message) {
          ShowStatusFailed(info.Message);
          return;
        }
        if (info.Location) {
          showLocation(info.Location);
          ShowStatusSuccess('Saved');
        }
      });
    });
    resetSearch();

    local.invalidReasonsHtml = prepareReasons();
    local.invalidReasonsShortHtml = prepareReasons('Unreadable');

    site.qTips.push({ selector: '#qTipMissing', title: 'Add Missing', text: 'If the name on the ballot paper cannot be found by searching, then use this button to add a new name.<br><br>If this person named is ineligible to receive votes, this can be noted as you add the name.' });
    site.qTips.push({ selector: '#qTipSpoiled', title: 'Add Spoiled', text: 'If the line on the ballot paper cannot be read for some reason, then use this button to add a line to respresent it.<br><br>If the name can be read, then use the "Add name not in list" button instead.' });
    site.qTips.push({ selector: '#qTipNumVotes', title: '# Votes', text: 'After the paper ballots are sorted by name, enter the number of votes each person has received.  When typing the number, the Up and Down keys will change the number of votes for you.' });
    site.qTips.push({ selector: '#qTipBallotGroups', title: 'Teller Computers', text: 'Since this is an election for just one person, a ballot and a vote are effectively the same.  For each person, the number of votes cast for them is counted by hand and entered here.  If multiple computers are used, then each computer will enter a group of ballots.' });

    site.onbroadcast(site.broadcastCode.personSaved, newPersonSaved);
    site.onbroadcast(site.broadcastCode.personNameChanging, function (ev, fullname) {
      local.inputField.val(fullname);
      searchTextChanged();
    });
    site.onbroadcast(site.broadcastCode.locationChanged, function () {
      // do instant reload
      changedLocation();
    });

    $('#votesPanel, .sidePanel').show();

    showBallot(publicInterface);
    showLocationStatus();

    connectToFrontDeskHub();

  };

  function connectToFrontDeskHub() {
    $.connection().logging = true;
    var hub = $.connection.frontDeskHubCore;

    hub.client.updatePeople = function (info) {
      console.log('signalR: updatePeople');
      var updatedExisting = local.peopleHelper.UpdatePeople(info);
      local.lastSearch = '';
      searchTextChanged();
      if (updatedExisting) {
        startToRefreshBallotList();
      }
    };

    startSignalR(function () {
      console.log('Joining frontDesk hub');
      CallAjaxHandler(publicInterface.frontDeskUrl + '/JoinFrontDeskHub', { connId: site.signalrConnectionId });
    });

  };

  function changedLocation() {
    ShowStatusDisplay('Loading location...');
    CallAjaxHandler(publicInterface.controllerUrl + '/GetLocationInfo', null, function (info) {
      if (info.Message) {
        ShowStatusFailed(info.Message);
        return;
      }

      showLocation(info.Location);
      showLocationStatus();

      showBallot(info);

      showRelevantTabs();
    });
  };

  function sortVotes() {
    var votesList = $('#votesList');
    var votes = votesList.children();
    $.each(votes, function (i, r) {
      r.sortValue = null;
    });

    var getValue = function(a) {
      return $(a).find('.Name').text();
    };

    votes.sort(function (a, b) {
      var aValue = a.sortValue || (a.sortValue = getValue(a));
      var bValue = b.sortValue || (b.sortValue = getValue(b));
      return aValue > bValue ? 1 : -1;
    });

    votes.detach().appendTo(votesList);
    orderChanged();
  }

  function orderChanged(ev, ui) {
    var ids = [];
    var toUpdate = [];

    local.votesList.children().each(function () {
      var voteHost = $(this);
      var id = +voteHost.data('vote-id');
      if (id > 0) {
        ids.push(id);
        toUpdate.push(voteHost.find('.VoteNum'));
      }
    });
    var form = {
      idList: ids
    };
    ShowStatusDisplay("Saving...");
    CallAjaxHandler(publicInterface.controllerUrl + '/SortVotes', form, function (info) {
      if (info.Message) {
        ShowStatusFailed(info.Message);
        return;
      }
      if (info) {
        // no need to update client with new order
        ShowStatusSuccess("Saved");
        // update to reflect changes
        $.each(toUpdate, function (i, o) {
          o.text(i + 1);
        });
      }
    });

  };

  function showRelevantTabs() {
    // start with all closed...
    local.tabList.find('h3').hide().next().hide();

    // show location status if not online
    if (!local.location.IsOnline) {
      local.tabList.find('h3').eq(tabNum.location).show().next().show();
    }

    // always show ballots
    local.tabList.find('h3').eq(tabNum.ballotListing).show().next().show();

    // show add if a ballot is selected (regular) or raw vote is selected (online)
    if (local.location.IsOnline) {
      $('#addAnother').text('Update matched person');
      if ($('.VoteHost.rawTarget').length !== 0) {
        local.tabList.find('h3').eq(tabNum.ballotEdit).show().next().show();
      }
    }
    else {
      $('#addAnother').text('Add another Person');
      if ($('#ballotList .selected').length !== 0) {
        local.tabList.find('h3').eq(tabNum.ballotEdit).show().next().show();
      }
    }
    //    if (showLocationTab && !local.location.IsOnline) {
    //      local.tabList.accordion('option', 'active', tabNum.location);
    //    } else {
    //      local.tabList.accordion('option', 'active', tabNum.ballotListing);
    //    }

    //    toggleAddToBallotTab(!local.location.IsOnline);
    //    local.tabList.accordion('option', 'active', tabNum.ballotListing);
    //    showAddToThisBtn(!local.location.IsOnline);

  }

  //  function toggleAddToBallotTab(show) {
  //    if (show) {
  //      local.tabList.find('h3').eq(tabNum.ballotEdit).show().next().show();
  //      $('.nameListKey').fadeIn();
  //    } else {
  //      local.tabList.find('h3').eq(tabNum.ballotEdit).hide().next().hide();
  //      $('.nameListKey').hide();
  //      resetSearch();
  //    }
  //  };


  function newBallot() {
    if (!needBallotForThisComputer()) {
      // btn should not be shown
      return;
    }
    // disable on click...
    $('.NewBallotBtns').prop('disabled', true);

    CallAjaxHandler(publicInterface.controllerUrl + "/NewBallot", null, function (info) {
      if (info.Message) {
        ShowStatusFailed(info.Message);
        $('.NewBallotBtns').prop('disabled', false);
        return;
      }

      showBallot(info);

      showRelevantTabs();

      local.inputField.focus().val('').change();
      local.nameList.html('');
      $('.NewBallotBtns').prop('disabled', false);
    }, null, function () {
      // failed
      $('.NewBallotBtns').prop('disabled', false);
    });
  };


  function showAddToThisBtn(show) {
    if (show) {
      $('#btnAddToThis').show();
    } else {
      $('#btnAddToThis').hide();
    }
  }

  //  function hideBallotTab() {
  //    local.tabList.find('h3').eq(tabNum.ballotEdit).hide().next().hide();
  //  };

  function focusOnTextInput() {
    local.inputField.focus().select();
  };

  function startToRefreshBallotList(successMsg) {
    ShowStatusDisplay('Refreshing ballots');
    CallAjaxHandler(publicInterface.controllerUrl + '/RefreshBallotsList', null, function (info) {
      showBallots(info);
      highlightBallotInList();
      showRelevantTabs();
      ShowStatusSuccess(successMsg || 'Updated');
    });
  };

  function showLocationStatus() {
    // copy selected status to the heading
    var statusDdl = $('#ddlLocationStatus');
    if (!statusDdl.length) {
      return;
    }

    var select = statusDdl[0];
    if (select.selectedIndex === -1) {
      $('.LocationStatus').text(': Unknown');
    } else {
      var text = select.options[select.selectedIndex].text;
      $('.LocationStatus').text(': ' + text);
    }
    var location = local.location;
    var regularLocation = !location.IsOnline;

//    console.log('hide btns', regularLocation, location.IsOnline, location);

    $('#btnNewBallot').toggle(regularLocation && needBallotForThisComputer());
    $('.ballotDiv1').toggle(regularLocation);
    $('.ballotNumEntered').toggle(regularLocation);
  }

  function needBallotForThisComputer() {
    var thisCode = $('.CompCode').text();
    return !local.ballots.find(function (b) { return b.ComputerCode === thisCode; });
  }

  function changeLocationStatus() {
    if (!local.location) {
      ShowStatusFailed("Select a location first!");
      return;
    }
    var form = {
      id: local.location.Id,
      status: $('#ddlLocationStatus').val()
    };
    CallAjaxHandler(publicInterface.controllerUrl + '/UpdateLocationStatus', form, function (info) {
      if (info.Location) {
        showLocation(info.Location);
        showLocationStatus();

        $('span[data-location]').each(function () {
          var span = $(this);
          if (span.data('location') === info.Location.Id) {
            span.text(info.Location.TallyStatus);
          }
        });
      }
      ShowStatusSuccess('Updated');
    });
  };

  function capitalized(s) {
    if (!s) return '';
    return s.substr(0, 1).toUpperCase() + s.substr(1);
  };

  function addMissing(voteId, host) {
    toggleAddMissingPanel();

    $('#addMissingPanel').data('voteId', voteId);

    var ddl = $('#ddlIneligible');
    ddl[0].size = ddl.find('option, optgroup').length + 1;
    var first, last;

    if (host.hasClass('withRawtrue')) {
      first = host.find('.F').text();
      last = host.find('.L').text();
    } else {
      var searchParts = local.inputField.val().split(' ');
      first = searchParts.shift();
      last = searchParts.join(' ');
    }

    site.broadcast(site.broadcastCode.startNewPerson, {
      panelSelector: '#addMissingPanel',
      ineligible: 'ce27534d-d7e8-e011-a095-002269c41d11', // Unidentifiable_Unknown_person
      first: capitalized(first),
      last: capitalized(last)
    });
  };

  function cancelAddMissing() {
    var panel = $('#addMissingPanel');

    if (panel.is(':visible')) {
      var voteId = panel.data('voteId');
      var select = $('#V' + voteId + ' select');
      select[0].selectedIndex = 0;
      local.settingNameForOnlineVote = null;

      toggleAddMissingPanel();
    }
  };

  function newPersonSaved(ev, info) {
    local.lastSearch = ''; // force a reload
    searchTextChanged();
    toggleAddMissingPanel();

    if ($('#ddlTopLocation').val() === -1) {
      ShowStatusFailed('Must select your location first!');
      return;
    }
    
    var vote, newHost;
    var person = info.Person;

    var voteId = local.settingNameForOnlineVote;
    if (voteId) {
      vote = local.votes.find(function (v) { return v.vid === voteId; });
      vote.pid = person.C_RowId;
      vote.name = person.C_FullName;
      vote.count = 1;
      vote.ineligible = person.CanReceiveVotes ? null : person.IneligibleReasonGuid;

      newHost = local.votesList.find('.VoteHost#V' + voteId).eq(0);
      newHost.data('person-id', person.C_RowId);
    } else {
      var votesId0 = $.grep(local.votes,
        function (v) {
          return v.vid === 0;
        });
      if (votesId0.length) {
        vote = votesId0[0];
        vote.pid = person.C_RowId;
        vote.name = person.C_FullName;
        vote.ineligible = person.CanReceiveVotes ? null : person.IneligibleReasonGuid;
      } else {
        vote = {
          vid: 0,
          pid: person.C_RowId,
          name: person.C_FullName,
          count: 1,
          ineligible: person.CanReceiveVotes ? null : person.IneligibleReasonGuid
        };
        local.votes.push(vote);
      }
      newHost = local.votesList.find('.VoteHost#V0').eq(0);
    }
    if (!vote.ineligible) {
      vote.InvalidReasons = null;
      vote.invalidType = null;
      vote.invalid = null;
    }

    showVotes();

    newHost = local.votesList.find('.VoteHost#V' + voteId).eq(0);

    startSavingVote(newHost);
  };

  function toggleAddMissingPanel() {
    $('#votesPanel, #addMissingPanel').toggle();
  };

  function showBallot(info) {

    if (info.Ballots) {
      showBallots(info.Ballots);
    }

    var ballotInfo = info.BallotInfo;
    if (ballotInfo) {
      $('#votesPanel').css('visibility', 'visible').toggleClass('online', local.location.IsOnline);

      var ballot = ballotInfo.Ballot;
      $('.ballotCode').text(ballot.Code);
      $('#ballotStatus').text(ballot.StatusCode);

      local.votesNeeded = ballotInfo.NumNeeded;
      local.ballotStatus = ballot.StatusCode;
      local.votes = ballotInfo.Votes;
      local.ballotId = ballot.Id;

      showVotes();

      updateStatusDisplay(ballot);

      //      var toShow = local.location.IsOnline 
      //        ? tabNum.ballotListing 
      //        : (ballot.StatusCode === 'TooFew' || ballot.StatusCode === 'Empty') 
      //          ? tabNum.ballotEdit 
      //          : tabNum.ballotListing;

      highlightBallotInList();

    } else {
      $('.ballotCode').text('');

      $('#votesPanel').css('visibility', 'hidden');
      $('#votesList').html('');

      showAddToThisBtn(true);

    }

    if (info.Location) {
      showLocation(info.Location);
      //      publicInterface.Location = info.Location;
    }

    showRelevantTabs();
  }

  function showVotes(rawTargetVid) {
    var votes = extendVotes(local.votes);

    cancelAddMissing();

    local.votesList.html(local.SingleVoteLineTemplate.filledWithEach(votes));
    if (rawTargetVid) {
      local.votesList.find('#V' + rawTargetVid).addClass('rawTarget');
    }
    local.votesList.find('select').each(function () {
      var select = $(this);
      var reason = select.data('invalid');
      if (reason) {
        select.val(reason);
      }
    });
    findAndMarkDups(local.votesList.find('.VoteHost'));
  };

  function findAndMarkDups(votes) {
    var found = false;
    var dups = {};
    var list = [];
    var vote;
    local.votesList.find('.Duplicate').hide();
    votes.each(function () {
      vote = $(this);
      vote.removeClass('duplicateVote');
      var thisPerson = vote.data('person-id');
      if (thisPerson && dups[thisPerson]) {
        dups[thisPerson].push(vote);

        if ($.inArray(thisPerson, list) === -1) {
          list[list.length] = thisPerson;
        }
      } else {
        dups[thisPerson] = [vote];
      }
    });

    for (var i = 0; i < list.length; i++) {
      var id = list[i];
      var dupVotes = dups[id];
      if (dupVotes.length > 1) {
        found = true;
        for (var j = 0; j < dupVotes.length; j++) {
          vote = dupVotes[j];
          vote.addClass('duplicateVote');
          //          vote.children().eq(0).after('<span class=Duplicate>Duplicate:</span>');
          vote.find('.Duplicate').show();
        }
      }
    }
    return found;
  };

  function updateStatusInList(info) {
    $('#BallotStatus' + local.ballotId).html(local.ballotListDetailTemplate.filledWith(info));
  };

  function updateStatusDisplay(info) {

    if (info.StatusCode) {
      // backward compatibility... convert values
      info.BallotStatus = info.StatusCode;
      info.BallotStatusText = info.StatusCodeText;
    }

    $('#cbReview').prop('checked', info.BallotStatus === 'Review');

    var status = info.BallotStatus;

    var topDisplay = $('.ballotStatus');

    topDisplay.html(info.BallotStatusText);

    if (status === 'Ok') {
      topDisplay.removeClass('InvalidBallot');
      topDisplay.addClass('Ok');
    } else {
      topDisplay.removeClass('Ok');
      topDisplay.addClass('InvalidBallot');
    }
  };

  function showLocation(location) {
    local.location = location;
    $('#ddlTopLocation').val(local.location.Id);

    $('.locationInfo [data-name], .LocationName[data-name]').each(function () {
      var target = $(this);
      var value = location[target.data('name')];

      switch (this.tagName) {
        case 'SPAN':
        case 'DIV':
          target.html(value);
          break;
        default:
          target.val(value);
          break;
      }
    });

    $('#ballotHeading').text(location.IsOnline ? 'Match name for' : 'Add votes to Ballot #');
    $('body').toggleClass('IsOnline', location.IsOnline);
  };

  function showBallotCount(numEnteredOnThisComputer, numEnteredInLocation) {
    $('#lblNumEntered').text(numEnteredOnThisComputer || local.ballotCountAtLastLoad || '-');

    var remainingToEnter = (local.location.BallotsCollected || 0) - (numEnteredInLocation || 0);
    var title;
    if (remainingToEnter === 0) {
      title = ': All entered';
    } else if (remainingToEnter < 0) {
      title = ': {0} more than counted'.filledWith(0 - remainingToEnter);
    }
    else {
      title = ': {0} more to enter'.filledWith(remainingToEnter);
    }

    $('#collectedVsEnteredTitle').text(title);
  };

  function extendBallots(ballots) {
    ballots.forEach(function (ballot) {
      ballot.peoplePlural = ballot.people === 1 ? 'person' : 'people';
      ballot.ballotPlural = ballot.Count === 1 ? '' : 's';
      ballot.onlineStatus = ballot.hasOnlineRawToFinish
        ? 'Teller to Complete'
        : ballot.hasOnlineRaw
          ? 'Completed'
          : 'From List';
    });
    return ballots;
  }

  function showBallots(info) {
    var list = extendBallots(info.Ballots);
    local.ballots = list;

    var ballotListTemplate = local.location.IsOnline
      ? '<div id=B{Id}>Online Voter ({Code} - {onlineStatus})</div>'
      : '<div id=B{Id}>Teller Ballots {Code} &nbsp; (<span>{people}</span> {peoplePlural}, <span>{Count}</span> vote{ballotPlural})</div>';

    $('#ballotList').html(ballotListTemplate.filledWithEach(list));

    showBallotCount(list.reduce(function (acc, b) { return acc + b.Count; }, 0));
    local.ballotCountAtLastLoad = list.length;
  };

  function highlightBallotInList() {
    var ballotList = $('#ballotList');
    var highlighted = ballotList.children().removeClass('selected').end().find('#B{0}'.filledWith(local.ballotId)).addClass('selected');
    scrollIntoView(highlighted, ballotList);
  };

  function voteNumChange(ev) {
    ev.stopImmediatePropagation();
    var input = $(ev.target);
    var saveNow = ev.type === 'change';
    var focusOnNew = false;
    var changing = true;
    //    console.log(ev.type);
    //    console.log(ev.which);


    switch (ev.which) {
      case 13:
        // enter
        ev.preventDefault();
        ev.stopPropagation();
        ev.stopImmediatePropagation();
        saveNow = true;
        focusOnNew = true;
        break;

      case 9:
        changing = false;
        break;

      //case 38:
      //  //input.val(+input.val() + 1);
      //  ev.preventDefault();
      //  ev.stopPropagation();
      //  ev.stopImmediatePropagation();
      //  //local.inputField.focus();
      //  saveNow = false; // the change event will save it
      //  focusOnNew = false;
      //  break;

      //case 40:
      //  //input.val(+input.val() - 1);
      //  ev.preventDefault();
      //  ev.stopPropagation();
      //  ev.stopImmediatePropagation();
      //  //local.inputField.focus();
      //  saveNow = false;
      //  focusOnNew = false;
      //  break;

      default:
    }

    if (saveNow) {
      startSavingVote(input.closest('.VoteHost'), focusOnNew);
    } else if (changing) {
      //      input.addClass('changing');
    }
  };

  function invalidReasonKey(ev) {
    var select = ev.target;
    // console.log('a', select.size);
    if (select.size === 1) {
      invalidReasonChanged(ev);
      return;
    }
    if (ev.which === 13) {
      if (select.selectedIndex > 0) {
        invalidReasonChanged(ev);
        ev.preventDefault();
      }
    }
  }

  //var removedParent; // seems to be triggering twice!
  function invalidReasonChanged(ev, target) {
    target = target || ev.target;
    if (target.selectedIndex === -1) {
      return; //nothing selected
    }
    var select = $(target);
    var reason = select.val();
    if (reason === '0') {
      return;  // don't save with no reason
    }
    select.attr('size', 1);
    var parent = select.closest('.VoteHost');

    if (reason === '-1') {
      // remove this one

      //parent.remove();

      //if (!removedParent) {
      //  removedParent = parent;
      //  parent.remove();
      //} else {
      //  removedParent = null;
      //  //return;
      //}

      var voteId = parent.data('vote-id') || 0;

      if (!parent.hasClass('withRawtrue')) {
        for (var i = 0; i < local.votes.length; i++) {
          var vote = local.votes[i];
          if (vote.vid === voteId) {
            local.votes.splice(i, 1);
            break;
          }
        }
        if (voteId !== 0) {
          var form = {
            vid: voteId
          };
          CallAjaxHandler(publicInterface.controllerUrl + '/DeleteVote',
            form,
            function (info) {
              // if failed, user will have to manually remove it
            });
        }
      } else {
        local.settingNameForOnlineVote = voteId;
      }

      // add the new one
      addMissing(voteId, parent);
      return;
    }

    startSavingVote(parent);

    focusOnTextInput();
  };

  function resaveVote(ev) {
    var host = $(ev.target).closest('.VoteHost');
    startSavingVote(host, true);
  };


  function findWithRawVotePart(ev, part) {
    ev.stopImmediatePropagation();
    if (!selectRawVote(ev, true)) {
      return;
    }

    var btn = $(ev.target);
    var host = btn.closest('.rawVote');
    var vote = host.parent();

    var voteId = vote.data('vote-id');
    var lastPart = local.lastFindPart;
    var lastId = local.lastFindId;
    var lastNum = +(btn.data('last') || '');

    if (lastPart === part && lastId === voteId) {
      lastNum--;
    } else {
      local.lastFindPart = part;
      local.lastFindId = voteId;
      lastNum = +(btn.data('max') || '');
      if (!lastNum || isNaN(lastNum)) {
        lastNum = part === 'FL'
          ? Math.max(host.find('.F').text().length,
            host.find('.L').text().length)
          : host.find('.' + part).text().length;
        //        console.log('new', lastNum);
        btn.data('max', lastNum);
      }
    }
    btn.data('last', lastNum);

    var text = (
      part === 'FL'
        ? trimBy(host.find('.F').text(), lastNum) + ' ' +
        trimBy(host.find('.L').text(), lastNum)
        : trimBy(host.find('.' + part).text(), lastNum)
    ).trim();

    if (!text) {
      lastNum = btn.data('max');
      text = (
        part === 'FL'
          ? trimBy(host.find('.F').text(), lastNum) +
          ' ' +
          trimBy(host.find('.L').text(), lastNum)
          : trimBy(host.find('.' + part).text(), lastNum)
      ).trim();

      btn.data('last', lastNum + 1);
    }

    $('#txtSearch').val(text).select().trigger('keyup');
  }

  function trimBy(s, num) {
    return s.substr(0, num);
  }

  function startSavingVote(host, verifying) {
    if ($('#ddlTopLocation').val() === -1) {
      ShowStatusFailed('Must select your location first!');
      return;
    }

    var personId = host.data('person-id') || 0;
    var invalids = host.find('select:visible');
    var invalidId = invalids.val() || '';
    var voteId = +host.data('vote-id') || 0;
    var input = host.find('input');

    var vote = local.votes.find(function (v) { return v.vid === voteId; });

    if (personId && invalidId && voteId) {
      invalidId = '';
      invalids.data('invalid', '');
      vote.invalid = null;
    }

    var form = {
      pid: personId,
      vid: voteId,
      lastVid: local.lastVid,
      count: +input.val() || 0
    };

    if (isNaN(form.count) || +form.count < 0) {
      alert('Invalid number. Please correct.');
      input.focus();
      return;
    }

    if (invalidId) {
      form.invalid = invalidId;
    }
    if (verifying) {
      form.verifying = verifying;
    }

    //    if (vote) {
    //      // previously saved
    //      if (vote.count === +form.count && vote.invalid === invalidId) {
    //        host.removeClass('Changedtrue').addClass('Changedfalse');
    //        input.removeClass('changing');
    //        return;
    //      }
    //    }

    if (invalidId) {
      invalids.data('invalid', invalidId);
      if (vote && vote.vid === voteId) {
        vote.invalid = invalidId;
      }
    }

    ShowStatusDisplay('Saving...');

    CallAjaxHandler(publicInterface.controllerUrl + '/SaveVote', form, function (info) {
      if (info.Updated) {
        ShowStatusSuccess('Saved');
        // assume any error was removed
        host.removeClass('Changedtrue').addClass('Changedfalse');

        vote.count = form.count;

        if (invalids.length === 1) {
          invalids[0].size = 1;
        }

        //        if (!publicInterface.Location) {
        //          location.href = location.href;
        //          //TODO: use Ajax to reload the content?
        //          return;
        //        }

        local.lastVid = info.LastVid;

        if (info.Location) {
          showLocation(info.Location);
        }

        var ballotNums = $('#B' + info.BallotId + ' span');
        ballotNums.eq(0).text(info.SingleBallotNames);
        ballotNums.eq(1).text(info.SingleBallotCount);

        input.focus();

        if (form.vid === 0) {
          if (info.VoteId) {
            host.data('vote-id', info.VoteId);
            host.attr('id', 'V' + info.VoteId);
            host.find('.VoteNum').text(info.pos);

            if (vote && vote.vid === 0) {
              vote.vid = info.VoteId;
              vote.pos = info.pos;
            }
          }
          else {
            ShowStatusFailed('Error on save. Please reload this page.');
          }
        }

        vote.invalid = info.InvalidReasonGuid;

        if (info.Name) {
          vote.name = info.Name;
          host.find('.Name').html(info.Name + '<u>' + info.Area + '</u>');
        }

        showVotes();

        updateStatusDisplay(info);
        updateStatusInList(info);

        showRelevantTabs();

        local.peopleHelper.RefreshListing(local.inputField.val(), displaySearchResults, getUsedIds(), info);

        showBallotCount(info.LocationBallotsEntered);

        var compCode = $('.CompCode').text();
        if (!local.ballots.find(function (b) { return b.ComputerCode === compCode; })) {
          // this computer was not listed... refresh the list
          startToRefreshBallotList();
        }

      } else {
        var msg = info.Error || info.Message;
        ShowStatusFailed(msg);

        if (local.location.IsOnline) {
          loadBallot('B' + local.ballotId, true, msg);
        }
        else {
          // remove newly added
          for (var i = 0; i < local.votes.length; i++) {
            vote = local.votes[i];
            if (vote.vid === 0) {
              local.votes.splice(i, 1);
            }
          }
          showVotes();
        }
      }

    });
  };

  function deleteVote(ev) {
    ev.stopImmediatePropagation();

    var host = $(ev.target).closest('.VoteHost');
    var voteId = host.data('vote-id') || 0;
    var form = {
      vid: voteId
    };

    if (voteId === 0) {
      host.remove();
      return;
    }

    ShowStatusDisplay('Deleting...');
    CallAjaxHandler(publicInterface.controllerUrl + '/DeleteVote', form, function (info) {
      if (info.Deleted) {
        ShowStatusSuccess('Deleted');
        host.remove();

        if (info.NumNeeded) {
          local.votesNeeded = info.NumNeeded;
        }

        if (info.Votes) {
          local.votes = info.Votes;
          showVotes();
        }

        if (info.Location) {
          showLocation(info.Location);
        }

        var ballotNums = $('#B' + info.BallotId + ' span');
        ballotNums.eq(0).text(info.SingleBallotNames);
        ballotNums.eq(1).text(info.SingleBallotCount);

        updateStatusDisplay(info);
        updateStatusInList(info);

        showBallotCount(info.LocationBallotsEntered);
        local.peopleHelper.RefreshListing(local.inputField.val(), displaySearchResults, getUsedIds(), info);
      }
      else {
        ShowStatusFailed(info.Message);
      }
    });
  };

  function displaySearchResults(info, beingRefreshed, showNoneFound) {

    local.People = info.People || [];

    //console.log(local.People);

    local.nameList.html(local.searchResultTemplate.filledWithEach(local.People));
    $('#more').html(''); //info.MoreFound
    if (!local.People.length && local.lastSearch) {
      var search = local.inputField.val();
      if (search && showNoneFound) {
        local.nameList.append('<li>...no matches found...</li>');
      }
    } else {
      if (info.MoreFound && local.lastSearch) {
        local.nameList.append('<li title="Enter more of the name to limit how many are matched!">...more matched...</li>');
      }
      if (beingRefreshed) {
        local.rowSelected = info.currentFocusRow;
      } else {
        local.rowSelected = info.BestRowNum;
      }
      local.nameList.find('li[data-ineligible]').each(function (i, item) {
        var li = $(item);
        var ineligible = li.data('ineligible');
        if (!li.data('canreceivevotes')) {
          var desc = getIneligibleReasonDesc(ineligible);
          item.title = 'Ineligible: ' + desc;
        } else {
          if (ineligible) {
            li.data('ineligible', null);
          }
        }
      });
    }

    setSelected(local.nameList.children(), local.rowSelected);
  };

  function getIneligibleReasonDesc(guid) {
    var matched = $.grep(publicInterface.invalidReasons, function (item, i) {
      return item.Guid === guid;
    });
    if (matched.length === 0) return '';
    return matched[0].Desc;
  };

  function moveSelected(delta) {
    var children = local.nameList.children();
    var numChildren = children.length;
    if (children.eq(numChildren - 1).text() === '...') { numChildren--; }

    var rowNum = typeof local.rowSelected == 'undefined' ? -1 : local.rowSelected;
    rowNum = rowNum + delta;
    if (rowNum < 0) { rowNum = 0; }
    if (rowNum >= numChildren) { rowNum = numChildren - 1; }
    setSelected(children, rowNum);
  };

  function setSelected(children, rowNum) {
    children.removeClass('selected');
    local.rowSelected = rowNum;
    var newSelected = children.eq(rowNum);
    newSelected.addClass('selected');
    scrollIntoView(newSelected[0], local.nameList);
  };

  function scrollIntoView(element, container) {
    if (!element) return;
    var containerTop = $(container).scrollTop();
    var containerBottom = containerTop + $(container).height();
    var elemTop = element.offsetTop;
    var elemBottom = elemTop + $(element).height();
    if (elemTop < containerTop) {
      $(container).scrollTop(Math.max(0, elemTop - 10));
    } else if (elemBottom > containerBottom) {
      $(container).scrollTop(elemBottom - $(container).height() + 30);
    }
  };

  function edit(selectedPersonLi) {
    local.nameList.children().removeClass('selected');
    selectedPersonLi.addClass('selected');
    addToVotesList(selectedPersonLi);
  };

  function addToVotesList(selectedPersonLi) {
    if (!selectedPersonLi.length) return;

    var rawId = selectedPersonLi.attr('id');
    if (!rawId) return;

    var inUse = selectedPersonLi.children().eq(0).hasClass('InUse');
    if (inUse) {
      return;
    }

    var personId = +rawId.substr(1);
    if (personId === 0) return;

    var personName = selectedPersonLi.text();
    var area = selectedPersonLi.find('u').text();
    if (area) {
      personName.length -= area.length;
      personName = `${personName} <u>${area}</u>`;
    }

    if (local.location.IsOnline) {
      var voteHost = $('.rawTarget');
      if (voteHost.length !== 1) {
        return;
      }
      var vid = voteHost.data('vote-id');
      if (!vid) {
        return;
      }

      var vote = local.votes.find(function (v) { return v.vid === vid; });
      if (!vote) {
        console.log('not found: vid ', vid);
        return;
      }

      vote.pid = personId;
      vote.name = personName;
      vote.count = 0;
      vote.ineligible = selectedPersonLi.data('ineligible');

      voteHost.data('person-id', personId);
      startSavingVote(voteHost);

      showVotes();

    } else {

      focusOnTextInput();

      local.votes.push({
        vid: 0,
        pid: personId,
        name: personName,
        count: 1,
        ineligible: selectedPersonLi.data('ineligible')
      });

      showVotes();

      var newHost = local.votesList.find('.VoteHost').last();

      startSavingVote(newHost);
    }
  }

  function addSpoiled() {
    // if one is pending, don't add another
    var unresolved = false;
    local.votesList.find('select.Invalid:visible').each(function (i, s) {
      if (s.selectedIndex < 1) {
        unresolved = true;
        s.size = s.options.length + $(s).children().length - 1;
        setTimeout(function () {
          //          console.log('focus2', s);
          $(s).focus();
        },
          0);
      }
      if (s.selectedIndex === 1) {
        invalidReasonChanged(null, s);
      }
    });
    if (unresolved) {
      return;
    }

    var voteHost;

    if (local.location.IsOnline) {

      voteHost = $('.rawTarget');
      if (voteHost.length !== 1) {
        return;
      }

      var vid = voteHost.data('vote-id');
      if (!vid) {
        return;
      }

      var vote = local.votes.find(function (v) { return v.vid === vid; });
      if (!vote) {
        console.log('not found: vid ', vid);
        return;
      }

      vote.pid = null;
      vote.name = null;
      vote.count = 0;
      vote.invalid = 0;
      vote.changed = false;
      vote.ineligible = null;
      vote.InvalidReasons = local.invalidReasonsShortHtml;

      showVotes(vid);

      voteHost = $('.rawTarget');

    } else {

      //    console.log('spoiled');
      local.votes.push({
        vid: 0,
        count: 1,
        invalid: 0,
        changed: false,
        InvalidReasons: local.invalidReasonsShortHtml
      });

      showVotes();

      voteHost = local.votesList.find('.VoteHost').last();
    }

    var input = voteHost.find('select');
    input.attr('size', input[0].options.length + input.children().length - 1);

    // vote not saved until a reason is chosen
    setTimeout(function () {
      input.focus();
    }, 0);
  };

  function extendVotes(votes) {
    //    var num = 0;
    $.each(votes, function () {
      var vote = this;

      if (this.invalid && this.invalid !== null) {
        this.InvalidReasons = local.invalidReasonsShortHtml;
        this.invalidType = 'C';
      }
      if (this.ineligible && this.ineligible !== null) {
        // person is invalid!

        var reasonList = $.grep(publicInterface.invalidReasons, function (item) {
          return item.Guid === vote.ineligible;
        });
        var reason = 'Ineligible';
        if (reasonList.length === 1) {
          reason = reasonList[0].Desc;
        }
        //this.Display = '<span class=CannotReceiveVotes>{name}</span>'.filledWith(this); // ' &nbsp; <span class=Ineligible>{0}</span>'.filledWith(reason, this.name);
        this.Display = this.name;
        this.invalid = vote.ineligible;
        this.invalidType = 'P';
        this.InvalidDescription = reason;
      }
      else {
        this.Display = this.name;
      }

      this.hasRawVote = !!vote.onlineRawVote;
      if (this.hasRawVote) {
        var rawVote = JSON.parse(vote.onlineRawVote || {});
        this.rawFirst = rawVote.First;
        this.rawLast = rawVote.Last;
        this.rawOtherInfo = rawVote.OtherInfo;
        this.rawDone = vote.pid > 0 || !!vote.invalid;
      }
      //      num++;
    });
    return votes;
  }

  function selectRawVote(ev, inRow) {
    if ($('#ddlTopTeller1').val() === '0') {
      ShowStatusFailed('Must select "Teller at Keyboard" first!');
      return false;
    }

    var vote = $(ev.target).closest('.VoteHost');
    var oldVote = $('.rawTarget');
    oldVote.removeClass('rawTarget');

    //    if (!inRow && oldVote.attr('id') === vote.attr('id')) {
    //      toggleAddToBallotTab(false);
    //      return false; // clicked on same again
    //    }

    vote.addClass('rawTarget');

    if (!inRow) {
      vote.find('.btnFind').trigger('click');
    }

    return true;
  }

  function prepareReasons(onlyGroup) {
    var html = [
      '<option value="0">Select a reason...</option>',
      '<optgroup label="Name not in the List">',
      ballotPage.isGuest ?
        '<option value="0">(Ask head teller to add required name)</option>' :
        '<option value="-1">Add new name (including spoiled)</option>',
      '</optgroup>'
    ];
    var group = '';
    $.each(publicInterface.invalidReasons, function () {
      var reasonGroup = this.Group;
      if (onlyGroup && reasonGroup !== onlyGroup) {
        return;
      }
      if (reasonGroup !== group) {
        if (group) {
          html.push('</optgroup>');
        }
        html.push('<optgroup label="{0}">'.filledWith(reasonGroup));
        group = reasonGroup;
      }
      html.push('<option value="{Guid}">{Desc}</option>'.filledWith(this));
    });
    html.push('</optgroup>');
    return html.join('\n');
  };

  function navigating(ev) {
    switch (ev.which) {
      case 38: // up
        moveSelected(-1);
        ev.preventDefault();
        return true;

      case 33: // page up
        moveSelected(-6);
        ev.preventDefault();
        return true;

      case 40: // down
        moveSelected(1);
        ev.preventDefault();
        return true;

      case 34: // page down
        moveSelected(6);
        ev.preventDefault();
        return true;

      case 13: // enter
        ev.preventDefault();
        edit(local.nameList.children().eq(local.rowSelected));
        return true;

      case 27: // esc
        if (local.lastKey === 27) {
          // pressed esc twice - clear inputs
          local.inputField.val('');
          searchTextChanged();
        }
        return true;

      default:
        //        console.log(ev.which);
        break;
    }
    return false;
  };

  function searchTextChanged(ev) {
    var input = local.inputField;
    var text = input.val();
    if (ev) {
      if (navigating(ev)) {
        local.lastKey = ev.which;
        return;
      }
      local.lastKey = ev.which;
    }
    if (text === '') {
      resetSearch();
      return;
    }
    if (local.lastSearch === text) return;

    local.lastSearch = text;

    local.peopleHelper.Search(text, function (info) {
      displaySearchResults(info, false, true);
    }, getUsedIds());
  };

  function getUsedIds() {
    return $.map($('.VoteHost'), function (item) {
      return $(item).data('person-id');
    });
  };

  function resetSearch() {
    local.lastSearch = '';
    local.inputField.val('');
    displaySearchResults({
      People: [],
      MoreFound: ''
    }, true, false);
  };

  function ballotClick(ev) {
    var ballotId = $(ev.target).closest('div').attr('id');
    loadBallot(ballotId);
  };

  function loadBallot(ballotId, refresh, successMsg) {
    if (ballotId.substr(0, 1) === 'B') {
      ballotId = ballotId.substr(1);
    }
    CallAjaxHandler(publicInterface.controllerUrl + '/SwitchToBallot', { ballotId: ballotId, refresh: refresh || false }, function (info) {
      if (refresh) {
        startToRefreshBallotList(successMsg);
      }
      showBallot(info);
    });
  };

  function nameClick(ev) {
    var el = $(ev.target).closest('li');
    var nameId = el.attr('id');
    $.each(local.People, function (i, item) {
      if ('P' + item.Id === nameId) {
        local.rowSelected = i;
        return false;
      }
      return true;
    });

    edit(el);
  };

  publicInterface = {
    peopleUrl: '',
    controllerUrl: '',
    invalidReasons: [],
    BallotInfo: null,
    Ballots: null,
    BallotStatus: [],
    Location: null,
    HasLocations: false,
    PreparePage: preparePage,
    peopleHelper: function () { return local.peopleHelper; },
    local: local
  };

  return publicInterface;
};

var ballotPage = BallotSinglePageFunc();

$(function () {
  ballotPage.PreparePage();
});