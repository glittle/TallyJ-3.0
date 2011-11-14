/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />

var ImportExportPage = function () {
  var local = {
  };

  var preparePage = function () {
    $('#btnResetList').click(function () {
      ShowStatusDisplay('Resetting...');
      CallAjaxHandler(publicInterface.setupUrl + '/ResetAll', null, function (info) {
        ResetStatusDisplay();
      });
    });
  };
  var publicInterface = {
    setupUrl: '',
    PreparePage: preparePage
  };
  return publicInterface;
};

var importExportPage = ImportExportPage();

$(function () {
  importExportPage.PreparePage();
});