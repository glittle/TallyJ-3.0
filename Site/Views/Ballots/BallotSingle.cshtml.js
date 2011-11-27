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
        rowSelected: 0,
        template: '<li id=P{0}>{1}</li>'
    };

    var preparePage = function () {
        local.peopleHelper = new PeopleHelper(publicInterface.peopleUrl);
        local.peopleHelper.Prepare();

        local.inputField = $('#txtSearch').live('keypress paste', runSearch).focus();
        local.actionTag = $('#action');
        local.nameList = $('#nameList');
        $('#nameList').on('click', 'li', nameClick);
        $('#votesList').on('change keypress', 'input', voteNumChange);
        resetSearch();
    };
    var voteNumChange = function (ev) {
        var input = $(ev.target);

        switch (ev.which) {
            case 13: // enter
                ev.preventDefault();
                ev.stopPropagation();
                ev.stopImmediatePropagation();
                local.inputField.focus();
                return;

            default:
        }

        saveVote(input);
    };

    var saveVote = function (input) {
        var form = {
            pid: input.parent().attr('id').substr(1),
            vid: input.parent().data('voteId') || 0,
            count: input.val() || 0
        };
        CallAjaxHandler(publicInterface.controllerUrl + '/SaveVoteSingle', form, function (info) {
            
        });
    };


    var onNamesReady = function (info) {
        local.People = markUp(info.People);
        local.nameList.html(local.template.filledWithEach(local.People));
        $('#more').html(info.MoreFound);
        if (!local.People.length && local.lastSearch) {
            local.nameList.append('<li>...no matches found...</li>');
        }
        else {
            if (info.MoreFound && local.lastSearch) {
                local.nameList.append('<li>...</li>');
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

        var existing = $('#V' + personId);
        if (existing.length != 0) {
            existing.find('input').focus();
            return;
        }

        var info = {
            id: personId,
            name: selectedPersonLi.text()
        };

        $('#votesList').append("<div id=V{id}><input type=number value=0><span>{name}<span>{id}</div>".filledWith(info));

        saveVote($('#V' + personId));
    }

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
            return false;
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
        PreparePage: preparePage
    };

    return publicInterface;
};

var ballotPage = BallotPageFunc();

$(function () {
    ballotPage.PreparePage();
});