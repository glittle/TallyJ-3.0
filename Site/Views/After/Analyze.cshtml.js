function AnalyzePage() {
  var settings = {
    rowTemplate: '',
    info: {},
    invalidsRowTemplate: '',
    tieResultRowTemplate: '',
    chart: null,
    hasCloseVote: false,
    hasTie: false,
    calledInTotal: 0,
    onlineTotal: 0
  };

  function preparePage() {
    connectToAnalyzeHub();

    setReadyStatus(site.electionState === 'Finalized');

    site.qTips.push({ selector: '#qTipUnEntered', title: 'Spoiled Ballots', text: 'The calculated number shows the entered ballots that were found to be spoiled. If some spoiled ballots were not entered into TallyJ, enter the number here. ' });
    site.qTips.push({ selector: '#qTipSpoiledVotes', title: 'Spoiled Votes', text: 'These are spoiled votes that are on valid ballots.' });

    $('#btnRefresh').click(function () {
      runAnalysis(false);
      $('#btnShowLog').show();
    });

    $('#btnShowLog').click(function () {
      showLog();
    });

    $('#body').on('click', '.btnSaveTieCounts', saveTieCounts);
    $('#body').on('click', '.btnSaveManualCounts', saveManualCounts);
    $('#body').on('change', 'input[name=status]', changeStatus);

    $('#body').on('change keyup', 'input.Manual', function () {
      $('.btnSaveManualCounts').addClass('btn-primary');
      diableAnalysisBtnIfNeeded();
    });
    $('#body').on('change keyup', 'input.TieBreakCount', function () {
      $('.btnSaveTieCounts').addClass('btn-primary');
      diableAnalysisBtnIfNeeded();
    });

    settings.rowTemplate = $('#mainTableRow').text();
    settings.tieResultRowTemplate = $('#tieTableRow').text();
    settings.invalidsRowTemplate = $('#invalidsItem').text();

    if (publicInterface.results) {
      $('#InitialMsg').text('Loading results...');
      showInfo(publicInterface.results, true);
    }
    else {
      $('#btnRefreshDiv').show();
      //setTimeout(function () {
      //  runAnalysis(true);
      //}, 0);
    }

    site.onbroadcast(site.broadcastCode.electionStatusChanged, function (ev, info) {
      console.log(info);
      var id = info.StateName === 'Finalized' ? '#rbFinalized' : '#rbNotFinalized';
      $(id).prop('checked', true);
    });

    $(window).on('beforeunload', function () {
      if ($('.btnSaveManualCounts').hasClass('btn-primary') || $('.btnSaveTieCounts').hasClass('btn-primary')) {
        return "Changes have been made and not saved.";
      }
    });

  };

  function diableAnalysisBtnIfNeeded() {
    var disable = $('.btnSaveManualCounts').hasClass('btn-primary') || $('.btnSaveTieCounts').hasClass('btn-primary');
    $('#btnRefresh').prop('disabled', disable);
  }

  function setReadyStatus(ready) {
    $('body').removeClass('analyzing ready notReady');
    $('body').addClass(ready ? 'ready' : 'notReady');
    $('.setStatus input').prop('disabled', !ready);
  }

  function changeStatus(ev) {
    var rb = $(ev.target);

    var form = {
      state: (rb.val() === 'Finalized' && rb.prop('checked')) ? 'Finalized' : 'Tallying'
    };
    ShowStatusDisplay('Saving...');
    CallAjaxHandler(site.rootUrl + 'Elections/UpdateElectionStatus', form, function (info) {
      if (info.Message) {
        ShowStatusFailed(info.Message);
        return;
      }
      ResetStatusDisplay();
      site.broadcast(site.broadcastCode.electionStatusChanged, info);
    });

  }

  function showLog(display) {
    var wasShowing = $('#loadingLog').is(':visible');
    if (typeof display != 'undefined') {
      wasShowing = !display;
    }
    $('#loadingLog').toggle(!wasShowing);
    $('#btnShowLog').html(wasShowing ? 'Show Analysis Log' : 'Hide Analysis Log');
  }

  function connectToAnalyzeHub() {
    var hub = $.connection.analyzeHubCore;

    hub.client.loadStatus = function (msg, isTemp) {
      var mainLogDiv = $('#log');

      if (msg.search('Starting Analysis') == 0) {
        mainLogDiv.html('');
      }

      showLog(true);

      var tempLogDiv = $('#tempLog');
      if (isTemp) {
        tempLogDiv.html(msg);
      } else {
        tempLogDiv.html('');
        mainLogDiv.append('<div>' + msg + '</div>');
      }
    };

    startSignalR(function () {
      console.log('Joining analyze hub');
      CallAjaxHandler(analyzePage.analyzeHubUrl, { connId: site.signalrConnectionId }, function (info) {
        if (!info) {
          console.log(info);
        }
      });
    });
  };


  function runAnalysis(firstLoad) {
    if ($('.btnSaveManualCounts').hasClass('btn-primary') || $('.btnSaveTieCounts').hasClass('btn-primary')) {

    }

    ShowStatusDisplay('Analyzing ballots...', 0, 5 * 60 * 1000);
    $('body').removeClass('notReady ready');
    $('body').addClass('analyzing');
    $('#InitialMsg').text('Analyzing all ballots...').removeClass('bad').show();

    showLog(true);
    $('#log, #tempLog').html('');

    CallAjaxHandler(publicInterface.controllerUrl + '/RunAnalyze', null, showInfo, firstLoad);
  };

  function showInfo(info, firstLoad) {
    $('#btnRefresh').text('Re-run Analysis');
    $('#btnRefreshDiv').show();

    if (info.Interrupted) {
      console.log(info.Msg);
      $('#InitialMsg').addClass('bad').text('Analysis failed. This may happen if tellers are actively entering ballots.');
      return;
    }

    var votesTable = $('table.Main');
    var invalidsTable = $('table#invalids');
    var instructionsTable = $('table#instructions');
    var table;

    settings.info = info;

    showLog(false);
    $('#InitialMsg').hide();
    $('#tieResults').hide();
    $('#HasCloseVote').hide();

    ResetStatusDisplay();

    if (info.Votes) {
      $('.NoAnalysis').hide();

      table = votesTable;
      votesTable.show();
      invalidsTable.hide();
      instructionsTable.show();

      $('#mainBody').html(settings.rowTemplate.filledWithEach(expand(info.Votes)));
      showTies(info);

      $('#HasCloseVote').toggle(settings.hasCloseVote);
      $('.HasTie').toggle(settings.hasTie);

      if (info.Votes.length != 0) {
        var max = info.Votes[0].VoteCount;

        $('.ChartLine').each(function () {
          var item = $(this);
          item.animate({
            width: (item.data('value') / max * 100) + '%'
          }, {
              duration: 2000
            });
        });

        var groupMax = 0;
        var currentGroup = '';
        $('.ChartLineTie').each(function () {
          var item = $(this);
          var value = item.data('value');
          if (value) {
            var group = item.data('group');
            if (group != currentGroup) {
              currentGroup = group;
              groupMax = value;
            }
            item.animate({
              width: (value / groupMax * 100) + '%'
            }, {
                duration: 2000
              });
          }
        });

      }
    }
    else {
      table = invalidsTable;
      votesTable.hide();
      invalidsTable.show();
      instructionsTable.hide();

      $('#invalidsBody').html(settings.invalidsRowTemplate.filledWithEach(expandInvalids(info.NeedReview)));
    }

    settings.calledInTotal = 0;
    fillValues('Calc', info.ResultsCalc);
    fillValues('Manual', info.ResultsManual);
    fillValues('Final', info.ResultsFinal);
    summarizeCounts();

    table.show();
  };

  function summarizeCounts() {
    $('#totalCounts').toggleClass('hideCalledIn',settings.calledInTotal > 0 || !!settings.info.ShowCalledIn);
    $('#totalCounts').toggleClass('hideOnline', settings.onlineTotal > 0 || !!settings.info.ShowOnline);
    $('#totalCounts tr').each(function () {
      var row = $(this);
      var calcSpan = row.find('span.Calc');
      var calcText = calcSpan.text();

      var finalSpan = row.find('span.Final');
      var finalText = finalSpan.text();

      //finalSpan.toggleClass('changed', calcText != '' && calcText != finalText);
      calcSpan.toggleClass('changed', finalText != '' && calcText != finalText && !calcSpan.hasClass('NoChanges'));
    });

    var countsMatch = settings.info.ResultsFinal.NumBallotsWithManual === settings.info.ResultsFinal.SumOfEnvelopesCollected;
    $('#totalCounts').toggleClass('mismatch', !countsMatch);
    setReadyStatus(settings.info.ResultsFinal.UseOnReports && countsMatch);
  };

  function fillValues(name, results) {
    if (results.CalledInBallots) {
      settings.calledInTotal += results.CalledInBallots;
    }
    if (results.OnlineBallots) {
      settings.onlineTotal += results.OnlineBallots;
    }
    $('#totalCounts').find('span.{0}[data-name]'.filledWith(name)).each(function () {
      var span = $(this);
      var value = results[span.data('name')];
      span.text(value || '0');
    });
    $('#totalCounts').find('input.{0}[data-name]'.filledWith(name)).each(function () {
      var input = $(this);
      var value = results[input.data('name')];
      input.val(value);
    });

  };

  function expandInvalids(needReview) {
    $.each(needReview, function () {
      this.Ballot = '<a target=L{LocationId} href="../Ballots?l={LocationId}&b={BallotId}">{Ballot}</a>'.filledWith(this);
      this.Link = this.PositionOnBallot;
    });
    return needReview;
  };

  function expand(results) {
    settings.hasCloseVote = false;
    var okayMark = ' ✓';

    $.each(results, function (i) {
      if (!this.TieBreakRequired) {
        this.TieBreakCount = 0;
      }
      this.ClassName = 'Section{0} {1} {2} {3}'.filledWith(
        this.Section,
        this.Section == 'O' && this.ForceShowInOther ? 'Force' : '',
        (i % 2 == 0 ? 'Even' : 'Odd'),
        (this.IsTied && this.TieBreakRequired ? (this.IsTieResolved ? 'Resolved' : 'Tied') : ''));
      this.TieVote = this.IsTied ? (this.TieBreakRequired ? ('Tie ' + this.TieBreakGroup + (this.IsTieResolved ? okayMark : '')) : 'Tie ' + this.TieBreakGroup + okayMark) : '';

      if (this.IsTied) {
        this.CloseUpDown = '=';
      }
      else if (this.CloseToNext) {
        this.CloseUpDown = this.CloseToPrev ? '&#8597;' : '&#8595;';
      } else if (this.CloseToPrev) {
        this.CloseUpDown = '&#8593;';
      }

      if ((this.Section == 'T' || this.Section == 'E')
        && (this.CloseToNext || this.CloseToPrev)) {
        settings.hasCloseVote = true;
      }
      this.VoteDisplay = this.VoteCount + (this.TieBreakRequired ? ', ' + this.TieBreakCount : '');
    });
    return results;
  };

  function showTies(info) {
    var votes = info.Votes;
    var groups = info.Ties;

    var addConclusions = function (items) {
      $.each(items, function () {
        var tie = this;
        if (!tie.TieBreakRequired) {
          tie.Conclusion = 'This tie does not affect the election results.';
        }
        else {
          var firstPara;
          if (tie.IsResolved) {
            firstPara = '<p>This tie has been resolved.</p>';
          }
          else {
            tie.rowClass = 'TieBreakNeeded';
            firstPara = '<p>A tie-break election is required to break this tie.</p>';
          }
          tie.Conclusion = firstPara
            + '<p>Voters should vote for <strong><span class=Needed>{0}</span> {1}</strong> from this list of {2}. When the tie-break voting has been completed, enter the number of votes received by each person below.</p>'
              .filledWith(tie.NumToElect, tie.NumToElect == 1 ? 'person' : 'people', tie.NumInTie)
            ;
          var tieVotesFound = votes.reduce(function (acc, v) { return acc || v.TieBreakCount > 0 }, false);
          tie.After = ''
            + (tie.IsResolved || !tieVotesFound ? '' : '<p>In complex situations of ties in the tie-break, additional tie-break elections may be required that are not directly supported here. Once results are known, these tie-break vote numbers may need to be adjusted until those elected are clearly indicated. For example, multiply first round counts by 100, then add second round results.</p>')
            + '<p>Ties are acceptable in the top {0} position{1} of the election{2}.'.filledWith(info.NumToElect, Plural(info.NumToElect),
              info.NumExtra ? ' but not in the next {0} position{1}'.filledWith(info.NumExtra, Plural(info.NumExtra)) : '')
            + '</p>'
            + '<p>If minority status can resolve this tie, simply enter vote numbers of 1 and 0 here to indicate who is to be given preference.</p>'
            ;
          var list = $.map(votes, function (v) {
            return v.TieBreakGroup == tie.TieBreakGroup ? v : null;
          });
          tie.People = '<div><input data-rid="{rid}" class=TieBreakCount type=number min=0 value="{TieBreakCount}">{PersonName}</div>'.filledWithEach(list.sort(function (a, b) {
            if (a.PersonName < b.PersonName) return -1;
            if (a.PersonName > b.PersonName) return 1;
            return 0;
          }));
          tie.Buttons = '<button class="btn btn-mini btnSaveTieCounts" type=button>Save Counts & Re-run Analysis</button>';
        }
      });
      return items;
    };

    if (groups.length == 0) {
      $('#tieResults').hide();
      settings.hasTie = false;
    } else {
      $('#tieResults').show();
      var tbody = $('#tieResultsBody');
      tbody.html(settings.tieResultRowTemplate.filledWithEach(addConclusions(groups)));
      settings.hasTie = true;
    }

  };

  function saveManualCounts() {
    var form = {
      C_RowId: settings.info.ResultsManual.C_RowId
    };

    $('#totalCounts').find('input.Manual[data-name]').each(function () {
      var input = $(this);
      if (input.attr('readonly') == 'readonly') return;
      form[input.data('name')] = input.val();
    });

    ShowStatusDisplay("Saving...");
    CallAjaxHandler(publicInterface.controllerUrl + '/SaveManual', form, function (info) {
      ResetStatusDisplay();
      if (info.Saved) {
        fillValues('Manual', settings.info.ResultsManual = info.ResultsManual);
        fillValues('Final', settings.info.ResultsFinal = info.ResultsFinal);
        summarizeCounts();
        ShowStatusSuccess('Saved');
        showLog(false);
        $('.btnSaveManualCounts').removeClass('btn-primary');
        diableAnalysisBtnIfNeeded()
      } else {
        ShowStatusSuccess(info.Message);
      }
    });

  };

  function saveTieCounts() {
    var btn = $(this);
    //Jan2018 - save all tie break numbers, not just for this tie-break
    //var counts = btn.parent().find('input');
    var inputs = $('#tieResults input');
    var foundOkay = 0;
    var foundNegative = false;

    var values = $.map(inputs, function (item) {
      var $item = $(item);
      var value = +$item.val();
      if (value > 0) {
        foundOkay++;
      }
      if (value === 0) {
        $item.val(0); // may have been blank
      }
      if (value < 0) {
        foundNegative = true;
      }
      return $item.data('rid') + '_' + value;
    });
    if (foundNegative) {
      alert('All vote counts must be a positive number.');
      return;
    }
    var form = {
      counts: values
    };
    ShowStatusDisplay("Saving...");
    CallAjaxHandler(publicInterface.controllerUrl + '/SaveTieCounts', form, function (info) {
      if (info.Saved) {
        ShowStatusSuccess("Saved");
        runAnalysis(false);
        $('.btnSaveTieCounts').removeClass('btn-primary');
        diableAnalysisBtnIfNeeded()
      } else {
        ShowStatusFailed(info.Msg);
      }
    });
  };

  var publicInterface = {
    controllerUrl: '',
    PreparePage: preparePage,
    results: null,
    settings: settings
  };

  return publicInterface;
};

var analyzePage = AnalyzePage();

$(function () {
  analyzePage.PreparePage();
});