﻿var VoterHome = function () {
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
      election: {},
      lastSearch: '',
      searchText: '',
      nameList: [],
      pool: [],
      savedPool: '',
      savedLock: false,
      registration: '',
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
  },
  watch: {
    searchText: function (a, b) {
      this.runSearch();
    },
    lockInVotes: function (a) {
      var vue = this;
      if (a !== vue.savedLock) {
        CallAjaxHandler(GetRootUrl() + 'Voter/LockPool',
          {
            locked: a
          }, function (info) {
            if (info.success) {
              vue.savedLock = a;
              vue.registration = info.registration;
              ShowStatusSuccess('Saved');
            } else {
              ShowStatusFailed(info.Error);
            }
          });
      }
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
            },
              750);
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
        this.keepStatusCurrent = true; // found one that is online

        var o = info.online;
        o.WhenClose_M = moment(o.WhenClose); // if no date, will be invalid
        o.WhenOpen_M = moment(o.WhenOpen);

        if (o.WhenOpen_M.isAfter()) {
          // future
          info.Status_Display = 'Will open ' + o.WhenOpen_M.fromNow();
          info.classes = ['future'];
        } else if (o.WhenClose_M.isBefore()) {
          // past
          info.Status_Display = 'Closed ' + o.WhenClose_M.fromNow();
          info.classes = ['past'];
        } else if (o.WhenOpen_M.isBefore() && o.WhenClose_M.isAfter()) {
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
      var vue = this;
      this.election = eInfo;
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
              var list = (info.votingInfo.ListPool || '').split(',').map(function (s) { return +s; })
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
      this.election = {};
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
        }, 0);
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
            }, function (info) {
              if (info.success) {
                ShowStatusSuccess('Saved');
                vue.savedPool = list;
              } else {
                ShowStatusFailed(info.Error);
              }
            });
        }
      }, delay);
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