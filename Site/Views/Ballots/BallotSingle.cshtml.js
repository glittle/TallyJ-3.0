/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />

var BallotPageFunc = function () {
    var local = {
        //voteTemplate: "<div class=VoteHost id=V{vid} data-vote-id={vid} data-person-id={pid}><input name=Vote type=text value='{count}'><span>{name}<span> (Vote {vid} Person {pid})</div>",
        People: [],
        peopleHelper: null,
        keyTimer: null,
        keyTime: 300,
        lastSearch: '',
        actionTag: null,
        inputField: null,
        nameList: null,
        rowSelected: 0,
        searchResultTemplate: '<li id=P{0}>{1}</li>'
    };

    var preparePage = function () {
        local.peopleHelper = new PeopleHelper(publicInterface.peopleUrl);
        local.peopleHelper.Prepare();

        local.inputField = $('#txtSearch').live('keyup paste', runSearch).focus();
        local.actionTag = $('#action');
        local.nameList = $('#nameList');
        $('#nameList').on('click', 'li', nameClick);
        $('#votesList').on('change keyup', 'input', voteNumChange);
        $('#votesList').on('click', '.ui-icon-trash', deleteVote);
        resetSearch();

        loadExisting();
    };
    var loadExisting = function () {
        if (publicInterface.initialVotes) {
            $('#votesList').html(site.templates.SingleVoteLine.filledWithEach(publicInterface.initialVotes));
        }
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
            saveVote(input, input.parent());
        }
    };

    var saveVote = function (input, host) {
        var form = {
            pid: host.data('person-id') || 0,
            vid: host.data('vote-id') || 0,
            count: input.val() || 0
        };
        ShowStatusDisplay('Saving...');
        CallAjaxHandler(publicInterface.controllerUrl + '/SaveSingleNameVote', form, function (info) {
            // TODO:
            if (info.Updated) {
                ShowStatusDisplay('Saved', 0, 3000, false, true);
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

    var deleteVote = function () {
        var host = $(this).parent();
        var voteId = host.data('vote-id') || 0;
        var form = {
            vid: voteId
        };
        ShowStatusDisplay('Deleting...');
        CallAjaxHandler(publicInterface.controllerUrl + '/DeleteSingleNameVote', form, function (info) {
            if (info.Deleted) {
                ShowStatusDisplay('Deleted', 0, 3000, false, true);
                $('#votesList').html(site.templates.SingleVoteLine.filledWithEach(info.AllVotes));
            }
            else {
                ShowStatusFailed(info.Message);
            }
        });
    };

    var onNamesReady = function (info) {
        local.People = markUp(info.People);
        local.nameList.html(local.searchResultTemplate.filledWithEach(local.People));
        $('#more').html(info.MoreFound);
        if (!local.People.length && local.lastSearch) {
            local.nameList.append('<li>...no matches found...</li>');
        }
        else {
            if (info.MoreFound && local.lastSearch) {
                local.nameList.append('<li>...more than 9 matched...</li>');
            }
        }
        local.actionTag.removeClass('searching');
        local.inputField.removeClass('searching');

        // single:
        local.nameList.children().removeClass('selected').eq(local.rowSelected = 0).addClass('selected');
    };
    var markUp = function (people) {
        var results = [];
        var searchParts = [];
        var parts = local.lastSearch.split(' ');
        $.each(parts, function (i, part) {
            if (part) {
                searchParts.push(new RegExp(part, "ig"));
            }
        });
        $.each(people, function (i, personInfo) {
            var foundHit = false;
            $.each(personInfo, function (j, item) {
                if (j == 0 || !item) return; // skip ID; skip blanks
                if (typeof item != 'String') item = '' + item;
                $.each(searchParts, function (k, searchPart) {
                    var changed = false;
                    var r = item.replace(searchPart, function () {
                        foundHit = changed = true;
                        return '<b>' + arguments[0] + '</b>';
                    });
                    if (changed) { item = personInfo[j] = r; }
                });
            });
            if (!foundHit) {
                // must be soundex
                personInfo[1] = '<i>' + personInfo[1] + '</i>';
            }
            results.push(personInfo);
        });
        return results;
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
        var input = newHost.find('input');
        saveVote(input, newHost);
        input.focus();
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

            default:
        }
        return false;
    };

    var runSearch = function (ev) {
        clearTimeout(local.keyTimer);
        var input = $(this);
        var text = input.val();
        if (navigating(ev)) {
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

            local.peopleHelper.SearchNames(text, onNamesReady);
        }, local.keyTime);
    };

    var resetSearch = function () {
        onNamesReady({
            People: [],
            MoreFound: ''
        });
    };
    var nameClick = function (ev) {
        var el = ev.target;
        while (el.tagName != 'LI') {
            el = el.parentNode();
            if (el == null) return;
        }
        edit($(el));
    };
    var publicInterface = {
        peopleUrl: '',
        controllerUrl: '',
        initialVotes: null,
        PreparePage: preparePage
    };

    return publicInterface;
};

var ballotPage = BallotPageFunc();

$(function () {
    ballotPage.PreparePage();
});