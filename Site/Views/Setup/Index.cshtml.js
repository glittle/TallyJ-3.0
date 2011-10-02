/// <reference path=".././../Scripts/site.js" />
/// <reference path=".././../Scripts/jquery-1.6.4-vsdoc.js" />

var SetupIndexPage = function () {
  var ui = {
    canVoteLocked: false,
    canRecieveLocked: false,
    numLocked: false,
    extraLocked: false
  };
  var publicInterface = {
    info: {},
    setupUrl: '',
    PreparePage: function () {
      buildPage();

      adjustByType();

      applyValues(publicInterface.info.Election);

      $('#ddlType').live('change keyup', adjustByType);
      $('#ddlMode').live('change keyup', adjustByType);
      $('#ddlCanVote').live('change', changeCan);
      $('#ddlCanReceive').live('change', changeCan);

      $('#txtName').focus();
    }
  };

  var applyValues = function (election) {
    if (election == null) {
      // $('#txtDate').val(new Date());
      return;
    };

    $('#txtName').val(election.Name);
    $('#txtConvenor').val(election.Convenor);
    $('#txtDate').val(election.DateOfElection.parseJsonDate());

    $('#ddlType').val(election.ElectionType);
    $('#ddlMode').val(election.ElectionMode);


  };

  var changeCan = function () {
    // just need to save
  };

  var adjustByType = function () {
    var type = $('#ddlType').val();
    var mode = $('#ddlMode').val();

    //TODO: move this logic to server code, access by ajax call

    var num = 0;
    var extra = 0;
    var canVote = '';
    var canRecieve = '';

    $('#modeB').removeAttr('disabled');

    var combinedType = type + '-' + mode;
    switch (type) {
      case 'LSA':
        canVote = 'A';
        ui.canVoteLocked = true;

        extra = 0;
        ui.extraLocked = true;

        switch (mode) {
          case 'N':
            num = 9;
            ui.numLocked = true;
            canRecieve = 'A';
            break;
          case 'T':
            num = 1;
            ui.numLocked = false;
            canRecieve = 'N';
            break;
          case 'B':
            num = 1;
            ui.numLocked = false;
            canRecieve = 'A';
            break;

          default:
        }
        ui.canRecieveLocked = true;

        break;

      case 'NSA':
        canVote = 'N';  // delegates
        ui.canVoteLocked = true;

        extra = 0;
        ui.extraLocked = true;

        switch (mode) {
          case 'N':
            num = 9;
            ui.numLocked = true;
            canRecieve = 'A';
            break;
          case 'T':
            num = 1;
            ui.numLocked = false;
            canRecieve = 'N';
            break;
          case 'B':
            num = 1;
            ui.numLocked = false;
            canRecieve = 'A';
            break;

          default:
        }

        ui.canRecieveLocked = true;

        break;

      case 'Con':
        canVote = 'A';
        ui.canVoteLocked = true;

        $('#modeB').attr('disabled', 'disabled');
        if (mode == 'B') {
          $('#ddlMode').val('N');
          mode = 'N';
        }

        switch (mode) {
          case 'N':
            num = 5;
            ui.numLocked = false;

            extra = 3;
            ui.extraLocked = false;

            canRecieve = 'A';
            break;

          case 'T':
            num = 1;
            ui.numLocked = false;

            extra = 0;
            ui.extraLocked = true;

            canRecieve = 'N';
            break;

          default:
        }
        ui.canRecieveLocked = true;
        break;

      case 'Reg':
        canVote = 'N'; // LSA members
        ui.canVoteLocked = false;

        switch (mode) {
          case 'N':
            num = 9;
            ui.numLocked = false;

            extra = 3;
            ui.extraLocked = false;

            canRecieve = 'A';
            break;

          case 'T':
            num = 1;
            ui.numLocked = false;

            extra = 0;
            ui.extraLocked = true;

            canRecieve = 'N';
            break;

          case 'B':
            num = 1;
            ui.numLocked = false;

            extra = 0;
            ui.extraLocked = true;

            canRecieve = 'A';
            break;

          default:
        }
        ui.canRecieveLocked = true;
        break;

      case 'Oth':
        canVote = 'A';

        ui.canVoteLocked = false;
        ui.canRecieveLocked = false;
        ui.numLocked = false;
        ui.extraLocked = false;

        switch (mode) {
          case 'N':
            num = 9;
            extra = 0;
            canRecieve = 'A';
            break;

          case 'T':
            num = 1;
            extra = 0;
            canRecieve = 'N';
            break;

          case 'B':
            num = 1;
            extra = 0;
            canRecieve = 'A';
            break;

          default:
        }
        break;
    }

    $('#txtNames').prop('disabled', ui.numLocked).val(num);
    $('#txtExtras').prop('disabled', ui.extraLocked).val(extra);
    $('#ddlCanVote').prop('disabled', ui.canVoteLocked).val(canVote);
    $('#ddlCanReceive').prop('disabled', ui.canRecieveLocked).val(canRecieve);
  };

  var buildPage = function () {
    $('#editArea').html(site.templates.ElectionEditScreen.filledWith(publicInterface.info.Election));
  };

  return publicInterface;
};

var setupIndexPage = SetupIndexPage();

$(function () {
  setupIndexPage.PreparePage();
});