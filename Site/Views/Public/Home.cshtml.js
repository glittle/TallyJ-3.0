﻿var HomeIndexPage = function () {
  var local = {
    reconnectHubTimeout: null,
    hubReconnectionTime: 95000,
    warmupDone: false,
    isBad: false,
    vue: null,
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
    //startJoinClick(null, 'btnChooseVoter');
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
        showElections(info?.html);
      });
    });
  };

  function showElections(html) {
    var select = $('#ddlElections');
    if (!select.length) {
      return;
    }
    select.html(html);
    var size = select[0].children.length + 2;
    select.attr('size', Math.min(size, 9));
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
      connectToPublicHubToGetElectionList();
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
    whatsapp: false,
    smsAvailable: false,
  };

  /**
   * Initializes the Vue instance for the voter interface.
   * This function sets up the data properties, computed properties,
   * and methods necessary for handling voter interactions.
   * It also manages the connection to the SignalR hub for real-time updates.
   *
   * @function
   * @returns {void}
   *
   * @example
   * // Call this function to set up the voter interface
   * setupVoterVue();
   */
  function setupVoterVue() {
    //    console.log('online', homeIndexPage.vote, publicInterface.vote, $("#voterVue"));
    if (!homeIndexPage.vote) {
      return;
    }
    if (!$('#voterVue').length) {
      // user must be logged in already
      return;
    }

    local.vue = new Vue({
      el: '#voterVue',
      data: function () {
        return {
          mode: '',
          code: '',
          email: '',
          kiosk: '',
          phone: '',
          phoneMethod: '',
          sending: false,
          //          sent: false,
          status: '',
          sendingStatus: '',
          //          twilioCallDone: false,
          connectedToHub: false,
          hubKey: '',
          smsAvailable: publicInterface.smsAvailable,
          voiceAvailable: publicInterface.voiceAvailable,
          showCodeInput: false,
          showKiosk: GetFromStorage('kiosk', 'N') === 'Y' // set by teller on this device
        };
      },
      computed: {
        okayToSend: function () {
          return !!this[this.mode]; // something entered - should validate
        },
        //        codePrompt: function () {
        //          var msg;
        //          switch (this.phoneMethod) {
        //            case 'voice':
        //              msg = this.twilioCallDone ? 'Call completed' : 'Calling your phone...';
        //              break;
        //            case 'sms':
        //              msg = ''; // live updates will show the status
        //              break;
        //            default:
        //              msg = '?';
        //              break;
        //          }
        //          return msg;
        //        }
      },
      watch: {
        //        phone(a, b) {
        //          if (a && a.length < (b || '').length) {
        //            this.fixPhone();
        //          }
        //        },
        //        callStatus(a, b) {
        //          console.log(a, b);
        //          if (a === 'ringing') {
        //            this.showCodeInput = true;
        //          }
        //        }
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
          this.status = '';
          this.joinHub();
          setTimeout(() => $('.voterLogin input').focus(), 0);
        },
        //        var $phone = local.hostPanel.find('[data-name="Phone"]');
        //    $phone.on('change paste', fixPhone);

        fixPhone: function (phone) {
          if (!phone || phone.length < 3) {
            return phone || '';
          }
          var text = phone.replace(/[^\+\d]/g, '');
          if (text.substr(0, 1) !== '+') {
            // +123
            if (text.length === 10) {
              // USA 10 digits - add +1
              text = '+1' + text;
            } else {
              text = '+' + text;
            }
          }
          //          if (text.substr(0, 1) === '+') {
          //            // +123
          //            if (text.length === 11 && text.substr(2, 1) !== '1') {
          //              // + and 10 digits - remove the + and add +1
          //              text = '+1' + text.substr(1);
          //            }
          //          } else {
          //            if (text.length === 10) {
          //              // US 10 digits?
          //              text = '1' + text;
          //            }
          //            text = '+' + text;
          //          }
          return text;
        },
        sendEmail: function () {
          var email = this.email.trim();
          if (!email) {
            return;
          }
          this.issueCode('email', null, email);
        },
        sendPhone: function (method) {
          var phone = this.fixPhone(this.phone);
          if (phone.length < 3) {
            return;
          }
          this.phone = phone;
          this.phoneMethod = method;
          this.issueCode('phone', method, phone);
        },
        joinHub: function () {
          var vue = this;
          if (vue.connectedToHub) {
            return;
          }
          vue.hubKey = Math.random().toString().slice(-5);

          var hub = $.connection.voterCodeHubCore;

          hub.client.setStatus = function (message, sendingStatus) {
            console.log('signalR: voterCodeHub status', message, sendingStatus);
            if (sendingStatus) {
              vue.showCodeInput = true;
              //              vue.sent = false;

              switch (sendingStatus) {
                case 'completed':
                  vue.sendingStatus = 'Call completed';
                  //                  vue.sent = false;
                  //                  vue.twilioCallDone = true;
                  break;
                case 'delivered':
                  vue.sendingStatus = 'SMS sent. Enter the code below:';
                  //                  vue.twilioCallDone = true;
                  break;
                case 'emailSent':
                  vue.sendingStatus = 'Email sent. Enter the code below:';
                  //                  vue.twilioCallDone = true;
                  break;
                default:
                  vue.sendingStatus = message;
                  break;
              }
            } else {
              vue.status = message;
              vue.sendingStatus = '';
              //              vue.sent = false;
            }
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
        issueCode: function (type, method, target) {
          var vue = this;

          this.sending = true;
          if (method === 'email') {
            this.status = 'Sending...';
          }
          // do the call
          CallAjaxHandler(publicInterface.controllerUrl + 'IssueCode',
            {
              type: type,
              method: method,
              target: target,
              hubKey: vue.hubKey
            }, function (info) {
              vue.showCodeInput = true;
              vue.sending = false;

              if (info.Success) {
                // hub will update
                console.log('issued');

                //                vue.sent = true; //testing
                vue.sending = false; // testing
                setTimeout(() => {
                  $('.voterLogin .pendingCode input').focus();
                  $('.afterLogin')[0].scrollIntoView({ block: "end", inline: "nearest" });
                }, 100);

              } else {
                vue.sendingStatus = info.Message;
              }
            });
        },
        submitKiosk: function () {
          this.code = 'K_' + this.kiosk;
          this.submitCode();
        },
        submitCode: function () {
          var vue = this;
          vue.status = 'Checking code...';

          // do the call
          CallAjaxHandler(publicInterface.controllerUrl + 'LoginWithCode',
            {
              code: vue.code
            }, function (info) {
              if (info.Success) {
                vue.status = 'Success. Entering the site...';
                location.href = site.rootUrl + 'Vote';

              } else {
                vue.status = info.Message;
              }
            });
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