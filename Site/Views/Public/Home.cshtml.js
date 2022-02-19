var HomeIndexPage = function () {
  var local = {
    reconnectHubTimeout: null,
    hubReconnectionTime: 95000,
    warmupDone: false,
    isBad: false,
    vue: null
  };

  var isBadBrowser = function () {
    if (window.safari) {
      // odd layout issues?
      return 'Safari';
    }
    // catch ie11 and some old mobile chromes
    if (typeof Symbol === "undefined") return 'Symbol';

    var msg = '';
    try {
      eval('let x = 1');
    } catch (e) {
      return 'Old browser';
    }
    return '';
  };

  var preparePage = function () {
    var isBad = local.isBad = isBadBrowser();
    if (isBad) {
      $('.badBrowser .detail').text(isBad + ', ' + navigator.userAgent).show();
      $('.badBrowser').show();
      return;
    }

    $('#btnJoin').on('click', btnJoinClick);
    $('#btnRefresh').on('click', refreshElectionList);
    $('#txtPasscode').on('keypress', function (ev) {
      if (ev.which === 13) {
        btnJoinClick();
      }
    });
    $('#btnChooseJoin').click(startJoinClick);
    $('#btnChooseLogin').click(startJoinClick);
    $('#btnChooseVoter').click(startJoinClick);
    $(document).keydown(function (ev) {
      if (ev.which === 27) {
        cancelStart();
      }
    });
    $('img.closer').click(cancelStart);

    clearElectionRelatedStorageItems();

    //connectToPublicHubToGetElectionList();

    //    if ($('.VoterLoginError').length) {
    //      startJoinClick(null, 'btnChooseVoter');
    //    }

    // refreshElectionList();
    $('form').on('submit', function () {
      logoffSignalR();
    });

    if (window.location.search.indexOf('v=voter') !== -1) {
      $('#btnChooseVoter').click();
    }

    setupVoterVue();

    // for testing
    startJoinClick(null, 'btnChooseVoter');
  };

  var connectToPublicHubToGetElectionList = function () {
    var hub = $.connection.publicHubCore;

    hub.client.electionsListUpdated = function (listing) {
      console.log('signalR: electionsListUpdated');

      showElections(listing);
    };

    startSignalR(function () {
      console.log('Joining public hub');
      CallAjaxHandler(publicInterface.controllerUrl + 'PublicHub', { connId: site.signalrConnectionId }, function (info) {
        showElections(info.html);
      });
    });
  };

  function showElections(html) {
    var select = $('#ddlElections');
    if (!select.length) {
      return;
    }
    select.html(html);
    select.attr('size', select[0].children.length + 2);
    selectDefaultElection();
  };

  var refreshElectionList = function () {
    connectToPublicHubToGetElectionList();
  };

  function selectDefaultElection() {
    var children = $('#ddlElections').children();
    if (children.length === 1 && children.eq(0).val() !== 0) {
      children.eq(0).prop('selected', true);
    }
  };



  function startJoinClick(dummy, btnIdRequested) {
    var btnId = btnIdRequested || $(this).attr('id');


    if (btnId === 'btnChooseJoin') {
      $('.CenterPanel').addClass('chosen');
      $('.LoginPanel').hide();
      $('.VoterPanel').hide();
      $('.JoinPanel').fadeIn();
    }
    else if (btnId === 'btnChooseVoter') {
      //location.href = GetRootUrl() + 'VoterAccount/Login';
      $('.CenterPanel').addClass('chosen');
      $('.JoinPanel').hide();
      $('.LoginPanel').hide();
      $('.VoterPanel').fadeIn();
      warmupServer();
    }
    else {
      $('.CenterPanel').addClass('chosen');
      $('.VoterPanel').hide();
      $('.JoinPanel').hide();
      $('.LoginPanel').fadeIn();
      warmupServer();
    }
  };

  function warmupServer() {
    if (local.warmupDone) {
      return;
    }
    local.warmupDone = true;
    CallAjaxHandler(publicInterface.controllerUrl + 'Warmup');
  }

  function cancelStart() {
    $('.CenterPanel').removeClass('chosen');
    $('.JoinPanel').hide();
    $('.LoginPanel').hide();
    $('.VoterPanel').hide();
  }

  var btnJoinClick = function () {
    var statusSpan = $('#joinStatus').removeClass('error');

    var electionGuid = $('#ddlElections').val();
    if (!electionGuid || electionGuid === '0') {
      statusSpan.addClass('error').html('Please select an election');
      return false;
    }

    var passCode = $('#txtPasscode').val();
    if (!passCode) {
      statusSpan.addClass('error').html('Please type in the access code');
      return false;

    }
    statusSpan.addClass('active').removeClass('error').text('Checking...');

    var form = {
      electionGuid: electionGuid,
      pc: passCode,
      oldCompGuid: GetFromStorage('compcode_' + electionGuid, null)
    };

    CallAjaxHandler(publicInterface.controllerUrl + 'TellerJoin', form, function (info) {
      if (info.LoggedIn) {
        SetInStorage('compcode_' + electionGuid, info.CompGuid);
        statusSpan.addClass('success').removeClass('active').html('Success! &nbsp; Going to the Dashboard now...');
        location.href = publicInterface.dashBoardUrl;
        return;
      }

      refreshElectionList();
      statusSpan.addClass('error').removeClass('active').html(info.Error);
    });
    return false;
  };

  var publicInterface = {
    PreparePage: preparePage,
    controllerUrl: '',
    dashBoardUrl: '',
    local: local,
    vote: false,
    sms: false,
  };

  function setupVoterVue() {
    //    console.log('online', homeIndexPage.vote, publicInterface.vote, $("#voterVue"));
    if (!homeIndexPage.vote) {
      return;
    }

    local.vue = new Vue({
      el: '#voterVue',
      data: function () {
        return {
          mode: '',
          code: '',
          email: '',
          phone: '',
          sending: false,
          sent: false,
          status: '',
          connectedToHub: false,
          hubKey: ''
        };
      },
      computed: {
        okayToSend: function () {
          return !!this[this.mode]; // something entered - should validate
        },

      },
      watch: {
      },
      created: function () {
      },
      mounted: function () {
        if (!homeIndexPage.vote) {
          this.chooseMethod('email');
        }
      },
      methods: {
        chooseMethod: function (mode) {
          this.mode = mode;
          setTimeout(() => $('.voterLogin input').focus(), 0);
        },
        sendEmail: function () {
          this.send('email', null, this.email);
        },
        sendPhone: function (method) {
          this.send('phone', method, this.phone);
        },
        joinHub: function () {
          var vue = this;
          if (vue.connectedToHub) {
            return;
          }
          vue.hubKey = Math.random().toString().slice(-5);

          var hub = $.connection.voterCodeHubCore;

          hub.client.setStatus = function (message) {
            console.log('signalR: voterPersonalHub status', message);
            vue.status = message;
          };

          hub.client.final = function (okay, message) {
            console.log('signalR: voterPersonalHub final');
            vue.status = message;
            if (okay) {
              // go to inner page
            }
          };

          console.log('Joining voter hub');

          startSignalR(function () {
            CallAjaxHandler(publicInterface.controllerUrl + 'VoterCodeHub',
              {
                connId: site.signalrConnectionId,
                key: vue.hubKey
              },
              function (info) {
                vue.connectedToHub = true;
              });
          });

        },
        send: function (type, method, target) {
          var vue = this;
          this.joinHub();

          setTimeout(() => {


            this.status = 'A...';
            this.sending = true;

            // do the call
            CallAjaxHandler(publicInterface.controllerUrl + 'GetCode',
              {
                type: type,
                method: method,
                target: target,
                hubKey: vue.hubKey
              }, function (info) {

                vue.sent = true; //testing
                vue.sending = false; // testing

                if (info.Success) {
                  // hub will update

                } else {
                  ShowStatusFailedMessage(info);
                }
              });

          }, 100);


        }
      }
    });

  }

  return publicInterface;
};

var homeIndexPage = HomeIndexPage();

$(function () {
  homeIndexPage.PreparePage();
});