var EditPersonPage = function () {
  var local = {
    inputField: null,
    hostPanel: null
  };

  function startNewPerson(panel, ineligible, first, last) {
    var reason = $.grep(publicInterface.invalidReasons, function (e) {
      return e.Guid === ineligible;
    });
    var personInfo = {
      C_RowId: -1,
      CanVote: true,
      CanReceiveVotes: true,
      FirstName: first,
      LastName: last
    };
    if (reason.length === 1) {
      personInfo.IneligibleReasonGuid = reason.Guid;
      personInfo.CanVote = reason.CanVote;
      personInfo.CanReceiveVotes = reason.CanReceiveVotes;
    }
    //    IneligibleReasonGuid: ineligible,
    //      CanVote: ineligible ? false : publicInterface.defaultRules.CanVote == 'A',
    //      CanReceiveVotes: ineligible ? false : publicInterface.defaultRules.CanReceive == 'A'
    applyValues(panel, personInfo, true);
    startEdit();
  };

  function changedIneligible() {
    let ddl = $(this);
    var ineligible = ddl.val();
    ddl[0].size = 1;

    var reason = $.grep(publicInterface.invalidReasons, function (e) {
      return e.Guid === ineligible;
    });

    var canVote = reason.length > 0 ? reason[0].CanVote : true; // publicInterface.defaultRules.CanVote == 'A';
    var canReceiveVotes = reason.length > 0 ? reason[0].CanReceiveVotes : true; // publicInterface.defaultRules.CanReceive == 'A';

    applyValues(null, {
      InEligible: reason.Guid,
      CanVote: canVote, //ineligible ? false : publicInterface.defaultRules.CanVote == 'A',
      CanReceiveVotes: canReceiveVotes // ineligible ? false : publicInterface.defaultRules.CanReceive == 'A'
    }, false);
  };

  function startEdit() {
    var $first = local.hostPanel.find('[data-name="FirstName"]');
    var $last = local.hostPanel.find('[data-name="LastName"]');
    var update = function() {
      site.broadcast(site.broadcastCode.personNameChanging, $.trim($first.val() + ' ' + $last.val()));
    };
    $first.on('keyup', update).focus();
    $last.on('keyup', update);
  };

  function applyValues(panel, person, clearAll) {
    if (panel) {
      local.hostPanel = panel;
    } else {
      panel = local.hostPanel;
    }

    function setValue(input, value) {
      switch (input.attr('type')) {
        case 'checkbox':
          input.prop('checked', value);
          break;
        default:
          input.val(value.toString());
          break;
      }
    };

    if (clearAll) {
      panel.find(':input[data-name]').each(function () {
        var input = $(this);
        var value = person[input.data('name')];
        if (!value && value !== false) {
          value = '';
        }
        setValue(input, value);
      });
    } else {
      // apply only the properties given
      for (var prop in person) {
        if (person.hasOwnProperty(prop)) {
          panel.find(':input[data-name]').each(function() {
            var input = $(this);
            if (input.data('name') === prop) {
              setValue(input, person[prop]);
            }
          });
        }
      }
    }

    panel.fadeIn();
    // panel.find('[data-name="FirstName"]').focus();

    //TODO...
    //        if (publicInterface.defaultRules.CanVoteLocked) {
    //            $('[data-name="CanVote"]').prop('enabled', false).propertyIsEnumerable('checked', publicInterface.defaultRules.CanVote=='A');
    //        }
    //        if (publicInterface.defaultRules.CanReceiveLocked) {
    //            $('[data-name="CanReceiveVote"]').prop('enabled', false).propertyIsEnumerable('checked', publicInterface.defaultRules.CanReceive=='A');
    //        }

    //        panel.fadeIn();
    //        panel.find('[data-name="FirstName"]').focus();
  };

  function saveChanges() {
    var form = {};
    local.hostPanel.find(':input[data-name]').each(function () {
      var input = $(this);
      var value;
      switch (input.attr('type')) {
        case 'checkbox':
          value = input.prop('checked');
          break;
        default:
          value = input.val();
          break;
      }
      form[input.data('name')] = value;
    });

    if (!(form.FirstName && form.LastName)) {
      alert('First and Last names are required.');
      return;
    }

    ShowStatusDisplay("Saving...");
    CallAjaxHandler(publicInterface.controllerUrl + '/SavePerson', form, function (info) {
      if (info.Message) {
        ShowStatusFailed(info.Message);
        return;
      }
      if (info.Person) {
        applyValues(null, info.Person, true);
        startEdit();

        site.broadcast(site.broadcastCode.personSaved, info);
      }
      ShowStatusDisplay(info.Status);
    });
  };


  function preparePage() {
    $('#btnSave').click(saveChanges);
    $('#ddlIneligible').html(prepareReasons()).change(changedIneligible);

    site.onbroadcast(site.broadcastCode.startNewPerson, function (ev, data) {
      startNewPerson($(data.panelSelector), data.ineligible, data.first, data.last);
    });

    site.qTips.push({ selector: '#qTipFName', title: 'First Name', text: 'These are the main names for this person. Both first and last name must be filled in.' });
    site.qTips.push({ selector: '#qTipLName', title: 'Last Name', text: 'These are the main names for this person. Both first and last name must be filled in.' });
    site.qTips.push({ selector: '#qTipOtherName', title: 'Other Names', text: 'Optional. If a person may be known by other first names, enter them here.' });
    site.qTips.push({ selector: '#qTipOtherLastName', title: 'Other Names', text: 'Optional. If a person may be known by other last names, enter them here.' });
    site.qTips.push({ selector: '#qTipOtherInfo', title: 'Other Identifying Information', text: 'Optional. Anything else that may be commonly used to identify this person. E.g. Doctor' });
    site.qTips.push({ selector: '#qTipArea', title: 'Sector / Area', text: 'Optional. For a city, the sector or neighbourhood they live in. For a regional or national election, their home town.' });
    site.qTips.push({ selector: '#qTipBahaiId', title: 'Bahá\'í ID', text: 'Optional. The person\'s ID. Shows in final reports if elected.' });
    site.qTips.push({
      selector: '#qTipIneligible',
      title: 'Ineligible',
      text: 'Most people are eligible to participate in the election by voting or being voted for.'
        + '<br><br>However, if this person is ineligible in some way, select the best reason here.'
    });
    //    site.qTips.push({ selector: '#qTipCanVote', title: 'Can Vote?', text: 'Override eligibility status. Check this box if this person can vote.' });
    //    site.qTips.push({ selector: '#qTipCanReceive', title: 'Tie Break?', text: 'Override eligibility status. Check this box if this person can be voted for.' });
  };

  function prepareReasons() {
    var html = ['<optgroup label="Eligible"><option value="">Eligible to vote and be voted for</option></optgroup>'];
    var group = '';
    $.each(publicInterface.invalidReasons, function () {
      var reasonGroup = this.Group;
      if (reasonGroup === 'Unreadable') return;
      if (reasonGroup !== group) {
        if (group) {
          html.push('</optgroup>');
        }
        html.push('<optgroup label="{0}">'.filledWith(reasonGroup));
        group = reasonGroup;
      }
      html.push('<option value="{Guid}">{Desc}</option>'.filledWith(this));
    });
    html.push('</optgroup>');
    return html.join('\n');
  };

  var publicInterface = {
    peopleUrl: '',
    invalidReasons: [],
    defaultRules: null,
    applyValues: applyValues,
    startNewPerson: startNewPerson,
    PreparePage: preparePage
  };
  return publicInterface;
};

var editPersonPage = EditPersonPage();

$(function () {
  editPersonPage.PreparePage();
});