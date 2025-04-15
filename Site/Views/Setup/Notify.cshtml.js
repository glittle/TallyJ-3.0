function NotifyPage() {
  var settings = {
    vue: null
  };

  function preparePage() {

    settings.vue = new Vue({
      el: '#notifyBody',
      components: {
        ckeditor: CKEditor.component
      },
      data: {
        //election: publicInterface.Election,
        //        isMounted: false,
        electionId: 0,
        emailFromAddress: publicInterface.Election.EmailFromAddressWithDefault,
        emailFromName: publicInterface.Election.EmailFromNameWithDefault,
        emailSubject: publicInterface.Election.EmailSubject || '',
        emailText: publicInterface.Election.EmailText || '',
        smsText: (publicInterface.Election.SmsText || ''),
        emailChanged: false,
        smsChanged: false,
        toSend: [],
        //emailsToSend: [],
        //phonesToSend: [],
        allPeople: [],
        numEmailError: 0,
        //        testSmsNumber: GetFromStorage('htSms', ''), // not in db
        contactLog: [],
        ElectionGuid: publicInterface.Election.ElectionGuid,
        pendingSms: false,
        pendingEmail: false,
        lastLogId: 0,
        loadingLog: true,
        loadingContacts: true,
        closeTime: '',
        displayUpdateCount: 0,
        originalEmailText: '',
        editor: ClassicEditor,
        emailEditorConfig: {
          toolbar: ['undo', 'redo', '|', 'bold', 'italic', 'bulletedList', 'link'],
          image: {
            toolbar: ['imageTextAlternative']
          },
        },
        smsEditorConfig: {
          toolbar: ['undo', 'redo'],
          image: {
            toolbar: ['imageTextAlternative']
          },
        },
        dummy: 1
      },
      computed: {
        defaultFromAddress: function () {
          return publicInterface.defaultFromAddress;
        },
        emailsToSend: function () {
          return this.toSend.filter(p => p.Email);
        },
        phonesToSend: function () {
          return this.toSend.filter(p => p.Phone);
        },
        numWithEmails: function () {
          return this.emailsToSend.length;
        },
        numWithPhones: function () {
          return this.phonesToSend.length;
        },
        peopleWithEmail: function () {
          return this.allPeople.filter(function (p) { return p.Email; });
        },
        peopleWithPhone: function () {
          return this.allPeople.filter(function (p) { return p.Phone; });
        },
        enableSmsSend: function () {
          return !!(this.numWithPhones && this.smsText && !this.smsChanged);
        },
        enableEmailSend: function () {
          return !!(this.numWithEmails && this.emailFromAddress && this.emailSubject && !this.emailChanged);
        },
        defaultSort: function () {
          return {
            prop: GetFromStorage(storageKey.OVNSort, 'C_FullName'),
            order: GetFromStorage(storageKey.OVNSortDir, 'ascending')
          };
        },
        closeTime_Display: function () {
          var x = this.displayUpdateCount;
          var closeTime = this.closeTime;
          if (!closeTime) return '';
          var when = moment(closeTime);
          var prefix = moment().isBefore(when) ? 'Open for ' : 'Closed ';
          return prefix + when.fromNow();
        },
        closeTime_Date: function () {
          var closeTime = this.closeTime;
          if (!closeTime) return '';
          var when = moment(closeTime);
          return when.format('llll');
        },
        smsCost: function () {
          if (!this.numWithPhones) {
            return '';
          }
          var costPerSegment = 0.027;
          var cost = this.numWithPhones * this.smsSegments * costPerSegment;
          return Math.ceil(cost * 100) / 100;
        },
        smsSegments: function () {
          var hasSpecialCharacters = false; // to determine?

          var text = this.smsText;
          var rawLength = text.length;
          var hostTemplate = '{hostSite}';
          var numTemplates = text.split('{').length - 1;
          if (text.includes(hostTemplate)) {
            rawLength += publicInterface.hostUrlSize - hostTemplate.length;
            numTemplates--;
          }
          if (numTemplates > 0) {
            rawLength += numTemplates * 10; // guess average size of name or phone
          }

          // as per https://www.twilio.com/blog/2017/03/what-the-heck-is-a-segment.html
          var segmentSize = (hasSpecialCharacters ? 67 : 160);
          return 1 + Math.floor(rawLength / segmentSize);
        }
      },
      watch: {
      },
      created: function () {
      },
      mounted: function () {
        var vue = this;
        //vue.updateTextForSms();
        vue.refresh();

        setTimeout(function () {
          if (!vue.emailText && !vue.smsText) {
            vue.loadSamples();
          }
        }, 1000);

        setInterval(function () {
          vue.displayUpdateCount++;
        }, 60000);
      },
      methods: {
        refresh: function () {
          this.getContacts();
          this.getContactLog();
        },
        checkSms: function () {
          this.smsChanged = true;
          this.updateTextForSms();
        },
        copyEmailAddresses: function () {
          var emailString = this.emailsToSend.map(function (p) { return p.Email }).join(', ');
          navigator.clipboard.writeText(emailString).then(function () {
            console.log('Emails copied to clipboard');
          }).catch(function (err) {
            console.error('Could not copy emails: ', err);
          });

        },
        sendEmail: function (usePending) {
          var vue = this;

          if (usePending) {
            if (!vue.pendingEmail) {
              vue.pendingEmail = true;
              setTimeout(function () {
                vue.pendingEmail = false;
              }, 5000);
              return;
            }
          }

          //          var list = emailCode === 'Test' ? null : vue.emailsToSend.map(function (p) { return p.C_RowId });
          var list = vue.emailsToSend.map(function (p) { return p.C_RowId });

          //          SetInStorage('htEmailSubject', vue.emailSubject);

          var form = {
            //            emailCode: emailCode,
            //            subject: vue.emailSubject,
            list: JSON.stringify(list)
          };
          CallAjax2(publicInterface.controllerUrl + '/SendEmail', form,
            {
              busy: 'Sending Email'
            },
            function (info) {
              if (info.Success) {
                vue.getContactLog();
                ShowStatusDone(info.Status);
              }
              else {
                ShowStatusFailed(info.Status);
              }
            });

        },
        sendSms: function (usePending) {
          var vue = this;

          if (usePending) {
            if (!vue.pendingSms) {
              vue.pendingSms = true;
              setTimeout(function () {
                vue.pendingSms = false;
              }, 5000);
              return;
            }
          }

          var list = vue.phonesToSend.map(function (p) { return p.C_RowId });

          var form = {
            list: JSON.stringify(list),
          };
          CallAjax2(publicInterface.controllerUrl + '/SendSms', form,
            {
              busy: 'Sending SMS'
            },
            function (info) {
              if (info.Success) {
                vue.getContactLog();
                setTimeout(() => vue.getContactLog(), 2500);
                setTimeout(() => vue.getContactLog(), 5000);
                ShowStatusDone(info.Status);
              }
              else {
                ShowStatusFailed(info.Status);
              }
            });

        },
        loadSamples: function () {
          this.emailSubject = 'Voting in the Riḍván election';
          this.emailText = `<p>Hello {FirstName},</p>
<p>Online voting for the Riḍván election will be opening tomorrow and will remain open until 14:00 on 2020 April 20.</p>
<p>You can log in and cast your ballot at <a href="{hostSite}">TallyJ</a>.</p>
<p>The email address where you received this message ({VoterContact}) is registered for you to log in with. If you wish to vote using a different address or a phone number, please 
contact the Assembly as soon as possible!</p>
<p>If you have any questions about this process, please contact the head teller, John Smith by email at jsmith@example.com or phone at 123-456-7890.</p>
<p>With greetings from the Elections Committee</p>`;
          this.smsText = `<p>Hello {FirstName},</p>
<p>Online voting opens tomorrow.</p>
<p>Cast your ballot at tallyj.com</p>
<p>Elections Committee</p>`;
        },
        updateTextForSms: function () {
          //          var text = $('.ck-content').text();
          var html = this.smsText; // this.$refs.sms.value;

          // remove accents and some characters
          html = html.normalize("NFD").replace(/[\u0300-\u036f]/g, "");

          //var breakToken = 'ZXZXZ';
          //var tempHtml = (html || '')
          //  .replace(/<br\s?\/?>/gi, breakToken)
          //  .replace(/<p.*?>(.*?)<\/p>/gi, breakToken + '$1' + breakToken)
          //  .replace(/<li.*?>(.*?)<\/li>/gi, '- $1' + breakToken)
          //  .replace(/<a.*?href="(.*?)".*?>(.*?)<\/a>/gi, '$2 ($1)')
          //  .replace(/<ul.*?>(.*?)<\/ul>/gi, breakToken + '$1')
          //  .replace(/<ol.*?>(.*?)<\/ol>/gi, breakToken + '$1')
          //  ;


          //var text = $('<div>').html(tempHtml).text().replace(new RegExp(breakToken, 'g'), '\n');
          //          console.log(html, text);
          this.smsText = html;
        },
        selectAll: function () {
          var vue = this;
          vue.$refs.wholeList.clearSelection();
          vue.$refs.wholeList.toggleAllSelection();
          //          vue.$refs.emailList.clearSelection();
          //          vue.$refs.emailList.toggleAllSelection();
          //
          //          vue.$refs.smsList.clearSelection();
          //          vue.$refs.smsList.toggleAllSelection();
        },
        select: function (who) {
          var vue = this;

          vue.$refs.wholeList.clearSelection();
          //          vue.$refs.smsList.clearSelection();

          switch (who) {
            case 'notVoted':
              vue.allPeople.forEach(function (p) { vue.$refs.wholeList.toggleRowSelection(p, !p.VotingMethod) });
              //              vue.peopleWithPhone.forEach(function (p) { vue.$refs.smsList.toggleRowSelection(p, !p.VotingMethod) });
              break;

            case 'votedOnline':
              vue.allPeople.forEach(function (p) { vue.$refs.wholeList.toggleRowSelection(p, p.VotingMethod === 'O') });
              //              vue.peopleWithPhone.forEach(function (p) { vue.$refs.smsList.toggleRowSelection(p, p.VotingMethod === 'O') });
              break;

            case 'onlineUnfinished':
              vue.allPeople.forEach(function (p) { vue.$refs.wholeList.toggleRowSelection(p, !!p.Status && !p.VotingMethod) });
              break;

            case 'emailOnly':
              vue.allPeople.forEach(function (p) { vue.$refs.wholeList.toggleRowSelection(p, !p.Phone) });
              break;

            case 'smsOnly':
              vue.allPeople.forEach(function (p) { vue.$refs.wholeList.toggleRowSelection(p, !p.Email) });
              break;

            case 'none':
              break;
          }
        },
        selectionChanged: function (selected) {
          this.toSend = selected;
        },
        isSelectable: function (data, i) {
          return !!(data.Email || data.Phone);
        },
        //selectionChanged: function (type, selected) {
        //  var vue = this;
        //  switch (type) {
        //    case 'email':
        //      vue.emailsToSend = selected;
        //      break;
        //    case 'phone':
        //      vue.phonesToSend = selected;
        //      break;
        //  }
        //},
        tableRowClassName: function (info) {
          var row = info.row;
          var classes = [
            'method_' + row.VotingMethod_Display,
            'ballot_' + row.Status
          ];

          return classes.filter(s => s).join(' ');
        },
        getContacts: function () {
          var vue = this;
          vue.loadingContacts = true;
          CallAjaxHandler(publicInterface.controllerUrl + '/GetContacts', null, function (info) {
            vue.loadingContacts = false;
            if (info.Success) {
              vue.extendPeople(info.people);

              vue.closeTime = moment(info.onlineInfo.OnlineWhenClose).toISOString();
              vue.allPeople = info.people;

              //vue.selectAll();
            }
            else {
              ShowStatusFailed(info.Status);
            }
          });

        },
        getMoreLog: function () {
          this.getContactLog(this.lastLogId);
        },
        getContactLog: function (lastLogId) {
          var vue = this;
          vue.loadingLog = true;

          lastLogId = lastLogId || 0;

          CallAjaxHandler(publicInterface.controllerUrl + '/GetContactLog', { lastLogId: lastLogId }, function (info) {
            vue.loadingLog = false;

            if (info.Success) {

              var newEntries = vue.extendLog(info.Log);

              if (!lastLogId) {
                vue.contactLog = newEntries;
              } else {
                $('.emailHistoryHost tr:last-child()').addClass('endOfSection');
                newEntries.forEach(function (l) {
                  vue.contactLog.push(l);
                });
              }

              if (vue.contactLog.length) {
                vue.lastLogId = vue.contactLog[vue.contactLog.length - 1].C_RowId;
              }
            }
            else {
              ShowStatusFailed(info.Status);
            }
          });

        },
        extendLog: function (list) {
          list.forEach(function (lh) {
            var when_M = moment(lh.When);
            lh.age = when_M.fromNow();
            lh.when = when_M.format('llll');
          });
          return list;
        },
        downloadCompleteLog: function () {
          location.href = publicInterface.controllerUrl + '/DownloadContactLog';
        },
        extendPeople: function (list) {
          var vue = this;
          this.numEmailError = 0;

          list.forEach(function (p) {
            if (p.EmailError) {
              p.EmailErrorCopy = p.Email;
              p.Email = null;
              vue.numEmailError += 1;
            }

            p.VotingMethod_Display = publicInterface.voteMethods[p.VotingMethod] || p.VotingMethod || '';
            //            if (p.OnlineStatus) {
            //              if (p.VotingMethod === 'O') {
            //                p.VotingMethod_Display += ' - ' + p.OnlineStatus;
            //              } else {
            //                p.VotingMethod_Display += ' (Online: ' + p.OnlineStatus + ')';
            //              }
            //            }
          });
          return list;
        },
        plural: function (a, b, c, d) {
          return Plural(a, b, c, d);
        },
        applyValues: function (election) {
          //          this.emailText = election.EmailText;
          //          this.emailFromName = election.EmailFromName;
          //          this.emailFromAddress = election.EmailFromAddress;
        },
        sortChange: function (info) {
          var dir = info.order;
          var sortBy = info.prop || info.column.sortBy;
          SetInStorage(storageKey.OVNSort, sortBy);
          SetInStorage(storageKey.OVNSortDir, dir);
        },

        saveEmail: function () {
          var vue = this;

          var form = {
            emailSubject: encodeURIComponent(vue.emailSubject || '') || null,
            emailText: encodeURIComponent(vue.emailText || '') || null,
          };

          CallAjax2(publicInterface.controllerUrl + '/SaveNotification', form,
            {
              busy: 'Saving'
            },
            function (info) {
              if (info.success) {
                $('.btnSave').removeClass('btn-primary');
                vue.emailChanged = false;

                ShowStatusDone(info.Status);
              } else {
                ShowStatusFailed(info.Status);
              }
            });
        },
        saveSms: function () {
          var vue = this;

          var breakToken = 'QZQZQ';
          var text = vue.smsText
            .replace(/<p.*?>(.*?)<\/p>/gi, breakToken + '$1' + breakToken)
            .replace(/<br\s?\/?>/gi, breakToken);
          text = $('<div>').html(text).text().split(breakToken).filter(function (s) { return s; }).join('\n\n'); //.replace(new RegExp(breakToken, 'g'), '\n');

          var form = {
            smsText: encodeURIComponent(text || '') || null,
          };

          CallAjax2(publicInterface.controllerUrl + '/SaveNotification', form,
            {
              busy: 'Saving'
            },
            function (info) {
              if (info.success) {
                $('.btnSave').removeClass('btn-primary');
                vue.smsChanged = false;

                ResetStatusDisplay();
                ShowStatusDone(info.Status);
              } else {
                ShowStatusFailed(info.Status);
              }
            });
        },
      }
    });

    $(window).on('beforeunload', function () {
      if ($('.btnSave').hasClass('btn-primary')) {
        return "Changes have been made and not saved.";
      }
    });
  };




  var publicInterface = {
    controllerUrl: '',
    Election: null,
    settings: settings,
    PreparePage: preparePage
  };

  return publicInterface;
};

var notifyPage = NotifyPage();

$(function () {
  notifyPage.PreparePage();
});

