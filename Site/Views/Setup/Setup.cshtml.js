/**
 * Initializes the index page for the election setup.
 * This function sets up the necessary data structures, event listeners,
 * and Vue instance to manage the election setup process.
 *
 * @returns {Object} The public interface for the setup page, containing methods and properties.
 * @throws {Error} Throws an error if the initialization fails.
 *
 * @example
 * const setupPage = SetupIndexPage();
 * setupPage.PreparePage();
 */
﻿function SetupIndexPage() {
  var cachedRules = {
    // temporary cache of rules, for the life of this page
  };
  var settings = {
    tellerTemplate: '<div data-id={C_RowId}>{Name} <span class="ui-icon ui-icon-trash" title="Delete this teller name"></span></div>',
    badiDateGetter: null,
    dateKnown: false,
    isGlory13: null,
    vue: null
  };

  var publicInterface = {
    controllerUrl: '',
    hasBallots: false,
    hasOnlineBallots: false,
    Election: null,
    Locations: null,
    settings: settings,
    initialRules: function (type, mode, info) {
      var combined = type + '.' + mode;
      cachedRules[combined] = info;
    },
    PreparePage: preparePage
  };


  function preparePage() {

    settings.vue = new Vue({
      el: '#setupBody',
      name: 'setup',
      components: {
        'yes-no': YesNo,
      },
      data: {
        election: publicInterface.Election,
        locations: publicInterface.Locations,
        numLocations: publicInterface.Locations.length,
        MultipleLocations: publicInterface.Locations.length > 1,
        usingBallotProcess: false,
        isMounted: false,
        useOnline: false,
        isSaveNeeded: false,
        votingMethodsArray: [],
        custom1: '',
        custom2: '',
        custom3: '',
        electionType: '',
        originalElectionType: '',
        flags: [],
        flagsPre: '',
        dummy: 1,
        isKiosk: GetFromStorage('kiosk', 'N'),
      },
      computed: {
        onlineDatesOkay: function () {
          return (!this.useOnline && !this.election.OnlineWhenOpen && !this.election.OnlineWhenClose) ||
            this.useOnline &&
            this.election.OnlineWhenOpen &&
            this.election.OnlineWhenClose &&
            this.election.OnlineWhenOpen < this.election.OnlineWhenClose;
        },
        showImported: function () {
          return this.votingMethodsArray.includes('I');
        },
        closeIsPast: function () {
          return moment(this.election.OnlineWhenClose).isBefore();
        }
        //        onlineOpen: {
        //          get: function () {
        //            if (this.dummy > 0 && this.onlineElection && this.onlineElection.WhenOpen && !isNaN(this.onlineElection.WhenOpen)) {
        //              return this.onlineElection.WhenOpen.toISOString();
        //            }
        //            return '';
        //          },
        //          set: function (a) {
        //            this.onlineElection.WhenOpen = new Date(a);
        //            if (this.isMounted) $('.btnSave').addClass('btn-primary');
        //            this.dummy++;
        //          }
        //        },
        //        onlineClose: {
        //          get: function () {
        //            if (this.dummy > 0 && this.onlineElection && this.onlineElection.WhenClose && !isNaN(this.onlineElection.WhenClose)) {
        //              return this.onlineElection.WhenClose.toISOString();
        //            }
        //            return '';
        //          },
        //          set: function (a) {
        //            this.onlineElection.WhenClose = new Date(a);
        //            if (this.isMounted) $('.btnSave').addClass('btn-primary');
        //            this.dummy++;
        //          }
        //        }
      },
      watch: {
        'election.BallotProcessRaw': function (a) {
          this.replaceBodyBpClass(a);
        },
        isKiosk: function (a) {
          SetInStorage('kiosk', a);
        },
        usingBallotProcess: function (a) {
          if (!a) {
            this.election.BallotProcessRaw = 'None';
          } else {
            if (!this.election.BallotProcessRaw || this.election.BallotProcessRaw === 'None') {
              this.election.BallotProcessRaw = 'Roll'; // old default
              this.saveNeeded();
            }
          }
        },
        useOnline: function (a) {
          if (a) {
            if (this.election.OnlineCloseIsEstimate == null) {
              this.election.OnlineWhenOpen = '';
              this.election.OnlineWhenClose = '';
              this.election.OnlineCloseIsEstimate = true;
              this.election.OnlineSelectionProcess = 'B';
            };
            if (!this.votingMethodsArray.includes('O')) {
              this.votingMethodsArray.push('O');
            }
          } else {
            this.election.OnlineWhenOpen = null;
            this.election.OnlineWhenClose = null;
            this.election.OnlineCloseIsEstimate = null;
            this.election.OnlineSelectionProcess = null;
            this.votingMethodsArray = this.votingMethodsArray.filter(s => s !== 'O' && s !== 'K');
          }
        },
        flags: function (a, b) {
          if (!this.isMounted) return;

          if (a.some(str => /[^a-zA-Z]/.test(str))) {
            ShowStatusFailed('Checklist items must only be single words');
            this.flags = this.election.Flags.split('|');;
            return;
          }
          var newFlags = a.join('|');
          if (newFlags !== this.election.Flags) {
            this.election.Flags = newFlags;
            this.saveNeeded();
          }
        }
        //locations: function (a, b) {
        //  console.log('watch', a, b);
        //  // $('.btnSave').addClass('btn-primary');
        //}
      },
      created: function () {
        //        this.useOnline = !!this.election.OnlineWhenOpen;
        this.electionType = this.election.ElectionType;
        this.originalElectionType = this.election.ElectionType;
        if (this.election.OnlineWhenOpen || this.election.OnlineWhenClose) {
          this.useOnline = true;
          this.election.OnlineWhenOpen = this.election.OnlineWhenOpen ? moment.utc(this.election.OnlineWhenOpen).toISOString(true) : '';
          this.election.OnlineWhenClose = this.election.OnlineWhenClose ? moment.utc(this.election.OnlineWhenClose).toISOString(true) : '';
        }
      },
      mounted: function () {
        var bp = this.election.BallotProcessRaw;
        this.usingBallotProcess =
          bp === 'Unknown' || !bp ? null
            : bp === 'None' ? false : true;

        this.isMounted = true;

        this.flags = this.election.Flags?.split('|') || [];
        if (this.flags.length < 2) {
          this.flags.length = 2;
        }
        this.election.Flags = this.flags.join('|');

        var s = this.election.VotingMethods;
        this.votingMethodsArray = s ? s.split('') : [];

        this.useOnline = !!(this.election.OnlineWhenOpen || this.election.OnlineWhenClose); // do again, to set useOnline correctly

        var customs = ((this.election.CustomMethods || '') + '||').split('|');
        this.custom1 = customs[0];
        this.custom2 = customs[1];
        this.custom3 = customs[2];


        site.qTips.push({ selector: '#qTipLocked1', title: 'Election Locked', text: 'The core settings for the election will be locked when ballots are been entered.' });
        site.qTips.push({ selector: '#qTipLocked2', title: 'Election Locked', text: 'The core settings for the election are locked because ballots have been entered.' });
        site.qTips.push({ selector: '#qTipTest', title: 'Testing', text: 'This is just to help you keep your test elections separate. It has no other impact.' });
        site.qTips.push({ selector: '#qTipName', title: 'Election Name', text: 'This is shown at the top of each page, included in some reports, shown in the list of elections that tellers can join, and to online voters.' });
        site.qTips.push({ selector: '#qTipConvener', title: 'Convener', text: 'What institution is responsible for this election?  For local elections, this is typically the Local Spiritual Assembly.' });
        site.qTips.push({ selector: '#qTipDate', title: 'Election Date', text: 'When is this election being held?  LSA elections must be held on the day designated by the National Spiritual Assembly.' });
        //    site.qTips.push({ selector: '#qTipDate2', title: 'Choosing a Date', text: 'Date selection may have problems. Try different options, or type the date in the format: YYYY-MM-DD' });
        site.qTips.push({ selector: '#qTipType', title: 'Type of Election', text: 'Choose the type of election. This affects a number of aspects of TallyJ, including how tie-breaks are handled.' });
        site.qTips.push({ selector: '#qTipVariation', title: 'Variation of Election', text: 'Choose the variation for this election. This affects a number of aspects of TallyJ, including how vote spaces will appear on each ballot.' });
        site.qTips.push({ selector: '#qTipNum', title: 'Spaces on Ballot', text: 'This is the number of names that will be written on each ballot paper.' });
        site.qTips.push({ selector: '#qTipNumNext', title: 'Next Highest', text: 'For Conventions only. This is the number of those with the "next highest number of votes" to be reported to the National Spiritual Assembly. If changed after running Analyze, run Analyze again!' });
        site.qTips.push({ selector: '#qTipCanVote', title: 'Who can vote', text: 'Either "everyone" or "named" individuals. This is dictated by the type of election and can be adjusted per person.' });
        site.qTips.push({ selector: '#qTipCanReceive', title: 'Who can be voted for?', text: 'Either "everyone" or "named" individuals. This is dictated by the type of election and can be adjusted per person.' });
        //site.qTips.push({ selector: '#qTipUpdate', title: 'Update', text: 'This only needs to be clicked if the type of election has been changed.  This does not alter any data entered in the election.' });
        site.qTips.push({ selector: '#qTipShow', title: 'Allow Tellers Access?', text: 'If checked, this election is listed on the TallyJ home page so that other tellers can join in.  Even if turned on, the election will only appear when you, or a registered teller, is logged in and active.' });
        site.qTips.push({ selector: '#qTipShowCalled', title: 'Show "Called In"?', text: 'Are you accepting ballot by phone?' });
        site.qTips.push({ selector: '#qTipAccess', title: 'Access Code', text: 'This is a "pass phrase" that tellers need to supply to join the election.  It can be up to 50 letters long, and can include spaces.  You can change it here any time.  If this is empty, no other teller will be able to join.' });
        site.qTips.push({ selector: '#qTipLocation', title: 'Locations', text: 'If this election is being held simultaneously in multiple locations or polling stations, add names for each location here.' });
        site.qTips.push({ selector: '#qTipTellers', title: 'Tellers', text: 'When tellers are using computers for entering ballots or at the Front Desk, they should select their name near the top of that screen. These names can be informal, first names, and will not be included in printed reports.' });
        site.qTips.push({ selector: '#qTipPreBallot', title: 'Pre-Ballot', text: 'If you will not be using the Front Desk and Roll Call pages, only using TallyJ to input the ballots collected, you can hide those pages.' });
        site.qTips.push({ selector: '#qTipMask', title: 'Mask Voting Method', text: 'In the Roll Call, and final Tellers\' Report, show "Envelope" instead of "Mailed In", "Dropped Off" or "Called In."' });
        site.qTips.push({ selector: '#qTipReset', title: 'Reset', text: 'Everyone is updated automatically if the election type is changed. Click to set everyone now. After resetting, apply special eligibility as needed. (Be sure to Save changes before using Reset.)' });
        site.qTips.push({ selector: '#qTipNoteB', title: 'By-election', text: 'Be sure to set the eligibility of each current member of this institution to "On Institution already".' });
        site.qTips.push({ selector: '#qTipNoteT', title: 'Tie-break', text: 'Be sure to set the eligibility of each of the people tied in this tie-break.' });
        site.qTips.push({ selector: '#qTipNoteN', title: 'National Election', text: 'To use the Front Desk and Roll Call pages, be sure to set the eligibility of each delegate.' });
        site.qTips.push({ selector: '#qTipNoteLSA2', title: 'Second Stage Local Election', text: 'To use the Front Desk and Roll Call pages, be sure to set the eligibility of each delegate.' });
        site.qTips.push({ selector: '#qTipEnvNum', title: 'Envelope Numbers', text: 'For every ballot envelope received, a number is created. When appropriate this should be associated with the envelope until all envelopes are ready to be opened.' });
        //site.qTips.push({ selector: '#qTip', title: '', text: '' });

      },
      methods: {
        removeLocation: function (domIcon) {
          var input = $(domIcon).closest('div').find('input');
          locationChanged(input, true);
        },
        addFlag() {
          this.flags.push('');
        },
        removeFlag(index) {
          this.flags.splice(index, 1);
        },
        replaceBodyBpClass: function (process) {
          var list = window.document.body.classList;
          list.forEach(function (key) {
            if (key.substring(0, 3) === 'BP-') {
              list.remove(key);
            }
          });
          list.add('BP-' + process);
        },
        saveNeeded: function () {
          if (!this.isMounted) return;
          if (!this.onlineDatesOkay) return;

          $('.btnSave').addClass('btn-primary');
          this.isSaveNeeded = true;
        },
        showFrom: function (when) {
          if (!when) return '';
          return '(' + moment(when).fromNow() + ')';
        },
      }
    });

    $(document).on('change keyup', '#ddlType, #ddlMode', function (ev) {
      startToAdjustByType(ev);
      $('.showGlory13').toggle(($('#ddlType').val() === 'LSA' || $('#ddlType').val() === 'LSA2') && $('#ddlMode').val() === 'N');
      getBadiDate();
    });

    //$(document).on('change keyup', '#ddlMode', startToAdjustByType);

    $(document).on('click', '.btnSave', saveChanges);
    $(document).on('click', '#btnAddLocation', addLocation);

    $('.Demographics').on('change keyup', '*:input', function () {
      var input = $(this);

      if (input.closest('.forLocations').length || input.parent().parent().hasClass('ignore')) {
        return; // don't flag location related inputs
      }
      setTimeout(function () {
        settings.vue.saveNeeded();
      }, 0);
    });

    $('#chkPreBallot').on('change', showForPreBallot);
    //$('#chkMultipleLocations').on('change', showLocations);

    $('#locationList').on('change', 'input', function () {
      locationChanged($(this));
    });
    //$('#locationList').on('click', '.ui-icon-trash', function () {
    //  locationChanged($(this).parent().find('input'), true);
    //});

    $('#tellersList').on('click', '.ui-icon-trash', deleteTeller);

    //    $('#btnResetList').click(resetAllCanVote);

    $('#txtDate').on('change', getBadiDate);

    $('#txtDate').attr('type', 'date');

    applyValues(publicInterface.Election);
    showLocations(publicInterface.Locations);
    showTellers(publicInterface.Tellers);

    $('.showGlory13').toggle(($('#ddlType').val() === 'LSA' || $('#ddlType').val() === 'LSA2') && $('#ddlMode').val() === 'N');
    //$('#txtName').focus();


    $(window).on('beforeunload', function () {
      if ($('.btnSave').hasClass('btn-primary')) {
        return "Changes have been made and not saved.";
      }
    });

    settings.badiDateGetter = BadiDateToday({
      locationIdentification: 3,
      use24HourClock: settings.vue.election.T24
    });

    getBadiDate();
  };

  function getBadiDate() {
    settings.dateKnown = true;
    var dateStr = $('#txtDate').val();
    var dateParts = dateStr.split('-');
    if (dateStr.length !== 10 || dateParts.length !== 3) {
      return; // expecting 2020-04-21
    }
    settings.isGlory13 = null;
    var d = new Date(+dateParts[0], +dateParts[1] - 1, +dateParts[2]);

    d.setHours(12, 0, 0, 0, 0); // noon
    settings.badiDateGetter.refresh({
      currentTime: d,
      onReady: function (di) {
        var startWord = di.frag1 >= new Date() ? 'Starting' : 'Started';

        showBadiInfo(di, $('#badiDateBefore'), startWord + ' before sunset? &rarr; ');

        d.setDate(d.getDate() + 1);
        settings.badiDateGetter.refresh({
          currentTime: d,
          onReady: function (di) {
            showBadiInfo(di, $('#badiDateAfter'), startWord + ' after sunset? &rarr; ');
            showMoreBadiInfo(di);
          }
        });
      }
    });
  }

  function showLatLong(di) {
    if (!di.longitude) {
      return '';
    }
    var ns = di.latitude < 0 ? 'S' : 'N';
    var ew = di.longitude < 0 ? 'W' : 'E';
    return `<span class=latlong>(${Math.abs(di.latitude).toFixed(2)} ${ns}, ${Math.abs(di.longitude).toFixed(2)} ${ew})</span> `;
  }

  function showMoreBadiInfo(di) {
    var isFuture = di.frag1SunTimes.sunset > new Date();
    if (!di.location) {
      di.location = 'your area';
    }
    var msg = di.longitude
      ? 'Sunset in {^location} <span class=locationDetail title="{latitude}, {longitude}">' + showLatLong(di) + '</span> ' + (isFuture ? 'will be' : 'was') + ' at {startingSunsetDesc} on {frag1Day} {frag1MonthLong}.'
      : '';
    $('#badiDateIntro').html(msg.filledWith(di));

    //    console.log(di);

    // found 1st Ridván for an LSA election?
    $('.badiDateName').removeClass('isGlory13');
    var found = !!settings.isGlory13;
    if (found) {
      settings.isGlory13.addClass('isGlory13');
    }
    $('.showGlory13').toggleClass('missing', ($('#ddlType').val() === 'LSA' || $('#ddlType').val() === 'LSA2') && $('#ddlMode').val() === 'N' && !found);
  }

  function showBadiInfo(di, target, intro) {
    if (di.bMonth === 2 && di.bDay === 13 && ($('#ddlType').val() === 'LSA' || $('#ddlType').val() === 'LSA2') && $('#ddlMode').val() === 'N') {
      settings.isGlory13 = target;
    }

    var msg = intro + '<span class=badiDateValue>{bDay} {bMonthMeaning} ({bMonthNameAr}) {bYear}</span>';
    target.html(msg.filledWith(di));
  }


  function showTellers(tellers) {
    //        if (tellers == null) {
    //            $('#tellersList').html('[None]');
    //            return;
    //        }
    if (tellers == null || tellers.length === 0) {
      return;
    }
    $('#tellersList').html(settings.tellerTemplate.filledWithEach(tellers));
  };

  //  function resetAllCanVote() {
  //
  //    ShowStatusBusy("Updating...");
  //    CallAjaxHandler(publicInterface.controllerUrl + '/ResetInvolvementFlags', null, function (info) {
  //      ResetStatusDisplay();
  //    });
  //
  //  };

  function deleteTeller(ev) {
    var icon = $(ev.target);
    var targetDiv = $(icon.parent());
    var target = targetDiv.data('id');
    var form = { id: target };
    CallAjax2(GetRootUrl() + 'Dashboard/DeleteTeller', form,
      {
        busy: 'Deleting'
      },
      function (info) {
        if (info.Deleted) {
          targetDiv.remove();
        }
        else {
          ShowStatusFailed(info.Error);
        }
      });
  };


  function addLocation() {
    var location = {
      C_RowId: -1
    };

    settings.vue.locations.push(location);

    settings.vue.numLocations++;

    //var line = $(settings.locationTemplate.filledWith(location));
    //line.appendTo('#locationList').find('input').focus();

    //setupLocationSortable();
  };

  //function startMultipleLocations() {
  //  var count = $('#locationList input').length;
  //  console.log('add', count)
  //  for (var i = count; i < 2; i++) {
  //    addLocation();
  //  }
  //}

  //function removeAdditionalLocations() {
  //  // called when YN set to false
  //  //remove any with -1
  //  $('#locationList input').each(function (i, el) {
  //    var loc = $(el);
  //    if (loc.data('id') == -1) {
  //      locationChanged(loc, true);
  //    }
  //  });

  //  // remove all but one - 
  //  var toDelete = [];
  //  for (var i = 1; i < $('#locationList input').length; i++) {
  //    toDelete.push($('#locationList input').eq(i));
  //  }

  //  for (i = 0; i < toDelete.length; i++) {
  //    // fire off calls to delete these
  //    locationChanged(toDelete[i], true);
  //  }

  //}

  function showLocations(locations) {
    // use Vue to create them, but not manage after...
    settings.vue.locations = locations;
    //$('#locationList').html(settings.locationTemplate.filledWithEach(locations));
    setupLocationSortable();
  };

  function locationChanged(input, deleteThis) {
    var form = {
      id: input.data('id'),
      text: deleteThis ? '' : input.val()
    };

    if (form.id === -1 && !form.text) {
      // deleting a new one
      input.parent().remove();
      settings.vue.numLocations = $('#locationList input').length;
      return;
    }

    CallAjax2(publicInterface.controllerUrl + '/EditLocation', form,
      {
        busy: 'Saving'
      },
      function (info) {
        if (info.Success) {
          ShowStatusDone(info.Status);
          if (info.Id === 0) {
            // removed
            input.parent().remove();

            // vue array is disconnected from the DOM array... need the length matches the DOM
            settings.vue.numLocations = $('#locationList input').length;

            // setupLocationSortable();
          } else {
            input.val(info.Text);
            if (info.Id !== form.id) {
              input.data('id', info.Id);
            }
          }
        } else {
          ShowStatusFailed(info.Status);
        }
      });
  };

  function setupLocationSortable() {
    $('#locationList').sortable({
      handle: '.ui-icon',
      stop: orderChanged,
      axis: 'y',
      containment: 'parent',
      tolerance: 'pointer'
    });
  };

  function orderChanged(ev, ui) {
    var ids = [];
    $('#locationList input').each(function () {
      var id = $(this).data('id');
      if (+id < 1) {
        // an item not saved yet!?
      }
      ids.push(id);
    });
    var form = {
      ids: ids
    };
    CallAjax2(publicInterface.controllerUrl + '/SortLocations', form,
      {
        busy: 'Saving locations',
        done: 'Locations Saved'
      });
  };

  function applyValues(election) {
    if (election == null) {
      return;
    };

    $('.Demographics :input[data-name]').each(function () {
      var input = $(this);
      var dataName = input.data('name');
      var value = election[dataName] || '';
      switch (input.attr('type')) {
        case 'date':
          var dateString = value ? moment(value).format('YYYY-MM-DD') : ''; // ('' + value).parseJsonDateForInput();
          input.val(dateString);
          break;
        case 'checkbox':
          input.prop('checked', value);
          break;
        default:
          if (input.attr('id') === 'txtDate' && value) {
            value = moment(value).toISOString(true);
          }
          input.val(value);
          break;
      }
    });
    $('span[data-name]').each(function () {
      //console.log(this.tagName);
      var input = $(this);
      var value = election[input.data('name')] || '';
      input.html(value);
    });

    showForPreBallot();

    startToAdjustByType();
  };

  function showForPreBallot() {
    var usePreBallot = $('#chkPreBallot').prop('checked');
    $('.forPreBallot').toggle(usePreBallot);
  }

  /**
   * Saves the changes made to the election settings.
   *
   * This function gathers data from the Vue instance and constructs a form object
   * that is sent to the server via an AJAX call. It validates input fields, checks for
   * special characters in custom voting methods, and manages the state of the kiosk setting.
   *
   * @throws {Error} Throws an error if the election name is not provided or if there are
   *                 special characters in the custom voting methods.
   *
   * @returns {void}
   *
   * @example
   * // To save changes after modifying election settings
   * saveChanges();
   */
  function saveChanges() {
    var vue = settings.vue;
    var election = vue.election;

    var testCustom = vue.custom1 + vue.custom2 + vue.custom3;
    var customs = '';
    if (testCustom) {
      if (/[|<]/.test(testCustom)) {
        ShowStatusFailed('Special characters not allowed in voting methods.');
        return;
      }
      customs = [vue.custom1, vue.custom2, vue.custom3].join('|');
    }

    if (!vue.votingMethodsArray.includes('K') && vue.isKiosk === 'Y') {
      vue.isKiosk = 'N';
    }

    var votingMethods = vue.votingMethodsArray.join('');
    //    if (vue.useOnline && !votingMethods.includes('O')) {
    //      votingMethods += 'O';
    //    }
    var flags = election.Flags.split('|').filter(s => s).join('|'); // remove any empties

    var form = {
      C_RowId: election.C_RowId,
      ElectionType: election.ElectionType,
      ShowAsTest: election.ShowAsTest,
      BallotProcessRaw: election.BallotProcessRaw,
      EnvNumModeRaw: election.EnvNumModeRaw,
      //UseCallInButton: election.UseCallInButton,
      ListForPublic: election.ListForPublic,
      ElectionPasscode: election.ElectionPasscode,
      T24: election.T24,
      OnlineWhenOpen: election.OnlineWhenOpen,
      OnlineWhenClose: election.OnlineWhenClose,
      OnlineCloseIsEstimate: election.OnlineCloseIsEstimate,
      OnlineSelectionProcess: election.OnlineSelectionProcess,
      EmailFromName: election.EmailFromName,
      EmailFromAddress: election.EmailFromAddress,
      RandomizeVotersList: election.RandomizeVotersList,
      GuestTellersCanAddPeople: election.GuestTellersCanAddPeople,
      VotingMethods: votingMethods,
      CustomMethods: customs,
      Flags: flags,
    };

    $(':input[data-name]').each(function () {
      var input = $(this);
      var value;
      switch (input.attr('type')) {
        case 'checkbox':
          value = input.prop('checked');
          break;
        case 'date':
          value = moment(input.val()).toISOString();
          break;
        default:
          value = input.val();
          break;
      }
      form[input.data('name')] = value;
    });

    if (!form.Name) {
      ShowStatusFailed('Election name is required.');
      return;
    }

    CallAjax2(publicInterface.controllerUrl + '/SaveElection', form,
      {
        busy: 'Saving'
      },
      function (info) {
        if (info.success) {
          vue.isMounted = false;
          if (info.Election) {

            vue.flags = info.Election.Flags?.split('|') || [];
            if (vue.flags.length < 2) {
              vue.flags.length = 2; // we want at least 2 flags or blank spaces
            }
            vue.election.Flags = vue.flags.join('|'); // update the value in case it was null or less than 2


            applyValues(info.Election);
            $('.CurrentElectionName').text(info.displayName);
            site.passcodeRaw =
              site.passcode = info.Election.ElectionPasscode;
            updatePasscodeDisplay(info.Election.ListForPublic, info.Election.ElectionPasscode);

          }

          publicInterface.defaultFromAddress = info.defaultFromAddress;

          $('.btnSave').removeClass('btn-primary');
          vue.isSaveNeeded = false;
          vue.isMounted = true;

          if (form.OnlineWhenClose) {
            var isClosed = moment(form.OnlineWhenClose).isBefore();
            $('body').toggleClass('OnlineOpen', !isClosed);
            $('body').toggleClass('OnlineClosed', isClosed);
          } else {
            $('body').removeClass('OnlineClosed, OnlineOpen');
          }
          ShowStatusDone(info.Status);
        } else {
          ShowStatusFailed(info.Status);
          if (info.Election) {
            applyValues(info.Election);
            $('.CurrentElectionName').text(info.displayName);
          }
        }
      });
  };

  function startToAdjustByType() {

    var type = $('#ddlType').val();
    var mode = $('#ddlMode').val();

    if (type === 'Con' || type === 'LSA1') {
      if (mode === "B") {
        mode = "N";
        $("#ddlMode").val("N");
      }
      $("#modeB").attr("disabled", "disabled");
    } else {
      $("#modeB").removeAttr("disabled");
    }

    var classes = [];
    if (type === 'NSA') {
      classes.push('NoteN');
    }
    if (type === 'LSA2') {
      classes.push('NoteLSA2');
    }
    if (mode === 'B') {
      classes.push('NoteB');
    }
    if (mode === 'T') {
      classes.push('NoteT');
    }
    $('#VariationNotice').removeClass().addClass(classes.join(' ')).toggle(classes.length !== 0);

    var combined = type + '.' + mode;
    var cachedRule = cachedRules[combined];
    if (cachedRule) {
      applyRules(cachedRule);
      return;
    }

    var form = {
      type: type,
      mode: mode
    };

    CallAjaxHandler(publicInterface.controllerUrl + '/DetermineRules', form, applyRules, combined);
  };

  function applyRules(info, combined) {

    var setRule = function (target, locked, value) {
      target.prop('disabled', locked);
      target.data('disabled', locked);
      if (locked || !publicInterface.Election || !publicInterface.Election.DateOfElection) {
        target.val(value);
      }
    };

    setRule($('#txtNames'), info.NumLocked, info.Num);
    setRule($('#txtExtras'), info.ExtraLocked, info.Extra);

    var lockedAfterBallots = publicInterface.hasBallots || publicInterface.hasOnlineBallots;

    // core
    $('.electionDetail select, .electionDetail input[type=number]').each(function () {
      var input = $(this);
      if (!input.data('disabled')) {
        input.prop('disabled', lockedAfterBallots);
      }
    });
    if (lockedAfterBallots) {
      $('#qTipLocked2').css({ display: 'inline-block' });
      $('#qTipLocked1').hide();
    } else {
      $('#qTipLocked2').hide();
    }

    // online
    $('.lockAfterBallots input').each(function () {
      var input = $(this);
      if (!input.data('disabled')) {
        input.prop('disabled', publicInterface.hasOnlineBallots);
      }
    });
    $('.explainLock').toggle(publicInterface.hasOnlineBallots);

    cachedRules[combined] = info;
  }

  return publicInterface;
};

var setupIndexPage = SetupIndexPage();

$(function () {
  setupIndexPage.PreparePage();
});

var YesNo = Vue.component('yes-no',
  {
    template: '#yes-no',
    props: {
      value: Boolean,
      disabled: Boolean,
      yes: {
        type: String,
        default: 'Yes'
      },
      no: {
        type: String,
        default: 'No'
      },
      toggleColor: {
        type: Boolean,
        default: true
      }
    },
    data: function () {
      return {
        yesNo: this.value ? 'Y' : 'N'
      }
    },
    watch: {
      value: function (a) {
        this.yesNo = a ? 'Y' : 'N';
      },
      yesNo: function (a) {
        this.$emit('input', a === 'Y');
      }
    }
  });


var EnvMode = Vue.component('env-mode', {
  template: '#env-mode',
  props: {
    value: String
  },
  data: function () {
    return {
      mode: this.value
    }
  },
  watch: {
    mode: function (a) {
      this.$emit('input', this.mode);
    }
  }
})