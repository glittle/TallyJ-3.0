/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />
/// <reference path="../../Scripts/jquery-ui-1.8.16.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />

var BallotNormalPageFunc = function () {
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
        votesNeeded: 0,
        ballotStatus: '',
        votes: [],
        invalidReasonsHtml: null,
        rowSelected: 0,
        lastBallotRowVersion: 0,
        searchResultTemplate: '<li id=P{Id}{IneligibleData}>{Name}</li>',
        ballotListTemplate: '<li id=B{Id}>{Code} (Status) {Location} (<span data-location={LocationId}>{TallyStatus}</span>)</li>'
    };

    var preparePage = function () {
        local.peopleHelper = new PeopleHelper(publicInterface.peopleUrl);
        local.peopleHelper.Prepare();

        local.inputField = $('#txtSearch').live('keyup paste', runSearch).focus();
        local.actionTag = $('#action');
        local.nameList = $('#nameList');
        local.searchPanel = $('#nameSearch');
        local.ballotsPanel = $('#ballots');

        $('#nameList').on('click', 'li', nameClick);
        $('#ballotList').on('click', 'li', ballotClick);

        $('#btnAddSpoiled').on('click', addSpoiled);

        $('#votesList').on('click', '.ui-icon-trash', deleteVote);
        $('#votesList').on('click', '.btnClearChangeError', resaveVote);
        $('#votesList').on('change', 'select', invalidReasonChanged);

        $('#tabs').tabs();

        $('#btnRefreshBallotCount').on('click', changeLocationStatus);
        $('#btnDeleteBallot').on('click', deleteBallot);

        $('#ddlLocationStatus').on('change', changeLocationStatus);
        $('#txtContact').on('change', function () {
            CallAjaxHandler(publicInterface.controllerUrl + '/UpdateLocationInfo', { info: $(this).val() }, function () {
                ShowStatusDisplay('Updated', 0, 3000, false, true);
            });
        });
        $('#txtNumCollected').on('change', function () {
            CallAjaxHandler(publicInterface.controllerUrl + '/UpdateLocationCollected', { numCollected: +$(this).val() }, function (info) {
                if (info.Location) {
                    showLocation(info.Location);
                }
                ShowStatusDisplay('Updated', 0, 3000, false, true);
            });
        });
        resetSearch();

        local.invalidReasonsHtml = prepareReasons();

        showBallot(publicInterface);
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
            ShowStatusDisplay('Updated', 0, 3000, false, true);
        });
    };

    var showBallot = function (info) {

        if (info.BallotsList) {
            showBallots(info.BallotsList);
        }

        var ballotInfo = info.BallotInfo;
        if (ballotInfo) {
            $('.ballotCode').text(ballotInfo.Ballot.Code);
            $('#ballotStatus').text(ballotInfo.Ballot.StatusCode);

            local.votesNeeded = ballotInfo.NumNeeded;
            local.ballotStatus = ballotInfo.Ballot.StatusCode;
            local.votes = ballotInfo.Votes;

            showVotes();

            highlightBallotInList(ballotInfo.Ballot.Id);
        }

        if (info.Location) {
            showLocation(info.Location);
            publicInterface.Location = info.Location;
        }
    };

    var showVotes = function () {
        var list = $('#votesList');

        var votes = extendVotes(local.votes);

        list.html(site.templates.NormalVoteLine.filledWithEach(votes));
        list.find('select:visible').each(function () {
            var select = $(this);
            select.val(select.data('invalid'));
        });

        updateBallotStatus();
    };

    var updateBallotStatus = function () {
        var votes = $('#votesList .VoteHost');
        var votesDiff = local.votesNeeded - votes.length;
        var newStatus = 'Ok';

        if (findAndMarkDups(votes)) {
            newStatus = 'Dup';
            // want to show dups even if TooMany or TooFew
        }

        if (votesDiff < 0) {
            newStatus = 'TooMany';
        }
        else if (votesDiff > 0) {
            newStatus = 'TooFew';
        }

        showBallotStatus(newStatus);
    };

    var findAndMarkDups = function (votes) {
        var found = false;
        var dups = {};
        var list = [];
        votes.each(function () {
            vote = $(this);
            vote.removeClass('duplicateVote');
            var thisPerson = vote.data('person-id');
            if (dups[thisPerson]) {
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
                }
            }
        }
        return found;
    };

    var showBallotStatus = function (status) {
        var display = status;
        switch (status) {
            case 'TooFew': display = 'Too Few'; break;
            case 'TooMany': display = 'Too Many'; break;
            case 'Dup': display = 'Duplicate Names'; break;
        }

        var statusDisplay = $('.ballotStatus');
        statusDisplay.html(display);

        if (status == 'Ok') {
            statusDisplay.removeClass('InvalidBallot');
        } else {
            statusDisplay.addClass('InvalidBallot');
        }
    };

    var showLocation = function (location) {
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
        var remainingToEnter = (location.BallotsCollected || 0) - (location.BallotsEntered || 0);
        var html;
        if (remainingToEnter == 0) {
            html = '<span class=countsGood>All ballots entered</span>';
        } else if (remainingToEnter < 0) {
            html = '<span class=countsBad>{0} too many ballot{1} entered!</span>'.filledWith(0 - remainingToEnter, remainingToEnter == -1 ? '' : 's');
        }
        else {
            html = '<span class=countsNeutral>({0} more to enter)</span>'.filledWith(remainingToEnter);
        }
        $('#collectedVsEntered').html(html);
    };

    var showBallots = function (info) {
        var list = info.Ballots;
        list.sort(function (a, b) {
            if (a.LocationSort == b.LocationSort) {
                return a.Code > b.Code;
            }
            return a.LocationSort > b.LocationSort;
        });

        $('#ballotList')
            .html(local.ballotListTemplate.filledWithEach(list));

        local.lastBallotRowVersion = info.Last;
    };

    var highlightBallotInList = function (ballotId) {
        $('#ballotList').children().removeClass('selected').end().find('#B{0}'.filledWith(ballotId)).addClass('selected');
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
        var form = {
            pid: host.data('person-id') || 0,
            vid: host.data('vote-id') || 0,
            invalid: invalids.val() || 0,
            count: input.val() || 0
        };

        if (isNaN(form.count) || +form.count < 0) {
            alert('Invalid number. Please correct.');
            input.focus();
            return;
        }

        ShowStatusDisplay('Saving...');
        input.focus();

        CallAjaxHandler(publicInterface.controllerUrl + '/SaveVote', form, function (info) {
            if (info.Updated) {
                ShowStatusDisplay('Saved', 0, 3000, false, true);
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

                        for (var i = 0; i < local.votes; i++) {
                            var vote = local.votes[i];
                            if (vote.id == 0) {
                                vote.vid = info.VoteId;
                                vote.pos = info.pos;
                            }
                        }

                        updateBallotStatus();
                    }
                    else {
                        ShowStatusFailed('Error on save. Please reload this page.');
                    }
                }

                local.peopleHelper.RefreshListing(local.inputField.val(), onNamesReady, getUsedIds());
                local.inputField.focus().select();

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
                ShowStatusDisplay('Deleted', 0, 3000, false, true);
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

                local.peopleHelper.RefreshListing(local.inputField.val(), onNamesReady, getUsedIds());
            }
            else {
                ShowStatusFailed(info.Message);
            }
        });
    };

    var deleteBallot = function () {
        if (!confirm('Are you sure you want to delete this ballot?')) {
            return;
        }
        ShowStatusDisplay('Deleting...');
        CallAjaxHandler(publicInterface.controllerUrl + '/DeleteBallot', null, function (info) {
            if (info.Deleted) {
                ShowStatusDisplay('Deleted', 0, 3000, false, true);

                $('.ballotCode').text('___');

                //TODO: show new list of ballots
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
        local.People = info.People;
        local.nameList.html(local.searchResultTemplate.filledWithEach(local.People));
        $('#more').html(''); //info.MoreFound
        if (!local.People.length && local.lastSearch) {
            var search = local.inputField.val();
            if (search) {
                local.nameList.append('<li>...no matches found...</li>');
            }
        } else {
            //local.rowSelected = 0;
            if (info.MoreFound && local.lastSearch) {
                local.nameList.append('<li>...more matched...</li>');
            }
            if (beingRefreshed) {

            } else {
                $.each(local.People, function (i, item) {
                    if (item.BestMatch) {
                        local.rowSelected = i;
                    }
                });
            }
        }
        local.actionTag.removeClass('searching');
        local.actionTag.text('Matches:');
        local.inputField.removeClass('searching');

        // single:
        local.nameList.children().removeClass('selected');
        local.nameList.children().eq(local.rowSelected).addClass('selected');
    };

    var moveSelected = function (delta) {
        var children = local.nameList.children();
        var numChildren = children.length;
        if (children.eq(numChildren - 1).text() == '...') { numChildren--; }

        var rowNum = local.rowSelected;
        rowNum = rowNum + delta;
        if (rowNum < 0) { rowNum = numChildren - 1; }
        if (rowNum >= numChildren) { rowNum = 0; }
        setSelected(children, rowNum);
    };
    var setSelected = function (children, rowNum) {
        children.removeClass('selected');
        children.eq(local.rowSelected = rowNum).addClass('selected');
    };
    var edit = function (selectedPersonLi) {
        local.nameList.children().removeClass('selected');
        selectedPersonLi.addClass('selected');
        //local.inputField.val('');
        addToVotesList(selectedPersonLi);

    };

    var addToVotesList = function (selectedPersonLi) {
        if (!selectedPersonLi.length) return;

        var rawId = selectedPersonLi.attr('id');
        if (!rawId) return;

        var personId = +rawId.substr(1);
        if (personId == 0) return;

        //        var existingHost = votesList.find('.VoteHost[data-person-id={0}]'.filledWith(personId)).eq(0);
        //        if (existingHost.length != 0) {
        //            // already in the list... this is a duplicate
        //        }

        local.votes.push({
            vid: 0,
            pid: personId,
            name: selectedPersonLi.text(),
            count: 0,
            ineligible: selectedPersonLi.data('ineligible')
        });

        //var newHost = $(site.templates.NormalVoteLine.filledWithEach(extendVotes([info]))).appendTo(votesList);

        showVotes();

        var newHost = $('#votesList .VoteHost').last();

        startSavingVote(newHost);
    };

    var addSpoiled = function () {

        local.votes.push({
            vid: 0,
            count: 0,
            invalid: 0,
            changed: false,
            InvalidReasons: local.invalidReasonsHtml
        });

        //var newHost = $(site.templates.NormalVoteLine.filledWith(info)).appendTo(votesList);

        showVotes();

        var newHost = $('#votesList .VoteHost').last();
        var input = newHost.find('select');

        // vote not saved until a reason is chosen
        input.focus();
    };

    var extendVotes = function (votes) {
        var num = 0;
        $.each(votes, function () {
            if (this.invalid && this.invalid !== null) {
                this.InvalidReasons = local.invalidReasonsHtml;
            }
            if (this.ineligible && this.ineligible !== null) {
                // person is invalid!

                var vote = this;
                var reasonList = $.grep(publicInterface.invalidReasons, function (item) {
                    return item.Id == vote.ineligible;
                });
                var reason = 'Ineligible';
                if (reasonList.length == 1) {
                    reason = reasonList[0].Desc;
                }
                this.Display = '<span class=InvalidName>{1}</span> &nbsp; <span class=Ineligible>Ineligible: {0}</span>'.filledWith(reason, this.name);
            }
            else {
                this.Display = this.name;
            }
            num++;
            this.ExtraClass = null;

            if (num == local.votesNeeded + 1) {
                this.ExtraClass = 'ExtraVote';
            }
            else if (num > local.votesNeeded + 1) {
                this.ExtraClass = 'ExtraVotes';
            }
        });
        return votes;
    };

    var prepareReasons = function () {
        var html = ['<option value="0">Select a reason...</option>'];
        var group = '';
        $.each(publicInterface.invalidReasons, function () {
            var reasonGroup = this.Group;
            if (reasonGroup != group) {
                if (group) {
                    html.push('</optgroup>');
                }
                html.push('<optgroup label="{0}">'.filledWith(reasonGroup));
                group = reasonGroup;
            }
            html.push('<option value="{Id}">{Desc}</option>'.filledWith(this));
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

            case 40: // down
                moveSelected(1);
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
        local.actionTag.html('Typing...');
        local.actionTag.addClass('delaying');
        input.addClass('delaying');

        local.keyTimer = setTimeout(function () {
            local.lastSearch = text;

            local.actionTag.removeClass('delaying');
            input.removeClass('delaying');

            local.actionTag.addClass('searching');
            local.actionTag.text('Searching...');
            input.addClass('searching');

            local.peopleHelper.SearchNames(text, onNamesReady, true, getUsedIds());
        }, local.keyTime);
    };

    var getUsedIds = function () {
        return $.map($('.VoteHost'), function (item) {
            return $(item).data('person-id');
        });
    };

    var resetSearch = function () {
        onNamesReady({
            People: [],
            MoreFound: ''
        }, false);
    };
    var ballotClick = function (ev) {
        var el = ev.target;
        while (el.tagName != 'LI') {
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
        BallotsList: null,
        Location: null,
        PreparePage: preparePage,
        local: local
    };

    return publicInterface;
};

var ballotNormalPage = BallotNormalPageFunc();

$(function () {
    ballotNormalPage.PreparePage();
});