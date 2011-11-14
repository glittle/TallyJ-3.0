/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />

var HomeIndexPage = function () {
  var localSettings = {
  };

  var selectElection = function () {
    var btn = $(this);
    var form =
        {
          guid: btn.data('guid')
        };

    CallAjaxHandler(publicInterface.electionsUrl + '/SelectElection', form, function (success) {

      if (success) {
        location.href = site.rootUrl + 'Dashboard';
        return;
      }
      //      var electionDisplay = $('.CurrentElectionName');
      //      electionDisplay.text(election.Name);
      //      electionDisplay.effect('highlight', { mode: 'slow' });

      site.heartbeatActive = true;
      ActivateHeartbeat(true);
    });
  };

  var copyElection = function () {
    var btn = $(this);
    var form =
        {
          guid: btn.data('guid')
        };

    if (!confirm('Are you sure you want to make a new election based on this one?')) {
      return;
    }

    CallAjaxHandler(publicInterface.electionsUrl + '/CopyElection', form, function (info) {

      if (info.Success) {
        location.href = '.';
        return;
      }

      alert(info.Message);

      site.heartbeatActive = true;
      ActivateHeartbeat(true);
    });
  };

  var publicInterface = {
    elections: [],
    electionsUrl: '',
    PreparePage: function () {
      $(document).on('click', '.btnSelectElection', null, selectElection);
      $(document).on('click', '.btnCopyElection', null, copyElection);
    }
  };

  return publicInterface;
};

var chooseElectionPage = HomeIndexPage();

$(function () {
  chooseElectionPage.PreparePage();
});