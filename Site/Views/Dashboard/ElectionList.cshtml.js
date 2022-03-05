var ElectionListPage = function () {
  var publicInterface = {
    elections: [],
    isGuest: false,
    electionsUrl: '',
    loadElectionUrl: '',
    PreparePage: preparePage
  };


  function preparePage() {
    $(document).on('click', '.btnSelectElection', null, selectElection);

    showElections(publicInterface.elections);

    getElectionCounts();

    if (!publicInterface.isGuest) {
      //            $(document).on('click', '.btnCopyElection', null, copyElection);
      $(document).on('click', '.btnExport', null, exportElection);
      $(document).on('click', '.btnDelete', null, deleteElection);
      $(document).on('click', '#btnLoad', null, loadElection);
      $(document).on('click', '#file-uploader', null, loadElection2);
      $(document).on('click', '#btnCreate', null, createElection);
      $(document).on('change', '#loadFile', null, upload2);

      $(document).on('click', '#btnUpload2', null, upload2);

      connectToImportHub();

    } else {
      $('.btnExport, .btnDelete, #btnLoad, #btnCreate').hide();
    }
  };

  function connectToImportHub() {
    var hub = $.connection.importHubCore;

    hub.client.loaderStatus = function (msg, isTemp) {
      msg = msg.replace(/\n/g, '<br> &nbsp; ');
      if (isTemp) {
        $('#tempLog').html(msg);
      } else {
        $('#tempLog').html('');
        $('#log').append('<div>' + msg + '</div>');
      }
    };

    startSignalR(function () {
      console.log('Joining import hub');
      CallAjaxHandler(electionListPage.importHubUrl, { connId: site.signalrConnectionId }, function (info) {

      });
    });
  };



  function upload2() {
    var $input = $('#loadFile');
    if ($input.val() === '') {
      return;
    }

    ShowStatusBusy("Loading election...");

    $('#loadingLog').show();
    $('#log').html('');
    $('#tempLog').html('Sending the file to the server...');

    var form = $('#formLoadFile');
    var frameId = 'tempUploadFrame';
    var frame = $('#' + frameId);
    if (!frame.length) {
      $('body').append('<iframe id=' + frameId + ' name=' + frameId + ' style="display:none" />');
      frame = $('#' + frameId);
    }
    form.attr({
      target: frameId,
      action: publicInterface.loadElectionUrl,
      enctype: 'multipart/form-data',
      method: 'post'
    });

    var frameObject = frame.on('load', function () {
      frameObject.unbind('load');
      $input.val(''); // blank out file name

      var response = frameObject.contents().text();
      var info;
      try {
        info = $.parseJSON(response);
      } catch (e) {
        info = { Success: false, Message: "Unexpected server message" };
      }

      if (info.Success) {
        showElections(info.Elections);
        ShowStatusDone('Loaded');

        var newRow = $('div.Election[data-guid="{0}"]'.filledWith(info.ElectionGuid));
        scrollToMe(newRow);
      }
      else {
        ShowStatusFailed(info.Message);
      }
    });

    form.submit();
  };

  function showElections(info) {
    var electionTemplate = $('#electionListItem').text();
    var locationTemplate = $('#locationSelectItem').text();

    $.each(info, function () {
      if (this.Locations) {
        this.Locations = locationTemplate.filledWithEach(this.Locations);
      }
      this.TestClass = this.IsTest ? ' TestElection' : '';
      this.RowClass = this.IsSingleNameElection ? ' SingleName' : '';
      this.RowClass += this.IsFuture ? ' IsFuture' : '';
      if (this.OnlineCurrentlyOpen) {
        this.OnlineOpen = 'Online Voting Open. Closing ' + moment(this.OnlineWhenClose).fromNow() + '.';
      }
    });

    $('#ElectionList').html(electionTemplate.filledWithEach(info));

    if (publicInterface.isGuest) {
      $('#ElectionList').find('.Detail button').each(function () {
        $(this).prop('disabled', true);
      });
    }

  };

  function selectElection() {
    if (publicInterface.isGuest) return;

    var btn = $(this);
    var row = btn.parents('.Election');
    var guid = row.data('guid');
    var form =
    {
      guid: guid,
      oldComputerGuid: GetFromStorage('compcode_' + guid, null)
    };

    clearElectionRelatedStorageItems();

    ShowStatusBusy('Opening election...'); // will reload page so don't need to clear it
    CallAjaxHandler(publicInterface.electionsUrl + '/SelectElection', form, afterSelectElection);
  };

  function getElectionCounts() {
    CallAjaxHandler(publicInterface.countsUrl, null, function (info) {
      info.forEach(function (election) {
        var line = $('#el-' + election.guid);
        line.find('.numVoters').text('- {0} name{1}'.filledWith(election.numPeople, Plural(election.numPeople)));
        line.find('.numBallots').text('- {0} ballot{1}'.filledWith(election.numBallots, Plural(election.numBallots)));
      });
    });
  }

  function afterSelectElection(info) {

    if (info.Selected) {
      //TODO: store computer Guid
      SetInStorage('compcode_' + info.ElectionGuid, info.CompGuid);

      location.href = site.rootUrl + 'Dashboard';

    }
    else {
      ShowStatusFailed("Unable to select");
    }
  };
  

  function createElection() {
    if (publicInterface.isGuest) return;

    // get the server to make an election, then go see it
    CallAjaxHandler(publicInterface.electionsUrl + '/CreateElection', null, function (info) {
      if (info.Success) {
        location.href = site.rootUrl + 'Setup';
        return;
      }
    });
  };

  function exportElection() {
    if (publicInterface.isGuest) return;

    var btn = $(this);
    var guid = btn.parents('.Election').data('guid');

    ShowStatusBusy("Preparing file...");

    //var oldText = btn.text();

    btn.addClass('active');
    var iframe = $('body').append('<iframe style="display:none" src="{0}/ExportElection?guid={1}"></iframe>'.filledWith(publicInterface.electionsUrl, guid));
    iframe.ready(function () {
      setTimeout(function () {
        ResetStatusDisplay();
        btn.removeClass('active');
      }, 1000);
    });
  };

  function deleteElection() {
    if (publicInterface.isGuest) return;


    var btn = $(this);
    var row = btn.closest('.Election');
    var name = row.find('.Detail').find('b').text();

    btn.addClass('active');
    row.addClass('deleting');

    if (!confirm('Completely delete this election from TallyJ?\n\n  {0}\n\n'.filledWith(name))) {
      btn.removeClass('active');
      row.removeClass('deleting');
      ResetStatusDisplay();
      return;
    }


    var form =
    {
      guid: row.data('guid')
    };

    CallAjax2(publicInterface.electionsUrl + '/DeleteElection', form,
      {
        busy: 'Deleting'
      },
      function (info) {
        btn.removeClass('active');
        if (info.Deleted) {
          row.slideUp(1000, 0, function () {
            row.remove();
            ShowStatusDone('Deleted.');
          });
        } else {
          row.removeClass('deleting');
          ShowStatusFailed(info.Message);
        }
      });
  };

  function loadElection() {
    $('#fileName').show();
  };

  function loadElection2() {
    var name = $('#fileName').val();
    console.log(name);
  };
  
  function scrollToMe(nameDiv) {
    var target = $(nameDiv);
    target.addClass('justloaded');

    var top = target.offset().top;
    var fudge = -83;
    var time = 800;

    try {
      $(document).animate({
        scrollTop: top + fudge
      }, time);
    } catch (e) {
      // ignore error
      console.log(e.message);
    }

    setTimeout(function () {
      target.toggleClass('justloaded', 'slow');
    }, 10000);
  };

  return publicInterface;
};

var electionListPage = ElectionListPage();

$(function () {
  electionListPage.PreparePage();
});

