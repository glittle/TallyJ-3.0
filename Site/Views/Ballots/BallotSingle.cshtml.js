/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7.1.js" />
/// <reference path="../../Scripts/jquery-ui-1.8.16.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />

var BallotSinglePageFunc = function () {
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
        invalidReasonsHtml: null,
        rowSelected: 0,
        lastBallotRowVersion: 0,
        searchResultTemplate: '<li id=P{Id}>{^Name}</li>',
        ballotListTemplate: '<li id=B{Id}>{Location} (<span data-location={LocationId}>{TallyStatus}</span>) - {Code}</li>'
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
        $('#votesList').on('change keyup', 'input', voteNumChange);
        $('#votesList').on('click', '.ui-icon-trash', deleteVote);
        $('#votesList').on('click', '.btnClearChangeError', resaveVote);
        $('#votesList').on('change', 'select', invalidReasonChanged);

        $('#tabs').tabs();

        $('#btnRefreshBallotCount').on('click', changeLocationStatus);

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

        loadExisting(publicInterface);
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

    var loadExisting = function (info) {

        if (info.Ballots) {
            showBallots(info.Ballots);
        }

        var ballotInfo = info.BallotInfo;
        if (ballotInfo) {
            $('#ballotCode').text(ballotInfo.Ballot.Code);
            $('#ballotStatus').text(ballotInfo.Ballot.StatusCode);

            var list = $('#votesList');
            list.html(site.templates.SingleVoteLine.filledWithEach(extendVotes(ballotInfo.Votes)));
            list.find('select:visible').each(function () {
                var select = $(this);
                select.val(select.data('invalid'));
            });

            highlightBallotInList(ballotInfo.Ballot.Id);
        }

        if (info.Location) {
            showLocation(info.Location);
            publicInterface.Location = info.Location;
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
            html = '<span class=countsNeutral>{0} more ballot{1} to enter</span>'.filledWith(remainingToEnter, remainingToEnter == 1 ? '' : 's');
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

    var voteNumChange = function (ev) {
        var input = $(ev.target);

        switch (ev.which) {
            case 13:
                // enter
                ev.preventDefault();
                ev.stopPropagation();
                ev.stopImmediatePropagation();
                local.inputField.focus();
                return;
            default:
        }

        if (ev.type == 'change') {
            saveVote(input.parent());
        }
    };

    var invalidReasonChanged = function (ev) {
        var select = $(ev.target);
        var reason = select.val();
        if (reason == '0') {
            return;  // don't save with no reason
        }
        var parent = select.parent();
        saveVote(parent);
    };

    var resaveVote = function (ev) {
        var host = $(ev.target).parents('.VoteHost');
        saveVote(host);
    };

    var saveVote = function (host) {
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
            }

            if (!publicInterface.Location) {
                location.href = location.href;
                //TODO: use Ajax to reload the content
                return;
            }

            if (info.Location) {
                showLocation(info.Location);
            }

            if (form.vid == 0) {
                if (info.VoteId) {
                    host.data('vote-id', info.VoteId);
                }
                else {
                    ShowStatusFailed('Error on save. Please reload this page.');
                }
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

                if (info.Location) {
                    showLocation(info.Location);
                }
            }
            else {
                ShowStatusFailed(info.Message);
            }
        });
    };

    var onNamesReady = function (info) {
        local.People = info.People;
        local.nameList.html(local.searchResultTemplate.filledWithEach(local.People));
        $('#more').html(''); //info.MoreFound
        if (!local.People.length && local.lastSearch) {
            var search = $('#txtSearch').val();
            if (search) {
                local.nameList.append('<li>...no matches found...</li>');
            }
        } else {
            local.rowSelected = 0;
            if (info.MoreFound && local.lastSearch) {
                local.nameList.append('<li>...more matched...</li>');
            }
            $.each(local.People, function (i, item) {
                if (item.BestMatch) {
                    local.rowSelected = i;
                }
            });
        }
        local.actionTag.removeClass('searching');
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
        local.inputField.val('');

        addToVotesList(selectedPersonLi);
    };

    var addToVotesList = function (selectedPersonLi) {
        if (!selectedPersonLi.length) return;

        var personId = selectedPersonLi.attr('id').substr(1);
        if (!personId) return;

        var votesList = $('#votesList');
        var existingHost = votesList.find('.VoteHost[data-person-id={0}]'.filledWith(personId)).eq(0);
        if (existingHost.length != 0) {
            existingHost.find('input').focus();
            return;
        }

        var info = {
            vid: 0,
            pid: personId,
            name: selectedPersonLi.text(),
            count: 0
        };

        var newHost = $(site.templates.SingleVoteLine.filledWith(info)).appendTo(votesList);
        saveVote(newHost);
    };

    var addSpoiled = function () {
        var votesList = $('#votesList');

        var info = {
            vid: 0,
            count: 0,
            invalid: 0,
            changed: false,
            InvalidReasons: local.invalidReasonsHtml
        };

        var newHost = $(site.templates.SingleVoteLine.filledWith(info)).appendTo(votesList);
        var input = newHost.find('select');
        // vote not saved until a reason is chosen
        input.focus();
    };

    var extendVotes = function (votes) {
        $.each(votes, function () {
            if (this.invalid !== null) {
                this.InvalidReasons = local.invalidReasonsHtml;
            }
            if (this.ineligible !== null) {
                // person is invalid!
                var vote = this;
                var reasonList = $.grep(publicInterface.invalidReasons, function (item) {
                    return item.Id == vote.ineligible;
                });
                var reason = 'Ineligible';
                if (reasonList.length == 1) {
                    reason = reasonList[0].Desc;
                }
                this.name = '<span class=Ineligible>Spoiled: {0}</span> ({1})'.filledWith(reason, this.name);
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
                $('#txtSearch').val('');
                runSearch();
                return true;

            default:
        }
        return false;
    };

    var runSearch = function (ev) {
        clearTimeout(local.keyTimer);
        var input = $('#txtSearch');
        var text = input.val();
        if (ev && navigating(ev)) {
            return;
        }
        if (local.lastSearch === text.trim()) return;
        if (text == '') {
            resetSearch();
            return;
        }
        local.actionTag.addClass('delaying');
        input.addClass('delaying');

        local.keyTimer = setTimeout(function () {
            local.lastSearch = text;

            local.actionTag.removeClass('delaying');
            input.removeClass('delaying');

            local.actionTag.addClass('searching');
            input.addClass('searching');

            local.peopleHelper.SearchNames(text, onNamesReady, true);
        }, local.keyTime);
    };

    var resetSearch = function () {
        onNamesReady({
            People: [],
            MoreFound: ''
        });
    };
    var ballotClick = function (ev) {
        var el = ev.target;
        while (el.tagName != 'LI') {
            el = el.parentNode;
            if (el == null) return;
        }
        showBallot(el.id);
    };

    var showBallot = function (ballotId) {
        if (ballotId.substr(0, 1) == 'B') {
            ballotId = ballotId.substr(1);
        }
        CallAjaxHandler(publicInterface.controllerUrl + '/SwitchToBallot', { ballotId: ballotId }, loadExisting);
    };

    var nameClick = function (ev) {
        var el = ev.target;
        while (el.tagName != 'LI') {
            el = el.parentNode;
            if (el == null) return;
        }
        edit($(el));
    };
    var publicInterface = {
        peopleUrl: '',
        controllerUrl: '',
        invalidReasons: [],
        BallotInfo: null,
        Ballots: null,
        Location: null,
        PreparePage: preparePage
    };

    return publicInterface;
};

var ballotSinglePage = BallotSinglePageFunc();

$(function () {
    ballotSinglePage.PreparePage();
});