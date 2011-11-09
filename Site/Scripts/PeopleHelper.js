/// <reference path="site.js" />
/// <reference path="jquery-1.7-vsdoc.js" />

var PeopleHelper = function (url) {
  var local = {
    url: url
  };
  var startGettingPeople = function (search, onNamesReady) {
    ShowStatusDisplay('searching...', 500);
    CallAjaxHandler(local.url + '/GetPeople', {
      search: search,
      includeInelligible: true
    }, onComplete, onNamesReady, onFail);
  };

  var onComplete = function (info, onNamesReady) {
    ResetStatusDisplay();
    if (info.Error) {
      ShowStatusFailed(info.Error);
      return;
    }
    onNamesReady(info);
  };
  var onFail = function (xmlHttpRequest) {
    var msg = '';
    if (msg) {
      ShowStatusFailed(msg);
    }
  };
  var wrapPerson = function (flatPerson) {
    var person = {};
    $.each(local.Columns.Num, function (i, value) {
      person[value] = flatPerson[i];
    });
    return person;
  };
  var wrapPeople = function (people) {
    var wrapped = [];
    for (var i = 0; i < people.length; i++) {
      wrapped.push(wrapPerson(people[i]));
    }
    return wrapped;
  };
  var publicInterface = {
    Prepare: function () {
    },
    SearchNames: function (searchText, onNamesReady) {
      startGettingPeople(searchText, onNamesReady);
    }
  };

  return publicInterface;
};

