var VoterHome = function () {
  return {
    vue: null,
    voteMethods: [],
    People: [],
    peopleHelper: null,
    keyTimer: null,
    keyTime: 300,
    rowSelected: 0,
    lastLogin: null,
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
        var process = info.OnlineSelectionProcess;
        if (process) {
          host.vue.selectionProcess = process;
        }
      };

      voterPersonalHub.client.updateVoter = function (info) {
        console.log('signalR: voterPersonalHub updateVoter');
        //        host.vue.getElectionList();
        if (info.updateRegistration) {
          host.vue.updateRegistration(info);
          host.vue.getElectionList();
        }
        else if (info.login) {
          ShowStatusDone('This email has just been logged in with in another browser.');
          host.vue.getLoginHistory();
        }
      };

      startSignalR(function () {
        console.log('Joining voter hub');
        CallAjaxHandler(GetRootUrl() + 'Vote/JoinVoterHubs',
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
      poolLoaded: false,
      savedPool: '',
      savedLock: false,
      registration: '', // full text
      selectionProcess: 'R',
      numToElect: 0,
      movingInPool: null,
      lockInVotes: null,
      searchResultRow: 0,
      loading: true,
      saveDelay: null,
      activePage: 1,
      keepStatusCurrent: false,
      loadingLoginHistory: true,
      loginHistory: [],
      //      emailWhenOpen: false,
      emailWhenProcessed: false,
      emailCodesLoaded: false,
      randomFirst: '',
      randomLast: '',
      randomOtherInfo: '',
      randomResult: '',
      addRandomToList: true,
      clearingRandom: false,
      //      hasLocalId: false,
      meditate: false,
      movingIntoTop: false,
      showAllVotes: true,
      randomize: true
    };
  },
  computed: {
    randomInputCount: function () {
      return [this.randomFirst, this.randomLast, this.randomOtherInfo].filter(function (s) { return s.trim() }).length;
    },
    lastInTop: function () {
      return this.numToElect - 1;
    },
    canLockIn: function () {
      return this.pool.length >= this.numToElect &&
        !this.election.person.PoolLocked; // && (!this.registration || this.registration !== 'Online');
    },
    canUnlock: function () {
      return this.registration !== 'Processed' &&
        this.election.OnlineWhenClose_M.isAfter() &&
        this.election.person.PoolLocked;
    },
    election: function () {
      var vue = this;
      return vue.elections.find(function (e) { return e.id === vue.electionGuid; });
    },
    atLeastOneOpen: function () {
      return this.elections.filter(function (e) { return e.openNow; }).length > 0;
    },
    lastLoginAge: function () {
      var raw = voterHome.lastLogin;
      return raw ? moment(raw).fromNow() : '';
    },
    useList: function () {
      return this.selectionProcess === 'L' || this.selectionProcess === 'B';
    },
    useRandom: function () {
      return this.selectionProcess === 'R' || this.selectionProcess === 'B';
    },
    poolWithIndex: function () {
      var list = this.pool
        .filter(o => o)
        .map((o, i) => {
          o.index = i;
          return o;
        });

      for (var j = list.length; j < this.numToElect; j++) {
        list.push({
          index: j,
          inPool: true,
          moving: false,
          empty: true
        });
      }

      var numToElect = this.numToElect;
      var lastInTop = this.lastInTop;

      list.forEach(o => {
        o.classes = [];

        if (o.index === 0) {
          o.classes.push('startTop');
        }

        if (o.index < lastInTop) {
          o.classes.push('showBottomBorder');
        }

        if (o.index <= lastInTop) {
          o.classes.push('inTop');
        } else {
          o.classes.push('inOther');
        }

        if (o.index === lastInTop) {
          o.classes.push('endTop');
        }

        if (o.index === numToElect) {
          o.classes.push('firstInOther');
        }

        if (o.moving) {
          o.classes.push('moving');
        }
        if (o.ID < 0) {
          o.classes.push('random');
        } else {
          o.classes.push('inList');
        }

        if (o.empty) {
          o.classes.push('empty');
        }
      });
      return list;
    },
    poolInCoreBallot: {
      get: function () {
        return this.poolWithIndex.filter(o => o && o.index < this.numToElect);
      },
      set: function (v) {
        //        console.log('in', v);
      }
    },
    poolBeyondBallot:
    {
      get: function () {
        return this.poolWithIndex.filter(o => o && o.index >= this.numToElect);
      },
      set: function (v) {
        //        console.log('beyond', v);
      }
    }
  },
  watch: {
    searchText: function (a, b) {
      this.runSearch();
    },
    randomFirst: function () {
      this.randomFirst = this.cleanText(this.randomFirst);
      this.randomResult = '';
      this.searchForRandom();
    },
    randomLast: function () {
      this.randomLast = this.cleanText(this.randomLast);
      this.randomResult = '';
      this.searchForRandom();
    },
    randomOtherInfo: function () {
      this.randomOtherInfo = this.cleanText(this.randomOtherInfo);
      this.randomResult = '';
    },
    addRandomToList: function (a, b) {
      var vue = this;
      if (a) {
        setTimeout(function () {
          if (vue.$refs.firstInput) {
            vue.$refs.firstInput.focus();
          }
        },
          100);
      }
    }
  },
  created: function () {
  },
  mounted: function () {
    this.getElectionList();
  },
  methods: {
    poolChangedCore(ev) {
      //      console.log('core', ev);
      this.poolChangedInner(0, ev.added || ev.moved);
    },
    poolChangedBeyond(ev) {
      //      console.log('beyond', ev);
      this.poolChangedInner(this.numToElect, ev.added || ev.moved);
    },
    poolChangedInner(offset, action) {
      var vue = this;

      if (action) {
        // move from old to new location
        var fromIndex = action.element.index;
        var toIndex = offset + action.newIndex;

        var beingMoved = this.pool.splice(fromIndex, 1)[0];
        this.pool.splice(toIndex, 0, beingMoved);

        vue.savePool();
      }
    },
    movingStart(ev) {
      if (ev.from.id === 'pool2' && ev.to.id === 'pool1') {
        this.movingIntoTop = true;
      }
    },
    movingDone() {
      this.movingIntoTop = false;
    },
    toggleMeditate: function () {
      this.meditate = !this.meditate;
      $('body').toggleClass('meditate', this.meditate);
    },
    getElectionList: function () {
      var vue = this;

      CallAjaxHandler(GetRootUrl() + 'Vote/GetVoterElections',
        null,
        function (info) {
          vue.loading = false;
          if (info.list) {
            vue.elections = vue.extendElectionInfos(info.list);

            if (vue.keepStatusCurrent) {
              setInterval(vue.updateStatuses, 60 * 1000);
            }


            // other info
            if (info.emailCodes) {
              //              vue.emailWhenOpen = info.emailCodes.indexOf('o') !== -1;
              vue.emailWhenProcessed = info.emailCodes.indexOf('p') !== -1;
            }
            vue.emailCodesLoaded = true;
            //            vue.hasLocalId = info.hasLocalId;

            // for dev, go to first available election
            //            setTimeout(function () {
            //              vue.prepareBallot(vue.elections.find(function (e) { return e.openNow; }));
            //            }, 500);

          } else {
            ShowStatusFailed(info.Error);
          }

          vue.getLoginHistory(); // wait until election list is loaded
        });
    },
    extendElectionInfos: function (list) {
      list.forEach(this.extendElectionInfo);
      return list;
    },
    lockIn: function (locked) {
      var vue = this;
      var before = vue.savedLock;

      CallAjaxHandler(GetRootUrl() + 'Vote/LockPool',
        {
          locked: locked
        },
        function (info) {
          if (info.success) {
            vue.savedLock = locked;
            vue.lockInVotes = locked;
            vue.updateRegistration(info);
            ShowStatusDone('Submitted' + (info.notificationType ? `. ${info.notificationType} sent.` : ''));

            window.scrollTo(0, 0);
          } else {
            ShowStatusFailed(info.Error);
            vue.savedLock = before;
            vue.lockInVotes = before;
          }
        });
    },
    extendElectionInfo: function (info) {
      info.Type_Display = voterHome.electionTypes[info.ElectionType] || info.ElectionType || '';
      info.Date_Display = info.DateOfElection ? moment(info.DateOfElection).format('D MMM YYYY') : '(no date)';

      var person = info.person;

      person.VotingMethod_Display = voterHome.voteMethods[person.VotingMethod] || person.VotingMethod || '';

      if (person.WhenStatus) {
        person.WhenStatus_M = moment(person.WhenStatus);
        person.WhenStatus_Display = person.WhenStatus_M.format('D MMM YYYY h:mm a');

        person.BallotStatus = person.Status;
        person.BallotTime = person.WhenStatus_Display;

      } else {
        person.WhenStatus_M = null;
        person.WhenStatus_Display = null;
        person.BallotStatus = '-';
        person.BallotTime = '-';
      }

      this.updateStatus(info);
    },
    updateRegistration: function (info) {
      var vue = this;
      var election = vue.election;
      if (election && election.person) {
        election.person.RegistrationTime = info.RegistrationTime; // || info.RegistrationTimeRaw.parseJsonDate().toISOString();
        if (info.hasOwnProperty('PoolLocked')) {
          election.person.PoolLocked = info.PoolLocked;
        }
        election.person.VotingMethod = info.VotingMethod;
        election.person.WhenStatus = info.WhenStatus;
        vue.extendElectionInfo(election);
        vue.registration = election.person.VotingMethod_Display;
      }
    },
    updateStatuses: function () {
      this.elections.forEach(this.updateStatus);
    },
    updateStatus: function (info) {
      // refresh the status of each election
      info.openNow = false;

      info.canVote = info.person.Status !== 'Processed';
      var recent = moment().subtract(36, 'h');

      if (info.OnlineWhenOpen && info.OnlineWhenClose) {
        this.keepStatusCurrent = true; // found one that is online

        info.OnlineWhenOpen_M = moment(info.OnlineWhenOpen);
        info.OnlineWhenClose_M = moment(info.OnlineWhenClose); // if no date, will be invalid

        if (info.OnlineWhenOpen_M.isAfter()) {
          // future
          info.Status_Display = 'Will open ' + info.OnlineWhenOpen_M.fromNow();
          info.classes = ['onlineFuture'];
          info.StatusTitle = info.OnlineWhenOpen_M.format('llll');
        } else if (info.OnlineWhenClose_M.isBefore(recent)) {
          // old past
          info.Status_Display = 'Closed ' + info.OnlineWhenClose_M.fromNow();
          info.classes = ['onlineOld'];
          info.StatusTitle = info.OnlineWhenClose_M.format('llll');
        } else if (info.OnlineWhenClose_M.isBefore()) {
          // recent past
          info.Status_Display = 'Closed ' + info.OnlineWhenClose_M.fromNow();
          info.classes = ['onlinePast'];
          info.StatusTitle = info.OnlineWhenClose_M.format('llll');

        } else if (info.OnlineWhenOpen_M.isBefore() && info.OnlineWhenClose_M.isAfter()) {
          // now
          var minutes = info.OnlineWhenClose_M.diff(moment(), 'm');
          //          if (info.openNow && info.canVote) {
          info.classes = [minutes <= 5 ? 'onlineSoon' : 'onlineNow'];
          //          }
          info.openNow = true;
          var s = [];
          s.push('Open Now<br>');
          s.push(info.OnlineCloseIsEstimate ? ' Expected to' : ' Will');
          s.push(' close ');
          s.push(info.OnlineWhenClose_M.fromNow());
          if (minutes < 120) {
            s.push(` (at ${info.OnlineWhenClose_M.format('h:mm a')})`);
          }
          //          s.push('.');
          info.Status_Display = s.join('');
          info.StatusTitle = info.OnlineWhenClose_M.format('llll');
        }
      } else {
        info.Status_Display = 'No online voting';
      }
      if (this.election && !this.election.openNow) {
        this.closeBallot();
      }
    },
    scrollToTop: function (y) {
      window.scrollTo(0, y);
    },
    prepareBallot: function (eInfo) {
      if (!eInfo) {
        return;
      }
      var vue = this;

      CallAjaxHandler(GetRootUrl() + 'Vote/JoinElection',
        {
          electionGuid: eInfo.id
        },
        function (info) {
          if (info.open) {
            vue.electionGuid = eInfo.id;
            vue.numToElect = info.NumberToElect;
            vue.registration = info.registration;
            vue.selectionProcess = info.OnlineSelectionProcess;
            vue.randomize = info.RandomizeVotersList,
              vue.voterName = info.voterName;

            voterHome.peopleHelper.Prepare(function () {
              var list = JSON.parse(info.votingInfo.ListPool || '[]');
              vue.loadPool(list);

              var locked = info.votingInfo.PoolLocked && vue.pool.length >= vue.numToElect;
              vue.savedLock = locked;
              vue.lockInVotes = locked;
              //              vue.setInputFocus();

              if (list.length < 5) {
                vue.showAll();
              }
            });

            if (info.poolDecryptError) {
              ShowStatusFailed(info.poolDecryptError, null, null, null, 'error1');
            }

            vue.activePage = 2;
            vue.resetSearch();
            vue.scrollToTop(0);

          } else if (info.closed) {
            // show closed... show info if available
          } else {
            ShowStatusFailed(info.Error);
          }
        });
    },
    showAll: function () {
      var vue = this;
      if (vue.useList) {
        vue.searchText = '';

        setTimeout(function () {
          voterHome.peopleHelper.Search(voterHome.peopleHelper.local.showAllCode, vue.displaySearchResults);
        }, 0);
      }
    },
    plural: function (a, b, c, d) {
      return Plural(a, b, c, d);
    },
    runSearch: function (ev) {
      var text = this.searchText;
      //      if (this.navigating(ev)) {
      //        return;
      //      }
      if (text === '') {
        this.resetSearch();
        return;
      }
      if (this.lastSearch === text.trim()) return;

      this.lastSearch = text;
      voterHome.peopleHelper.Search(text, this.displaySearchResults);
    },
    keyDownInSearch: function (ev) {
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

        case 27: // esc
          this.resetSearch();
          ev.preventDefault();
          return false;

        default:
      }
      return true;
    },
    moveSelected: function (delta) {
      if (!this.nameList.length) {
        return;
      }

      this.hideMouseCursor();
      this.searchResultRow += delta;

      // ensure we stay in the list
      if (this.searchResultRow < 0) {
        this.searchResultRow = 0;
      } else if (this.searchResultRow >= this.nameList.length) {
        this.searchResultRow = this.nameList.length - 1;
      }

      $('#P' + this.nameList[this.searchResultRow].Id)[0].scrollIntoView({ behavior: 'smooth', block: 'center' });
    },
    //    specialSearch: function (code) {
    //      this.resetSearch();
    //      voterHome.peopleHelper.Special(code, this.displaySearchResults);
    //    },
    displaySearchResults: function (info) {
      voterHome.People = info.People;
      this.nameList = voterHome.People;

      this.searchResultRow = 0;
    },
    resetSearch: function () {
      this.searchText = '';
      this.lastSearch = '';
      this.nameList = [];

      this.resetRandomInput();

      $('.searchBox').focus();
    },
    addToPool: function (p) {
      // to do - check for duplicates 
      if (p.inPool) return;
      if (!p.CanReceiveVotes) return;
      if (this.election.person.PoolLocked) {
        ShowStatusDone('Must unlock your selection to add more people to the pool.');
        return;
      };

      p.inPool = true;
      //console.log(p);
      var position = this.numToElect - 1;
      this.pool.splice(position, 0, p);
      this.savePool();
      this.showMouseCursor();
      this.resetRandomInput();
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
      this.scrollToTop(0);
    },
    keydownInPool: function (p, i, ev) {
      //           console.log(p, i, ev);
      var vue = this;
      var beingMoved;
      this.hideMouseCursor();

      switch (ev.code) {
        case 'Enter':
          if (vue.lockInVotes) {
            return;
          }
          vue.movingInPool = !vue.movingInPool;
          this.pool.forEach(function (x) { x.moving = false; });
          p.moving = vue.movingInPool;
          return;
        case 'Delete':
          if (vue.lockInVotes) {
            return;
          }
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
              if (!next && ev.target.classList.contains('inTop')) {
                next = $('#pool2 > div')[0];
              }
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
              if (!prev && ev.target.classList.contains('inOther')) {
                prev = $('#pool1 > div').last()[0];
              }

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
        var list = vue.pool.map(function (p) {
          if (p.Id < 0) {
            return { Id: p.Id, First: p.First, Last: p.Last, OtherInfo: p.OtherInfo };
          } else {
            return { Id: p.Id };
          }
        });
        if (vue.savedPool !== list) {
          CallAjaxHandler(GetRootUrl() + 'Vote/SavePool',
            {
              pool: JSON.stringify(list)
            },
            function (info) {
              if (info.success) {
                ShowStatusDone('Saved');
                vue.savedPool = list;
                vue.election.person.Status = info.newStatus;
              } else {
                ShowStatusFailed(info.Error);
              }
            });
        }
      },
        delay);
    },
    loadPool: function (poolItems) {
      var vue = this;
      this.nameList.forEach(function (p) {
        p.inPool = false;
        p.index = 0;
        p.classes = [];
      });
      vue.pool = [];
      poolItems.forEach(function (item) {
        if (item.Id > 0) {
          var p = voterHome.peopleHelper.local.localNames.find(function (p) { return p.Id === item.Id; });
          if (p) {
            p.inPool = true;
            vue.pool.push(p);
            //          console.log(p);
          }
        } else {
          // random
          if (vue.useRandom) {
            item.Name = `${item.First} ${item.Last}`;
            item.inPool = true;
            vue.pool.push(item);
          }
        }
      });
      vue.poolLoaded = true;
    },
    getLoginHistory: function () {

      var vue = this;

      CallAjaxHandler(GetRootUrl() + 'Vote/GetLoginHistory',
        null,
        function (info) {
          vue.loadingLoginHistory = false;
          if (info.list) {
            vue.loginHistory = vue.extendLoginHistory(info.list);
          } else {
            ShowStatusFailed(info.Error);
          }

        });
    },
    extendLoginHistory: function (list) {
      list.forEach(function (lh) {
        var when_M = moment(lh.AsOf);
        lh.age = when_M.fromNow();
        lh.when = when_M.format('llll');
      });
      return list;
    },
    saveEmailCodes: function () {
      var vue = this;
      var codes = (vue.emailWhenProcessed ? 'p' : ''); // (vue.emailWhenOpen ? 'o' : '') + 
      var form = {
        emailCodes: codes || null
      };
      CallAjaxHandler(GetRootUrl() + 'Vote/SaveEmailCodes',
        form,
        function (info) {
          if (info.saved) {
            ShowStatusDone('Saved');
          } else {
            ShowStatusFailed(info.Error);
          }
        });
    },
    emailTest: function () {
      CallAjaxHandler(GetRootUrl() + 'Vote/SendTestMessage',
        null,
        function (info) {
          if (info.sent) {
            ShowStatusDone('Message Sent');
          } else {
            ShowStatusFailed(info.Error);
          }
        });
    },
    cleanText: function (s) {
      // no need to allow < or > or &
      return s.replace(/[<>&]/g, '');
    },
    searchForRandom: function () {
      //      console.log('searching', this.clearingRandom)
      if (!this.clearingRandom) {
        this.searchText = [this.randomFirst, this.randomLast].join(' ');
      }

      setTimeout(function() {
        $('.btnAddPerson')[0].scrollIntoView({ behavior: 'smooth', block: 'end' });
      }, 100);
    },
    clearRandom: function () {
      this.randomFirst = '';
      this.randomLast = '';
      this.randomOtherInfo = '';
      this.$refs.firstInput.focus();
    },
    addRandomName: function () {
      if (this.randomInputCount < 2) {
        // turn on button after one input, but only accept with two inputs
        return;
      }
      var nextFakeId = this.pool
        .filter(function (p) { return p.Id <= 0; })
        .reduce(function (acc, p) {
          return p.Id < acc ? p.Id : acc;
        },
          0) -
        1;
      var person = {
        CanReceiveVotes: true,
        inPool: false,
        First: this.cleanText(this.randomFirst).trim(),
        Last: this.cleanText(this.randomLast).trim(),
        OtherInfo: this.cleanText(this.randomOtherInfo).trim(),
        Id: nextFakeId,
      };
      person.Name = `${person.First} ${person.Last}`;
      //      if (person.Name.trim().indexOf(' ') === -1) {
      //        // only have one name
      //        this.randomResult = 'Require both First and Last name.';
      //        return;
      //
      //      }
      var nameLowerCase = person.Name.toLowerCase();

      // ensure that it is not a duplicate
      var existing = this.pool.find(function (p) {
        return p.Name.toLowerCase() === nameLowerCase && (p.OtherInfo || '') === (person.OtherInfo || '');
      });
      if (existing) {
        this.randomResult = 'Already in the pool.';
        return;
      }

      // if we have a list and can find it there, use that instead
      if (this.useList) {
        var p = voterHome.peopleHelper.local.localNames.find(function (p) {
          var nameMatches = p.Name.split(' <u>')[0].toLowerCase() === nameLowerCase;
          //          if (nameMatches) debugger;
          return nameMatches && p.OtherInfo === (person.OtherInfo || p.OtherInfo);
        });
        if (p) {
          if (p.inPool) {
            this.randomResult = 'Already in the pool.';
            return;
          }
          if (!p.CanReceiveVotes) {
            this.searchText = person.Name;
            this.randomResult = 'Cannot be voted for.';
            return;
          }
          this.addToPool(p);

          this.resetRandomInput();

          this.setInputFocus();

          return;
        }
      }

      this.addToPool(person);

      this.resetRandomInput();

      this.setInputFocus();
    },
    resetRandomInput: function () {
      this.clearingRandom = true;
      this.randomFirst = '';
      this.randomLast = '';
      this.randomOtherInfo = '';
      this.addRandomToList = true;

      setTimeout(() => {
        this.clearingRandom = false;
      }, 0);
    },
    setInputFocus: function () {
      if (this.selectionProcess === 'R') {
        this.$refs.firstInput && this.$refs.firstInput.focus();
      } else {
        this.$refs.searchBox && this.$refs.searchBox.focus();
      }
    },
    printBallot: function () {
      $(document.body).addClass('printingBallot');
      window.addEventListener("afterprint", function (ev) {
        $(document.body).removeClass('printingBallot');
      });

      Vue.nextTick(function () {
        //window.print();
      });
    }
  }
};

var voterHome = new VoterHome();

$(function () {
  voterHome.prepare();
  voterHome.vue = new Vue(vueOptions);
});