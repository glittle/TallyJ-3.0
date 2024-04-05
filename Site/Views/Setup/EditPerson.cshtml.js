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
    applyValues(panel, personInfo, true);

    startEdit();
  };

  function changedIneligible() {
    var ddl = $(this);
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

    $('#trCanVote').toggleClass('IsNo', !canVote);
    $('#trCanReceiveVotes').toggleClass('IsNo', !canReceiveVotes);
  };

  function startEdit(doNotFocus) {
    var $first = local.hostPanel.find('[data-name="FirstName"]');
    var $last = local.hostPanel.find('[data-name="LastName"]');
    var update = function () {
      site.broadcast(site.broadcastCode.personNameChanging, $.trim($first.val() + ' ' + $last.val()));
    };
    $first.on('keyup', update);
    if (!doNotFocus) {
      $first.focus();
    }
    $last.on('keyup', update);
  };

  function fixPhone(ev) {
    var input = $(ev.target);
    var original = input.val();
    if (!original) {
      return;
    }
    var text = original.replace(/[^\+\d]/g, '');
    if (text.substr(0, 1) !== '+') {
      if (text.length === 10) {
        text = '1' + text;
      }
      text = '+' + text;
    }
    if (text !== original) {
      input.val(text);
    }
  }

  function applyValues(panel, personProperties, clearAll) {
    if (panel) {
      local.hostPanel = panel;
    } else {
      panel = local.hostPanel;
    }

    clearKioskCode();

    $('#btnDelete').attr('title', personProperties.C_RowId);
    // console.log(person, canDelete);

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
        var value = personProperties[input.data('name')];
        if (!value && value !== false) {
          value = '';
        }
        setValue(input, value);
      });
    } else {
      // apply only the properties given
      for (var prop in personProperties) {
        if (personProperties.hasOwnProperty(prop)) {
          panel.find(':input[data-name]').each(function () {
            var input = $(this);
            if (input.data('name') === prop) {
              setValue(input, personProperties[prop]);
            }
          });
        }
      }
    }

    var $phone = local.hostPanel.find('[data-name="Phone"]');
    $phone.on('change paste', fixPhone);

    showVotingMethod(personProperties.VotingMethod, personProperties.RegistrationLog);

    startEdit(true);

    $('#trCanVote').toggleClass('IsNo', !personProperties.CanVote);
    $('#trCanReceiveVotes').toggleClass('IsNo', !personProperties.CanReceiveVotes);
    //$('#trDelete').toggle(canDelete);

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
    var inputs = local.hostPanel.find(':input[data-name]');
    inputs.each(function () {
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

    var errors = [];

    if (!form.FirstName || !form.LastName) {
      errors.push('First and Last names are required.');
    }

    var phoneInput = local.hostPanel.find('input[data-name="Phone"]');
    var validationMessage = phoneInput[0].validationMessage;
    if (validationMessage) {
      errors.push('Phone Number: ' + validationMessage);
    }

    if (errors.length) {
      ShowStatusFailed(errors.join('<br>'), 10000);
      return;
    }

    CallAjax2(publicInterface.controllerUrl + '/SavePerson', form,
      {
        busy: 'Saving'
      },
      function (info) {
        if (info.Person) {
          applyValues(null, info.Person, true);
        }
        if (info.Message) {
          ShowStatusFailed(info.Message);
          return;
        }
        if (info.Person) {
          site.broadcast(site.broadcastCode.personSaved, info);
        }
        ShowStatusDone(info.Status);
      });
  };

  function deletePerson() {
    var form = {
      personId: local.hostPanel.find(':input[data-name=C_RowId]').val()
    };

    if (!form.personId) {
      // page may need a refresh?
      return;
    }

    CallAjax2(publicInterface.controllerUrl + '/DeletePerson', form,
      {
        busy: 'Deleting'
      },
      function (info) {
        if (info.Message) {
          ShowStatusFailed(info.Message);
          return;
        }
        if (info.Success) {
          site.broadcast(site.broadcastCode.personDeleted, form.personId);
          applyValues(null, {}, true);
          ShowStatusDone('Deleted');
          $('#editPanel').hide();
        }

        ShowStatusDone(info.Status);
      });
  }

  function generateKioskCode() {
    var form = {
      personId: local.hostPanel.find(':input[data-name=C_RowId]').val()
    };

    CallAjax2(publicInterface.controllerUrl + '/GenerateKioskCode', form,
      {
        busy: 'Getting code'
      },
      function (info) {
        if (info.Message) {
          ShowStatusFailed(info.Message);
          return;
        }
        if (info.Success) {
          $('.kioskNote').show();
          $('.kioskCode').text(info.Code).show();
        } else {
          // must have voted already
          $('.kioskCode').addClass('used').show();
        }

        ShowStatusDone(info.Status);
      });
  }

  function clearKioskCode() {
    $('.kioskCode').hide();
    $('.kioskNote').hide();
  }

  function showVotingMethod(code, log) {
    // reset email/phone inputs
    $('.emailPhone').prop('disabled', false);

    // show the log
    var logDiv = $('.votingMethodLog');

    var timeTemplate = peoplePage.T24 ? 'YYYY MMM D, H:mm' : 'YYYY MMM D, h:mm a';
    var registrationLog = (log || []).map(l => {
      var parts = l.split(';').map(s => s.trim());
      var time = parts[0];
      if (time.length > 6 && time[4] === '-') {
        parts[0] = moment(time).format(timeTemplate);
      }
      return parts.map(s => `<td>${s}</td>`).join('');
    });
    logDiv.html(registrationLog.map(s => `<tr>${s}</tr>`)).toggle(registrationLog.length > 0);

    // show the method
    var div = $('.showVotingMethod');
    if (!code) {
      div.text('-');
      $('tr.kiosk').show(); // if there
      $('.forDelete').show();
      return;
    }

    var msg = peoplePage && peoplePage.methods[code] || code;
    div.text(msg);
    $('tr.kiosk').hide(); // if there
    $('.forDelete').hide();

    // apply rule
    if (code === 'O') {
      $('.emailPhone').prop('disabled', true);
    }
  }

  function preparePage() {
    $('#btnSave').click(saveChanges);
    $('#btnDelete').click(deletePerson);
    $('#generateKioskCode').click(generateKioskCode);
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
    site.qTips.push({ selector: '#qTipBahaiId', title: 'Bahá\'í ID', text: 'Optional. The person\'s ID. Shows on Front Page and in final reports if elected.' });
    site.qTips.push({ selector: '#qTipEmail', title: 'Email Address', text: 'Optional. The person\'s email address. Can be used to vote online. Cannot be changed after voting online.' });
    site.qTips.push({ selector: '#qTipPhone', title: 'Phone Number', text: 'Optional. The person\'s phone number. Can be used to vote online. Cannot be changed after voting online.' });
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
    //    defaultRules: null,
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