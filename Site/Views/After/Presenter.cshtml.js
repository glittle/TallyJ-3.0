var PresenterPage = function () {
  var settings = {
    rowTemplate: '',
    footTemplate: '',
    invalidsRowTemplate: '',
    refreshTimeout: null,
    chart: null,
    displayLevel: 0 // 0 - hidden, 1 - stats, 2 - stats + names
  };

  var preparePage = function () {

    $('#btnRefesh').click(function () {
      getReportData();
      this.blur();
    });

    $('#btnReturn').click(function () {
      //location.href = site.rootUrl + 'Dashboard';
      var isShowing = $('header').is(':visible');
      $('header').toggle(!isShowing);

      isShowing = !isShowing;
      $(this).text(isShowing ? 'Hide Menu' : 'Show Menu');
      $('.Nav').toggleClass('Show', isShowing);
      return false;
    });

    $('#btnShowNames').click(function () {
      showReport();
    });

    $(window).on('keydown', keyDown);
    $('#btnShow').on('click', function () {
      showReport();
    });

    var tableBody = $('#mainBody');
    settings.rowTemplate = tableBody.html();
    tableBody.html('');

    var tFoot = $('#mainFoot');
    settings.footTemplate = tFoot.html();
    tFoot.html('');

    site.onbroadcast(site.broadcastCode.electionStatusChanged, function (ev, info) {
      if (info.StateName === 'Finalized') {
        getReportData();
      }
      else {
        $('#Results').hide();
        $('#Wait').show();
        $('#Status').text(info.Name);
      }
    });

    setTimeout(function () {
      $('.Nav').animate({ opacity: 0 }, 1500, null, function () {
        $('.Nav').removeClass('Show').css({
          opacity: ''
        });
      });
    }, 1000);

    setTimeout(function () {
      getReportData();
    }, 0);
  };

  var getReportData = function () {
    ShowStatusDisplay('Getting results...');
    clearTimeout(settings.refreshTimeout);
    CallAjaxHandler(publicInterface.controllerUrl + '/GetReport', null, showInfo);
  };

  var showInfo = function (info) {
    var votesTable = $('table.Main');

    ResetStatusDisplay();

    if (info.Status != 'Finalized') {
      $('#Results').hide();
      $('#Wait').show();
      $('#Status').text(info.StatusText);
      ActivateHeartbeat(true, 15);
      return;
    }

    ActivateHeartbeat(true, 60);

    if (settings.displayLevel == 0) {
      $('.Holder').hide();
      $('#Results').show();
      $('.Ready').show();
      $('#Wait').hide();
    }

    if (info.ReportVotes) {
      $('#mainBody').html(settings.rowTemplate.filledWithEach(expand(info)));
    }
    else {
    }

    $('#totalCounts').find('span[data-name]').each(function () {
      var span = $(this);
      var name = span.data('name');
      var value = info[name];
      span.text(value);

      if (name == 'EnvelopesCalledIn') {
        $('#calledIn').toggle(value != 0);
      }
    });

  };

  var keyDown = function (ev) {
    switch (ev.which) {
      case 66: // B
      case 98: // b
      case 27: // esc
        hideReport();
        break;

      case 32: // space
        showReport();
        break;

      default:
        //LogMessage(ev.which);
        break;
    }
  };
  var showReport = function (level) {
    if (!level) {
      // go to next level
      level = settings.displayLevel + 1;
    }
    if (level > 2) level = 2;
    settings.displayLevel = level;

    if (settings.displayLevel == 1) {
      $('#btnShow').fadeOut(500, null, function () {
        $('#mainResults').hide();
        $('#btnShowNames').show();
        $('.Holder').fadeIn(3000);
      });
    } else if (settings.displayLevel == 2) {
      $('#btnShowNames').fadeOut(500, null, function () {
        $('#mainResults').show('blind', 3000);
      });
    }
  };
  var hideReport = function () {

    if (settings.displayLevel != 0) {
      $('.Holder').fadeOut(200, null, function () {
        $('.Ready').fadeIn(500);
      });
      settings.displayLevel = 0;
    }
  };
  var showChart = function (info) {
    var maxToShow = 10; //TODO what is good limit?

    var getVoteCounts = function () {
      return $.map(info.slice(0, maxToShow), function (item, i) {
        return item.VoteCount;
      });
    };

    var getNames = function () {
      return $.map(info.slice(0, maxToShow), function (item, i) {
        return item.Rank;
      });
    };


    settings.chart = new Highcharts.Chart({
      chart: {
        renderTo: 'chart',
        type: 'bar'
      },
      title: {
        text: 'Top Vote Counts'
      },
      legend: {
        enabled: false
      },
      xAxis: {
        categories: getNames()
      },
      yAxis: {
        title: {
          text: 'Votes'
        }
      },
      series: [{
        name: 'Votes',
        data: getVoteCounts()
      }]
    });
  };

  var expandInvalids = function (needReview) {
    $.each(needReview, function () {
      this.Ballot = '<a href="../Ballots?l={LocationId}&b={BallotId}">{Ballot}</a>'.filledWith(this);
      this.Link = this.PositionOnBallot;
    });
    return needReview;
  };

  var expand = function (info) {
    var results = info.ReportVotes;
    var foundExtra = false;

    $.each(results, function (i) {
      this.ClassName = this.Section == 'E' ? (foundExtra ? 'Extra' : ' FirstExtra') : '';
      this.VoteDisplay = this.VoteCount + (this.TieBreakCount ? ', ' + this.TieBreakCount : '');
      if (this.Section == 'E') {
        foundExtra = true;
      }
    });
    return results;
  };

  var publicInterface = {
    controllerUrl: '',
    PreparePage: preparePage
  };

  return publicInterface;
};

var presenterPage = PresenterPage();

$(function () {
  presenterPage.PreparePage();
});