/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />

var BallotPageFunc = function () {
    var local = {
        People: [],
        peopleHelper: null,
        keyTimer: null,
        keyTime: 300,
        lastSearch: '',
        actionTag: null,
        inputField: null,
        nameList: null,
        invalidReasonsHtml: null,
        rowSelected: 0,
        searchResultTemplate: '<li id=P{Id}>{Name}</li>'
    };

    var preparePage = function () {
        local.peopleHelper = new PeopleHelper(publicInterface.peopleUrl);
        local.peopleHelper.Prepare();

        local.inputField = $('#txtSearch').live('keyup paste', runSearch).focus();
        local.actionTag = $('#action');
        local.nameList = $('#nameList');
        $('#nameList').on('click', 'li', nameClick);
        $('#btnAddSpoiled').on('click', addSpoiled);
        $('#votesList').on('change keyup', 'input', voteNumChange);
        $('#votesList').on('click', '.ui-icon-trash', deleteVote);
        $('#votesList').on('change', 'select', invalidReasonChanged);
        resetSearch();

        local.invalidReasonsHtml = prepareReasons();

        loadExisting();
    };
    var loadExisting = function () {
        if (publicInterface.initialVotes) {
            var list = $('#votesList');
            list.html(site.templates.SingleVoteLine.filledWithEach(extend(publicInterface.initialVotes)));
            list.find('select:visible').each(function () {
                var select = $(this);
                select.val(select.data('invalid'));
            });
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

    var saveVote = function (host) {
        var input = host.find('input');
        var invalids = host.find('select:visible');
        var form = {
            pid: host.data('person-id') || 0,
            vid: host.data('vote-id') || 0,
            invalid: invalids.val() || 0,
            count: input.val() || 0
        };
        ShowStatusDisplay('Saving...');
        input.focus();

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
        local.People = info.People;
        local.nameList.html(local.searchResultTemplate.filledWithEach(local.People));
        $('#more').html(info.MoreFound);
        if (!local.People.length && local.lastSearch) {
            local.nameList.append('<li>...no matches found...</li>');
        }
        else {
            local.rowSelected = 0;
            if (info.MoreFound && local.lastSearch) {
                local.nameList.append('<li>...more than 9 matched...</li>');
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
            InvalidReasons: local.invalidReasonsHtml
        };

        var newHost = $(site.templates.SingleVoteLine.filledWith(info)).appendTo(votesList);
        var input = newHost.find('select');
        // vote not saved until a reason is chosen
        input.focus();
    };

    var extend = function (votes) {
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
                this.name = '<span class=Ineligible>{0}</span> ({1})'.filledWith(reason, this.name);
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

            local.peopleHelper.SearchNames(text, onNamesReady, true);
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
        invalidReasons: [],
        initialVotes: null,
        PreparePage: preparePage
    };

    return publicInterface;
};

var ballotPage = BallotPageFunc();

$(function () {
    ballotPage.PreparePage();
});