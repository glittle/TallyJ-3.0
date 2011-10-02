/// <reference path=".././../Scripts/site.js" />
/// <reference path=".././../Scripts/jquery-1.6.4-vsdoc.js" />

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
    CallAjaxHandler(publicInterface.electionsUrl + '/SelectElection', form, function (info) {
      $('.CurrentElectionTitle').html(info).effect('highlight', { mode: 'slow' });
      site.heartbeatActive = true;
      localStorage.setItem(lsName.electionInfo, info);
      ActivateHeartbeat(true);
    });
  };
  var publicInterface = {
    elections: [],
    electionsUrl: '',
    PreparePage: function () {
      showElections();

      $('.btnSelectElection').click(selectElection);

      $('section.feature').live('click', function () {
        location.href = $(this).find('h3 a').eq(0).attr('href');
      });
    }
  };

  return publicInterface;
};

var homeIndexPage = HomeIndexPage();

$(function () {
    homeIndexPage.PreparePage();
});