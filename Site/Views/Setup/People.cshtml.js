var PeoplePage = function () {
    var local = {
        People: [],
        peopleHelper: null,
        keyTimer: null,
        keyTime: 300,
        lastSearch: '',
        totalOnFile: 0,
        actionTag: null,
        inputField: null,
        nameList: null,
        rowSelected: 0,
        showPersonId: null,
        selectByVoteCount: false,
        maintainCurrentRow: false,
        template2: '<li id=P{Id}{^Classes}{^IneligibleData}>{^HtmlName}</li>'
    };


    var preparePage = function () {
        local.inputField = $('#txtSearch').focus();

        local.peopleHelper = new PeopleHelper(publicInterface.peopleUrl, false);
        local.peopleHelper.Prepare(function () {
            if (local.totalOnFile < 25) {
                specialSearch('All');
            } else {
                local.nameList.html('<li class=Match5>(Ready for searching)</li>');
            }
            local.inputField.prop('disabled', false);
            local.inputField.focus();
        });

        local.inputField.prop('disabled', true);
        local.inputField.bind('keyup paste', runSearch);
        local.actionTag = $('#action');
        local.nameList = $('#nameList');
        $(document).on('click', '#nameList li', nameClick).focus();
        $(document).on('click', '#btnAddNew', addNewPerson);

        local.totalOnFile = publicInterface.namesOnFile;

        //$('#btnListVoters').click(function () {
        //  specialSearch('Voters');
        //});
        //$('#btnListTied').click(function () {
        //  specialSearch('Tied');
        //});

        resetSearch();

        site.onbroadcast(site.broadcastCode.personSaved, personSaved);

        site.qTips.push({ selector: '#qTipSearch', title: 'Searching', text: 'Type one or two parts of the person\'s name. ' });
    };

    var displaySearchResults = function (info) {
        local.People = info.People;
        local.nameList.html(local.template2.filledWithEach(local.People));
        $('#more').html(info.MoreFound || moreFound(local.totalOnFile));

        if (!local.People.length && local.lastSearch) {
            local.nameList.append('<li>...no matches found...</li>');
        }
        else {
            //if (info.MoreFound && local.lastSearch) {
            //  local.nameList.append('<li>...more matched...</li>');
            //}
            if (local.showPersonId) {
                local.rowSelected = local.nameList.find('#P' + local.showPersonId).index();
                local.showPersonId = 0;
            } else if (local.selectByVoteCount) {
                $.each(local.People, function (i, item) {
                    if (item.NumVotes && !local.maintainCurrentRow) {
                        local.rowSelected = i;
                    }
                });
            }
        }
        local.maintainCurrentRow = false;
        local.actionTag.removeClass('searching');
        local.inputField.removeClass('searching');
        local.actionTag.removeClass('delaying');
        local.inputField.removeClass('delaying');

        // if none selected, selects first name
        var selectedName = local.nameList.children().eq(local.rowSelected);
        selectedName.addClass('selected');
        if (local.rowSelected) {
            scrollIntoView(selectedName, local.nameList);
        }
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
        ShowStatusDisplay("Loading...");
        CallAjaxHandler(publicInterface.peopleUrl + '/GetDetail', { id: personId }, showPersonDetail);
    };

    var addNewPerson = function () {
        editPersonPage.startNewPerson($('#editPanel'));
    };

    var showPersonDetail = function (info) {
        applyValues(info);
        ResetStatusDisplay();
    };

    var applyValues = function (info) {
        var panel = $('#editPanel');
        if (info.Person == null) {
            panel.hide();
            return;
        };

        editPersonPage.applyValues(panel, info.Person, true, info.CanDelete);

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
                var selected = local.nameList.children().eq(local.rowSelected).attr('id');
                if (selected) {
                    var id = +selected.substr(1);
                    edit(id);
                    ev.preventDefault();
                    return true;
                }
                ev.preventDefault();
                return false;

            default:
        }
        return false;
    };

    var runSearch = function (ev) {
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

        local.lastSearch = text;

        local.peopleHelper.Search(text, displaySearchResults);
    };
    var resetSearch = function () {
        $('#txtSearch').val('');
        local.actionTag.removeClass('delaying');
        local.inputField.removeClass('delaying');
        local.lastSearch = '';
        displaySearchResults({
            People: [],
            MoreFound: moreFound(local.totalOnFile)
        });
    };
    var moreFound = function (num) {
        return comma(num) + '  people on file';
    };
    var nameClick = function (ev) {
        var el = ev.target;
        while (el.tagName != 'LI') {
            el = el.parentNode;
            if (el == null) return;
        }
        edit(+el.id.substr(1));
    };

    var scrollIntoView = function (jNode, container) {
        if (!jNode.length) return;

        jNode[0].scrollIntoView({
          block: 'center'
        });
        return;

//        var containerTop = $(container).scrollTop();
//        var containerBottom = containerTop + $(container).height();
//        var elemTop = jNode.offset().top;
//        var elemBottom = elemTop + $(jNode).height();
//        if (elemTop < containerTop) {
//            $(container).scrollTop(Math.max(0, elemTop - 10));
//        } else if (elemBottom > containerBottom) {
//            $(container).scrollTop(elemBottom - $(container).height() + 30);
//        }
    };


    var specialSearch = function (code) {
        resetSearch();
        local.peopleHelper.Special(code, displaySearchResults);
    };

    var personSaved = function (ev, info) {

        var searchText = $('#txtSearch').val();
        local.totalOnFile = info.OnFile;
        if (searchText) {
            local.maintainCurrentRow = true;
            local.showPersonId = info.Person.C_RowId;
            local.peopleHelper.Search(searchText, displaySearchResults);
        }
        else {
            $('#more').html(moreFound(info.OnFile));
        }
    };

    var publicInterface = {
        peopleUrl: '',
        PreparePage: preparePage,
        peopleHelper: function () {
            return local.peopleHelper;
        }
    };
    return publicInterface;
};

var peoplePage = PeoplePage();

$(function () {
    peoplePage.PreparePage();
});