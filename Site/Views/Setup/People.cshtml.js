/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />

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
    template: '<li id=P{0}>{1}</li>'
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
    local.nameList.children().eq(local.rowSelected = info.DefaultTo).addClass('selected');
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
  var edit = function (personId) {
    var id = 'P' + personId;
    var children = local.nameList.children();
    children.each(function (i, el) {
      if (el.id == id) {
        setSelected(children, i);
        return false;
      }
    });
    $('#editPanel').text('edit ' + personId);
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

      local.peopleHelper.SearchNames(text, onNamesReady);
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
      el = el.parentNode();
      if (el == null) return;
    }
    edit(+el.id.substr(1));
  };
  var publicInterface = {
    peopleUrl: '',
    namesOnFile: 0,
    PreparePage: function () {
      local.peopleHelper = new PeopleHelper(publicInterface.peopleUrl);
      local.peopleHelper.Prepare();

      local.inputField = $('#txtSearch').live('keyup paste', runSearch).focus();
      local.actionTag = $('#action');
      local.nameList = $('#nameList');
      $('#nameList li').live('click', nameClick).focus();
      resetSearch();
    }
  };
  return publicInterface;
};

var peoplePage = PeoplePage();

$(function () {
  peoplePage.PreparePage();
});