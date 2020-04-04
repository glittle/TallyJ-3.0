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
        election: publicInterface.Election,
        isSaveNeeded: false,
        //        isMounted: false,
        electionId: 0,
        emailText: publicInterface.Election.EmailText,
        emailFromAddress: publicInterface.Election.EmailFromAddress,
        emailFromName: publicInterface.Election.EmailFromName,
        smsText: '',
        peopleWithEmail: [],
        peopleWithPhone: [],
        emailsToSend: [],
        phonesToSend: [],
        emailSubject: GetFromStorage('htEmailSubject', ''), // not in db
        testSmsNumber: GetFromStorage('htSms', ''), // not in db
        contactLog: [],
        ElectionGuid: publicInterface.Election.ElectionGuid,
        pendingSms: false,
        pendingEmail: false,
        lastLogId: 0,
        originalEmailText: '',
        emailEditor: ClassicEditor,
        editorConfig: {
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
        dummy: 1
      },
      computed: {
        defaultFromAddress: function () {
          return publicInterface.defaultFromAddress;
        },
        numWithEmails: function () {
          return this.emailsToSend.length;
        },
        numWithPhones: function () {
          return this.phonesToSend.length;
        },

      },
      watch: {
      },
      created: function () {
      },
      mounted: function () {
        var vue = this;
        vue.updateTextForSms();
        vue.refresh();

        setTimeout(function () {
          if (!vue.emailText) {
            vue.loadSampleEmail();
          }
        }, 1000);
      },
      methods: {
        refresh: function () {
          this.getContacts();
          this.getContactLog();
        },
        saveNeeded: function () {
          //          if (!this.isMounted) return;
          $('.btnSave').addClass('btn-primary');
          this.isSaveNeeded = true;
        },
        textChanged: function () {
          var vue = this;
          this.saveNeeded();
          setTimeout(function () {
            vue.updateTextForSms();
          }, 0);
        },

        sendEmail: function (emailCode, usePending) {
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

          var list = emailCode === 'Test' ? null : vue.emailsToSend.map(function (p) { return p.C_RowId });

          ShowStatusDisplay('Sending...');

          SetInStorage('htEmailSubject', vue.emailSubject);

          var form = {
            emailCode: emailCode,
            subject: vue.emailSubject,
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
        loadSampleEmail: function () {
          this.emailText = `<p>Hello {PersonName},</p>
<p>Online voting for the Riḍván election will be opening tomorrow and will remain open until 14:00 on 2020 April 20.</p>
<p>You can log in and cast your ballot at <a href="{hostSite}">TallyJ</a>.</p>
<p>The email address or mobile phone number where you got this message is registered for you to log in with. If you wish to vote using a different address or number, please 
contact the Assembly as soon as possible!</p>
<p>If you have any question about this process, please contact the head teller, John Smith by email at jsmith@example.com or phone at 123-456-7890.</p>
<p>With greeting from the Elections Committee</p>
`;
        },
        updateTextForSms: function () {
          //          var text = $('.ck-content').text();
          var html = this.$refs.email.value;

          var breakToken = 'ZXZXZ';
          var tempHtml = (html || '')
            .replace(/<br\s?\/?>/gi, breakToken)
            .replace(/<p.*?>(.*?)<\/p>/gi, breakToken + '$1' + breakToken)
            .replace(/<li.*?>(.*?)<\/li>/gi, '- $1' + breakToken)
            .replace(/<a.*?href="(.*?)".*?>(.*?)<\/a>/gi, '$2 ($1)')
            .replace(/<ul.*?>(.*?)<\/ul>/gi, breakToken + '$1')
            .replace(/<ol.*?>(.*?)<\/ol>/gi, breakToken + '$1')
            ;
          var text = $('<div>').html(tempHtml).text().replace(new RegExp(breakToken, 'g'), '\n');
          //          console.log(html, text);
          this.smsText = text;
        },
        sendSms: function (emailCode, usePending, testPhone) {
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

          var list = null;

          if (testPhone) {
            if (!testPhone.match(/\+\d{4,15}/)) {
              ShowStatusFailed('Invalid mobile phone number.');
              return;
            }
            SetInStorage('htSms', testPhone);
          } else {
            list = vue.phonesToSend.map(function (p) { return p.C_RowId });
          }

          ShowStatusDisplay('Sending...');

          var form = {
            emailCode: emailCode,
            testPhone: testPhone,
            list: JSON.stringify(list),
            text: vue.smsText
          };
          CallAjaxHandler(publicInterface.controllerUrl + '/SendSms', form, function (info) {
            if (info.Success) {
              //              vue.getContactLog();
              vue.getContactLog();
              ShowStatusSuccess(info.Status);
            }
            else {
              ShowStatusFailed(info.Status);
            }
          });

        },
        selectAll: function () {
          var vue = this;
          vue.$refs.emailList.clearSelection();
          vue.$refs.emailList.toggleAllSelection();

          vue.$refs.smsList.clearSelection();
          vue.$refs.smsList.toggleAllSelection();
        },
        select: function (who) {
          var vue = this;

          vue.$refs.emailList.clearSelection();
          vue.$refs.smsList.clearSelection();

          switch (who) {
            case 'notVoted':
              vue.peopleWithEmail.forEach(function (p) { vue.$refs.emailList.toggleRowSelection(p, !p.VotingMethod) });
              vue.peopleWithPhone.forEach(function (p) { vue.$refs.smsList.toggleRowSelection(p, !p.VotingMethod) });
              break;

            case 'votedOnline':
              vue.peopleWithEmail.forEach(function (p) { vue.$refs.emailList.toggleRowSelection(p, p.VotingMethod === 'O') });
              vue.peopleWithPhone.forEach(function (p) { vue.$refs.smsList.toggleRowSelection(p, p.VotingMethod === 'O') });
              break;

            case 'none':
              break;
          }
        },
        selectionChanged: function (type, selected) {
          var vue = this;
          switch (type) {
            case 'email':
              vue.emailsToSend = selected;
              break;
            case 'phone':
              vue.phonesToSend = selected;
              break;
          }
        },
        getContacts: function () {
          var vue = this;
          CallAjaxHandler(publicInterface.controllerUrl + '/GetContacts', null, function (info) {
            if (info.Success) {
              vue.extendPeople(info.people);

              vue.peopleWithEmail = info.people.filter(function (p) { return p.Email });
              vue.peopleWithPhone = info.people.filter(function (p) { return p.Phone });

              vue.selectAll();
            }
            else {
              ShowStatusFailed(info.Status);
            }
          });

        },
        fixPhone: function () {
          var vue = this;
          var original = vue.testSmsNumber;
          if (!original) {
            return;
          }
          var text = original.replace(/[^\+\d]/g, '');
          if (text.substr(0, 1) !== '+') {
            if (text.length === 10) {
              text = '1' + text;
            }
            text = '+' + text;
          }
          if (text !== original) {
            vue.testSmsNumber = text;
          }
        },
        getMoreLog: function () {
          this.getContactLog(this.lastLogId);
        },
        getContactLog: function (lastLogId) {
          var vue = this;

          lastLogId = lastLogId || 0;

          CallAjaxHandler(publicInterface.controllerUrl + '/GetContactLog', { lastLogId: lastLogId }, function (info) {
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
            var when_M = moment(lh.AsOf);
            lh.age = when_M.fromNow();
            lh.when = when_M.format('llll');
          });
          return list;
        },
        extendPeople: function (list) {
          list.forEach(function (p) {
            p.VotingMethod_Display = publicInterface.voteMethods[p.VotingMethod] || p.VotingMethod || '';
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
        saveChanges: function () {
          var vue = this;

          var form = {
            emailText: encodeURIComponent(vue.emailText || '') || null,
          };

          ShowStatusDisplay("Saving...");
          CallAjaxHandler(publicInterface.controllerUrl + '/SaveNotification', form, function (info) {
            if (info.success) {
              $('.btnSave').removeClass('btn-primary');
              vue.isSaveNeeded = false;

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

