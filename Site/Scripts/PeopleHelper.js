var PeopleHelper = function (url) {
  var loadStoredNames = function () {
    var obj = {};
    for (var key in localStorage) {
      if (key.substr(0, 11) == 'chosenName_') {
        var cn = GetFromStorage(key);
        obj[cn.Person.Id] = cn;
        local.chosenNamesCount++;
      }
    }
    return obj;
  };

  var local = {
    url: url,
    lastInfo: null,
    People: [],
    nameSplitter: /[\s-']/,
    chosenNamesCount: 0
  };
  local.chosenNames = loadStoredNames();

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
    local.People = info.People;
    extra.callback(markUp(info, extra.search, extra.usedIds));
  };

  var refreshListing = function (search, onNamesReady, usedPersonIds) {
    quickSearch(search, onNamesReady, usedPersonIds);
    //    var info = $.extend(true, {}, local.lastInfo);
    //    onNamesReady(markUp(info, search, usedPersonIds), true);
  };

  var markUp = function (info, searchPhrases, usedIds, forceMatching) {
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
          personInfo.Classes = ' class=First';
        }
        personInfo.RawName = personInfo.RawName || personInfo.Name;
        personInfo.Name = showMatchedLetters(searchParts, personInfo, forceMatching);

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
          $.each(results, function (i, item) {
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
    //    var msg = '';
    //    if (msg) {
    //      ShowStatusFailed(msg);
    //    }
  };

  var prepForSearching = function (s) {
    return s.replace(/[\(\[]/ig, '');
  };

  var addGroupToChosenNames = function (people) {
    for (var i = 0; i < people.length; i++) {
      var person = people[i];
      if (person.BestMatch) {
        // only add from group if they have a BestMatch value
        addToChosenNames(null, person);
      }
    }
  }

  var addToChosenNames = function (id, person) {
    if (person) {
      id = person.Id;
    } else {
      var personList = $.grep(local.People, function (el, i) {
        return el.Id == id;
      });
      if (personList.length) {
        person = personList[0];
      }
    }
    if (person) {
      if (local.chosenNames[id]) {
        // update our cache
        if (person.BestMatch) {
          local.chosenNames[id].Person.BestMatch = person.BestMatch;
        }
      } else {
        var $name = $(person.Name);
        var nameText = $name.text();
        person.Name = nameText;
        person.MatchType = 1;
        local.chosenNames[id] = {
          Person: person,
          nameParts: $.map($.grep(nameText.toLowerCase().split(local.nameSplitter), function (s) { return s; }), prepForSearching)
        };
        // store in large object? or one for each item?
        SetInStorage('chosenName_' + id, local.chosenNames[id]);
        local.chosenNamesCount++;
      }
    }
  };

  var showMatchedLetters = function (searchParts, personInfo, forceMatching) {
    var name = personInfo.RawName.replace(/<b>/ig, '');
    name = name.replace(/<\/b>/, '');
    if (forceMatching || personInfo.MatchType === 1 || personInfo.MatchType === 3) {
      $.each(searchParts, function (k, searchPart) {
        var searchReg;
        if (typeof searchPart == 'string') {
          if ($.trim(searchPart) == '') return;
          searchReg = new RegExp(searchPart.replace(/[^\w\s]/g, ''), 'ig');
        } else {
          //?? not sure when it becomes a RegExp!
          searchReg = searchPart;
        }
        name = name.replace(searchReg, function () {
          return '####' + arguments[0] + '@@@@';
        });
      });
      // if "B" is a search term, was matching in <B>
      name = name.replace(/####/g, '<b>');
      name = name.replace(/@@@@/g, '</b>');
    }
    return name;
  };

  var checkSearchInName = function (s, names) {
    for (var j = 0; j < names.length; j++) {
      if (s == names[j].substr(0, s.length)) {
        return true;
      }
    }
    return false;
  };

  var addMatchedNames = function (peopleList, idsFound, searchParts, person, nameParts) {
    if (searchParts.length == 1) {
      // check the single search term in all name parts
      if (checkSearchInName(searchParts[0], nameParts)) {
        peopleList.push(person);
        idsFound[person.Id] = true;
        return;
      }
    } else {
      // match each search and name part
      var matchedParts = [];
      var toMatch = searchParts.length;
      for (var i = 0; i < searchParts.length; i++) {
        var searchPart = searchParts[i];
        var matched = false;
        for (var j = 0; j < nameParts.length; j++) {
          if (searchPart == nameParts[j].substr(0, searchPart.length)) {
            if ($.inArray(j, matchedParts) == -1) {
              toMatch--;
              matched = true;
              matchedParts.push(j);
              break;
            }
          }
        }
        if (!matched) {
          // if a search term did not match, abort -- all must match
          return;
        }
        if (toMatch <= 0) {
          peopleList.push(person);
          idsFound[person.Id] = true;
          return;
        }
      }
    }
  }
  var quickSearch = function (searchText, afterQuickSearch, usedPersonIds) {
    if (!afterQuickSearch) return;

    // look through chosen names
    var info = {
      People: []
    };

    var idsFound = {};
    var searchParts = $.grep(searchText.toLowerCase().split(local.nameSplitter), function (s) { return s; });
    if (local.chosenNamesCount) {
      //names previously added to a ballot
      for (var id in local.chosenNames) {
        var cn = local.chosenNames[id];
        addMatchedNames(info.People, idsFound, searchParts, cn.Person, cn.nameParts);
      }
    }
    // names recently loaded from the server
    for (var p = 0; p < local.People.length; p++) {
      var person = local.People[p];
      if (idsFound[person.Id]) continue;
      var nameParts = $.map($.grep(person.RawName.toLowerCase().split(local.nameSplitter), function (s) { return s; }), prepForSearching);

      addMatchedNames(info.People, idsFound, searchParts, person, nameParts);

      //        if (searchParts.length == 1) {
      //          // check the single search term in all name parts
      //          if (checkSearchInName(searchParts[0], nameParts)) {
      //            //person.Name = showMatchedLetters(searchParts, person, true);
      //            info.People.push(person);
      //            continue;
      //          }
      //        } else {
      //          // match each search and name part
      //          debugger;
      //          toMatch = searchParts.length;
      //          for (i = 0; i < searchParts.length; i++) {
      //            searchPart = searchParts[i];
      //            for (j = 0; j < nameParts.length; j++) {
      //              if (searchPart == nameParts[j].substr(0, searchPart.length)) {
      //                // matched in order
      //                toMatch--;
      //                break;
      //              }
      //            }
      //            if (toMatch <= 0) {
      //              info.People.push(Person);
      //              idsFound[Person.Id] = true;
      //              continue nameLoop2;
      //            }
      //          }
    }

    info.People.sort(function (a, b) {
      return a.RawName.toLowerCase().localeCompare(b.RawName.toLowerCase());
    });
    afterQuickSearch(markUp(info, searchText, usedPersonIds), true);
  };

  //  var wrapPerson = function (flatPerson) {
  //    var person = {};
  //    $.each(local.Columns.Num, function (i, value) {
  //      person[value] = flatPerson[i];
  //    });
  //    return person;
  //  };
  var publicInterface = {
    Prepare: function () {
    },
    local: local,
    SearchNames: function (searchText, onNamesReady, includeMatches, usedPersonIds, forBallot) {
      startGettingPeople(searchText, onNamesReady, includeMatches, usedPersonIds, forBallot);
    },
    QuickSearch: function (searchText, onNamesReady, usedPersonIds) {
      quickSearch(searchText, onNamesReady, usedPersonIds);
    },
    RefreshListing: function (searchText, onNamesReady, usedPersonIds) {
      refreshListing(searchText, onNamesReady, usedPersonIds);
    },
    AddToChosenNames: addToChosenNames,
    AddGroupToChosenNames: addGroupToChosenNames
  };

  return publicInterface;
};

