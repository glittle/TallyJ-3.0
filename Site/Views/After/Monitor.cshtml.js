var MonitorPage = function () {
  var settings = {
    rowTemplateMain: '',
    rowTemplateExtra: '',
    rowTemplateBallot: '',
    rowTemplateOnline: '',
    refreshTimeout: null,
    refreshCounter: null,
    lastRefresh: null,
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
    $('#btnRefresh').click(function () {
      refresh(true);
    });

    if (publicInterface.isGuest) {
      $('#chkList').prop('disabled', true);
      $('#ddlElectionStatus').prop('disabled', true);
    }

    connectToFrontDeskHub();

    setAutoRefresh(false);
  };

  function connectToFrontDeskHub() {
    $.connection().logging = true;
    var hub = $.connection.frontDeskHubCore;

    //console.log('signalR prepare: updatePeople');
    hub.client.updatePeople = function (info) {
      console.log('signalR: frontDesk updatePeople');
      var needRefresh = false;
      info.PersonLines.forEach(function (pl) {
        if (publicInterface.initial.OnlineBallots.findIndex(function (ob) {
          return ob.PersonId === pl.PersonId;
        }) !== -1) {
          needRefresh = true;
        }
      });
      // should not happen too often as Monitor is usually not open with FrontDesk. May be with Voters.
      if (needRefresh) {
        refresh();
      }
    };

    hub.client.updateOnlineElection = function (info) {
      console.log('signalR: frontDesk updateOnlineElection');
      if (settings.vue) {
        settings.vue.showOnlineTimes(info);
      }
    };


    startSignalR(function () {
      console.log('Joining frontDesk hub');
      CallAjaxHandler(publicInterface.beforeUrl + '/JoinFrontDeskHub', { connId: site.signalrConnectionId });
    });

  };

  function showInfo(info, firstLoad) {
    publicInterface.initial = info;

    clearInterval(settings.autoMinutesTimeout);

    info.OnlineBallots.forEach(extendVoter);

    if (settings.vue) {
      settings.vue.onlineBallots = info.OnlineBallots;
      settings.vue.showOnlineTimes(info.OnlineInfo);
    }

    var tables = $('#mainBody, .el-table__body');
    if (!firstLoad) {
      tables.animate({
        opacity: 0.4
      },
        10,
        function () {
          tables.animate({
            opacity: 1
          },
            500);
        });
    }
    var table = $('#mainBody');
    table.html(expandLocations(info.Locations));

    var ballotHost = $('table.Ballots');
    if (info.Ballots.length === 0) {
      ballotHost.hide();
    } else {
      ballotHost.show();
      var ballotTable = $('#ballotsBody');
      ballotTable.html(expandBallots(info.Ballots));
    }

    //    var onlineBallotHost = $('table.OnlineBallots');
    //    if (onlineBallotHost.length) {
    //      if (info.OnlineBallots.length === 0) {
    //        onlineBallotHost.hide();
    //      } else {
    //        onlineBallotHost.show();
    //        var onlineBallotTable = $('#onlineBallotsBody');
    //        onlineBallotTable.html(expandOnlineBallots(info.OnlineBallots));
    //      }
    //    }

    var now = settings.lastRefresh = new Date();
    $('#lastRefresh').html(monitorPage.T24
      ? now.toLocaleTimeString()
      : now.toLocaleString('en-US', { hour: 'numeric', minute: 'numeric', second: 'numeric', hour12: true }));

    startAutoMinutes();
    setAutoRefresh();
    $('#mainBody, #ballotsBody, #onlineBallotsBody').removeClass('Hidden');
  };
  function startAutoMinutes() {
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

  function updateAutoMinutes() {
    $('#age').text(` (${moment(settings.lastRefresh).fromNow()})`);

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

  function padRight(num) {
    var s = num.toString();
    return ('0' + s).substr(-2);
  };

  function setAutoRefresh(ev) {
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
          $('#age').text(` (${moment(settings.lastRefresh).fromNow()})`);
        },
          speed * 1000);
      },
        100);

      if (ev) { // called from a handler
        refresh();
      }
    } else {
      showCountDown(0, 1);
    }
  };

  function showCountDown(remaining, original) {
    var pct = 100 - Math.max(0, remaining / original * 100);
    $('#lastRefreshArea .countdown').width(pct + '%');
  }

  function updateListing() {
    var chk = $('#chkList');
    var form = {
      listOnPage: chk.prop('checked')
    };
    CallAjax2(publicInterface.controllerUrl + '/UpdateListing',
      form,
      {
        busy: 'Changing Open Status'
      },
      function () {
        updatePasscodeDisplay(form.listOnPage, site.passcodeRaw);
      });
  };

  function refresh(showMsg) {
    //    console.log(showMsg)
    var options = showMsg === true ? {
      busy: 'Refreshing',
      done: 'Refreshed'
    } : {};
    CallAjax2(publicInterface.controllerUrl + '/RefreshMonitor', null,
      options,
      showInfo);
  };

  function expandBallots(ballots) {
    var html = [];
    $.each(ballots,
      function () {
        this.Btn =
          '<a target=L{LocationId} class=ZoomIn title=View href="../Ballots?l={LocationId}&b={Id}"><span class="ui-icon ui-icon-zoomin"></span></a>'
            .filledWith(this);

        html.push(settings.rowTemplateBallot.filledWith(this));
      });
    return html.join('');
  }

  function extendVoter(voter) {
    var timeTemplate = monitorPage.T24 ? 'YYYY MMM D, H:mm' : 'YYYY MMM D, h:mm a';

    var history = (voter.HistoryStatus || '')
      .split(';')
      .filter(function (x) { return x; })
      .map(function (x) {
        var parts = x.split('|');
        var when = parts.length > 1 ? parts[1].replace(/[\\"]/g, '') : '';
        return '{0} at {1}'.filledWith(parts[0], moment(when).format(timeTemplate));
      });
    voter.WhenStatus_Display = voter.WhenStatus ? moment(voter.WhenStatus).format(timeTemplate) : '';
    voter.WhenStatus_Sort = voter.WhenStatus ? moment(voter.WhenStatus).toISOString() : '';
    voter.VoteMethodClass = voter.VotingMethod === 'O' ? 'online' : 'other';
    voter.StatusClass = voter.VotingMethod === 'O' ? voter.Status : '';
    voter.History_Display = history.length ? history[history.length - 1] : '-';
    voter.History_Tip = '\n' + history.join('\n');
    voter.HasHistory_Tip = history.length > 0;
    if (!voter.Status) {
      if (!voter.votesReady) {
        voter.Status = '-';
      }
    }
    voter.EmailPhone = [voter.Email, voter.Phone].filter(function (s) { return !!s; }).join('<br>');
  }

  function expandOnlineBallots(voters) {
    var html = [];
    $.each(voters, function (i, voter) {
      html.push(settings.rowTemplateOnline.filledWith(voter));
    });
    return html.join('');
  }

  function expandLocations(locations) {
    //    var lastName = '';
    //    var count = 0;
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

        //        count++;
        //        this.ClassName = count % 2 === 0 ? 'Even' : 'Odd';
        //        lastName = this.Name;
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
  }

  function startVue() {
    if (!document.getElementById('onlineDiv')) {
      return;
    }

    settings.vue = new Vue({
      el: '#onlineDiv',
      //      components: {
      //        'yes-no': YesNo
      //      },
      data: {
        election: {},
        CloseTime: null,
        T24: false,
        dummy: 0,
        processingReadyBallots: false,
        onlineBallots: []
      },
      computed: {
        onlineDatesOkay: function () {
          return this.election.OnlineWhenOpen &&
            this.election.OnlineWhenClose &&
            this.election.OnlineWhenOpen < this.election.OnlineWhenClose;
        },
        CloseTime_Display: function () {
          var x = this.dummy;

          var closeTime = this.CloseTime;
          if (!closeTime) return '';
          var when = moment(closeTime);
          var prefix = moment().isBefore(when)
            ? (this.election.OnlineCloseIsEstimate ? 'Expected to close ' : 'Will close ') : 'Closed ';
          return prefix + when.fromNow();
        },
        onlineToProcess: function () {
          return this.onlineBallots.filter(function (ob) {
            return ob.Status === 'Submitted' && ob.VotingMethod === 'O';
          }).length;
        },
        OnlineWhenOpen_M: function () {
          return moment(this.election.OnlineWhenOpen);
        },
        OnlineWhenClose_M: function () {
          return moment(this.election.OnlineWhenClose);
        },
        isClosed: function () {
          return this.OnlineWhenClose_M.isBefore();
        },
        closeStatusClass: function () {
          var x = this.dummy;
          if (this.OnlineWhenOpen_M.isAfter()) {
            return 'onlineFuture';
          } else if (this.OnlineWhenClose_M.isBefore()) {
            // past
            return 'onlinePast';
          } else if (this.OnlineWhenOpen_M.isBefore() && this.OnlineWhenClose_M.isAfter()) {
            // now
            var minutes = this.OnlineWhenClose_M.diff(moment(), 'm');

            return minutes <= 5 ? 'onlineSoon' : 'onlineNow';
          }
          return '';
        },
        defaultSort: function () {
          return {
            prop: GetFromStorage(storageKey.OVSort, 'WhenStatus_Sort'),
            order: GetFromStorage(storageKey.OVSortDir, 'descending')
          };
        }
      },
      watch: {
      },
      created: function () {
        this.showOnlineTimes(monitorPage.initial.OnlineInfo);
        this.T24 = monitorPage.T24;
        var vue = this;
        setInterval(function () {
          vue.dummy++;
          vue.checkStatus();
        }, 15 * 1000);
      },
      mounted: function () {
      },
      methods: {
        showOnlineTimes: function (info) {
          this.election = info;
          this.election.OnlineWhenOpen = moment(this.election.OnlineWhenOpen).toISOString();
          this.CloseTime = this.election.OnlineWhenClose = moment(this.election.OnlineWhenClose).toISOString();
        },
        checkStatus: function () {
          // just to change the top corner display
          var isClosed = this.OnlineWhenClose_M.isBefore();
          $('body').toggleClass('OnlineOpen', !isClosed);
          $('body').toggleClass('OnlineClosed', isClosed);
        },
        saveNeeded: function () {
          $('.btnSave').addClass('btn-primary');
        },
        closeOnline: function (minutes, expected) {
          var vue = this;
          if (typeof expected === 'boolean') {
            vue.election.OnlineCloseIsEstimate = expected;
          }
          if (minutes) {
            vue.CloseTime = moment().add(minutes, 'minutes').toISOString();
          } else {
            vue.CloseTime = moment().subtract(1, 'second').toISOString();
          }
          vue.saveClose();
          //          CallAjaxHandler(publicInterface.controllerUrl + '/CloseOnline',
          //            {
          //              minutes: minutes,
          //              est: vue.election.OnlineCloseIsEstimate
          //            },
          //            function (info) {
          //              if (info.success) {
          //                ShowStatusDone('Saved');
          //                vue.CloseTime = vue.election.OnlineWhenClose = info.OnlineWhenClose.parseJsonDate().toISOString();
          //              }
          //            });
        },
        saveClose: function () {
          var vue = this;
          if (vue.CloseTime.toISOString) {
            vue.CloseTime = vue.CloseTime.toISOString();
          }
          var form = {
            when: vue.CloseTime,
            est: vue.election.OnlineCloseIsEstimate
          };

          CallAjax2(publicInterface.controllerUrl + '/SaveOnlineClose',
            form,
            {
              busy: 'Adjusting close time'
            },
            function (info) {
              if (info.success) {
                vue.CloseTime = vue.election.OnlineWhenClose = info.OnlineWhenClose.parseJsonDate().toISOString();
                vue.election.OnlineCloseIsEstimate = info.OnlineCloseIsEstimate;
                vue.checkStatus();
              }
            });
        },
        tableRowClassName: function (info) {
          var row = info.row;
          var classes = [
            'method_' + row.VotingMethod_Display,
            'ballot_' + row.StatusClass
          ];

          return classes.filter(s => s).join(' ');
        },
        sortChange: function(info) {
          var dir = info.order;
          var sortBy = info.prop || info.column.sortBy;
          SetInStorage(storageKey.OVSort, sortBy);
          SetInStorage(storageKey.OVSortDir, dir);
        },
        processReadyBallots: function () {
          var vue = this;
          this.processingReadyBallots = true;
          CallAjax2(publicInterface.controllerUrl + '/ProcessOnlineBallots',
            null,
            {
              busy: 'Processing'
            },
            function (info) {
              vue.processingReadyBallots = false;
              if (info.success) {
                if (info.Message) {
                  ShowStatusDone(info.Message);
                }
                refresh();
              } else {
                var msg = info.Message || info.problems.join('<br>');
                ShowStatusFailed(msg);
              }
            });

        }
      }
    });
  }

  var publicInterface = {
    controllerUrl: '',
    refresh: refresh,
    isGuest: false,
    initial: null,
    YesNo: null,
    PreparePage: preparePage
  };

  return publicInterface;
};

var monitorPage = MonitorPage();

$(function () {
  monitorPage.PreparePage();
});



Vue.component('yes-no',
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