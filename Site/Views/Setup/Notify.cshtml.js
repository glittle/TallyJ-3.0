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
        smsText: publicInterface.Election.SmsText || '',
        emailChanged: false,
        smsChanged: false,
        toSend: [],
        //emailsToSend: [],
        //phonesToSend: [],
        allPeople: [],
        //        testSmsNumber: GetFromStorage('htSms', ''), // not in db
        contactLog: [],
        ElectionGuid: publicInterface.Election.ElectionGuid,
        pendingSms: false,
        pendingEmail: false,
        lastLogId: 0,
        loadingLog: true,
        originalEmailText: '',
        editor: ClassicEditor,
        emailEditorConfig: {
          toolbar: ['undo', 'redo', '|', 'bold', 'italic', 'bulletedList', 'link'],
          image: {
            toolbar: ['imageTextAlternative']
          },
          //          heading: {
          //            options: [
          //              { model: 'paragraph', title: 'Paragraph', class: 'ck-heading_paragraph' },
          //              { model: 'heading2', view: 'h2', title: 'Heading 2', class: 'ck-heading_heading2' },
          //              { model: 'heading3', view: 'h3', title: 'Heading 3', class: 'ck-heading_heading3' }
          //            ],
          //            table: {
          //              contentToolbar: ['tableColumn', 'tableRow', 'mergeTableCells']
          //            }
          //          }
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

          ShowStatusDisplay('Sending...');

          //          SetInStorage('htEmailSubject', vue.emailSubject);

          var form = {
            //            emailCode: emailCode,
            //            subject: vue.emailSubject,
            list: JSON.stringify(list)
          };
          CallAjaxHandler(publicInterface.controllerUrl + '/SendEmail', form, function (info) {
            if (info.Success) {
              vue.getContactLog();
              ShowStatusSuccess(info.Status);
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

          ShowStatusDisplay('Sending...');

          var form = {
            list: JSON.stringify(list),
          };
          CallAjaxHandler(publicInterface.controllerUrl + '/SendSms', form, function (info) {
            if (info.Success) {
              vue.getContactLog();
              setTimeout(() => vue.getContactLog(), 1000);
              setTimeout(() => vue.getContactLog(), 2000);
              ShowStatusSuccess(info.Status);
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
<p>Log in and cast your ballot at {hostSite} using this phone number.</p>
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
              vue.allPeople.forEach(function (p) { vue.$refs.wholeList.toggleRowSelection(p, !!p.OnlineStatus && !p.VotingMethod) });
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
        getContacts: function () {
          var vue = this;
          CallAjaxHandler(publicInterface.controllerUrl + '/GetContacts', null, function (info) {
            if (info.Success) {
              vue.extendPeople(info.people);

              vue.allPeople = info.people;

              //vue.selectAll();
            }
            else {
              ShowStatusFailed(info.Status);
            }
          });

        },
        //        fixPhone: function () {
        //          var vue = this;
        //          var original = vue.testSmsNumber;
        //          if (!original) {
        //            return;
        //          }
        //          var text = original.replace(/[^\+\d]/g, '');
        //          if (text.substr(0, 1) !== '+') {
        //            if (text.length === 10) {
        //              text = '1' + text;
        //            }
        //            text = '+' + text;
        //          }
        //          if (text !== original) {
        //            vue.testSmsNumber = text;
        //          }
        //        },
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
          list.forEach(function (p) {
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
        saveEmail: function () {
          var vue = this;

          var form = {
            emailSubject: encodeURIComponent(vue.emailSubject || '') || null,
            emailText: encodeURIComponent(vue.emailText || '') || null,
          };

          ShowStatusDisplay("Saving...");
          CallAjaxHandler(publicInterface.controllerUrl + '/SaveNotification', form, function (info) {
            if (info.success) {
              $('.btnSave').removeClass('btn-primary');
              vue.emailChanged = false;

              ResetStatusDisplay();
              ShowStatusSuccess(info.Status);
            } else {
              ShowStatusFailed(info.Status);
            }
          });
        },
        saveSms: function () {
          var vue = this;

          var form = {
            smsText: encodeURIComponent(vue.smsText || '') || null,
          };

          ShowStatusDisplay("Saving...");
          CallAjaxHandler(publicInterface.controllerUrl + '/SaveNotification', form, function (info) {
            if (info.success) {
              $('.btnSave').removeClass('btn-primary');
              vue.smsChanged = false;

              ResetStatusDisplay();
              ShowStatusSuccess(info.Status);
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
  ELEMENT.locale(ELEMENT.lang.en);
  notifyPage.PreparePage();
});

