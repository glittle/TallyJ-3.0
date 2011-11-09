/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />

var HomeIndexPage = function () {
  var localSettings = {
  };
  var showElections = function () {
    $('#ElectionList').html(site.templates.ElectionListItem.filledWithEach(publicInterface.elections));
  };
  var selectElection = function () {
    var btn = $(this);
    var form =
        {
          guid: btn.data('guid')
        };

    CallAjaxHandler(publicInterface.electionsUrl + '/SelectElection', form, function (election) {
      SetInStorage(lsName.Election, adjustElection(election));

      var electionDisplay = $('.CurrentElectionName');
      electionDisplay.text(election.Name);
      electionDisplay.effect('highlight', { mode: 'slow' });

      site.heartbeatActive = true;
      ActivateHeartbeat(true);
    });
  };

  var publicInterface = {
    elections: [],
    electionsUrl: '',
    PreparePage: function () {
      showElections();

      $('.btnSelectElection').click(selectElection);
    }
  };

  return publicInterface;
};

var homeIndexPage = HomeIndexPage();

$(function () {
  homeIndexPage.PreparePage();
});