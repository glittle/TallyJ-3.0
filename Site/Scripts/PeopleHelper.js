var PeopleHelper = function (url) {
  var local = {
    url: url,
    lastInfo: null
  };
  var startGettingPeople = function (search, onNamesReady, includeMatches, usedPersonIds, forBallot) {
    ShowStatusDisplay('searching...', 500);
    CallAjaxHandler(local.url + '/GetPeople', {
      search: search,
      includeMatches: includeMatches,
      forBallot: forBallot
    }, onComplete, { callback: onNamesReady, search: search, usedIds: usedPersonIds }, onFail);
  };

  var onComplete = function (info, extra) {
    ResetStatusDisplay();
    if (info && info.Error) {
      ShowStatusFailed(info.Error);
      return;
    }
    local.lastInfo = $.extend(true, {}, info);
    extra.callback(markUp(info, extra.search, extra.usedIds));
  };

  var refreshListing = function (search, onNamesReady, usedPersonIds) {
    var info = $.extend(true, {}, local.lastInfo);
    onNamesReady(markUp(info, search, usedPersonIds), true);
  };

  var markUp = function (info, searchPhrases, usedIds) {
    var results = [];
    var searchParts = [];
    var parts = searchPhrases.split(' ');
    $.each(parts, function (i, part) {
      if (part) {
        try {
          searchParts.push(new RegExp(part, "ig"));
        } catch (e) {
          // typed input may include \ or other invalid characters
        }
      }
    });

    if (info && typeof info.People != 'undefined') {
      var currentType = 0;
      var highestBestMatch = 0;
      
      $.each(info.People, function (i, personInfo) {
        if (personInfo.BestMatch > highestBestMatch) {
          highestBestMatch = personInfo.BestMatch;
        }
        if (currentType == 0) currentType = personInfo.MatchType;
        var classes = [];
        classes.push('Match' + personInfo.MatchType);
        if (personInfo.MatchType != currentType) {
          currentType = personInfo.MatchType;
          classes.push('First');
        }
        if (personInfo.MatchType === 1 || personInfo.MatchType === 3) {
          $.each(searchParts, function (k, searchPart) {
            personInfo.Name = personInfo.Name.replace(searchPart, function () {
              return '$$' + arguments[0] + '%%';
            });
          });
          // if "B" is a search term, was matching in <B>
          personInfo.Name = personInfo.Name.replace(/\$\$/ig, '<b>');
          personInfo.Name = personInfo.Name.replace(/\%\%/ig, '</b>');
        }

        if (usedIds && $.inArray(personInfo.Id, usedIds) != -1) {
          classes.push('InUse');
          personInfo.InUse = true;
        }
        if (personInfo.Ineligible) {
          if (!personInfo.CanReceiveVotes) {
            classes.push('CannotReceiveVotes');
          }
          // only add if the only restriction
          if (!personInfo.CanVote) {
            classes.push('CannotVote');
          }

          personInfo.IneligibleData = ' data-ineligible="{Ineligible}" data-canVote={CanVote} data-canReceiveVotes={CanReceiveVotes}'.filledWith(personInfo);
        }
        if (classes.length != 0) {
          personInfo.Name = '<span class="{0}">{^1}</span>'.filledWith(classes.join(' '), personInfo.Name);
        }
        results.push(personInfo);
      });

      var foundBest = false;
      info.BestRowNum = 1;

      for (var matchType = 1; matchType <= 4; matchType++) {
        for (var targetMatch = highestBestMatch; !foundBest && targetMatch >= 0; targetMatch--) {
          $.each(results, function(i, item) {
            if (item.MatchType === matchType && item.BestMatch === targetMatch && !item.InUse && !item.Ineligible) {
              info.BestRowNum = i;
              foundBest = true;
              return false;
            }
          });
        }
      }
      info.People = results;
    }
    return info;
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
  var publicInterface = {
    Prepare: function () {
    },
    SearchNames: function (searchText, onNamesReady, includeMatches, usedPersonIds, forBallot) {
      startGettingPeople(searchText, onNamesReady, includeMatches, usedPersonIds, forBallot);
    },
    RefreshListing: function (searchText, onNamesReady, usedPersonIds) {
      refreshListing(searchText, onNamesReady, usedPersonIds);
    }
  };

  return publicInterface;
};

