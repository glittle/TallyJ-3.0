/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />
/// <reference path="../../Scripts/jquery-1.7.1.js" />

var PeoplePage = function () {
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
        template: '<li id=P{Id}>{^Name}</li>'
    };
    var onNamesReady = function (info) {
        local.People = info.People;
        local.nameList.html(local.template.filledWithEach(local.People));
        $('#more').html(info.MoreFound);
        if (!local.People.length && local.lastSearch) {
            local.nameList.append('<li>...no matches found...</li>');
        }
        else {
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
    var edit = function (personId) {
        var id = 'P' + personId;
        var children = local.nameList.children();
        children.each(function (i, el) {
            if (el.id == id) {
                setSelected(children, i);
                return false;
            }
        });
        ShowStatusDisplay("Loading...", 0);
        CallAjaxHandler(publicInterface.peopleUrl + '/GetDetail', { id: personId }, showPersonDetail);
    };

    var addNewPerson = function () {
        showPersonDetail({
            Person: {
                C_RowId: -1,
                CanVote: publicInterface.defaultRules.CanVote == 'A',
                CanReceiveVotes: publicInterface.defaultRules.CanReceive == 'A'
            }
        });
    };

    var showPersonDetail = function (info) {
        applyValues(info.Person);
        ResetStatusDisplay();
    };

    var applyValues = function (person) {
        var panel = $('#editPanel');
        if (person == null) {
            panel.hide();
            return;
        };

        panel.find(':input[data-name]').each(function () {
            var input = $(this);
            var value = person[input.data('name')] || '';
            switch (input.attr('type')) {
                case 'checkbox':
                    input.prop('checked', value);
                    break;
                default:
                    input.val(value);
                    break;
            }
        });

        //TODO...
        //        if (publicInterface.defaultRules.CanVoteLocked) {
        //            $('[data-name="CanVote"]').prop('enabled', false).propertyIsEnumerable('checked', publicInterface.defaultRules.CanVote=='A');
        //        }
        //        if (publicInterface.defaultRules.CanReceiveLocked) {
        //            $('[data-name="CanReceiveVote"]').prop('enabled', false).propertyIsEnumerable('checked', publicInterface.defaultRules.CanReceive=='A');
        //        }

        panel.fadeIn();
        panel.find('[data-name="FirstName"]').focus();
    };

    var saveChanges = function () {
        var form = {};
        $(':input[data-name]').each(function () {
            var input = $(this);
            var value;
            switch (input.attr('type')) {
                case 'checkbox':
                    value = input.prop('checked');
                    break;
                default:
                    value = input.val();
                    break;
            }
            form[input.data('name')] = value;
        });

        ShowStatusDisplay("Saving...");
        CallAjaxHandler(publicInterface.controllerUrl + '/SavePerson', form, function (info) {
            if (info.Person) {
                applyValues(info.Person);
                var searchText = $('#txtSearch').val();
                if (searchText) {
                    local.peopleHelper.SearchNames(searchText, onNamesReady, false);
                }
            }
            ShowStatusDisplay(info.Status, 0, null, false, true);
        });
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
                var id = +local.nameList.children().eq(local.rowSelected).attr('id').substr(1);
                ev.preventDefault();
                edit(id);
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

            local.peopleHelper.SearchNames(text, onNamesReady, false);
        }, local.keyTime);
    };
    var resetSearch = function () {
        onNamesReady({
            People: [],
            MoreFound: comma(publicInterface.namesOnFile) + '  people on file'
        });
    };
    var nameClick = function (ev) {
        var el = ev.target;
        while (el.tagName != 'LI') {
            el = el.parentNode;
            if (el == null) return;
        }
        edit(+el.id.substr(1));
    };
    var preparePage = function () {
        local.peopleHelper = new PeopleHelper(publicInterface.peopleUrl);
        local.peopleHelper.Prepare();

        local.inputField = $('#txtSearch').live('keyup paste', runSearch).focus();
        local.actionTag = $('#action');
        local.nameList = $('#nameList');
        $('#nameList li').live('click', nameClick).focus();
        $('#btnSave').live('click', saveChanges);
        $('#txtAddNew').live('click', addNewPerson);
        resetSearch();
    };

    var publicInterface = {
        peopleUrl: '',
        defaultRules: null,
        namesOnFile: 0,
        PreparePage: preparePage
    };
    return publicInterface;
};

var peoplePage = PeoplePage();

$(function () {
    peoplePage.PreparePage();
});