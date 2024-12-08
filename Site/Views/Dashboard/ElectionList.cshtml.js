var ElectionListPage = function () {
  var settings = {
    vue: null
  };

  var publicInterface = {
    elections: [],
    electionsUrl: '',
    loadElectionUrl: '',
    settings: settings,
    PreparePage: preparePage
  };

  function preparePage() {
    site.qTips.push({ selector: '#qTipTellers', title: 'Open for Tellers', text: 'If an election is open for tellers, it will remain open for up to one hour after the head teller leaves.' });
    site.qTips.push({ selector: '#qTipAdd', title: 'Adding Unit Elections', text: 'List the units to be added, one per line. On each line, specify the number of people to elect (which can be changed later).' });

    settings.vue = new Vue({
      el: '#electionListPage',
      data: {
        elections: [],
        topList: [],
        loaded: false,
        exporting: '',
        deleting: '',
        loadingElection: false,
        log: '',
        tempLog: '',
        showTest: true,
        //        hideOld: false,
        reloading: false,
        formatDateTime: 'YYYY MMM D [at] HH:mm',
        formatDateOnly: 'YYYY MMM D',
      },
      computed: {
        //        oldElections() {
        //          return this.elections.filter(e => e.old);
        //        },
        numForVoting() {
          return this.elections.filter(e => e.OnlineCurrentlyOpen).length;
        },
        numForTellers() {
          return this.elections.filter(e => e.openForTellers).length;
        },
        //        currentElections() {
        //          return this.elections; // .filter(e => this.showTest || !e.IsTest);
        //        },
        //        oldElectionGuids() {
        //          return this.oldElections.map(e => e.ElectionGuid);
        //        },
        electionGuids() {
          return this.elections.map(e => e.ElectionGuid);
        },
        //        oldListBtnText() {
        //          var n = this.oldElections.length;
        //          return this.hideOld ? `${n} hidden election${Plural(n)}` : 'Hide';
        //        }
      },
      watch: {

      },
      mounted() {
        //        this.hideOld = GetFromStorage('hideOld', false);

        this.showElections(publicInterface.elections);

        if (this.elections.length) {
          var electionGuidList = this.elections.map(e => e.ElectionGuid).join(',');
          connectToElectionHub(electionGuidList);
        }

        this.getMoreStatic();
        this.getMoreLive();
        this.connectToImportHub();

        site.onbroadcast(site.broadcastCode.electionStatusChanged, this.electionStatusChanged);

        this.$root.$on('delete', this.deleteElection);
        this.$root.$on('export', this.exportElection);
      },
      methods: {
        electionStatusChanged(ev, info) {
          //          var election = this.elections.find(e => e.ElectionGuid === info.ElectionGuid);
          //          console.log(election, info);
        },
        //        toggleOldList() {
        //          this.hideOld = !this.hideOld;
        //          SetInStorage('hideOld', this.hideOld);
        //        },
        extendElection(e) {
          e.onlineOpen = moment(e.OnlineWhenOpen); // if null, will be Now
          e.onlineClose = moment(e.OnlineWhenClose); // if null, will be Now

          var d = moment(e.DateOfElection);
          e.dateDisplay = e.DateOfElection ? d.format(this.formatDateOnly) : '(No date)';
          e.dateSort = e.DateOfElection ? d.toISOString() : '0';
          e.nameDisplay = (e.ElectionType !== 'LSA2U'
            ? e.Name + (e.Convenor ? (` (${e.Convenor})`) : '')
            : e.UnitName);
          //            + ` (${e.ElectionGuid})`;

          //var isCurrent = e.CanBeAvailableForGuestTellers || e.OnlineCurrentlyOpen || d.isSameOrAfter(moment(), 'day');
          e.old = false; //!isCurrent;
          // OnlineCurrentlyOpen
          //            e.old = !e.DateOfElection || !e.OnlineCurrentlyOpen || d.isBefore(moment(), 'day');

          // more static info
          e.numVoters = '';
          e.numToElect = 'Elect ' + e.NumberToElect;

          e.tellers = [];
          e.users = [];
          e.showUsers = false;
          e.inEdit = false;

          e.isTop = !e.ParentElectionGuid;
          e.childElections = [];
          e.addedToParent = false;

          // more live info
          e.numBallots = '';
          e.registered = '';
          e.numRegistered = 0;
          e.onlineVoters = {};
          e.lastLog = {};
          e.openToAdd = false;
          e.newChildren = '';

          // online voters
          if (!e.OnlineEnabled) {
            e.voterStatus = 'not used';
            e.voterStatusCircleClass = 'na';
            e.openCloseTime = '';
          } else
            if (e.OnlineCurrentlyOpen) {
              e.voterStatus = 'Open. Closing';
              e.voterStatusCircleClass = 'green';
              e.openCloseTime = e.onlineClose.fromNow();
            } else
              if (e.onlineOpen.isAfter()) {
                e.voterStatus = 'will open';
                e.voterStatusCircleClass = 'future';
                e.openCloseTime = e.onlineOpen.fromNow();
              } else
                if (e.onlineClose.isBefore()) {
                  e.voterStatus = 'closed';
                  e.voterStatusCircleClass = 'past';
                  e.openCloseTime = e.onlineClose.fromNow();
                } else {
                  e.voterStatus = '';
                  e.voterStatusCircleClass = '';
                }

          e.openForTellers = e.CanBeAvailableForGuestTellers;
          e.pendingOpenForTellers = e.openForTellers;

          return e;
        },
        showElections(list) {
          list.forEach(e => {
            this.extendElection(e);
          });

          // move sub elections to their parents
          var topList = list.filter(e => e.isTop);

          var fnAddChildren = (e) => {
            e.childElections = list.filter(c => c.ParentElectionGuid === e.ElectionGuid);
            e.childElections.forEach(c => {
              c.addedToParent = true;
              // append their children
              fnAddChildren(c);
            });

            e.childElections.sort((a, b) => {
              if (a.dateSort !== b.dateSort) {
                return a.dateSort > b.dateSort ? -1 : 1;
              }
              return a.Name > b.Name ? 1 : -1;
            });
          };

          topList.forEach(e => {
            fnAddChildren(e);

            // add the non-top ones that are not added to a parent
            var orphanList = list.filter(e => !e.addedToParent && !e.isTop);
            orphanList.forEach(e => {
              // adjust their name to show the parent's name as well
              e.nameDisplay = e.Name;
            });

            // add the orphans to the topList
            topList = topList.concat(orphanList);

            // sort by date, then name
            topList.sort((a, b) => {
              if (a.dateSort !== b.dateSort) {
                return a.dateSort > b.dateSort ? -1 : 1;
              }
              return a.Name > b.Name ? 1 : -1;
            });

            this.elections = list;
            this.topList = topList;
            this.loaded = true;
          },
            getMoreStatic() {
            // relative stable during an election
            var vue = this;
            CallAjax2(publicInterface.moreStaticUrl,
            null,
            {
              busy: 'Loading Details'
            },
            function (info) {
              info.forEach(function (incoming) {
                var matched = vue.elections.find(e => e.ElectionGuid === incoming.guid);
                if (matched) {
                  matched.tellers = incoming.tellers || [];
                  matched.users = vue.extendUsers(incoming.users);

                  matched.isOwner = matched.users.findIndex(u => u.isCurrentUser && u.Role === 'Owner') !== -1;

                  matched.showUsers = matched.users.length > 1 && matched.isOwner;
                  matched.numVoters = '- {0} can vote'.filledWith(incoming.numPeople || 0); //, Plural(incoming.numPeople));
                  matched.registered = '- {0} registered'.filledWith(incoming.numRegistered || 0); //, Plural(incoming.numPeople));
                } else {
                  console.log('unknown election', incoming);
                }
              });
            });
        },
        extendUsers(users) {
          if (!users) return [];
          users.forEach(u => {
            u.lastActivityDate = u.LastActivityDate ? moment(u.LastActivityDate).format(this.formatDateTime) : '';
            u.inviteWhen = u.InviteWhen ? moment(u.InviteWhen).format(this.formatDateTime) : '';
            if (!u.Role) {
              u.Role = 'Owner';
            }
            u.selected = false;
          });
          return users;
        },
        refreshLive() {
          this.getMoreLive(this.electionGuids);
        },
        getMoreLive(electionGuids) {
          // dynamically changing during an election
          var vue = this;
          CallAjax2(publicInterface.moreLiveUrl,
            {
              electionGuids: electionGuids
            },
            {
              busy: 'Loading Statuses'
            },
            function (info) {
              info.forEach(function (election) {
                var matched = vue.elections.find(e => e.ElectionGuid === election.guid);
                if (matched) {
                  matched.numBallots = '- {0} ballot{1} entered'.filledWith(election.numBallots, Plural(election.numBallots));

                  matched.onlineVoters = election.onlineVoters || {};
                  matched.lastLog = election.lastLog;
                } else {
                  console.log('unknown election', election);
                }
              });
              if (electionGuids) {
                ShowStatusDone('Refreshed');
              }

            });
        },
        reloadAll() {
          var vue = this;
          vue.reloading = true;
          vue.loaded = false;
          CallAjax2(publicInterface.reloadAllUrl,
            null,
            {
              busy: 'Loading List'
            },
            function (info) {
              if (info.Success) {
                vue.showElections(info.elections);
                vue.reloading = false;
                vue.getMoreStatic();
                vue.getMoreLive();
              } else {
                vue.reloading = false;

                ShowStatusFailedMessage(info);
              }
            });
        },
        connectToImportHub() {
          var hub = $.connection.importHubCore;
          var vue = this;

          hub.client.loaderStatus = function (msg, isTemp) {
            console.log(msg);
            msg = msg.replace(/\n/g, '<br> &nbsp; ');
            if (isTemp) {
              vue.tempLog = msg;
            } else {
              vue.tempLog = '';
              vue.log += `<div>${msg}</div>`;
            }
            window.scrollTo(0, document.body.scrollHeight);
          };

          startSignalR(function () {
            console.log('Joining import hub');
            CallAjaxHandler(electionListPage.importHubUrl, { connId: site.signalrConnectionId }, function (info) {

            });
          });
        },
        deleteElection(election) {
          var vue = this;
          var name = election.Name;
          var guid = election.ElectionGuid;

          vue.deleting = guid;

          if (!confirm('Completely delete this election from TallyJ?\n\n  {0}\n\n'.filledWith(name))) {
            ResetStatusDisplay();
            vue.deleting = '';
            return;
          }

          var form =
          {
            guid: guid
          };

          CallAjax2(publicInterface.electionsUrl + '/DeleteElection', form,
            {
              busy: 'Deleting'
            },
            function (info) {
              if (info.Deleted) {
                $(`#${guid}`).slideUp(1000, 0, function () {
                  debugger;
                  vue.removeElection(vue.elections, guid);
                });
              } else {
                ShowStatusFailed(info.Message);
              }
              vue.deleting = '';
            });
        },
        removeElection(elections, guid) {
          // remove from any childElection list
          elections.forEach(e => {
            this.removeElection(e.childElections, guid);
          });

          // remove from this list
          var i = elections.findIndex(c => c.ElectionGuid === guid);
          if (i !== -1) {
            elections.splice(i, 1);
          }
        },
        createElection() {
          // get the server to make an election, then go see it
          CallAjaxHandler(publicInterface.electionsUrl + '/CreateElection', null, function (info) {
            if (info.Success) {
              location.href = site.rootUrl + 'Setup';
              return;
            }
          });
        },
        exportElection(election) {
          var vue = this;
          var guid = election.ElectionGuid;

          ShowStatusBusy("Preparing file...");

          vue.exporting = guid;

          var iframe = $('body').append('<iframe style="display:none" src="{0}/ExportElection?guid={1}"></iframe>'.filledWith(publicInterface.electionsUrl, guid));
          iframe.ready(function () {
            setTimeout(function () {
              ResetStatusDisplay();
              vue.exporting = '';
            }, 1000);
          });
        },
        upload2() {
          var $input = $('#loadFile');
          if ($input.val() === '') {
            return;
          }
          var vue = this;

          ShowStatusBusy("Loading election...");

          vue.loadingElection = true; // turn on, not turned off
          vue.log = '';
          vue.tempLog = 'Sending the file to the server...';

          var form = $('#formLoadFile');
          var frameId = 'tempUploadFrame';
          var frame = $('#' + frameId);
          if (!frame.length) {
            $('body').append('<iframe id=' + frameId + ' name=' + frameId + ' style="display:none" />');
            frame = $('#' + frameId);
          }
          form.attr({
            target: frameId,
            action: publicInterface.loadElectionUrl,
            enctype: 'multipart/form-data',
            method: 'post'
          });

          var frameObject = frame.on('load', function () {
            frameObject.unbind('load');
            $input.val(''); // blank out file name

            var response = frameObject.contents().text();
            var info;
            try {
              info = $.parseJSON(response);
            } catch (e) {
              info = { Success: false, Message: "Unexpected server message" };
            }

            if (info.Success) {
              vue.showElections(info.Elections);
              ShowStatusDone('Loaded');

              var election = vue.elections.find(el => el.ElectionGuid === info.ElectionGuid);
              if (election) {
                setTimeout(() => {
                  var row = vue.$refs['e-' + election.ElectionGuid];
                  if (row) {
                    scrollToMe(row[0]);
                  }
                }, 1000);
              }
            }
            else {
              ShowStatusFailed(info.Message);
            }
          });

          form.submit();
        }
      }
    });


    //    $(document).on('click', '.btnSelectElection', null, selectElection);
    //    $(document).on('click', '.btnExport', null, exportElection);
    //    $(document).on('click', '.btnDelete', null, deleteElection);
    //    $(document).on('click', '#btnLoad', null, loadElection);
    //    $(document).on('click', '#file-uploader', null, loadElection2);
    //    $(document).on('click', '#btnCreate', null, createElection);
    //    $(document).on('change', '#loadFile', null, upload2);
    //    $(document).on('click', '#btnUpload2', null, upload2);
  };






  function scrollToMe(nameDiv) {
    var target = $(nameDiv);
    target.addClass('justloaded');

    var top = target.offset().top;
    var fudge = -83;
    var time = 800;

    try {
      $(document).animate({
        scrollTop: top + fudge
      }, time);
    } catch (e) {
      // ignore error - may not work
      console.log(e.message);
    }

    setTimeout(function () {
      target.toggleClass('justloaded', 'slow');
    }, 15 * 1000);
  };

  return publicInterface;
};

var electionListPage = ElectionListPage();

$(function () {
  ELEMENT.locale(ELEMENT.lang.en);
  electionListPage.PreparePage();
});


Vue.component('election-detail',
  {
    template: '#election-detail',
    props: {
      e: Object,
      old: Boolean,
      test: Boolean,
      exporting: String,
      deleting: String,
    },
    data: function () {
      return {
        formatDateTime: 'YYYY MMM D [at] HH:mm',
        formatDateOnly: 'YYYY MMM D',
        showOtherButtons: false,
        addingNew: false,
        addingNow: false,
        form: {
          email: '',
          invited: false,
        },
        rules: {
          email: [
            {
              required: true,
              trigger: 'blur',
              type: 'email'
            }
          ]
        }
      }
    },
    computed: {
      showForm: function () {
        return this.addingNew || !!this.selectedUser;
      },
      users: function () {
        return this.e.users;
      },
      selectedUser: function () {
        return this.users.find(u => u.selected);
      },
      isLsa2U: function () {
        return this.e.ElectionType === 'LSA2U';
      },
      isLsa2M: function () {
        return this.e.ElectionType === 'LSA2M';
      },
      onlineOpenText: function () {
        return this.e.OnlineWhenOpen ? 'Open: ' + this.e.onlineOpen.format(this.formatDateTime) + ' - ' + this.e.onlineOpen.fromNow() :
          '-';
      },
      onlineCloseText: function () {
        return this.e.OnlineWhenClose ? 'Close: ' + this.e.onlineClose.format(this.formatDateTime) + ' - ' + this.e.onlineClose.fromNow() :
          '-';
      },
      tellerToggleTitle: function () {
        return this.e.ElectionPasscode ? 'Open/Close for Tellers' : 'A passcode must be set before tellers can join.';
      },
      onlineVoteCounts: function () {
        var statusCodes = Object.keys(this.e.onlineVoters);
        statusCodes.sort();

        return statusCodes.map(key => {
          var statusCount = this.e.onlineVoters[key];
          var when = moment(statusCount.AsOf);
          return `${key}: ${statusCount.Count} as of ${when.fromNow()}`;
        })
          .map(s => `<div>${s}</div>`)
          .join('');

      }
      //      onlineOpenTitle: function () {
      //        return this.e.OnlineWhenOpen ? this.e.onlineOpen.format(this.format2) : '-';
      //      },
      //      onlineCloseTitle: function () {
      //        return this.e.OnlineWhenClose ? this.e.onlineClose.format(this.format2) : '-';
      //      },
    },
    watch: {
    },
    methods: {
      selectElection(election) {
        var guid = election.ElectionGuid;
        var form =
        {
          guid: guid,
          oldComputerGuid: GetFromStorage('compcode_' + guid, null)
        };

        clearElectionRelatedStorageItems();

        ShowStatusBusy('Entering election...'); // will reload page so don't need to clear it
        CallAjaxHandler(electionListPage.electionsUrl + '/SelectElection', form, this.afterSelectElection);

      },
      afterSelectElection(info) {

        if (info.Selected) {
          //TODO: store computer Guid
          SetInStorage('compcode_' + info.ElectionGuid, info.CompGuid);

          location.href = site.rootUrl + 'Dashboard';

        }
        else {
          ShowStatusFailed("Unable to select");
        }
      },
      addUnitElections(election) {
        if (!election.openToAdd) {
          election.openToAdd = true;
          return;
        }

        // check for invalid letters
        if (/[;<]/.test(election.newChildren)) {
          ShowStatusFailed('Cannot include ; or < in names');
          return;
        }

        // clean up the names; each needs , N to indicate number to elect
        // x, 3\n y,2
        var unitsInfo = election.newChildren
          .split('\n')
          .map(n => n.trim())
          .filter(n => n && n.includes(','))
          .map(s => {
            var x = s.split(',');
            return { name: x[0], num: +x[1] };
          });

        if (!unitsInfo.length) {
          election.openToAdd = false;

          return;
        }
        //        if (unitsInfo.filter(a => a.length !== 2)) {
        //          ShowStatusFailed('Must include a count for each unit');
        //          return;
        //        }

        var vue = this;

        CallAjax2(electionListPage.electionsUrl + '/CreateUnitElection',
          {
            parentElectionGuid: election.ElectionGuid,
            unitsInfo: JSON.stringify(unitsInfo)
          },
          {
            busy: 'Creating unit election' + Plural(unitsInfo.length)
          },
          function (info) {
            if (info.Success) {
              // add all the info.elections to the childElections list
              info.elections.forEach(e => {
                var election = electionListPage.settings.vue.extendElection(e);
                electionListPage.settings.vue.elections.push(election);
                election.childElections.push(election);
              });
              election.newChildren = '';
              election.openToAdd = false;
            }
          });
      },
      deleteElection() {
        this.$root.$emit('delete', this.e,);
      },
      exportElection() {
        this.$root.$emit('export', this.e);
      },
      updateListing() {
        var vue = this;
        var open = this.e.pendingOpenForTellers;
        var form = {
          listOnPage: open,
          electionGuid: this.e.ElectionGuid
        };
        CallAjax2(electionListPage.updateListingUrl + '/UpdateListingForElection',
          form,
          {
            busy: 'Changing Open Status'
          },
          function (info) {
            if (info.Success) {
              vue.e.openForTellers = info.IsOpen;
              ShowStatusDone("Changed");
            }
            else {
              vue.e.pendingOpenForTellers = !open;
              ShowStatusFailedMessage(info);
            }
          });
      },
      selectUser(user) {
        this.addingNew = false;
        this.users.forEach(u => u.selected = false);
        user.selected = true;
        this.form.email = user.InviteEmail;
        this.form.invited = !!user.inviteWhen;
      },
      openForAdd() {
        this.users.forEach(u => u.selected = false);
        this.form.email = '';
        this.form.invited = false;
        this.addingNew = true;
        setTimeout(function () {
          $('.addNew')[0].scrollIntoView({ behavior: 'smooth', block: 'end' });
        }, 0);
      },
      closeForm() {
        this.addingNew = false;
        if (this.selectedUser) {
          this.selectedUser.selected = false;
        }
      },
      processForm() {
        this.$refs.form.validate((valid) => {
          if (valid) {
            this.addUser();
          } else {
            return false;
          }
        });
      },
      addUser() {
        // this.form.invited = !this.form.invited;
        var vue = this;
        var email = this.form.email;

        // ensure it is not a duplicate
        var dup = this.users.find(u => u.Email === email || u.InviteEmail === email);
        if (dup) {
          // todo give message
          return;
        }

        this.addingNow = true;

        var form = {
          email: email,
          election: this.e.ElectionGuid
        }
        CallAjax2(electionListPage.updateListingUrl + '/AddFullTeller',
          form,
          {
            busy: 'Adding'
          },
          function (info) {
            if (info.Success) {

              var u = info.user;
              u.lastActivityDate = u.LastActivityDate ? moment(u.LastActivityDate).format(vue.formatDateTime) : '';
              u.inviteWhen = u.InviteWhen ? moment(u.InviteWhen).format(vue.formatDateTime) : '';
              u.selected = true;

              vue.users.push(u);

              ShowStatusDone("Added");

              vue.sendInvitation();
            }
            else {
              ShowStatusFailedMessage(info);
            }
            vue.addingNow = false;
            vue.addingNew = false;
          });
      },
      removeUser() {
        var vue = this;
        var user = this.selectedUser;
        var form = {
          email: user.Email,
          joinId: user.C_RowId
        }
        CallAjax2(electionListPage.updateListingUrl + '/RemoveFullTeller',
          form,
          {
            busy: 'Removing'
          },
          function (info) {
            if (info.Success) {
              var i = vue.users.findIndex(u => u.C_RowId === form.joinId);
              if (i !== -1) {
                vue.users.splice(i, 1);
              }
              ShowStatusDone("Removed");
            }
            else {
              ShowStatusFailedMessage(info);
            }
          });
      },
      sendInvitation() {
        var vue = this;
        var user = this.selectedUser;
        var form = {
          joinId: user.C_RowId
        }
        CallAjax2(electionListPage.updateListingUrl + '/SendInvitation',
          form,
          {
            busy: 'Sending Email'
          },
          function (info) {
            if (info.Success) {
              ShowStatusDone("Email sent");
            }
            else {
              ShowStatusFailedMessage(info);
            }
          });
      }
    }
  });