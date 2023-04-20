/// <reference path="../../Scripts/vue.js" />
/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/moment.js" />

var LogView = function () {
  var hostSelector = '#LogView';

  var localSettings = {
    vue: null
  };

  var preparePage = function () {

    localSettings.vue = new Vue({
      name: 'LogView',
      el: hostSelector,
      data: function () {
        return {
          report: GetFromStorage('syslog', ''),
        };
      },
      methods: {
        showReport: function (report) {
          this.report = report;
        }
      }
    });
  };

  return {
    preparePage: preparePage,
    settings: localSettings
  };
}


var logView = LogView();

$(function () {
  ELEMENT.locale(ELEMENT.lang.en);
  logView.preparePage();
});

Vue.component('eventLog',
  {
    template: '#eventLog',
    props: {
      report: String,
    },
    data: function () {
      return {
        logInfo: {
          mainLog: {
            log: [],
            loaded: false
          },
          onlineVotingLog: {
            log: [],
            loaded: false
          },
          electionList: {
            log: [],
            loaded: false
          },
          unconnectedVoters: {
            log: [],
            loaded: false
          },
        },
        numToShow: +GetFromStorage('syslognum', 25),
        lastType: GetFromStorage('syslog', ''),
        searchText: '',
        autoUpdateSeconds: +GetFromStorage('admintimer', 0),
        // autoUpdateTimer: null,
        ageTimer: null,
        ageStart: null,
        secondsSinceLoaded: 0,
        age: '',
        ageSeconds: 0,
        lastSort: null,
        moreDates: [],
        searchName: '',
        numMoreMainLog: 50,
        numMoreOptions: [10, 25, 50, 100, 500, 1000],
        currentSort: null,
        autoUpdateSettings: [
          { s: 0, t: '(no auto refresh)' },
          { s: 3, t: 'in 3 seconds' },
          { s: 5, t: 'in 5 seconds' },
          { s: 10, t: 'in 10 seconds' },
          { s: 30, t: 'in 30 seconds' },
          { s: 60, t: 'in 1 minute' },
          { s: 3 * 60, t: 'in 3 minutes' },
          { s: 5 * 60, t: 'in 5 minutes' },
          { s: 10 * 60, t: 'in 10 minutes' },
          { s: 30 * 60, t: 'in 30 minutes' },
        ]
      };
    },
    computed: {
      urlRegExp: function () {
        return new RegExp(site.rootUrl, 'ig');
      }
    },
    watch: {
      report: function (a, b) {
        this.getLog(a, true); // load the events on first view
      },
      autoUpdateSeconds: function (a, b) {
        if (this.autoUpdateSeconds && this.autoUpdateSeconds < this.ageSeconds) {
          this.refreshActive();
        }

        SetInStorage('admintimer', a);
      }
    },
    mounted: function () {
      this.getLog(this.report, true); // load the events on first view
    },
    methods: {
      startAgeTimer: function () {
        var vue = this;
        vue.ageStart = moment();
        clearInterval(vue.ageTimer);
        vue.ageTimer = setInterval(function () {
          var now = moment();
          vue.ageSeconds = now.diff(vue.ageStart, 's');

          if (vue.autoUpdateSeconds && vue.autoUpdateSeconds <= vue.ageSeconds) {
            vue.refreshActive();
            vue.age = 'Loading';
            return;
          };

          if (vue.ageSeconds < 2) {
            vue.age = 'Loaded 1 second ago';
          } else if (vue.ageSeconds < 60) {
            vue.age = 'Loaded {0} seconds ago'.filledWith(vue.ageSeconds);
          }
          else {
            vue.age = 'Loaded ' + vue.ageStart.fromNow();
          }
        }, 1000);
      },
      cancelTimers: function () {
        var vue = this;
        clearInterval(vue.ageTimer);
        // clearTimeout(vue.autoUpdateTimer);
        vue.age = '';
        vue.ageSeconds = 0;
      },
      refreshActive: function () {
        var vue = this;
        var types = Object.keys(vue.logInfo);
        types.forEach(function (type) {
          var logInfo = vue.logInfo[type];

          if (logInfo.loaded) {
            if (type === vue.report) {
              vue.getLog(type, true);
            }
            else {
              logInfo.loaded = false;
            }
          }
        });
      },
      refresh: function () {
        this.getLog(null, true);
      },
      //      changedTab: function () {
      //        var type = this.report;
      //        if (!this.logInfo[type].loaded) {
      //          this.getLog(type, true);
      //        }
      //      },
      getLog: function (type, reload) {
        var vue = this;

        type = type || this.lastType;

        var logInfo = vue.logInfo[type];

        vue.cancelTimers();

        var form = {
          //          search: vue.searchText
        };

        var last;
        var niceName = 'report';
        switch (type) {
          case 'mainLog':
            niceName = "general log";
            if (reload) {
              logInfo.log = [];
            }

            form.searchText = vue.searchText;
            form.searchName = vue.searchName;

            if (logInfo.log.length) {
              last = logInfo.log[logInfo.log.length - 1];
              form.lastRowId = last.C_RowId;
              last.moreFollowing = true;
            }
            break;
          case 'onlineVotingLog':
            niceName = "online voting";
            logInfo.log = [];
            break;
          case 'electionList':
            niceName = "elections list";
            logInfo.log = [];
            break;
          case 'unconnectedVoters':
            niceName = "unconnected voters";
            logInfo.log = [];
            break;
        }

        if (vue.moreDates && vue.moreDates.length) {
          form.fromDate = moment(vue.moreDates[0]).format();
          form.toDate = moment(vue.moreDates[1]).format();
        }

        form.numToShow = vue.numMoreMainLog;

        CallAjax2(_url[type],
          form,
          {
            busy: 'Getting {0} details...'.filledWith(niceName)
          },
          function (info) {
            logInfo.loaded = true;

            if (info && info.Success) {
              vue.lastType = type;

              vue['extend_' + type + 'List'](info.logLines, logInfo.log);

              vue.startAgeTimer();
              // vue.setUpdateTimer();
              SetInStorage('syslog', type);
              SetInStorage('syslognum', vue.numToShow);
            }
            else {
              ShowStatusFailed(info && info.Message || 'No Data? Are you logged in?');
            }
          });

      },
      extend_mainLogList: function (logLines, currentList) {
        for (var i = 0; i < logLines.length; i++) {
          this.extendMainLogLine(logLines[i]);
          currentList.push(logLines[i]);
        }
        // return logLines;
      },
      extend_onlineVotingLogList: function (logLines, currentList) {
        for (var i = 0; i < logLines.length; i++) {
          this.extendOnlineVotingLogLine(logLines[i]);
          currentList.push(logLines[i]);
        }
        // return logLines;
      },
      extend_electionListList: function (logLines, currentList) {
        for (var i = 0; i < logLines.length; i++) {
          this.extendElectionListLine(logLines[i]);
          currentList.push(logLines[i]);
        }
        // trick it to sort descending
        this.lastSort = this.currentSort = 'DateOfElection_Date';
        this.sort(this.currentSort);
      },
      extend_unconnectedVotersList: function (logLines, currentList) {
        for (var i = 0; i < logLines.length; i++) {
          this.extendUnconnectedVoterLine(logLines[i]);
          currentList.push(logLines[i]);
        }
        // trick it to sort descending
        this.lastSort = this.currentSort = 'C_RowId';
        this.sort(this.currentSort);
      },
      extendMainLogLine: function (item) {
        var vue = this;

        var details = item.Details || '';

        details = details
          .replace(/<br\/>/g, '\n')
          .replace(/<br>/g, '\n')
          .replace(/  /g, '\n') // two space - some old code got \n converted to two spaces
          .replace(/<u>/g, '!!u!!')
          .replace(/<\/u>/g, '!!/u!!')
          .replace(/\\n/g, '\n')
          .replace(/</g, '&lt;')
          .replace(/!!u!!/g, '<u>')
          .replace(/!!\/u!!/g, '</u>')
          .replace(/\n/g, '<br>')
          ;

        while (details.slice(-4) === '<br>') {
          details = details.slice(0, -4);
        }

        var lines = details.split('<br');

        var maxForShortDetails = 120;

        item.shortDetails = details.replace(/<br>/g, '').substr(0, maxForShortDetails);

        item.hasFullDetails = lines.length > 1 || lines[0].length > maxForShortDetails;

        item.fullDetails = item.hasFullDetails ? details : null;

        item.showFullDetails = false;

        item.moreFollowing = false;

        this.extendDate(item, 'AsOf', 'YYYY MMM DD, h:mm a');
      },
      extendOnlineVotingLogLine: function (item) {
        this.extendDate(item, 'OnlineWhenOpen', 'YYYY MMM DD, h:mm a');
        this.extendDate(item, 'OnlineWhenClose', 'YYYY MMM DD, h:mm a');
        this.extendDate(item, 'First', 'YYYY MMM DD, h:mm a');
        this.extendDate(item, 'MostRecent', 'YYYY MMM DD, h:mm a');
      },
      extendElectionListLine: function (item) {
        this.extendDate(item, 'RecentActivity', 'YYYY MMM DD, h:mm a');
        this.extendDate(item, 'DateOfElection', 'YYYY MMM DD');
      },
      extendUnconnectedVoterLine: function (item) {
        this.extendDate(item, 'WhenRegistered', 'YYYY MMM DD, h:mm a');
        this.extendDate(item, 'WhenLastLogin', 'YYYY MMM DD, h:mm a');
      },
      extendDate: function (obj, name, format) {
        var raw = obj[name];
        if (!raw || raw === '/Date(-62135571600000)/') { // DateTime.MinValue
          obj[name + '_Date'] = null;
          obj[name + '_M'] = {
            isAfter: function() { return false; },
            isBefore: function() { return false; },
        };
          obj[name + '_Display'] = '';
          return;
        }
        var m = moment(raw);

        obj[name + '_M'] = m;
        obj[name + '_Date'] = m.toDate();
        obj[name + '_Display'] = m.format(format || 'MMMM D, YYYY');
      },
      rowClassNames: function (report, lineItem, i) {
        var classes = [];
        if (lineItem.moreFollowing) {
          classes.push('moreFollowing');
        }

        if (report === 'onlineVotingLog') {
          var now = moment();
          if (lineItem.OnlineWhenClose_M.isAfter(now) && lineItem.OnlineWhenOpen_M.isBefore(now)) {
            classes.push('open');
          }
        }

        // if (row.isMostRecent) classes.push('MostRecent');
        // if (row.isLargest) classes.push('Largest');
        return classes.join(' ');
      },
      sort: function (field, event) {
        field = field || this.currentSort;
        if (!field) {
          return;
        }

        var dir = field === this.lastSort ? -1 : 1;
        var list = this.logInfo[this.report].log;

        list.sort(function (a, b) {
          return a[field] > b[field] ? dir : -1 * dir;
        });

        this.lastSort = dir === -1 ? null : field;
        this.currentSort = field;

        $('thead th span').remove();
        if (event) {
          $(event.target).parent();
          //todo
        }
      }

    },
  });