/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />
/// <reference path="../../Scripts/jquery-1.6.4-vsdoc.js" />

var NamesPage = function () {
  var local = {
    People: [],
    peopleHelper: null,
    keyTimer: null,
    lastSearch: '',
    actionTag: null,
    template: '<div>{Name} ({C_RowId})</div>'
  };
  var onNamesReady = function (info) {
    $('#nameList').html(local.template.filledWithEach(info.People));
    $('#more').html(info.MoreFound);
    local.actionTag.removeClass('searching');
  };
  var publicInterface = {
    peopleUrl: '',
    PreparePage: function () {
      local.peopleHelper = new PeopleHelper(publicInterface.peopleUrl);
      local.peopleHelper.Prepare();

      $('#txtSearch').live('keyup paste', runSearch).focus();
      local.actionTag = $('#action');
    }
  };
  var runSearch = function () {
    clearTimeout(local.keyTimer);
    var text = $(this).val();
    if (local.lastSearch === text) return;
    local.actionTag.addClass('delaying');

    local.keyTimer = setTimeout(function () {
      local.lastSearch = text;
      local.actionTag.removeClass('delaying');
      local.actionTag.addClass('searching');

      local.peopleHelper.SearchNames(text, onNamesReady);
    }, 250);
  };

  return publicInterface;
};

var namesPage = NamesPage();

$(function () {
  namesPage.PreparePage();
});