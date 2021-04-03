var StatusDisplay = function () {
  var settings = {
    prepared: false,
    //    activeMessages: [],
    uniqueKeys: {},
    busyIconBase64: 'data:image/gif;base64,R0lGODlhEAAQAPIAAP/krFByUNTIlX2PZ1ByUJOdc6mrf7SzhCH/C05FVFNDQVBFMi4wAwEAAAAh/hpDcmVhdGVkIHdpdGggYWpheGxvYWQuaW5mbwAh+QQJCgAAACwAAAAAEAAQAAADMwi63P4wyklrE2MIOggZnAdOmGYJRbExwroUmcG2LmDEwnHQLVsYOd2mBzkYDAdKa+dIAAAh+QQJCgAAACwAAAAAEAAQAAADNAi63P5OjCEgG4QMu7DmikRxQlFUYDEZIGBMRVsaqHwctXXf7WEYB4Ag1xjihkMZsiUkKhIAIfkECQoAAAAsAAAAABAAEAAAAzYIujIjK8pByJDMlFYvBoVjHA70GU7xSUJhmKtwHPAKzLO9HMaoKwJZ7Rf8AYPDDzKpZBqfvwQAIfkECQoAAAAsAAAAABAAEAAAAzMIumIlK8oyhpHsnFZfhYumCYUhDAQxRIdhHBGqRoKw0R8DYlJd8z0fMDgsGo/IpHI5TAAAIfkECQoAAAAsAAAAABAAEAAAAzIIunInK0rnZBTwGPNMgQwmdsNgXGJUlIWEuR5oWUIpz8pAEAMe6TwfwyYsGo/IpFKSAAAh+QQJCgAAACwAAAAAEAAQAAADMwi6IMKQORfjdOe82p4wGccc4CEuQradylesojEMBgsUc2G7sDX3lQGBMLAJibufbSlKAAAh+QQJCgAAACwAAAAAEAAQAAADMgi63P7wCRHZnFVdmgHu2nFwlWCI3WGc3TSWhUFGxTAUkGCbtgENBMJAEJsxgMLWzpEAACH5BAkKAAAALAAAAAAQABAAAAMyCLrc/jDKSatlQtScKdceCAjDII7HcQ4EMTCpyrCuUBjCYRgHVtqlAiB1YhiCnlsRkAAAOwAAAAAAAAAAAA==',
    lastMsg: null,
    vue: null
  };
  var prepare = function () {
    if (settings.prepared)
      return;

    $('body').prepend(
      '<div class="StatusOuter">' +
       '<div class="StatusMiddle">' +
        '<div class="StatusInner">' +
         '<div id="statusDisplayVue" class="StatusActive" v-if="summary.length">' +
          '<div v-for="m in summary" v-bind:class="[m.type]">' +
            '<i v-if="m.type===\'error\'" class="el-icon-warning"></i>' +
            '<i v-if="m.type===\'busy\'" class="el-icon-loading"></i>' +
           '<span class=msg v-text="m.msgText + (m.count > 1 ? \' (\' + m.count + \' times)\' : \'\')"></span>' +
           '<span class=close v-if="m.type===\'error\'">' +
            '<i v-on:click="reset(m)" class="el-icon-circle-close" title="Dismiss this message"></i>' +
           '</span>' +
          '</div>' +
         '</div>' +
        '</div>' +
       '</div>' +
      '</div>');

    settings.vue = new Vue({
      name: 'SiteStatus',
      el: '#statusDisplayVue',
      data: {
        messages: []
      },
      computed: {
        hasError: function () {
          return this.summary.findIndex(function (m) { return m.type === 'error'; }) !== -1;
        },
        summary: function () {
          var nonDupList = [];
          this.messages.forEach(function (m) {
            m.count = 1;
            var key = m.key || (m.key = m.msgText + m.type);
            var dup = nonDupList.find(function (a) { return a.key === key });
            if (dup) {
              dup.count = dup.count + 1;
            }
            else {
              nonDupList.push(m);
            }
          });
          return nonDupList;
        }
      },
      methods: {
        reset: function (msgInfo) {
          if (!msgInfo)
            return false;

          clearTimeout(msgInfo.timeoutReset);
          clearTimeout(msgInfo.timeoutDelay);

          var list = this.messages;
          var toRemoveIndex = list.findIndex(function (m) { return m.key === msgInfo.key });
          if (toRemoveIndex !== -1) {
            list.splice(toRemoveIndex, 1);
          }
          return true;
        }
      }
    });

    settings.prepared = true;
  };

  var showMessageExternal = function (msgText, type, howLong, delayBeforeShowing, uniqueKey) {
    if (site.printMode) {
      return null;
    }

    if (!settings.prepared) {
      console.log('Status module not ready yet.', msgText);
      return null;
    }

    if (!msgText) {
      console.log('Empty status message');
      return null;
    }

    var uniqueKeyStr = uniqueKey || '';
    if (uniqueKey === true) {
      // use the message as the key
      uniqueKeyStr = msgText;
    }

    if (type === 'static') {
      if (settings.lastMsg && settings.lastMsg.type === 'busy') {
        settings.vue.reset(settings.lastMsg);
      }
    }

    if (uniqueKeyStr) {
      var oldMsg = settings.uniqueKeys[uniqueKeyStr];
      if (oldMsg) {
        settings.vue.reset(oldMsg);
      }
    }

    if (!howLong) {
      howLong = 0; // indefinite

      if (type === 'static') {
        // set reasonable length for static display
        howLong = Math.max(5000, Math.max(0, msgText.length - 20) * 600 + msgText.split(/\s/).length * 500);
      }
    }

    var msgInfo = {
      msgText: msgText,
      howLong: howLong,
      type: type,
      delayBeforeShowing: delayBeforeShowing,
      uniqueKey: uniqueKeyStr
    };

    showMessageInternal(msgInfo);

    settings.lastMsg = msgInfo;


    if (uniqueKeyStr) {
      settings.uniqueKeys[uniqueKeyStr] = msgInfo;
    }

    return msgInfo;
  };

  var showMessageInternal = function (msgInfo) {

    if (typeof msgInfo.delayBeforeShowing !== 'number') {
      msgInfo.delayBeforeShowing = 750;
    }

    if (msgInfo.delayBeforeShowing) {
      msgInfo.timeoutDelay = setTimeout(function () {
        msgInfo.delayBeforeShowing = 0;
        showMessageInternal(msgInfo);
      }, msgInfo.delayBeforeShowing);
      return;
    }

    if (msgInfo.howLong) {
      // console.log('will reset', msgInfo.howLong, msgInfo.msgText);
      msgInfo.timeoutReset = setTimeout(function () {
        settings.vue.reset(msgInfo);
      }, msgInfo.howLong);
    }

    // going to show it now
    console.log(msgInfo.msgText);

    settings.vue.messages.push(msgInfo);

    //    var test = settings.vue.summary;
    settings.vue.$forceUpdate();
  };

  var resetAnyMessage = function (msgInfo) {
    var targetMsg = null;
    if (msgInfo) {
      targetMsg = msgInfo;
    }
    else {
      if (!settings.vue) {
        return false;
      }
      // unknown message
      // reset most recent that does not have a unique key
      var list = settings.vue.messages;
      for (var i = list.length - 1; i >= 0; i--) {
        var msg = list[i];
        if (msg.uniqueKey) {
          // do not reset if the last message has a key
          continue;
        }
        targetMsg = msg;
        break;
      }

      if (!targetMsg && list.length) {
        // if didn't find a message without a unique key, clear the last one even with a key
        targetMsg = list[list.length - 1];
      }

    }
    return settings.vue.reset(targetMsg);
  }

  return {
    prepare: prepare,
    settings: settings,
    showMessage: showMessageExternal,
    resetAnyMessage: resetAnyMessage,
    busyIcon: settings.busyIconBase64
  };
};



var statusModule = StatusDisplay();

$(function () {
  statusModule.prepare();
});

// --------------------------------------------------------------------------------------------------------------------------------------------
// global routines used in other files
// --------------------------------------------------------------------------------------------------------------------------------------------
function ShowStatusBusy(msg, howLong, uniqueKey, delay) {
  /// <summary>Show this message. By default, wait 1/2 second, and show loading image</summary>
  /// <param name="msg" type="String">Message to display.</param>
  /// <param name="howLong" type="Number">If not set, msg will stay indefinitely.</param>
  /// <param name="uniqueKey">If provided in two subsequent calls, the second will reset the previous instance.  If true, will use the msg as the key.</param>
  return statusModule.showMessage(msg, 'busy', howLong, delay, uniqueKey);
}
function ShowStatusDone(msg, howLong, uniqueKey) {
  /// <summary>Show this static message immediately, keeping it for a short time. If there is an active 'Busy' message, this will reset it.</summary>
  /// <param name="msg" type="String">Message to display.</param>
  /// <param name="howLong" type="Number">If not set, msg will stay for a reasonable time.</param>
  /// <param name="uniqueKey">If provided in two subsequent calls, the second will reset the previous instance.  If true, will use the msg as the key.</param>
  return statusModule.showMessage(msg, 'static', howLong, 0, uniqueKey);
}
function ShowStatusImmediately(msg, howLong, uniqueKey) {
  /// <summary>Show this message immediately</summary>
  /// <param name="msg" type="String">Message to display.</param>
  /// <param name="howLong" type="Number">If not set, msg will stay for a reasonable time.</param>
  /// <param name="uniqueKey">If provided in two subsequent calls, the second will reset the previous instance.  If true, will use the msg as the key.</param>
  return statusModule.showMessage(msg, 'busy', howLong, 0, uniqueKey);
}

function ShowStatusFailedMessage(info) {
  ShowStatusFailed(info.Message);
  if (info.Why) {
    console.log(info.Why);
  }
}

function ShowStatusFailed(msg) {
  var text;
  if (!msg) {
    return null;
  }
  if (typeof msg === 'string') {
    text = msg;
  }
  else if (typeof msg.statusText === 'string') {
    if (msg.status === 200 || msg.status === 406) {
      text = msg.statusText + '<br>' + msg.responseText;
    }
    else if (msg.status === 0 && msg.state() === 'rejected') {
      console.log('call rejected');
      return null; // signalR call rejected
    }
    else if (msg.status === 0 && msg.statusText === 'error') {
      text = 'Busy... please try again in a few seconds.'; // client Ajax subsystem busy?
    }
    else if (msg.status === 503) {
      top.location.href = top.location.href;
      text = '';
    }
    else {
      if (msg.status === 500) {
        if (msg.responseText.search(/A potentially dangerous Request.Form /) !== -1
          // this only works for Developers - the details for other users are not in the responseText
          || msg.responseText.search(/Unable to save with/) !== -1) {
          text = 'Unable to save with "<" embedded. Please edit and try again.';
        }
        else {
          var di = msg.responseText.search(/Developer Information: /);
          if (di !== -1) {
            text = msg.responseText.substring(di).split('</h2>')[0];
          }
        }
      }
    }
    if (!text) {
      text = 'Error ' + msg.status + ': ';
      var matches = msg.responseText ? msg.responseText.match(/\<title\>(.*?)\<\/title\>/i) : null;
      if (matches !== null) {
        text = text + matches[1];
      }
      else if (msg.responseText) {
        text = text + (msg.responseText || msg.statusText);
      }
    }
  }
  else {
    text = "Error";
  }
  return statusModule.showMessage(text, 'error', null, 0, true);
}
function ResetStatusDisplay(msgInfo, resetAllMessages) {
  /// <summary>Clear the message previously put up. If msgInfo parameter is not supplied, will remove the most 
  ///    recently shown if it does NOT have a unique id.</summary>
  /// <param name="msgInfo">If provided, will attempt to reset it. Okay if null. If not provided, will reset the most recently displayed message.</param>
  if (statusModule) {
    var done;
    do {
      done = statusModule.resetAnyMessage(msgInfo);
    } while (resetAllMessages && done)
  }
}

//
//setTimeout(function () {
//
//  ShowStatusBusy('show display', 10000);
//  ShowStatusBusy('show display', 1000);
//  ShowStatusBusy('show display', 2000);
//  ShowStatusBusy('show display');
//  ShowStatusBusy('show display 2000 - this will be gone');
//  ShowStatusDone('show done 2000 - cleared the busy one');
//  ShowStatusBusy('show display', null, 'abc');
//  ShowStatusBusy('show display', null, 'abc');
//  ShowStatusFailed('show failed');
//  ShowStatusImmediately('show immediately!!');
//
//  ShowStatusImmediately('show immediately 2');
//  ResetStatusDisplay();
//  ResetStatusDisplay(null, true);
//
//}, 500);