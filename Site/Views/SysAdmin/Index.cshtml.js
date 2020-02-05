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

    });
  };

  return {
    preparePage: preparePage,
    settings: localSettings
  };
}


var logView = LogView();

$(function () {
  logView.preparePage();
});

Vue.component('eventLog',
  {
    template: '#eventLog',
    props: {
    },
    data: function () {
      return {
        logInfo: {
          mainLog: {
            log: [],
            loaded: false
          },
        },
        activeTab: 'mainLog',
        searchText: '',
        autoUpdateSeconds: +GetFromStorage('admintimer', 0),
        // autoUpdateTimer: null,
        ageTimer: null,
        ageStart: null,
        secondsSinceLoaded: 0,
        age: '',
        ageSeconds: 0,
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
      autoUpdateSeconds: function (a, b) {
        if (this.autoUpdateSeconds && this.autoUpdateSeconds < this.ageSeconds) {
          this.refreshActive();
        }

        SetInStorage('admintimer', a);
      }
    },
    mounted: function () {
      this.getLog('mainLog', true); // load the events on first view
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
            if (type === vue.activeTab) {
              vue.getLog(type, true);
            }
            else {
              logInfo.loaded = false;
            }
          }
        });
      },
      changedTab: function () {
        var type = this.activeTab;
        if (!this.logInfo[type].loaded) {
          this.getLog(type, true);
        }
      },
      getLog: function (type, reload) {
        var vue = this;
        var logInfo = vue.logInfo[type];

        vue.cancelTimers();

        if (reload) {
          logInfo.log = [];
        }

        var lastId = 0;
        var last;
        switch (type) {
          case 'mainLog':
            if (logInfo.log.length) {
              last = logInfo.log[logInfo.log.length - 1];
              lastId = last.C_RowId;
              last.moreFollowing = true;
            }
            break;
        }

        var form = {
          lastRowId: lastId,
          search: vue.searchText
        };

        ShowStatusDisplay('Getting {0} details...'.filledWith(type));

        CallAjaxHandler(_url[type],
          form,
          function (info) {
            logInfo.loaded = true;

            if (info.Success) {
              vue['extend_' + type + 'List'](info.logLines, logInfo.log);

              vue.startAgeTimer();
              // vue.setUpdateTimer();
            }
            else {
              ShowStatusFailed(info.Message);
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
      extendMainLogLine: function (item) {
        var vue = this;

        var details = item.Details || '';

        details = details
          .replace(/<br\/>/g, '\n')
          .replace(/<br>/g, '\n')
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

        let lines = details.split('<br');

        item.shortDetails = lines[0];

        item.numLines = lines.length;

        item.fullDetails = item.numLines > 1 ? details : null;

        item.showFullDetails = false;

        item.moreFollowing = false;

        this.extendDate(item, 'AsOf', 'YYYY MMM DD, h:mm a');
      },
      extendDate: function (obj, name, format) {
        var raw = obj[name] || '';
        if (!raw.getTime) {
          obj[name + '_Date'] = raw.substring(1, 6) === 'Date(' ? raw.parseJsonDate() : null;
        }
        obj[name + '_Display'] = raw ? moment(raw).format(format || 'MMMM D, YYYY') : '';
      },
      rowClassNames: function (lineItem, i) {
        var classes = [];
        if (lineItem.moreFollowing) {
          classes.push('moreFollowing');
        }

        // if (row.isMostRecent) classes.push('MostRecent');
        // if (row.isLargest) classes.push('Largest');
        return classes.join(' ');
      },


    },
  });