var vueVoterHome = {
  vue: null,
  voteMethods: []
};
var vueOptions = {
  el: '#body',
  data: function () {
    return {
      elections: [],
      election: {},
      loading: true,
      activePage: 1,
      keepStatusCurrent: false
    };
  },
  watch: {
  },
  mounted: function () {
    this.getElectionList();
  },
  methods: {
    getElectionList: function () {
      var vue = this;

      CallAjaxHandler(GetRootUrl() + 'Voter/GetVoterElections',
        null,
        function (info) {
          vue.loading = false;
          if (info.list) {
            vue.elections = vue.extendElectionInfos(info.list);

            if (vue.keepStatusCurrent) {
              // TODO if dates are changed on server, send SignalR message with new times and update status
              setInterval(vue.updateStatuses, 60 * 1000);
            }

            // for dev, go to first election
            setTimeout(function() {
              vue.manageBallot(vue.elections.find(function (e) { return e.online; }));
            }, 750);
          } else {
            ShowStatusFailed(info.Error);
          }
        });
    },
    extendElectionInfos: function (list) {
      list.forEach(this.extendElectionInfo);
      return list;
    },
    updateStatuses: function () {
      console.log('update status');
      this.elections.forEach(this.updateStatus);
    },
    updateStatus: function (info) {
      info.openNow = false;

      if (info.online) {
        this.keepStatusCurrent = true;  // found one that is online

        var o = info.online;
        o.WhenClose_M = moment(o.WhenClose); // if no date, will be invalid
        o.WhenOpen_M = moment(o.WhenOpen);

        if (o.WhenOpen_M.isAfter()) {
          // future
          info.Status_Display = 'Will open ' + o.WhenOpen_M.fromNow();
          info.classes = ['future'];
        }
        else if (o.WhenClose_M.isBefore()) {
          // past
          info.Status_Display = 'Closed ' + o.WhenClose_M.fromNow();
          info.classes = ['past'];
        }
        else if (o.WhenOpen_M.isBefore() && o.WhenClose_M.isAfter()) {
          // now
          info.classes = ['now'];
          info.openNow = true;
          var s = [];
          s.push('Open Now!<br>');
          s.push(o.CloseIsEstimate ? ' Expected to' : ' Will');
          s.push(' close ');
          s.push(o.WhenClose_M.fromNow());
          if (o.WhenClose_M.diff(moment(), 'm') < 120) {
            s.push(` (at ${o.WhenClose_M.format('h:mm a')})`);
          }
          s.push('.');
          info.Status_Display = s.join('');
        }
      } else {
        info.Status_Display = 'Not online';
      }
    },
    extendElectionInfo: function (info) {
      var person = info.person;
      if (person.RegistrationTime) {
        person.RegistrationTime_M = moment(person.RegistrationTime);
        person.RegistrationTime_Display = person.RegistrationTime_M.format('D MMM YYYY hh:mm a');
      }
      person.VotingMethod_Display = vueVoterHome.methods[person.VotingMethod] || person.VotingMethod || '-';
      this.updateStatus(info);
    },
    manageBallot: function (eInfo) {
      if (!eInfo) {
        return;
      }
      this.election = eInfo;
      this.activePage = 2;
    },
    closeBallot: function() {
      this.activePage = 1;
      this.election = {};
    }
  }
};

$(function () {
  vueVoterHome.vue = new Vue(vueOptions);
});