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

    settings.vue = new Vue({
      el: '#electionListPage',
      data: {
        elections: [],
        loaded: false,
        exporting: '',
        deleting: '',
        loadingElection: false,
        log: '',
        tempLog: '',
        hideOld: false
      },
      computed: {
        oldElections() {
          return this.elections.filter(e => e.old);
        },
        currentElections() {
          return this.elections.filter(e => !e.old);
        },
        oldElectionGuids() {
          return this.oldElections.map(e => e.ElectionGuid);
        },
        currentElectionGuids() {
          return this.currentElections.map(e => e.ElectionGuid);
        },
        oldListBtnText() {
          var n = this.oldElections.length;
          return this.hideOld ? `${n} hidden election${Plural(n)}` : 'Hide';
        }
      },
      watch: {

      },
      mounted() {
        this.hideOld = GetFromStorage('hideOld', false);

        this.showElections(publicInterface.elections);

        if (!this.currentElections.length) {
          // no current, show the old
          this.hideOld = false;
        }

        this.getMoreStatic();
        this.getMoreLive();
        this.connectToImportHub();
      },
      methods: {
        toggleOldList() {
          this.hideOld = !this.hideOld;
          SetInStorage('hideOld', this.hideOld);
        },
        test(election) {
          var vue = this;
          debugger;
        },
        showElections(list) {
          list.forEach(e => {
            e.onlineOpen = moment(e.OnlineWhenOpen); // if null, will be Now
            e.onlineClose = moment(e.OnlineWhenClose); // if null, will be Now

            e.date = moment(e.DateOfElection);
            e.dateDisplay = e.DateOfElection ? e.date.format("YYYY-MMM-DD") : '(No date)';
            e.dateSort = e.DateOfElection ? e.date.format("YYYY-MM-DD") : '0';

            if (e.OnlineCurrentlyOpen) {
              e.OnlineOpen = 'Online Voting Open. Closing '
                + e.onlineClose.fromNow() + '.';
            }
            e.old = !e.OnlineOpen && e.date.isBefore() || !e.DateOfElection;
            e.numVoters = '';
            e.tellers = [];

            e.numBallots = '';
            e.onlineVoters = {};
            e.lastLog = {};

            e.openForTellers = e.CanBeAvailableForGuestTellers;
          });

          list.sort((a, b) => {
            if (a.dateSort !== b.dateSort) {
              return a.dateSort > b.dateSort ? -1 : 1;
            }
            return a.Name > b.Name ? 1 : -1;
          });

          this.elections = list;
          this.loaded = true;
        },
        selectElection(election) {
          var guid = election.ElectionGuid;
          var form =
          {
            guid: guid,
            oldComputerGuid: GetFromStorage('compcode_' + guid, null)
          };

          clearElectionRelatedStorageItems();

          ShowStatusBusy('Opening election...'); // will reload page so don't need to clear it
          CallAjaxHandler(publicInterface.electionsUrl + '/SelectElection', form, afterSelectElection);

        },
        getMoreStatic() {
          // relative stable during an election
          var vue = this;
          CallAjaxHandler(publicInterface.moreStaticUrl, null, function (info) {
            info.forEach(function (incoming) {
              var matched = vue.elections.find(e => e.ElectionGuid === incoming.guid);
              if (matched) {
                matched.numVoters = '- {0} name{1}'.filledWith(incoming.numPeople, Plural(incoming.numPeople));
                matched.tellers = incoming.tellers || [];
              } else {
                console.log('unknown election', incoming);
              }
            });
          });
        },
        refreshLive() {
          this.getMoreLive(this.currentElectionGuids);
        },
        getMoreLive(electionGuids) {
          // dynamically changing during an election
          var vue = this;
          CallAjaxHandler(publicInterface.moreLiveUrl,
            {
              electionGuids: electionGuids
            },
            function (info) {
              info.forEach(function (election) {
                var matched = vue.elections.find(e => e.ElectionGuid === election.guid);
                if (matched) {
                  matched.numBallots = '- {0} ballot{1}'.filledWith(election.numBallots, Plural(election.numBallots));

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
                var row = vue.$refs['e-' + guid];
                $(row[0]).slideUp(1000, 0, function () {

                  var i = vue.elections.findIndex(e => e.ElectionGuid === guid);
                  vue.elections.splice(i, 1);

                  ShowStatusDone('Deleted.');
                });
              } else {
                ShowStatusFailed(info.Message);
              }
              vue.deleting = false;
            });
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



  function afterSelectElection(info) {

    if (info.Selected) {
      //TODO: store computer Guid
      SetInStorage('compcode_' + info.ElectionGuid, info.CompGuid);

      location.href = site.rootUrl + 'Dashboard';

    }
    else {
      ShowStatusFailed("Unable to select");
    }
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

