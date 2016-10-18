var BallotSinglePageFunc = function () {
  var temp1 = '{StatusCodeText}{BallotStatusText} <span class="SpoiledCount HideZero{SpoiledCount}"> ({SpoiledCount} spoiled)<span>';
  var local = {
    People: [],
    peopleHelper: null,
    keyTimer: null,
    keyTime: 1200,
    lastSearch: '',
    //    actionTag: null,
    inputField: null,
    lastKey: null,
    nameList: null,
    searchPanel: null,
    ballotsPanel: null,
    //btnDeleteBallot: null,
    location: {},
    votesNeeded: 0,
    ballotStatus: '',
    ballotId: 0,
    votes: [],
    votesList: null,
    tabList: null,
    invalidReasonsHtml: null,
    invalidReasonsShortHtml: null,
    rowSelected: 0,
    lastBallotRowVersion: 0,
    keyTimeShowSpan: null,
    searchResultTemplate: '<li id=P{Id}{^Classes}{^IneligibleData}>{^HtmlName}</li>',
    ballotListDetailTemplate: temp1,
    ballotListTemplate: '<div id=B{Id}>Computer {Code}</div>'
  };
  var tabNum = {
    ballotList: 0,
    ballot: 1
  };

  var preparePage = function () {
    local.peopleHelper = new PeopleHelper(publicInterface.peopleUrl);
    local.peopleHelper.Prepare();

    local.inputField = $('#txtSearch').on('keyup paste', searchTextChanged);
    local.inputField.focus();
    $(document).on('keydown', function (ev) {
      if ($(ev.target).is(':text')) {
        return;
      }
      switch (ev.which) {
        case 8:
          ev.preventDefault();
          return;
      }
    });

    local.keyTimeShowSpan = $("#keyTimeShow"),
    local.nameList = $('#nameList');
    local.searchPanel = $('#nameSearch');
    local.ballotsPanel = $('#ballots');
    local.votesList = $('#votesList');

    local.nameList.on('click', 'li', nameClick);
    $('#ballotList').on('click', 'div', ballotClick);

    $('#btnAddSpoiled').on('click', addSpoiled);

    local.votesList.on('click', '.ui-icon-trash', deleteVote);
    local.votesList.on('click', '.btnClearChangeError', resaveVote);
    local.votesList.on('change', 'select', invalidReasonChanged);

    local.votesList.on('change keyup', 'input', voteNumChange);

    local.votesList.sortable({
      handle: '.ui-icon-arrow-2-n-s',
      items: '.VoteHost',
      axis: 'y',
      containment: 'parent',
      tolerance: 'pointer',
      stop: orderChanged
    });

    local.tabList = $('#tabs');
    local.tabList.tabs();

    local.tabList.tabs('option', 'active', tabNum.ballot);

    $('#btnAddMissing').click(addMissing);
    $('#btnCancelAddMissing').click(cancelAddMissing);

    $('#txtContact').on('change', function () {
      CallAjaxHandler(publicInterface.controllerUrl + '/UpdateLocationInfo', { info: $(this).val() }, function () {
        ShowStatusSuccess('Updated');
      });
    });
    $('#txtNumCollected').on('change', function () {
      var num = Math.max(0, +$(this).val());
      ShowStatusDisplay('Saving...');
      CallAjaxHandler(publicInterface.controllerUrl + '/UpdateLocationCollected', { numCollected: num }, function (info) {
        if (info.Location) {
          showLocation(info.Location);
        }
        ShowStatusSuccess('Saved');
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
      changeLocation();
    });

    showBallot(publicInterface);

    $('#votesPanel, .div1').show();
  };

  function changeLocation() {
    ShowStatusDisplay('Loading location...');
    CallAjaxHandler(publicInterface.controllerUrl + '/GetLocationInfo', null, function (info) {
      if (info.Message) {
        ShowStatusFailed(info.Message);
        return;
      }
      showLocation(info.Location);
      showBallot(info);
    });
  };

  var focusOnTextInput = function () {
    local.inputField.focus().select();
  };

  function orderChanged(ev, ui) {
    var ids = [];
    var pos = 1;
    var toUpdate = [];

    local.votesList.children().each(function () {
      var voteHost = $(this);
      var id = +voteHost.data('vote-id');
      if (id > 0) {
        ids.push(id);
        toUpdate.push(voteHost.find('.VoteNum'));
      }
      pos++;
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

  //  var startToRefreshBallotList = function () {
  //    CallAjaxHandler(publicInterface.controllerUrl + '/RefreshBallotsList', null, function (info) {
  //      showBallots(info);
  //      highlightBallotInList();
  //      ShowStatusSuccess('Updated');
  //    });
  //  };

  var changeLocationStatus = function () {
    var form = {
      id: publicInterface.Location.Id,
      status: $('#ddlLocationStatus').val()
    };
    CallAjaxHandler(publicInterface.controllerUrl + '/UpdateLocationStatus', form, function (info) {
      if (info.Location) {
        showLocation(info.Location);
        $('span[data-location]').each(function () {
          var span = $(this);
          if (span.data('location') == info.Location.Id) {
            span.text(info.Location.TallyStatus);
          }
        });
      }
      ShowStatusSuccess('Updated');
    });
  };

  function addMissing() {
    toggleAddMissingPanel();

    var ddl = $('#ddlIneligible');
    ddl[0].size = ddl.find('option, optgroup').length + 1;

    var searchParts = local.inputField.val().split(' ');
    var capitalized = function (s) {
      if (!s) return '';
      return s.substr(0, 1).toUpperCase() + s.substr(1);
    };
    var first = searchParts.shift();
    var last = searchParts.join(' ');
    site.broadcast(site.broadcastCode.startNewPerson, {
      panelSelector: '#addMissingPanel',
      ineligible: 'ce27534d-d7e8-e011-a095-002269c41d11', // Unidentifiable_Unknown_person
      first: capitalized(first),
      last: capitalized(last)
    });
  };

  function cancelAddMissing() {
    if ($('#addMissingPanel').is(':visible')) {
      toggleAddMissingPanel();
    }
  };

  function newPersonSaved(ev, info) {
    local.lastSearch = ''; // force a reload
    searchTextChanged();
    toggleAddMissingPanel();

    var person = info.Person;

    local.votes.push({
      vid: 0,
      pid: person.C_RowId,
      name: person.C_FullName,
      count: 1,
      ineligible: person.CanReceiveVotes ? null : person.IneligibleReasonGuid
    });

    showVotes();

    var newHost = local.votesList.find('.VoteHost').last();

    startSavingVote(newHost, false);
  };

  function toggleAddMissingPanel() {
    $('#votesPanel, #addMissingPanel').toggle();
  };

  function showBallot(info) {
    local.votesList.scrollTop(0);

    if (info.Ballots) {
      showBallots(info.Ballots);
    }

    var ballotInfo = info.BallotInfo;
    if (ballotInfo) {
      $('#votesPanel').css('visibility', 'visible');

      var ballot = ballotInfo.Ballot;
      $('.ballotCode').text(ballot.Code);
      $('#ballotStatus').text(ballot.StatusCode);

      local.votesNeeded = ballotInfo.NumNeeded;
      local.ballotStatus = ballot.StatusCode;
      local.votes = ballotInfo.Votes;
      local.ballotId = ballot.Id;

      showVotes();

      updateStatusDisplay(ballot);

      highlightBallotInList();

    } else {
      $('.ballotCode').text('');

      $('#votesPanel').css('visibility', 'hidden');
      $('#votesList').html('');
    }

    if (info.Location) {
      showLocation(info.Location);
      publicInterface.Location = info.Location;
    }

  };

  function showVotes() {
    var votes = extendVotes(local.votes);

    cancelAddMissing();

    //var scroll = local.votesList.scrollTop();

    local.votesList.html(site.templates.SingleVoteLine.filledWithEach(votes));
    local.votesList.find('select:visible').each(function () {
      var select = $(this);
      select.val(select.data('invalid'));
    });

    //    local.votesList.scrollTop(scroll);
  };

  function scrollVotesTo(offset) {
    $('#votersList').scrollTop(offset);
  };

  var updateStatusInList = function (info) {
    $('#BallotStatus' + local.ballotId).html(local.ballotListDetailTemplate.filledWith(info));
  };

  function updateStatusDisplay(info) {

    if (info.StatusCode) {
      // backward compatibilty... convert values
      info.BallotStatus = info.StatusCode;
      info.BallotStatusText = info.StatusCodeText;
    }

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
    $('.locationInfo').find('[data-name]').each(function () {
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

    showBallotCount(location.BallotsEntered);
  };

  var showBallotCount = function (numEntered) {
    var remainingToEnter = (local.location.BallotsCollected || 0) - (numEntered || 0);
    var title;
    if (remainingToEnter == 0) {
      title = 'All entered';
    } else if (remainingToEnter < 0) {
      title = '{0} more than received'.filledWith(0 - remainingToEnter);
    }
    else {
      title = '{0} more to enter'.filledWith(remainingToEnter);
    }

    $('#collectedVsEntered').text(title ? ' (' + title + ')' : '');
    $('#numEntered').text(numEntered || 0);
  };

  function showBallots(info) {
    var list = info.Ballots;

    $('#ballotList').html(local.ballotListTemplate.filledWithEach(list));

    var count = list.length;
    $('#tabBallotTitle').text('{0} Teller Computer{1}'.filledWith(count, Plural(count)));

    if (info.LocationBallotsEntered) {
      showBallotCount(info.LocationBallotsEntered);
    }

    local.lastBallotRowVersion = info.Last;
  };

  function highlightBallotInList() {
    var ballotList = $('#ballotList');
    var highlighted = ballotList.children().removeClass('selected').end().find('#B{0}'.filledWith(local.ballotId)).addClass('selected');
    scrollIntoView(highlighted, ballotList);
  };

  var voteNumChange = function (ev) {
    var input = $(ev.target);
    var saveNow = ev.type === 'change';
    var focusOnNew = false;
    var changing = true;
    //    LogMessage(ev.type);
    //    LogMessage(ev.which);


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
      startSavingVote(input.parent(), focusOnNew);
    } else if (changing) {
      //      input.addClass('changing');
    }
  };

  var removedParent; // seems to be triggering twice!
  var invalidReasonChanged = function (ev) {
    if (ev.target.selectedIndex == -1) {
      return; //nothing selected
    }
    var select = $(ev.target);
    var reason = select.val();
    if (reason == '0') {
      return;  // don't save with no reason
    }

    select.attr('size', 1);
    var parent = select.closest('.VoteHost');

    if (reason == '-1') {
      // remove this one

      if (!removedParent) {
        removedParent = parent;
        parent.remove();
      } else {
        removedParent = null;
        return;
      }

      var voteId = parent.data('vote-id') || 0;
      for (var i = 0; i < local.votes.length; i++) {
        var vote = local.votes[i];
        if (vote.vid == voteId) {
          local.votes.splice(i, 1);
          break;
        }
      }
      if (voteId != 0) {
        var form = {
          vid: voteId
        };
        CallAjaxHandler(publicInterface.controllerUrl + '/DeleteVote', form, function (info) {
          // if failed, user will have to manually remove it
        });
      }

      // add the new one
      addMissing();
      return;
    }

    startSavingVote(parent);
  };

  function resaveVote(ev) {
    var host = $(ev.target).parents('.VoteHost');
    startSavingVote(host);
  };

  function startSavingVote(host) {
    if ($('#ddlTopLocation').val() == -1) {
      ShowStatusFailed('Must select your location first!');
      return;
    }


    var input = host.find('input');
    var invalids = host.find('select:visible');
    var invalidId = invalids.val() || '';
    var voteId = +host.data('vote-id') || 0;

    var form = {
      pid: host.data('person-id') || 0,
      vid: voteId,
      invalid: invalidId,
      count: input.val() || 0
    };

    if (isNaN(form.count) || +form.count < 0) {
      alert('Invalid number. Please correct.');
      return;
    }
    var i, max, vote;

    if (form.vid) {
      // previously saved
      for (i = 0, max = local.votes.length; i < max; i++) {
        vote = local.votes[i];
        if (vote.vid == form.vid) {
          if (vote.count === +form.count && vote.invalid === invalidId) {
            host.removeClass('Changedtrue').addClass('Changedfalse');
            input.removeClass('changing');
//            if (focusOnNew) {
//              focusOnTextInput();
//            }
            return;
          }
          break;
        }
      }
    }

    if (invalidId) {
      invalids.data('invalid', invalidId);
      for (i = 0; i < local.votes.length; i++) {
        vote = local.votes[i];
        if (vote.vid == voteId) {
          vote.invalid = invalidId;
        }
      }
    }

    if (isNaN(form.count) || +form.count < 0) {
      alert('Invalid number. Please correct.');
      input.focus();
      return;
    }

    ShowStatusDisplay('Saving...');
    input.focus();

    CallAjaxHandler(publicInterface.controllerUrl + '/SaveVote', form, function (info) {
      var i, vote;

      if (info.Updated) {
        ShowStatusSuccess('Saved');
        // assume any error was removed
        host.removeClass('Changedtrue').addClass('Changedfalse');

        if (!publicInterface.Location) {
          location.href = location.href;
          //TODO: use Ajax to reload the content?
          return;
        }


        if (info.Location) {
          showLocation(info.Location);
        }

        if (form.vid == 0) {
          if (info.VoteId) {
            host.data('vote-id', info.VoteId);
            host.attr('id', 'V' + info.VoteId);
            host.find('.VoteNum').text(info.pos);

            for (i = 0; i < local.votes.length; i++) {
              vote = local.votes[i];
              if (vote.vid == 0) {
                vote.vid = info.VoteId;
                vote.pos = info.pos;
              }
            }
          }
          else {
            ShowStatusFailed('Error on save. Please reload this page.');
          }
        }

        scrollToVote(host, info.pos);
        //setBallotStatus(info.BallotStatus, info.BallotStatusText, true, info.SpoiledCount);
        updateStatusDisplay(info);
        updateStatusInList(info);

        local.peopleHelper.RefreshListing(local.inputField.val(), onNamesReady, getUsedIds());

        showBallotCount(info.LocationBallotsEntered);

        //        if (info.BallotStatus == 'Ok') {
        //local.tabList.tabs('option', 'active', tabNum.ballots);
        //$('#btnNewBallot2').effect('highlight', null, 1500);
        //        }
//        if (focusOnNew) {
//          focusOnTextInput();
//        }
      } else {
        ShowStatusFailed(info.Message);
      }

    });
  };

  function scrollToVote(host, num) {
    var parent = host.parent();
    var size = host.outerHeight();

    var newScroll = num * size;
    if (newScroll > parent.height() - 2 * size) {
      parent.scrollTop(newScroll);
    } else {
      parent.scrollTop(0);
    }
  };
  function deleteVote(ev) {
    var host = $(ev.target).parents('.VoteHost');
    var voteId = host.data('vote-id') || 0;
    var form = {
      vid: voteId
    };

    if (voteId == 0) {
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

        updateStatusDisplay(info);
        updateStatusInList(info);

        showBallotCount(info.LocationBallotsEntered);

        //        if (info.BallotStatus == 'Ok') {
        //          local.tabList.tabs('option', 'active', tabNum.ballots);
        //        }

        if (info.Location) {
          showLocation(info.Location);
        }
        local.peopleHelper.RefreshListing(local.inputField.val(), onNamesReady, getUsedIds());
      }
      else {
        ShowStatusFailed(info.Message);
      }
    });
  };

  //  var deleteBallot = function () {
  //    ShowStatusDisplay('Deleting...');
  //    CallAjaxHandler(publicInterface.controllerUrl + '/DeleteBallot', null, function (info) {
  //      if (info.Deleted) {
  //        ShowStatusSuccess('Deleted');
  //
  //        showBallot(info);
  //
  //        if (info.Location) {
  //          showLocation(info.Location);
  //        }
  //      }
  //      else {
  //        ShowStatusFailed(info.Message);
  //      }
  //    });
  //  };

  function onNamesReady(info, beingRefreshed, fromQuickSearch) {
    local.People = info.People || [];

    if (!fromQuickSearch) {
      resetKeyTimeShow();
      //local.peopleHelper.AddGroupToLocalNames(local.People);
    }

    local.nameList.html(local.searchResultTemplate.filledWithEach(local.People));
    $('#more').html(''); //info.MoreFound
    if (!local.People.length && local.lastSearch) {
      var search = local.inputField.val();
      if (search && !fromQuickSearch) {
        local.nameList.append('<li>...no matches found...</li>');
      }
    } else {
      if (info.MoreFound && local.lastSearch) {
        local.nameList.append('<li title="Enter more of the name to limit how many are matched!">...more matched...</li>');
      }
      if (beingRefreshed) {

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
    //    local.actionTag.removeClass('searching');
    //    local.actionTag.text('');
    //    local.inputField.removeClass('searching');

    // single:
    //    local.nameList.children().removeClass('selected');
    //    LogMessage(local.rowSelected);
    //    local.nameList.children().eq(local.rowSelected).addClass('selected');
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
    //    if (wraparound) {
    //      if (rowNum < 0) { rowNum = numChildren - 1; }
    //      if (rowNum >= numChildren) { rowNum = 0; }
    //    }
    //    else {
    if (rowNum < 0) { rowNum = 0; }
    if (rowNum >= numChildren) { rowNum = numChildren - 1; }
    //    }
    setSelected(children, rowNum);
  };

  function setSelected(children, rowNum) {
    children.removeClass('selected');
    var newSelected = children.eq(local.rowSelected = rowNum);
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
      $(container).scrollTop(elemBottom - $(container).height() + 10);
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

    focusOnTextInput();

    local.votes.push({
      vid: 0,
      pid: personId,
      name: selectedPersonLi.text(),
      count: 0,
      ineligible: selectedPersonLi.data('ineligible')
    });

    //local.peopleHelper.AddToLocalNames(personId);

    showVotes();
    scrollVotesTo(9999);

    var newHost = local.votesList.find('.VoteHost').last();

    startSavingVote(newHost);
  };

  function addSpoiled() {
    //    LogMessage('spoiled');
    local.votes.push({
      vid: 0,
      count: 0,
      invalid: 0,
      changed: false,
      InvalidReasons: local.invalidReasonsShortHtml
    });

    showVotes(false);
    scrollVotesTo(9999);

    var newHost = local.votesList.find('.VoteHost').last();
    var input = newHost.find('select');
    input.attr('size', input[0].options.length + input.children().length - 1);

    // vote not saved until a reason is chosen
    input.focus();
  };

  function extendVotes(votes) {
    var num = 0;
    $.each(votes, function () {
      if (this.invalid && this.invalid !== null) {
        this.InvalidReasons = local.invalidReasonsShortHtml;
        this.invalidType = 'C';
      }
      if (this.ineligible && this.ineligible !== null) {
        // person is invalid!

        var vote = this;
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

      num++;
    });
    return votes;
  };

  function prepareReasons(onlyGroup) {
    var html = [
          '<option value="0">Select a reason...</option>',
          '<optgroup label="Name not in the List">',
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
        clearTimeout(local.keyTimer);
        resetKeyTimeShow();
        if (local.lastKey === 27) {
          // pressed esc twice - clear inputs
          local.inputField.val('');
          searchTextChanged();
        }
        return true;

      default:
        //        LogMessage(ev.which);
        break;
    }
    return false;
  };
  function resetKeyTimeShow() {
    local.keyTimeShowSpan
      .stop(true, true)
      .removeClass('searching')
      .css({ height: 0 });
  };
  function searchTextChanged(ev) {
    clearTimeout(local.keyTimer);
    resetKeyTimeShow();
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

    local.keyTimeShowSpan
      .animate({
        height: 25
      }, {
        duration: local.keyTime,
        queue: false,
        start: resetKeyTimeShow,
        complete: function () {
          local.keyTimeShowSpan.addClass('searching');
        }
      });

    local.peopleHelper.QuickSearch(text, function (info) {
      onNamesReady(info, false, true);
    }, getUsedIds());

    local.keyTimer = setTimeout(function () {
      local.lastSearch = text;
      local.peopleHelper.SearchNames(text, onNamesReady, true, getUsedIds(), true);
    }, local.keyTime);
  };

  function getUsedIds() {
    return $.map($('.VoteHost'), function (item) {
      return $(item).data('person-id');
    });
  };

  function resetSearch() {
    local.lastSearch = '';
    local.inputField.val('');
    onNamesReady({
      People: [],
      MoreFound: ''
    }, false);
  };

  function ballotClick(ev) {
    var ballotId = $(ev.target).closest('div').attr('id');
    loadBallot(ballotId);
  };

  function loadBallot(ballotId, refresh) {
    if (ballotId.substr(0, 1) === 'B') {
      ballotId = ballotId.substr(1);
    }
    CallAjaxHandler(publicInterface.controllerUrl + '/SwitchToBallot', { ballotId: ballotId, refresh: refresh || false }, function (info) {
      if (refresh) {
        startToRefreshBallotList();
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
    Location: null,
    PreparePage: preparePage,
    local: local
  };

  return publicInterface;
};

var ballotSinglePage = BallotSinglePageFunc();

$(function () {
  ballotSinglePage.PreparePage();
});