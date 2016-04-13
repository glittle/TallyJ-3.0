var PeopleHelper = function (url) {
  var local = {
    url: url,
    lastInfo: null,
    People: [],
    nameSplitter: /[\s-']/,
    currentAjaxSearch: null,
    localNames: loadStoredNames()
  };

  function loadStoredNames() {
    var obj = {};
    for (var key in localStorage) {
      if (localStorage.hasOwnProperty(key)) {
        if (key.substr(0, 5) === 'name_') {
          var cn = GetFromStorage(key);
          obj[cn.Person.Id] = cn;
        }
      }
    }
    return obj;
  };

  function resetSearch  () {
    if (local.currentAjaxSearch) {
      local.currentAjaxSearch.abort();
      //LogMessage('aborted previous');
    }
  };
  function startGettingPeople(search, onNamesReady, includeMatches, usedPersonIds, forBallot) {
    resetSearch();
    if (!search) {
      return;
    }

    ShowStatusDisplay('Searching...', 500);

    local.currentAjaxSearch = CallAjaxHandler(local.url + '/GetPeople', {
      search: search,
      includeMatches: includeMatches,
      forBallot: forBallot
    }, onComplete, { callback: onNamesReady, search: search, usedIds: usedPersonIds }, onFail);
  };

  function onComplete(info, extra) {
    local.currentAjaxSearch = null;

    ResetStatusDisplay();
    if (info && info.Error) {
      ShowStatusFailed(info.Error);
      return;
    }
    local.lastInfo = $.extend(true, {}, info);
    local.People = info.People;
    updateStoredPeople(info.People);
    extra.callback(markUp(info, extra.search, extra.usedIds));
  };

  function refreshListing(search, onNamesReady, usedPersonIds) {
    quickSearch(search, onNamesReady, usedPersonIds);
    //    var info = $.extend(true, {}, local.lastInfo);
    //    onNamesReady(markUp(info, search, usedPersonIds), true);
  };

  function markUp(info, searchPhrases, usedIds, forceMatching, inQuickSearch) {
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

    var currentFocus = $('#nameList > li.selected');
    var rawId = currentFocus.attr('id');
    var currentFocusId = rawId ? +rawId.substr(1) : 0;

    if (info && typeof info.People != 'undefined') {
      var currentType = 0;
      var highestNumVotes = 0;

      $.each(info.People, function (i, personInfo) {
        if (personInfo.NumVotes > highestNumVotes) {
          highestNumVotes = personInfo.NumVotes;
        }
        if (currentType == 0) currentType = personInfo.MatchType;
        var classes = [];
        classes.push('Match' + personInfo.MatchType);
        if (personInfo.MatchType !== currentType && !inQuickSearch) {
          currentType = personInfo.MatchType;
          personInfo.Classes = ' class=First';
        }
        //personInfo.RawName = personInfo.RawName || personInfo.Name;
        personInfo.DisplayName = showMatchedLetters(searchParts, personInfo, forceMatching);

        if (usedIds && $.inArray(personInfo.Id, usedIds) !== -1) {
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
        if (classes.length !== 0) {
          personInfo.HtmlName = '<span class="{0}">{^1}</span>'.filledWith(classes.join(' '), personInfo.DisplayName);
        }
        results.push(personInfo);
      });

      var foundBest = false;
      info.BestRowNum = 1;

      if (currentFocusId) {
        $.each(results, function (i, item) {
          if (item.Id == currentFocusId) {
            info.BestRowNum = i;
            foundBest = true;
            return false;
          }
        });
      }
      if (!foundBest) {
        for (var matchType = 1; matchType <= 4; matchType++) {
          for (var targetMatch = highestNumVotes; !foundBest && targetMatch >= 0; targetMatch--) {
            $.each(results, function (i, item) {
              if (item.MatchType === matchType && item.NumVotes === targetMatch && !item.InUse && !item.Ineligible) {
                info.BestRowNum = i;
                foundBest = true;
                return false;
              }
            });
          }
        }
      }
      info.People = results;
    }
    return info;
  };

  function onFail(xmlHttpRequest) {
    //    var msg = '';
    //    if (msg) {
    //      ShowStatusFailed(msg);
    //    }
  };

  function prepForSearching(s) {
    return s.replace(/[\(\[]/ig, '');
  };

  //var addGroupToLocalNames = function (people) {
  //  for (var i = 0; i < people.length; i++) {
  //    var person = people[i];
  //    if (person.NumVotes) {
  //      // only add from group if they have a NumVotes value
  //      addToLocalNames(null, person);
  //    }
  //  }
  //}

  //var addToLocalNames = function (id, person) {
  //  if (person) {
  //    id = person.Id;
  //    person.NumVotes = +person.NumVotes + 1;
  //  } else {
  //    var personList = $.grep(local.People, function (el, i) {
  //      return el.Id == id;
  //    });
  //    if (personList.length) {
  //      person = personList[0];
  //    }
  //  }
  //  if (person) {
  //    if (local.localNames[id]) {
  //      // update our cache
  //      if (person.NumVotes) {
  //        local.localNames[id].Person.NumVotes = person.NumVotes;
  //      }
  //    } else {
  //      var $name = $(person.Name);
  //      var nameText = $name.text();
  //      person.Name = nameText;
  //      person.MatchType = 1;
  //      local.localNames[id] = {
  //        Person: person,
  //        nameParts: $.map($.grep(nameText.toLowerCase().split(local.nameSplitter), function (s) { return s; }), prepForSearching)
  //      };
  //      /////////
  //      SetInStorage('name_' + id, local.localNames[id]);
  //    }
  //  }
  //};

  function updateStoredPeople(people) {
    // update our local copy if the NumVotes is different
    $.each(people, function (i, person) {
      var stored = local.localNames[person.Id];
      var save = false;
      if (!stored) {
        stored = {
          Person: person,
          nameParts: $.map($.grep(person.Name.toLowerCase().split(local.nameSplitter), function (s) { return s; }), prepForSearching)
        };
        save = true;
      } else if (stored.Person.RowVersion !== person.RowVersion) {
        //LogMessage('updated {Id} - {Name}'.filledWith(person));
        stored = {
          Person: person,
          nameParts: $.map($.grep(person.Name.toLowerCase().split(local.nameSplitter), function (s) { return s; }), prepForSearching)
        };
        save = true;
      } else if (stored.Person.NumVotes !== person.NumVotes) {
        //LogMessage('vote change {Id} - {Name}'.filledWith(person));
        stored.Person.NumVotes = person.NumVotes;
        save = true;
      }
      if (save) {
        // save in memory and echo into localStorage
        stored.MatchType = 1;
        local.localNames[person.Id] = stored;
        SetInStorage('name_' + person.Id, stored);
      }
    });
  }

  function showMatchedLetters(searchParts, personInfo, forceMatching) {
    var name = personInfo.Name;//.replace(/<b>/ig, '');
    //name = name.replace(/<\/b>/, '');
    if (forceMatching || personInfo.MatchType === 1 || personInfo.MatchType === 3) {
      $.each(searchParts, function (k, searchPart) {
        var searchReg;
        if (typeof searchPart == 'string') {
          if ($.trim(searchPart) === '') return;
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

  function checkSearchInName(s, names) {
    for (var j = 0; j < names.length; j++) {
      if (s === names[j].substr(0, s.length)) {
        return true;
      }
    }
    return false;
  };

  function  addMatchedNames  (peopleList, idsFound, searchParts, person, nameParts) {
    if (searchParts.length === 1) {
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
          if (searchPart === nameParts[j].substr(0, searchPart.length)) {
            if ($.inArray(j, matchedParts) === -1) {
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
  function quickSearch(searchText, afterQuickSearch, usedPersonIds) {
    if (!afterQuickSearch) return;


    // look through chosen names
    var info = {
      People: []
    };
    var idsFound = {};
    var searchParts = $.grep(searchText.toLowerCase().split(local.nameSplitter), function (s) { return s; });
    //names previously seen
    for (var id in local.localNames) {
      if (local.localNames.hasOwnProperty(id)) {
        var cn = local.localNames[id];
        if (!cn.nameParts) {
          cn.nameParts = $.map($.grep(cn.Person.Name.toLowerCase().split(local.nameSplitter), function(s) { return s; }), prepForSearching);
        }
        addMatchedNames(info.People, idsFound, searchParts, cn.Person, cn.nameParts);
      }
    }
    // names recently loaded from the server
    for (var p = 0; p < local.People.length; p++) {
      var person = local.People[p];
      if (idsFound[person.Id]) continue;
      var nameParts = $.map($.grep(person.Name.toLowerCase().split(local.nameSplitter), function (s) { return s; }), prepForSearching);

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
      return a.Name.toLowerCase().localeCompare(b.Name.toLowerCase());
    });
    afterQuickSearch(markUp(info, searchText, usedPersonIds, null, true), true);
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
    ResetSearch: resetSearch,
    SearchNames: function (searchText, onNamesReady, includeMatches, usedPersonIds, forBallot) {
      startGettingPeople(searchText, onNamesReady, includeMatches, usedPersonIds, forBallot);
    },
    QuickSearch: function (searchText, onNamesReady, usedPersonIds) {
      quickSearch(searchText, onNamesReady, usedPersonIds);
    },
    RefreshListing: function (searchText, onNamesReady, usedPersonIds) {
      refreshListing(searchText, onNamesReady, usedPersonIds);
    }
    //AddToLocalNames: addToLocalNames,
    //AddGroupToLocalNames: addGroupToLocalNames
  };

  return publicInterface;
};

