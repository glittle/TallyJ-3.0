var MonitorPage = function () {
  var settings = {
    rowTemplateMain: '',
    rowTemplateExtra: '',
    rowTemplateBallot: '',
    refreshTimeout: null
  };

  var preparePage = function () {
    var tableBody = $('#mainBody');
    settings.rowTemplateMain = '<tr class="{ClassName}">' + tableBody.children().eq(0).html() + '</tr>{^DetailRow}';
    settings.rowTemplateExtra = '<tr class="Extra {ClassName}">' + tableBody.children().eq(1).html() + '</tr>';

    var ballotTableBody = $('#ballotsBody');
    settings.rowTemplateBallot = '<tr class="{ClassName}">' + ballotTableBody.children().eq(0).html() + '</tr>';

    showInfo(publicInterface.LocationInfos, true);

    var desiredTime = GetFromStorage(storageKey.MonitorRefresh, 60);

    //        $('#ddlElectionStatus').on('change', function () {
    //            //ShowStatusDisplay('Updating...');
    //            var ddl = $(this);
    //            CallAjaxHandler(site.rootUrl + 'Elections/UpdateElectionStatus', {
    //                status: ddl.val()
    //            }, function () {
    //                //ShowStatusDisplay('Updated', 0, 1000, false, true);
    //                ResetStatusDisplay();
    //                $('.ElectionState').text(ddl.find(':selected').text());
    //            });
    //        });

    $('#ddlRefresh').val(desiredTime).change(function () {
      $('#chkAutoRefresh').prop('checked', true);
      setAutoRefresh(true);
      SetInStorage(storageKey.MonitorRefresh, $(this).val());
    });

    $('#chkAutoRefresh').click(setAutoRefresh);
    $('#chkList').click(updateListing);
    $('#btnRefesh').click(function () {
      ShowStatusDisplay("Refreshing...");
      refresh();
    });

    if (publicInterface.isGuest) {
      $('#chkList').prop('disabled', true);
      $('#ddlElectionStatus').prop('disabled', true);
    }

    setAutoRefresh(false);
  };

  var showInfo = function (info, firstLoad) {
    clearInterval(settings.autoMinutesTimeout);

    var table = $('#mainBody');
    if (!firstLoad) {
      table.animate({
        opacity: 0.4
      }, 10, function () {
        table.animate({
          opacity: 1
        }, 500);
      });
    }
    table.html(expandLocations(info.Locations));

    var ballotHost = $('table.Ballots');
    if (info.Ballots.length == 0) {
      ballotHost.hide();
    } else {
      ballotHost.show();
      var ballotTable = $('#ballotsBody');
      ballotTable.html(expandBallots(info.Ballots));
    }

    $('#lastRefresh').html(new Date().toLocaleTimeString());

    startAutoMinutes();
    setAutoRefresh();
    $('#mainBody, #ballotsBody').removeClass('Hidden');
  };
  var startAutoMinutes = function () {
    var startTime = new Date();
    $('.minutesOld').each(function () {
      var span = $(this);
      span.data('startTime', startTime);
    });
    updateAutoMinutes();
    settings.autoMinutesTimeout = setInterval(function () {
      updateAutoMinutes();
    }, 12 * 1000);
  };

  var updateAutoMinutes = function () {
    $('.minutesOld').each(function () {
      var span = $(this);
      var start = span.data('start');
      if (start !== '') {
        start = +start; // force to be a number
        if (start <= 2) {
          span.html(' (now)');
          return;
        }
        var startTime = span.data('startTime');
        var now = new Date();
        var ms = now.getTime() - startTime.getTime();
        var seconds = Math.floor(ms / 1000 + start);
        var txt = '';
        if (seconds > 59) {
          var minutes = Math.floor(seconds / 60);
          seconds = seconds - minutes * 60;
          txt = minutes + ':' + padRight(seconds);
        }
        else {
          txt = '0:' + padRight(seconds);
        }
        span.html(' (' + txt + ' ago)');
      }
    });
  };

  var padRight = function (num) {
    var s = num.toString();
    return ('0' + s).substr(-2);
  };

  var setAutoRefresh = function (ev) {
    var wantAutorefresh = $('#chkAutoRefresh').prop('checked');
    clearTimeout(settings.refreshTimeout);

    if (wantAutorefresh) {
      settings.refreshTimeout = setTimeout(function () {
        refresh();
      }, 1000 * $('#ddlRefresh').val());

      if (ev) { // called from a handler
        refresh();
      }
    }
  };

  var updateListing = function () {
    var chk = $('#chkList');
    var form = {
      listOnPage: chk.prop('checked')
    };
    ShowStatusDisplay('Saving...');
    CallAjaxHandler(publicInterface.controllerUrl + '/UpdateListing', form, function () {
      ShowStatusSuccess('Saved');
    });
  };

  var refresh = function () {
    CallAjaxHandler(publicInterface.controllerUrl + '/RefreshMonitor', null, showInfo);
  };

  var expandBallots = function (ballots) {
    var html = [];
    $.each(ballots, function () {
      this.Btn = '<a target=L{LocationId} class=ZoomIn title=View href="../Ballots?l={LocationId}&b={Id}"><span class="ui-icon ui-icon-zoomin"></span></a>'.filledWith(this);

      html.push(settings.rowTemplateBallot.filledWith(this));
    });
    return html.join('');
  };
  var expandLocations = function (locations) {
    var lastName = '';
    var count = 0;
    var rows = -1;
    var last = null;
    var html = [];

    $.each(locations, function () {
      rows++;

      this.Btn = '<a target=L{LocationId} class=ZoomIn title=View href="../Ballots?l={LocationId}"><span class="ui-icon ui-icon-zoomin"></span></a>'.filledWith(this);

      if (last != null) {
        last.rows = rows;
        rows = 0;
      }

      count++;
      this.ClassName = count % 2 == 0 ? 'Even' : 'Odd';
      lastName = this.Name;
      last = this;
      //      } else {
      //        this.Extra = true;
      //        this.ClassName = last.ClassName;
      //      }

      if (this.BallotsCollected) {
        var pct = Math.floor(this.BallotsAtLocation / this.BallotsCollected * 100);
        this.BallotsReport = '{2}%'.filledWith(this.BallotsAtLocation, this.BallotsCollected, pct); // ' of {0} ({1} to go)'.filledWith(this.BallotsCollected, this.BallotsCollected - this.Ballots);
        if (pct > 100) {
          this.BallotsReport = '<span class=error>{^0}</span>'.filledWith(this.BallotsReport);
        }
      } else {
        this.BallotsReport = '-';
      }
      if (!this.TallyStatus) {
        this.TallyStatus = '-';
      }
      if (this.BallotCodes.length) {
        var detailRow = '<table class=compList><thead><tr><th>Code</th><th>Ballots</th><th>Current Tellers</th></tr></thead><tbody>{^0}</tbody></table>';

        $.each(this.BallotCodes, function () {
          this.TellerInfo = '<span class="tellers">{Tellers}<span> <span class="minutesOld" data-start="{SecondsOld}"></span><br>'.filledWithEach(this.Computers);
        });

        this.ComputerList = detailRow.filledWith(settings.rowTemplateExtra.filledWithEach(this.BallotCodes));
      }

    });

    if (last != null) {
      last.rows = rows + 1;
    }
    html.push(settings.rowTemplateMain.filledWithEach(locations));

    return html.join('');
  };

  var publicInterface = {
    controllerUrl: '',
    isGuest: false,
    LocationInfos: null,
    PreparePage: preparePage
  };

  return publicInterface;
};

var monitorPage = MonitorPage();

$(function () {
  monitorPage.PreparePage();
});