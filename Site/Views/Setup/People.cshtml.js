/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />
/// <reference path="../../Scripts/jquery-1.6.4-vsdoc.js" />

var NamesPage = function () {
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
    template: '<li id=P{0}><u>{1}</u><u>{2}</u></li>'
  };
  var onNamesReady = function (info) {
    local.People = markUp(info.People);
    local.nameList.html(local.template.filledWithEach(local.People));
    $('#more').html(info.MoreFound);
    if (info.MoreFound) {
      local.nameList.append('<li>...</li>');
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
      $.each(personInfo, function (j, item) {
        if (j == 0 || !item) return; // skip ID; skip blanks
        if (typeof item != 'String') item = '' + item;
        $.each(searchParts, function (k, searchPart) {
          var changed = false;
          var r = item.replace(searchPart, function () {
            changed = true;
            return '<b>' + arguments[0] + '</b>';
          });
          if (changed) { item = personInfo[j] = r; }
        });
      });
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

    children.removeClass('selected');
    children.eq(local.rowSelected = rowNum).addClass('selected');
  };
  var navigating = function (ev) {
    switch (ev.which) {
      case 38: // up
        moveSelected(-1);
        ev.preventDefault();
        break;
      case 40: // down
        moveSelected(1);
        ev.preventDefault();
        break;
      default:
    }
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
  var publicInterface = {
    peopleUrl: '',
    PreparePage: function () {
      local.peopleHelper = new PeopleHelper(publicInterface.peopleUrl);
      local.peopleHelper.Prepare();

      local.inputField = $('#txtSearch').live('keyup paste', runSearch).focus();
      local.actionTag = $('#action');
      local.nameList = $('#nameList');

      resetSearch();
    }
  };
  return publicInterface;
};

var namesPage = NamesPage();

$(function () {
  namesPage.PreparePage();
});