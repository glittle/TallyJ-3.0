var AnalyzePage = function () {
  var settings = {
    rowTemplate: '',
    info: {},
    footTemplate: '',
    invalidsRowTemplate: '',
    tieResultRowTemplate: '',
    chart: null,
    hasCloseVote: false,
    hasTie: false,
    calledInTotal: 0
  };

  var preparePage = function () {
    site.qTips.push({ selector: '#qTipUnEntered', title: 'Spoiled Ballots Not Entered', text: 'It is best if every ballot is entered into TallyJ, even if it is spoiled.  However, if some spoiled ballots were not entered into TallyJ, enter the number here. ' });

    $('#btnRefresh').click(function () {
      runAnalysis(false);
    });
    $('#btnShowLog').click(function () {
      showLog();
    });


    $('#body').on('click', '.btnSaveTieCounts', saveTieCounts);
    $('#body').on('click', '.btnSaveManualCounts', saveManualCounts);

    var tableBody = $('#mainBody');
    settings.rowTemplate = tableBody.html();
    tableBody.html('');

    var tFoot = $('#mainFoot');
    settings.footTemplate = tFoot.html();
    tFoot.html('');

    var invalidsBody = $('#invalidsBody');
    settings.invalidsRowTemplate = invalidsBody.html();
    invalidsBody.html('');

    connectToAnalyzeHub();

    var tieResultsRowTemplate = $('#tieResultsBody');
    settings.tieResultRowTemplate = tieResultsRowTemplate.html();
    tieResultsRowTemplate.html('');

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
  };

  var showLog = function (display) {
    var wasShowing = $('#loadingLog').is(':visible');
    if (typeof display != 'undefined') {
      wasShowing = !display;
    }
    $('#loadingLog').toggle(!wasShowing);
    $('#btnShowLog').html(wasShowing ? 'Show Analysis Log' : 'Hide Analysis Log');
  }

  var connectToAnalyzeHub = function () {
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

    activateHub(hub, function () {
      LogMessage('Join analyze Hub');
      CallAjaxHandler(analyzePage.analyzeHubUrl, { connId: site.signalrConnectionId }, function (info) {
        if (!info) {
          LogMessage(info);
        }
      });
    });
  };


  var runAnalysis = function (firstLoad) {
    ShowStatusDisplay('Analyzing ballots...', 0, 5 * 60 * 1000);
    $('body').removeClass('notReady');
    $('.LeftHalf, .RightHalf').fadeOut();
    $('#InitialMsg').text('Analyzing all ballots...').removeClass('bad').show();

    showLog(true);
    $('#log, #tempLog').html('');
    connectToAnalyzeHub(); // in case it has been lost

    CallAjaxHandler(publicInterface.controllerUrl + '/RunAnalyze', null, showInfo, firstLoad);
  };

  var showInfo = function (info, firstLoad) {
    $('#btnRefresh').text('Re-run Analysis');
    $('#btnRefreshDiv').show();

    if (info.Interrupted) {
      LogMessage(info.Msg);
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
      table = votesTable;
      votesTable.show();
      invalidsTable.hide();
      instructionsTable.show();

      $('#mainBody').html(settings.rowTemplate.filledWithEach(expand(info.Votes)));
      showTies(info);

      $('#HasCloseVote').toggle(settings.hasCloseVote);
      $('.HasTie').toggle(settings.hasTie);
      //LogMessage(settings.hasTie);

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
      $('#chart').hide();

      $('#invalidsBody').html(settings.invalidsRowTemplate.filledWithEach(expandInvalids(info.NeedReview)));
    }

    settings.calledInTotal = 0;
    fillValues('Calc', info.ResultsCalc);
    fillValues('Manual', info.ResultsManual);
    fillValues('Final', info.ResultsFinal);
    summarizeCounts();

    table.show();
    $('.LeftHalf, .RightHalf').fadeIn();

  };

  var summarizeCounts = function () {
    $('#calledIn').toggle(settings.calledInTotal > 0 || settings.info.ShowCalledIn);
    $('#totalCounts tr').each(function () {
      var row = $(this);
      var calcSpan = row.find('span.Calc');
      var calcText = calcSpan.text();

      var finalSpan = row.find('span.Final');
      var finalText = finalSpan.text();

      finalSpan.toggleClass('changed', calcText != '' && calcText != finalText);
      //calcSpan.toggleClass('changed', finalText != '' && calcText != finalText && !calcSpan.hasClass('NoChanges'));
    });
    $('#totalCounts').toggleClass('mismatch', settings.info.ResultsFinal.NumBallotsWithManual != settings.info.ResultsFinal.SumOfEnvelopesCollected);
    $('body').toggleClass('ready', settings.info.ResultsFinal.UseOnReports);
    $('body').toggleClass('notReady', !settings.info.ResultsFinal.UseOnReports);
  };

  var fillValues = function (name, results) {
    if (results.CalledInBallots) {
      settings.calledInTotal += results.CalledInBallots;
    }
    $('#totalCounts').find('span.{0}[data-name]'.filledWith(name)).each(function () {
      var span = $(this);
      var value = results[span.data('name')];
      span.text(value || '-');
    });
    $('#totalCounts').find('input.{0}[data-name]'.filledWith(name)).each(function () {
      var input = $(this);
      var value = results[input.data('name')];
      input.val(value);
    });

  };

  var expandInvalids = function (needReview) {
    $.each(needReview, function () {
      this.Ballot = '<a target=L{LocationId} href="../Ballots?l={LocationId}&b={BallotId}">{Ballot}</a>'.filledWith(this);
      this.Link = this.PositionOnBallot;
    });
    return needReview;
  };

  var expand = function (results) {
    settings.hasCloseVote = false;
    $.each(results, function (i) {
      if (!this.TieBreakRequired) {
        this.TieBreakCount = 0;
      }
      this.ClassName = 'Section{0} {1} {2} {3}'.filledWith(
          this.Section,
          this.Section == 'O' && this.ForceShowInOther ? 'Force' : '',
          (i % 2 == 0 ? 'Even' : 'Odd'),
          (this.IsTied && this.TieBreakRequired ? (this.IsTieResolved ? 'Resolved' : 'Tied') : ''));
      this.TieVote = this.IsTied ? (this.TieBreakRequired ? ('Tie ' + this.TieBreakGroup + ' (' + (this.IsTieResolved ? 'Done' : 'Tied') + ')') : 'Tie ' + this.TieBreakGroup + ' (Okay)') : '';
      if (this.CloseToNext) {
        this.CloseUpDown = this.CloseToPrev ? '&#8597;' : '&#8595;';
      } else if (this.CloseToPrev) {
        this.CloseUpDown = '&#8593;';
      }
      if ((this.Section == 'T' || this.Section == 'E')
          && (this.CloseToNext || this.CloseToPrev)) {
        settings.hasCloseVote = true;
      }
      this.VoteDisplay = this.VoteCount + (this.TieBreakCount ? ', ' + this.TieBreakCount : '');
    });
    return results;
  };

  var showTies = function (info) {
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
            firstPara = '<p><b>This tie has been resolved.</b></p>';
          }
          else {
            tie.rowClass = 'TieBreakNeeded';
            firstPara = '<p>A tie-break election is required to break this tie.</p>';
          }
          tie.Conclusion = firstPara
            + '<p>Voters should vote for <span class=Needed>{0}</span> {1} from this list of {2}. When the tie-break vote has been completed, enter the number of votes received by each person below.</p>'
            .filledWith(tie.NumToElect, tie.NumToElect == 1 ? 'person' : 'people', tie.NumInTie)
          ;
          tie.After = ''
            + '<p>If minority status can resolve this tie, simply enter vote numbers of 1 and 0 here to indicate who is to be given preference.</p>'
            + '<p>If there are ties in the tie-break election, they are acceptable in the top {0} positions of the main election{1}.'.filledWith(info.NumToElect,
              info.NumExtra ? ' but not in the next {0} positions'.filledWith(info.NumExtra) : '')
            + (tie.IsResolved ? '' : ' <b>In complex situations of ties in the tie-break, additional tie-break elections may be required that are not directly supported here.')
            + '</p>';
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

  var saveManualCounts = function () {
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
      } else {
        ShowStatusSuccess(info.Message);
      }
    });

  };

  var saveTieCounts = function () {
    var btn = $(this);
    var counts = btn.parent().find('input');
    var needed = +btn.parent().find('.Needed').text();
    //    var dups = [];
    //    var foundDup = false;
    var foundOkay = 0;
    var foundNegative = false;

    var values = $.map(counts, function (item) {
      var $item = $(item);
      var value = +$item.val();
      if (value > 0) {
        //        if (dups[value]) {
        //          foundDup = true;
        //        }
        //        else {
        foundOkay++;
        //        }
        //dups[value] = (dups[value] ? dups[value] : 0) + 1;
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
    //    if (foundDup) {
    //      var foundBeforeDup = 0;
    //      for (var i = dups.length - 1; i >= 0; i--) {
    //        var dup = dups[i];
    //        if (dup > 1) break;
    //        foundBeforeDup = foundBeforeDup + dup; // will be 1 or 0
    //      }
    //      if (foundBeforeDup < needed) {
    //        alert('A tie has been entered within the top {0} vote counts.\n\nWhen the tie-breaking vote is done, tied results cannot be accepted. Please resolve those tied votes.'.filledWith(needed));
    //      }
    //    }
    if (foundOkay < needed) {
      alert('Please ensure that {0} or more votes are entered.'.filledWith(needed));
    }
    var form = {
      counts: values
    };
    ShowStatusDisplay("Saving...");
    CallAjaxHandler(publicInterface.controllerUrl + '/SaveTieCounts', form, function (info) {
      if (info.Saved) {
        ShowStatusSuccess("Saved");
        runAnalysis(false);
      } else {
        ShowStatusFailed(info.Msg);
      }
    });
  };

  var publicInterface = {
    controllerUrl: '',
    PreparePage: preparePage,
    results: null
  };

  return publicInterface;
};

var analyzePage = AnalyzePage();

$(function () {
  analyzePage.PreparePage();
});