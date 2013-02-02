/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7.1.js" />
/// <reference path="../../Scripts/jquery-ui-1.8.16.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />

var BallotNormalPageFunc = function () {
  var temp1 = '{StatusCodeText}{BallotStatusText} <span class="SpoiledCount HideZero{SpoiledCount}"> ({SpoiledCount} spoiled)<span>';
  var local = {
    People: [],
    peopleHelper: null,
    keyTimer: null,
    keyTime: 300,
    lastSearch: '',
    actionTag: null,
    inputField: null,
    nameList: null,
    searchPanel: null,
    ballotsPanel: null,
    btnDeleteBallot: null,
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
    searchResultTemplate: '<li id=P{Id}{^IneligibleData}>{^Name}</li>',
    ballotListDetailTemplate: temp1,
    ballotListTemplate: '<div id=B{Id}>{Code} - <span id=BallotStatus{Id}>' + temp1 + '</span></div>',
  };
  var tabNum = {};

  var preparePage = function () {
    tabNum = publicInterface.HasLocations ? {
      ballot: 2,
      ballots: 1,
      location: 0
    } : {
      ballot: 1,
      ballots: 0,
    };

    local.peopleHelper = new PeopleHelper(publicInterface.peopleUrl);
    local.peopleHelper.Prepare();

    local.inputField = $('#txtSearch').live('keyup paste', runSearch).focus();
    local.actionTag = $('#action');
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

    local.votesList.sortable({
      handle: '.ui-icon-arrow-2-n-s',
      items: '.VoteHost',
      axis: 'y',
      containment: 'parent',
      tolerance: 'pointer',
      stop: orderChanged
    });

    local.tabList = $('#tabs');
    local.tabList.tabs({
      show: function (event, ui) {
        switch (ui.index) {
          case tabNum.ballot:
            local.inputField.focus().select();
            break;
        }
      }
    });

    local.btnDeleteBallot = $('#btnDeleteBallot');
    local.btnDeleteBallot.on('click', deleteBallot);

    $('#btnRefreshBallotCount').on('click', changeLocationStatus);
    $('#btnRefreshBallotList').on('click', startToRefreshBallotList);

    $('#btnNewBallot').on('click', newBallot);
    $('#btnNewBallot2').on('click', newBallot);

    $('#btnAddMissing').on('click', addMissing);
    $('#btnCancelAddMissing').on('click', cancelAddMissing);

    $('#cbReview').on('click, change', cbReviewChanged);

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

    site.onbroadcast(site.broadcastCode.personSaved, personSaved);

    showBallot(publicInterface);
  };

  var orderChanged = function (ev, ui) {
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
    LogMessage(ids);
    ShowStatusDisplay("Saving...");
    CallAjaxHandler(publicInterface.controllerUrl + '/SortVotes', form, function (info) {
      if (info) {
        // no need to update client with new order
        ShowStatusSuccess("Saved");
        // update to reflect changes
        $.each(toUpdate, function (i, o) {
          o.text(i + 1);
        });
        showExtraVotes();
      }
    });

  };

  var newBallot = function () {
    // disable on click...
    $('.NewBallotBtns').prop('disabled', true);

    CallAjaxHandler(publicInterface.controllerUrl + "/NewBallot", null, function (info) {
      showBallot(info);
      local.tabList.tabs('option', 'active', tabNum.ballot);
      local.inputField.focus().val('').change();
      local.nameList.html('');
      $('.NewBallotBtns').prop('disabled', false);
    });
  };

  var focusOnTextInput = function () {
    local.inputField.focus().select();
  };

  var startToRefreshBallotList = function () {
    CallAjaxHandler(publicInterface.controllerUrl + '/RefreshBallotsList', null, function (info) {
      showBallots(info);
      highlightBallotInList();
      ShowStatusSuccess('Updated');
    });
  };

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

  var addMissing = function () {
    toggleAddMissingPanel();
    site.broadcast(site.broadcastCode.startNewPerson, '#addMissingPanel');
  };

  var cancelAddMissing = function () {
    if ($('#addMissingPanel').is(':visible')) {
      toggleAddMissingPanel();
    }
  };

  var personSaved = function (ev, info) {
    local.lastSearch = ''; // force a reload
    runSearch();
    toggleAddMissingPanel();

    var person = info.Person;

    local.votes.push({
      vid: 0,
      pid: person.C_RowId,
      name: person.C_FullName,
      count: 0,
      ineligible: person.IneligibleReasonGuid
    });

    showVotes();

    var newHost = local.votesList.find('.VoteHost').last();

    startSavingVote(newHost);
  };

  var toggleAddMissingPanel = function () {
    $('#votesPanel, #addMissingPanel').toggle();
  };

  var showBallot = function (info) {
    if (info.Ballots) {
      showBallots(info.Ballots);
    }

    var ballotInfo = info.BallotInfo;
    if (ballotInfo) {
      local.tabList.tabs('enable', tabNum.ballot);
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

      local.tabList.tabs('option', 'active', ballot.StatusCode == 'TooFew' ? tabNum.ballot : tabNum.ballots);

      highlightBallotInList();

    } else {
      $('.ballotCode').text('');

      $('#votesPanel').css('visibility', 'hidden');
      local.tabList.tabs('option', 'active', tabNum.ballots);
      local.tabList.tabs('disable', tabNum.ballot);
      local.btnDeleteBallot.prop('disabled', true);
    }

    if (info.Location) {
      showLocation(info.Location);
      publicInterface.Location = info.Location;
    }

  };

  var showVotes = function () {
    var votes = extendVotes(local.votes);

    cancelAddMissing();

    local.votesList.html(site.templates.NormalVoteLine.filledWithEach(votes));
    local.votesList.find('select:visible').each(function () {
      var select = $(this);
      select.val(select.data('invalid'));
    });

    showExtraVotes();

    findAndMarkDups(local.votesList.find('.VoteHost'));

    local.btnDeleteBallot.prop('disabled', votes.length > 0);
  };

  //        var showTempBallotStatusAndDups = function () {
  //            var votes = local.votesList.find('.VoteHost');
  //            var votesDiff = local.votesNeeded - votes.length;
  //            var newStatus = 'Ok';

  //            if (findAndMarkDups(votes)) {
  //                newStatus = 'Dup';
  //                // want to show dups even if TooMany or TooFew
  //            }

  //            if (votesDiff < 0) {
  //                newStatus = 'TooMany';
  //            }
  //            else if (votesDiff > 0) {
  //                newStatus = 'TooFew';
  //            }

  //            setBallotStatus(newStatus, null, false, 0);
  //        };

  var findAndMarkDups = function (votes) {
    var found = false;
    var dups = {};
    var list = [];
    local.votesList.remove('.Duplicate');
    votes.each(function () {
      vote = $(this);
      vote.removeClass('duplicateVote');
      var thisPerson = vote.data('person-id');
      if (thisPerson && dups[thisPerson]) {
        dups[thisPerson].push(vote);

        if ($.inArray(thisPerson, list) == -1) {
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
          var vote = dupVotes[j];
          vote.addClass('duplicateVote');
          vote.children().eq(0).after('<span class=Duplicate>Duplicate:</span>');
        }
      }
    }
    return found;
  };

  //    var setBallotStatus = function (status, display, fromServer) {
  //        local.ballotStatus = status;
  //        if (!display) {
  //            var matched = $.grep(publicInterface.BallotStatus, function () {
  //                return this.v == status;
  //            });
  //            display = matched.length == 1 ? matched[0].d : '??';
  //        }
  //        if (fromServer) {
  //            $('#cbReview').attr('checked', status == 'Review');
  //        }

  //        // {"Id":745,"Code":"A1","StatusCode":"Ok","StatusCodeText":"Ok","SpoiledCount":3}
  //        $('#B' + local.ballotId).html(local.ballotListTemplate.filledWith({ Id: local.ballotId }));

  //        var statusDisplay = $('.ballotStatus');
  //        statusDisplay.html(display);

  //        if (status == 'Ok') {
  //            statusDisplay.removeClass('InvalidBallot');
  //            statusDisplay.addClass('Ok');
  //        } else {
  //            statusDisplay.removeClass('Ok');
  //            statusDisplay.addClass('InvalidBallot');
  //        }
  //        if (fromServer) {
  //            statusDisplay.addClass('Confirmed');
  //            statusDisplay.removeClass('NotConfirmed');
  //        }
  //        else {
  //            statusDisplay.removeClass('Confirmed');
  //            statusDisplay.addClass('NotConfirmed');
  //        }
  //    };

  var updateStatusInList = function (info) {
    $('#BallotStatus' + local.ballotId).html(local.ballotListDetailTemplate.filledWith(info));
  };

  var updateStatusDisplay = function (info) {
    //  info = { "BallotStatus": "TooFew", "BallotStatusText": "Too Few", "SpoiledCount": 0 };

    if (info.StatusCode) {
      // backward compatibilty... convert values
      info.BallotStatus = info.StatusCode;
      info.BallotStatusText = info.StatusCodeText;
    }

    $('#cbReview').attr('checked', info.BallotStatus == 'Review');

    var status = info.BallotStatus;

    var topDisplay = $('.ballotStatus');

    topDisplay.html(info.BallotStatusText);

    if (status == 'Ok') {
      topDisplay.removeClass('InvalidBallot');
      topDisplay.addClass('Ok');
    } else {
      topDisplay.removeClass('Ok');
      topDisplay.addClass('InvalidBallot');
    }
  };

  var cbReviewChanged = function () {
    var checked = $('#cbReview').prop('checked');
    //var isReview = local.ballotStatus == 'Review';

    //if (checked != isReview) {
    ShowStatusDisplay('Saving');

    CallAjaxHandler(publicInterface.controllerUrl + '/NeedsReview', { needs: checked }, function (info) {
      updateStatusDisplay(info);
      updateStatusInList(info);

      ShowStatusSuccess('Saved');
    });

    //}
  };

  var showLocation = function (location) {
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
    var html, title;
    if (remainingToEnter == 0) {
      title = ': All entered';
    } else if (remainingToEnter < 0) {
      title = ': {0} too many'.filledWith(0 - remainingToEnter);
    }
    else {
      title = ': {0} more'.filledWith(remainingToEnter);
    }

    $('#collectedVsEnteredTitle').text(title);
    $('#lblNumEntered').text(numEntered || 0);
  };

  var showBallots = function (info) {
    var list = info.Ballots;

    $('#ballotList')
        .html(local.ballotListTemplate.filledWithEach(list));

    showBallotCount(list.length);

    local.lastBallotRowVersion = info.Last;
  };

  var highlightBallotInList = function () {
    $('#ballotList').children().removeClass('selected').end().find('#B{0}'.filledWith(local.ballotId)).addClass('selected');
  };

  var invalidReasonChanged = function (ev) {
    var select = $(ev.target);
    var reason = select.val();
    if (reason == '0') {
      return;  // don't save with no reason
    }

    select.attr('size', 1);
    var parent = select.parent();
    startSavingVote(parent);
  };

  var resaveVote = function (ev) {
    var host = $(ev.target).parents('.VoteHost');
    startSavingVote(host);
  };

  var startSavingVote = function (host) {
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

    if (invalidId) {
      invalids.data('invalid  ', invalidId);
      for (var i = 0; i < local.votes.length; i++) {
        var vote = local.votes[i];
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

            for (var i = 0; i < local.votes.length; i++) {
              var vote = local.votes[i];
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

        //setBallotStatus(info.BallotStatus, info.BallotStatusText, true, info.SpoiledCount);
        updateStatusDisplay(info);
        updateStatusInList(info);

        local.peopleHelper.RefreshListing(local.inputField.val(), onNamesReady, getUsedIds());

        if (info.BallotStatus == 'Ok') {
          local.tabList.tabs('option', 'active', tabNum.ballots);
          $('#btnNewBallot2').effect('highlight', null, 1500);
        }

        focusOnTextInput();
      }
      else {
        ShowStatusFailed(info.Error);
      }

    });
  };

  var deleteVote = function (ev) {
    var host = $(ev.target).parent();
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

        if (info.BallotStatus == 'Ok') {
          local.tabList.tabs('option','active', tabNum.ballots);
        }

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

  var deleteBallot = function () {
    ShowStatusDisplay('Deleting...');
    CallAjaxHandler(publicInterface.controllerUrl + '/DeleteBallot', null, function (info) {
      if (info.Deleted) {
        ShowStatusSuccess('Deleted');

        showBallot(info);

        if (info.Location) {
          showLocation(info.Location);
        }
      }
      else {
        ShowStatusFailed(info.Message);
      }
    });
  };

  var onNamesReady = function (info, beingRefreshed) {
    local.People = info.People || [];

    local.nameList.html(local.searchResultTemplate.filledWithEach(local.People));
    $('#more').html(''); //info.MoreFound
    if (!local.People.length && local.lastSearch) {
      var search = local.inputField.val();
      if (search) {
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
        var ineligible = $(item).data('ineligible');
        if (ineligible) {
          var desc = getIneligibleReasonDesc(ineligible);
          item.title = 'Ineligible: ' + desc;
        }
      });
    }
    local.actionTag.removeClass('searching');
    local.actionTag.text('');
    local.inputField.removeClass('searching');

    // single:
    local.nameList.children().removeClass('selected');
    local.nameList.children().eq(local.rowSelected).addClass('selected');
  };

  var getIneligibleReasonDesc = function (guid) {
    var matched = $.grep(publicInterface.invalidReasons, function (item, i) {
      return item.Guid == guid;
    });
    if (matched.length == 0) return '';
    return matched[0].Desc;
  };

  var moveSelected = function (delta) {
    var children = local.nameList.children();
    var numChildren = children.length;
    if (children.eq(numChildren - 1).text() == '...') { numChildren--; }

    var rowNum = local.rowSelected;
    rowNum = rowNum + delta;
    var wraparound = false;
    if (wraparound) {
      if (rowNum < 0) { rowNum = numChildren - 1; }
      if (rowNum >= numChildren) { rowNum = 0; }
    }
    else {
      if (rowNum < 0) { rowNum = 0; }
      if (rowNum >= numChildren) { rowNum = numChildren - 1; }
    }
    setSelected(children, rowNum);
  };

  var setSelected = function (children, rowNum) {
    children.removeClass('selected');
    var newSelected = children.eq(local.rowSelected = rowNum);
    newSelected.addClass('selected');
    scrollIntoView(newSelected[0], local.nameList);
  };

  var scrollIntoView = function (element, container) {
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

  var edit = function (selectedPersonLi) {
    local.nameList.children().removeClass('selected');
    selectedPersonLi.addClass('selected');
    addToVotesList(selectedPersonLi);
  };

  var addToVotesList = function (selectedPersonLi) {
    if (!selectedPersonLi.length) return;

    var rawId = selectedPersonLi.attr('id');
    if (!rawId) return;

    var personId = +rawId.substr(1);
    if (personId == 0) return;

    local.votes.push({
      vid: 0,
      pid: personId,
      name: selectedPersonLi.text(),
      count: 0,
      ineligible: selectedPersonLi.data('ineligible')
    });

    showVotes();

    var newHost = local.votesList.find('.VoteHost').last();

    startSavingVote(newHost);
  };

  var addSpoiled = function () {
    LogMessage('spoiled');
    local.votes.push({
      vid: 0,
      count: 0,
      invalid: 0,
      changed: false,
      InvalidReasons: local.invalidReasonsShortHtml
    });


    showVotes(false);

    var newHost = local.votesList.find('.VoteHost').last();
    var input = newHost.find('select');
    input.attr('size', input[0].options.length + input.children().length - 1);

    // vote not saved until a reason is chosen
    input.focus();
  };

  var extendVotes = function (votes) {
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
          return item.Guid == vote.ineligible;
        });
        var reason = 'Ineligible';
        if (reasonList.length == 1) {
          reason = reasonList[0].Desc;
        }
        //this.Display = '<span class=InvalidName>{name}</span>'.filledWith(this); // ' &nbsp; <span class=Ineligible>{0}</span>'.filledWith(reason, this.name);
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

  var showExtraVotes = function () {
    var votes = local.votesList.find('.VoteHost, .VoteHostFake');
    var num = 0;
    var extra = local.votesNeeded + 1;
    votes.each(function (i, o) {
      var host = $(o);
      num++;
      host.removeClass('ExtraVote');
      host.removeClass('ExtraVotes');
      if (num == extra) {
        host.addClass('ExtraVote');
      }
      else if (num > extra) {
        host.addClass('ExtraVotes');
      }
    });

    var missing = local.votesNeeded - votes.length;
    if (missing) {
      var emptyVote = { pos: '-', Fake: 'Fake' };
      for (var i = 0; i < missing; i++) {
        local.votesList.append(site.templates.NormalVoteLine.filledWith(emptyVote));
      }
    }

  };

  var prepareReasons = function (onlyGroup) {
    var html = ['<option value="0">Select a reason...</option>'];
    var group = '';
    $.each(publicInterface.invalidReasons, function () {
      var reasonGroup = this.Group;
      if (onlyGroup && reasonGroup != onlyGroup) {
        return;
      }
      if (reasonGroup != group) {
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

  var navigating = function (ev) {
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
        local.inputField.val('');
        runSearch();
        return true;

      default:
        LogMessage(ev.which);
        break;
    }
    return false;
  };

  var runSearch = function (ev) {
    clearTimeout(local.keyTimer);
    var input = local.inputField;
    var text = input.val();
    if (ev && navigating(ev)) {
      return;
    }
    if (local.lastSearch === text.trim()) return;
    if (text == '') {
      resetSearch();
      return;
    }
    local.actionTag.html('');
    local.actionTag.addClass('delaying');
    input.addClass('delaying');

    local.keyTimer = setTimeout(function () {
      local.lastSearch = text;

      local.actionTag.removeClass('delaying');
      input.removeClass('delaying');

      local.actionTag.addClass('searching');
      local.actionTag.text('Searching...');
      input.addClass('searching');

      local.peopleHelper.SearchNames(text, onNamesReady, true, getUsedIds(), true);
    }, local.keyTime);
  };

  var getUsedIds = function () {
    return $.map($('.VoteHost'), function (item) {
      return $(item).data('person-id');
    });
  };

  var resetSearch = function () {
    local.lastSearch = '';
    onNamesReady({
      People: [],
      MoreFound: ''
    }, false);
  };
  var ballotClick = function (ev) {
    var el = ev.target;
    while (el.tagName != 'DIV') {
      el = el.parentNode;
      if (el == null) return;
    }
    loadBallot(el.id);
  };

  var loadBallot = function (ballotId) {
    if (ballotId.substr(0, 1) == 'B') {
      ballotId = ballotId.substr(1);
    }
    CallAjaxHandler(publicInterface.controllerUrl + '/SwitchToBallot', { ballotId: ballotId }, showBallot);
  };

  var nameClick = function (ev) {
    var el = ev.target;
    while (el.tagName != 'LI') {
      el = el.parentNode;
      if (el == null) return;
    }
    $.each(local.People, function (i, item) {
      if ('P' + item.Id == el.id) {
        local.rowSelected = i;
        return false;
      }
    });

    edit($(el));
  };
  var publicInterface = {
    peopleUrl: '',
    controllerUrl: '',
    invalidReasons: [],
    BallotInfo: null,
    Ballots: null,
    BallotStatus: [],
    Location: null,
    HasLocations: false,
    PreparePage: preparePage,
    local: local
  };

  return publicInterface;
};

var ballotNormalPage = BallotNormalPageFunc();

$(function () {
  ballotNormalPage.PreparePage();
});