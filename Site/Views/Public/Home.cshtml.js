/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />

var HomeIndexPage = function () {
  var localSettings = {
  };

  var publicInterface = {
    PreparePage: function () {
      
      $('#btnJoin').on('click', null, null, btnJoinClick);
      $('#txtCode').on('keypress', null, null, function () {
        $('#ddlElections').prop('selectedIndex', -1);
      });
      $('#ddlElections').on('change', null, null, function () {
        $('#txtCode').val('');
      });
    }

  };

  var btnJoinClick = function () {
    var electionCode = $('#txtCode').val() || $('#ddlElections').val();
    var who = $('#txtWhoAmI').val();
    var statusSpan = $('#joinStatus').removeClass('error');
    if (!electionCode) {
      statusSpan.addClass('error').html('Identify election first.');
      return;
    }
    if (!who) {
      statusSpan.addClass('error').html('Who are you?');
      return;
    }
    statusSpan.addClass('error').text('Test: ' + electionCode + '. Asking... please wait...');
    setTimeout(function () {
      statusSpan.addClass('error').html('Sorry. Not permitted.');
    }, 2000);
  };

  return publicInterface;
};

var homeIndexPage = HomeIndexPage();

$(function () {
  homeIndexPage.PreparePage();
});