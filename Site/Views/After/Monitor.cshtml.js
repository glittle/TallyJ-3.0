var MonitorPage = function () {
  var settings = {
    rowTemplateMain: '',
    rowTemplateExtra: '',
    rowTemplateBallot: '',
    rowTemplateOnline: '',
    refreshTimeout: null,
    refreshCounter: null,
    vue: null
  };

  var preparePage = function () {
    if (typeof Vue !== 'undefined') {
      startVue();
    }

    var tableBody = $('#mainBody');
    settings.rowTemplateMain = '<tr class="{ClassName}">' + tableBody.children().eq(0).html() + '</tr>{^DetailRow}';
    settings.rowTemplateExtra = '<tr class="Extra {ClassName}">' + tableBody.children().eq(1).html() + '</tr>';

    var ballotTableBody = $('#ballotsBody');
    settings.rowTemplateBallot = '<tr class="{ClassName}">' + ballotTableBody.children().eq(0).html() + '</tr>';
    settings.rowTemplateOnline = '<tr class="{ClassName}">' + $('#onlineBallotsBody').children().eq(0).html() + '</tr>';

    var useAutoRefresh = GetFromStorage(storageKey.MonitorRefreshOn, true);
    $('#chkAutoRefresh').prop('checked', useAutoRefresh).click(setAutoRefresh);

    var desiredTime = GetFromStorage(storageKey.MonitorRefresh, 60);
    $('#ddlRefresh').val(desiredTime).change(function () {
      $('#chkAutoRefresh').prop('checked', true);
      setAutoRefresh(true);
      SetInStorage(storageKey.MonitorRefresh, $(this).val());
      SetInStorage(storageKey.MonitorRefreshOn, true);
    });

    showInfo(publicInterface.initial, true);

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
      },
        10,
        function () {
          table.animate({
            opacity: 1
          },
            500);
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

    var onlineBallotHost = $('table.OnlineBallots');
    if (onlineBallotHost.length) {
      if (info.OnlineBallots.length === 0) {
        onlineBallotHost.hide();
      } else {
        onlineBallotHost.show();
        var onlineBallotTable = $('#onlineBallotsBody');
        onlineBallotTable.html(expandOnlineBallots(info.OnlineBallots));
      }
    }

    var now = new Date();
    $('#lastRefresh').html(monitorPage.T24
      ? now.toLocaleTimeString()
      : now.toLocaleString('en-US', { hour: 'numeric', minute: 'numeric', second: 'numeric', hour12: true }));

    startAutoMinutes();
    setAutoRefresh();
    $('#mainBody, #ballotsBody, #onlineBallotsBody').removeClass('Hidden');
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
    },
      12 * 1000);
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
        } else {
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
    var wantAutoRefresh = $('#chkAutoRefresh').prop('checked');
    clearTimeout(settings.refreshTimeout);
    clearInterval(settings.refreshCounter);
    SetInStorage(storageKey.MonitorRefreshOn, wantAutoRefresh);

    if (wantAutoRefresh) {
      var seconds = $('#ddlRefresh').val();

      settings.refreshTimeout = setTimeout(function () {
        clearInterval(settings.refreshCounter);
        showCountDown(0, seconds);

        refresh();
      },
        1000 * seconds);


      showCountDown(seconds, seconds);

      var remaining = seconds - .5;
      var speed = seconds / 40;

      setTimeout(function () {
        clearInterval(settings.refreshCounter);
        settings.refreshCounter = setInterval(function () {
          remaining -= speed;
          showCountDown(remaining, seconds);
        },
          speed * 1000);
      },
        100);

      if (ev) { // called from a handler
        refresh();
      }
    } else {
      showCountDown(1, 1);
    }
  };

  function showCountDown(remaining, original) {
    var pct = 100 - Math.max(0, remaining / original * 100);
    $('#lastRefreshArea .countdown').width(pct + '%');
  }

  var updateListing = function () {
    var chk = $('#chkList');
    var form = {
      listOnPage: chk.prop('checked')
    };
    ShowStatusDisplay('Saving...');
    CallAjaxHandler(publicInterface.controllerUrl + '/UpdateListing',
      form,
      function () {
        ShowStatusSuccess('Saved');
        updatePasscodeDisplay(form.listOnPage, site.passcodeRaw);
      });
  };

  var refresh = function () {
    CallAjaxHandler(publicInterface.controllerUrl + '/RefreshMonitor', null, showInfo);
  };

  var expandBallots = function (ballots) {
    var html = [];
    $.each(ballots,
      function () {
        this.Btn =
          '<a target=L{LocationId} class=ZoomIn title=View href="../Ballots?l={LocationId}&b={Id}"><span class="ui-icon ui-icon-zoomin"></span></a>'
            .filledWith(this);

        html.push(settings.rowTemplateBallot.filledWith(this));
      });
    return html.join('');
  };
  var expandOnlineBallots = function (voters) {
    var html = [];
    var template = monitorPage.T24 ? 'MMMM D HH:mm' : 'MMMM D hh:mm a';

    $.each(voters, function (i, voter) {
      var history = (voter.HistoryStatus || '')
        .split(';')
        .filter(function (x) { return x; })
        .map(function (x) {
          var parts = x.split('|');
          var when = parts.length > 1 ? parts[1].replace(/[\\"]/g, '') : '';
          return '{0} at {1}'.filledWith(parts[0], moment(when).format(template));
        });
      voter.History_Display = history.length ? history[history.length - 1] : '-';
      voter.History_Tip = history.join('\n');

      html.push(settings.rowTemplateOnline.filledWith(voter));
    });
    return html.join('');
  };
  var expandLocations = function (locations) {
    var lastName = '';
    var count = 0;
    var rows = -1;
    var last = null;
    var html = [];

    $.each(locations,
      function () {
        rows++;

        this.Btn =
          '<a target=L{LocationId} class=ZoomIn title=View href="../Ballots?l={LocationId}"><span class="ui-icon ui-icon-zoomin"></span></a>'
            .filledWith(this);

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
          this.BallotsReport =
            '{2}%'.filledWith(this.BallotsAtLocation,
              this.BallotsCollected,
              pct); // ' of {0} ({1} to go)'.filledWith(this.BallotsCollected, this.BallotsCollected - this.Ballots);
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
          var detailRow =
            '<table class=compList><thead><tr><th>Code</th><th>Ballots</th><th>Current Tellers</th></tr></thead><tbody>{^0}</tbody></table>';

          $.each(this.BallotCodes,
            function () {
              this.TellerInfo =
                '<span class="tellers">{Tellers}<span> <span class="minutesOld" data-start="{SecondsOld}"></span><br>'
                  .filledWithEach(this.Computers);
            });

          this.ComputerList =
            detailRow.filledWith(settings.rowTemplateExtra.filledWithEach(this.BallotCodes));
        }

      });

    if (last != null) {
      last.rows = rows + 1;
    }
    html.push(settings.rowTemplateMain.filledWithEach(locations));

    return html.join('');
  };

  function startVue() {
    settings.vue = new Vue({
      el: '#onlineDiv',
      components: {
        'yes-no': publicInterface.YesNo
      },
      data: {
        election: null
      },
      computed: {
        onlineDatesOkay: function () {
          return this.election.OnlineWhenOpen &&
            this.election.OnlineWhenClose &&
            this.election.OnlineWhenOpen < this.election.OnlineWhenClose;
        }
      },
      watch: {
      },
      created: function () {
        this.election = monitorPage.initial.OnlineInfo;
        this.election.OnlineWhenOpen = this.election.OnlineWhenOpen.parseJsonDate().toISOString();
        this.election.OnlineWhenClose = this.election.OnlineWhenClose.parseJsonDate().toISOString();
      },
      mounted: function () {
      },
      methods: {
        saveNeeded: function () {
          $('.btnSave').addClass('btn-primary');
        },
        showFrom: function (when) {
          if (!when) return '';
          return moment(when).fromNow();
        }
      }
    });

  }

  var publicInterface = {
    controllerUrl: '',
    isGuest: false,
    initial: null,
    YesNo: null,
    PreparePage: preparePage
  };

  return publicInterface;
};

var monitorPage = MonitorPage();

$(function () {
  if (typeof Vue !== 'undefined') {

    monitorPage.YesNo = Vue.component('yes-no',
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
  };

  monitorPage.PreparePage();
});
