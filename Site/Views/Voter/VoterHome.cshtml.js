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
      this.peopleHelper = new PeopleHelper(this.peopleUrl, false, true, this.customExtendPerson);
      this.connectToVoterHubs();
    },
    connectToVoterHubs: function () {
      $.connection().logging = true;
      var host = this;

      var allVotersHub = $.connection.allVotersHubCore;
      var voterPersonalHub = $.connection.voterPersonalHubCore;

      allVotersHub.client.updateVoters = function (info) {
        console.log('signalR: allVotersHub updateVoters');
        host.vue.getElectionList();
      };

      voterPersonalHub.client.updateVoter = function (info) {
        console.log('signalR: voterPersonalHub updateVoter');
        //        host.vue.getElectionList();
        host.vue.updateRegistration(info);
      };

      startSignalR(function () {
        console.log('Joining voter hub');
        CallAjaxHandler(GetRootUrl() + '/Voter/JoinVoterHubs',
          {
            connId: site.signalrConnectionId
          });
      });
    },
    customExtendPerson: function (p) {
      p.inPool = false;
      p.moving = false;
    }
  };
};
var vueOptions = {
  el: '#body',
  data: function () {
    return {
      elections: [],
      electionGuid: null,
      lastSearch: '',
      searchText: '',
      nameList: [],
      pool: [],
      savedPool: '',
      savedLock: false,
      registration: '', // full text
      numToElect: 0,
      movingInPool: null,
      lockInVotes: null,
      searchResultRow: 0,
      loading: true,
      saveDelay: null,
      activePage: 1,
      keepStatusCurrent: false
    };
  },
  computed: {
    lastInTop: function () {
      return this.numToElect - 1;
    },
    canLockIn: function () {
      return this.pool.length >= this.numToElect && !this.election.person.PoolLocked && (!this.registration || this.registration !== 'Online');
    },
    canUnlock: function () {
      return this.registration !== 'Processed' && this.election.OnlineWhenClose_M.isAfter() && this.election.person.PoolLocked;
    },
    election: function () {
      return this.elections.find(function (e) { return e.ElectionGuid === this.electionGuid; });
    }
  },
  watch: {
    searchText: function (a, b) {
      this.runSearch();
    },
  },
  created: function () {
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
              setInterval(vue.updateStatuses, 60 * 1000);
            }


            // for dev, go to first election
            //            setTimeout(function () {
            //              vue.manageBallot(vue.elections.find(function (e) { return e.online; }));
            //            }, 750);
          } else {
            ShowStatusFailed(info.Error);
          }
        });
    },
    extendElectionInfos: function (list) {
      list.forEach(this.extendElectionInfo);
      return list;
    },
    lockIn: function (locked) {
      var vue = this;
      var before = vue.savedLock;

      CallAjaxHandler(GetRootUrl() + 'Voter/LockPool',
        {
          locked: locked
        },
        function (info) {
          if (info.success) {
            vue.savedLock = locked;
            vue.lockInVotes = locked;
            vue.updateRegistration(info);
            ShowStatusSuccess('Saved');
          } else {
            ShowStatusFailed(info.Error);
            vue.savedLock = before;
            vue.lockInVotes = before;
          }
        });
    },
    extendElectionInfo: function (info) {
      var person = info.person;
      if (person.WhenStatus) {
        person.WhenStatus_M = moment(person.WhenStatus);
        person.WhenStatus_Display = person.WhenStatus_M.format('D MMM YYYY hh:mm a');

        person.BallotStatus = '{0}<br>{1}'.filledWith(person.Status, person.WhenStatus_Display);
      } else {
        if (person.RegistrationTime) {
          person.RegistrationTime_M = moment(person.RegistrationTime);
          person.RegistrationTime_Display = person.RegistrationTime_M.format('D MMM YYYY hh:mm a');

          person.BallotStatus = 'Received<br>' + person.RegistrationTime_Display;
        } else {
          person.BallotStatus = '';
        }
      }

      person.VotingMethod_Display = voterHome.voteMethods[person.VotingMethod] || person.VotingMethod || '';
      this.updateStatus(info);
    },
    updateRegistration: function (info) {
      var vue = this;
      var election = vue.election;
      if (election && election.person) {
        election.person.RegistrationTime =
          info.RegistrationTime; // || info.RegistrationTimeRaw.parseJsonDate().toISOString();
        if (info.hasOwnProperty('PoolLocked')) {
          election.person.PoolLocked = info.PoolLocked;
        }
        election.person.VotingMethod = info.VotingMethod;
        vue.extendElectionInfo(election);
        vue.registration = election.person.VotingMethod_Display;
      }
    },
    updateStatuses: function () {
      this.elections.forEach(this.updateStatus);
    },
    updateStatus: function (info) {
      info.openNow = false;

      info.canVote = info.person.Status !== 'Processed';

      if (info.OnlineWhenOpen && info.OnlineWhenClose) {
        this.keepStatusCurrent = true; // found one that is online

        info.OnlineWhenOpen_M = moment(info.OnlineWhenOpen);
        info.OnlineWhenClose_M = moment(info.OnlineWhenClose); // if no date, will be invalid

        if (info.OnlineWhenOpen_M.isAfter()) {
          // future
          info.Status_Display = 'Will open ' + info.OnlineWhenOpen_M.fromNow();
          info.classes = ['onlineFuture'];
        } else if (info.OnlineWhenClose_M.isBefore()) {
          // past
          info.Status_Display = 'Closed ' + info.OnlineWhenClose_M.fromNow();
          info.classes = ['onlinePast'];
        } else if (info.OnlineWhenOpen_M.isBefore() && info.OnlineWhenClose_M.isAfter()) {
          // now
          var minutes = info.OnlineWhenClose_M.diff(moment(), 'm');

          info.classes = [minutes <= 5 ? 'onlineSoon' : 'onlineNow'];
          info.openNow = true;
          var s = [];
          s.push('Open Now!<br>');
          s.push(info.OnlineCloseIsEstimate ? ' Expected to' : ' Will');
          s.push(' close ');
          s.push(info.OnlineWhenClose_M.fromNow());
          if (minutes < 120) {
            s.push(` (at ${info.OnlineWhenClose_M.format('h:mm a')})`);
          }
          s.push('.');
          info.Status_Display = s.join('');
        }
      } else {
        info.Status_Display = 'No online voting';
      }
    },

    manageBallot: function (eInfo) {
      if (!eInfo) {
        return;
      }
      var vue = this;
      this.electionGuid = eInfo.id;
      this.activePage = 2;

      CallAjaxHandler(GetRootUrl() + 'Voter/JoinElection',
        {
          electionGuid: eInfo.id
        },
        function (info) {
          if (info.open) {
            vue.numToElect = info.NumberToElect;
            vue.registration = info.registration;

            voterHome.peopleHelper.Prepare(function () {
              var list = (info.votingInfo.ListPool || '').split(',').map(function (s) { return +s; });
              vue.loadPool(list);

              var locked = info.votingInfo.PoolLocked && vue.pool.length >= vue.numToElect;
              vue.savedLock = locked;
              vue.lockInVotes = locked;
            });

          } else if (info.closed) {
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
    navigating: function (ev) {
      switch (ev.which) {
        case 38: // up
          this.moveSelected(-1);
          ev.preventDefault();
          return true;

        case 40: // down
          this.moveSelected(1);
          ev.preventDefault();
          return true;

        case 13: // enter
          this.addToPool(this.nameList[this.searchResultRow]);
          ev.preventDefault();
          return false;

        default:
      }
      return false;
    },
    moveSelected: function (delta) {
      //      var children = local.nameList.children();
      //      var numChildren = children.length;
      //      if (children.eq(numChildren - 1).text() == '...') { numChildren--; }
      this.searchResultRow += delta;

      if (this.searchResultRow < 0) {
        this.searchResultRow = 0;
      } else if (this.searchResultRow >= this.pool.length) {
        this.searchResultRow = this.pool.length - 1;
      }
      //      var rowNum = this.searchResultRow;
      //      rowNum = rowNum + delta;
      //      if (rowNum < 0) { rowNum = numChildren - 1; }
      //      if (rowNum >= numChildren) { rowNum = 0; }
      //      setSelected(children, rowNum);
    },
    specialSearch: function (code) {
      this.resetSearch();
      voterHome.peopleHelper.Special(code, this.displaySearchResults);
    },
    displaySearchResults: function (info) {
      voterHome.People = info.People;
      this.nameList = voterHome.People;

      this.searchResultRow = 0;
    },
    resetSearch: function () {
      this.searchText = '';
      this.lastSearch = '';
    },
    addToPool: function (p) {
      // to do - check for duplicates 
      if (p.inPool) return;
      if (!p.CanReceiveVotes) return;

      p.inPool = true;
      //console.log(p);
      this.pool.unshift(p);
      this.savePool();
    },
    removeFromPool: function (p) {
      var where = this.pool.findIndex(function (i) { return i.Id === p.Id; });
      if (where !== -1) {
        this.pool.splice(where, 1);
        p.inPool = false;
        this.savePool();
      }
    },
    closeBallot: function () {
      this.activePage = 1;
      this.electionGuid = null;
    },
    keydown: function (p, i, ev) {
      //           console.log(p, i, ev);
      var vue = this;
      var beingMoved;
      this.hideMouseCursor();

      switch (ev.code) {
        case 'Enter':
          vue.movingInPool = !vue.movingInPool;
          this.pool.forEach(function (x) { x.moving = false; });
          p.moving = vue.movingInPool;
          return;
        case 'Delete':
          vue.pool.splice(i, 1);
          // focus on new one
          beingMoved = vue.pool[i];
          break;
        case 'ArrowDown':
          ev.preventDefault();
          if (i < vue.pool.length - 1) {
            if (vue.movingInPool) {
              beingMoved = vue.pool.splice(i, 1)[0];
              vue.pool.splice(i + 1, 0, beingMoved);
            } else {
              // move cursor
              var next = ev.target.nextElementSibling;
              if (next) {
                setTimeout(function () {
                  next.focus();
                },
                  0);
              }
            }
          }
          break;
        case 'ArrowUp':
          ev.preventDefault();
          if (i > 0) {
            if (vue.movingInPool) {
              beingMoved = vue.pool.splice(i, 1)[0];
              vue.pool.splice(i - 1, 0, beingMoved);
            } else {
              // move cursor
              var prev = ev.target.previousElementSibling;
              if (prev) {
                setTimeout(function () {
                  prev.focus();
                },
                  0);
              }
            }
          }
          break;
      }

      if (beingMoved) {
        setTimeout(function () {
          vue.$refs['p' + beingMoved.Id][0].focus();
          vue.savePool();
        },
          0);
      }
    },

    hideMouseCursor: function () {
      document.body.classList.add('noCursor');
      document.body.requestPointerLock();
      document.addEventListener('mousemove', this.showMouseCursor);
    },
    showMouseCursor: function () {
      document.body.classList.remove('noCursor');
      document.exitPointerLock();
      document.removeEventListener('mousemove', this.showMouseCursor);
    },
    savePool: function (ev) {
      // delay unless done by mouse 
      var delay = ev && ev.type ? 0 : 750;
      var vue = this;
      clearTimeout(vue.saveDelay);
      vue.saveDelay = setTimeout(function () {
        var list = vue.pool.map(function (p) { return p.Id; }).join(',');
        if (vue.savedPool !== list) {
          CallAjaxHandler(GetRootUrl() + 'Voter/SavePool',
            {
              pool: list
            },
            function (info) {
              if (info.success) {
                ShowStatusSuccess('Saved');
                vue.savedPool = list;
              } else {
                ShowStatusFailed(info.Error);
              }
            });
        }
      },
        delay);
    },
    loadPool: function (list) {
      var vue = this;
      this.nameList.forEach(function (p) {
        p.inPool = false;
      });
      vue.pool = [];
      list.forEach(function (id) {
        var p = voterHome.peopleHelper.local.localNames.find(function (p) { return p.Id === id; });
        if (p) {
          p.inPool = true;
          vue.pool.push(p);
          //          console.log(p);
        }
      });
    },
  }
};

var voterHome = new VoterHome();

$(function () {
  voterHome.prepare();
  voterHome.vue = new Vue(vueOptions);
});