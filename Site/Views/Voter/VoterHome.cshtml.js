var VoterHome = function () {
  return {
    vue: null,
    voteMethods: [],
    People: [],
    peopleHelper: null,
    keyTimer: null,
    keyTime: 300,
    rowSelected: 0,
    prepare: function () {
      this.peopleHelper = new PeopleHelper(this.peopleUrl, false, true);
    }
  };
};
var vueOptions = {
  el: '#body',
  data: function () {
    return {
      elections: [],
      election: {},
      lastSearch: '',
      searchText: '',
      nameList: [],
      loading: true,
      activePage: 1,
      keepStatusCurrent: false
    };
  },
  watch: {
    searchText: function (a, b) {
      this.runSearch();
    }
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
            setTimeout(function () {
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
      //      console.log('update status');
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
      person.VotingMethod_Display = voterHome.voteMethods[person.VotingMethod] || person.VotingMethod || '-';
      this.updateStatus(info);
    },
    manageBallot: function (eInfo) {
      if (!eInfo) {
        return;
      }
      this.election = eInfo;
      this.activePage = 2;

      CallAjaxHandler(GetRootUrl() + 'Voter/JoinElection',
        {
          electionGuid: eInfo.id
        },
        function (info) {
          if (info.open) {
            voterHome.peopleHelper.Prepare(function (info) {

              console.log('ready to search');
              //        if (totalOnFile < 25) {
              //          specialSearch('All');
              //        } else {
              //          nameList.html('<li class=Match5>(Ready for searching)</li>');
              //        }
              //        inputField.prop('disabled', false);
              //        inputField.focus();
            });
          }
          else if (info.closed) {
            // show closed... show info if available
          } else {
            ShowStatusFailed(info.Error);
          }
        });
    },
    runSearch: function (ev) {
      var text = this.searchText;
      //      if (this.navigating(ev)) {
      //        return;
      //      }
      if (this.lastSearch === text.trim()) return;
      if (text === '') {
        this.resetSearch();
        return;
      }

      this.lastSearch = text;
      voterHome.peopleHelper.Search(text, this.displaySearchResults);
    },
    displaySearchResults: function (info) {
      voterHome.People = info.People;
      this.nameList = voterHome.People;

      return;

      //      $('#more').html(info.MoreFound || moreFound(local.totalOnFile));

      if (!local.People.length && local.lastSearch) {
        local.nameList.append('<li>...no matches found...</li>');
      }
      else {
        //if (info.MoreFound && local.lastSearch) {
        //  local.nameList.append('<li>...more matched...</li>');
        //}
        if (local.showPersonId) {
          local.rowSelected = local.nameList.find('#P' + local.showPersonId).index();
          local.showPersonId = 0;
        } else if (local.selectByVoteCount) {
          $.each(local.People, function (i, item) {
            if (item.NumVotes && !local.maintainCurrentRow) {
              local.rowSelected = i;
            }
          });
        }
      }
      local.maintainCurrentRow = false;
      local.actionTag.removeClass('searching');
      local.inputField.removeClass('searching');
      local.actionTag.removeClass('delaying');
      local.inputField.removeClass('delaying');

      // if none selected, selects first name
      var selectedName = local.nameList.children().eq(local.rowSelected);
      selectedName.addClass('selected');
      if (local.rowSelected) {
        scrollIntoView(selectedName, local.nameList);
      }
    },
    resetSearch: function () {
      this.searchText = '';
      this.lastSearch = '';
      //      local.actionTag.removeClass('delaying');
      //      local.inputField.removeClass('delaying');
      //      displaySearchResults({
      //        People: [],
      //        MoreFound: moreFound(local.totalOnFile)
      //      });
    },
    closeBallot: function () {
      this.activePage = 1;
      this.election = {};
    },
    updateSearch: function () {
      console.log(this.searchText);
    }
  }
};

var voterHome = new VoterHome();

$(function () {
  voterHome.prepare();
  voterHome.vue = new Vue(vueOptions);
});