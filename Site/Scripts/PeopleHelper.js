var PeopleHelper = function (url) {
  var local = {
    url: url,
    nameSplitter: /[\s-']/,
    localNames: []
  };

  var soundex = new DmSoundex();

  function prepare(cb) {
    ShowStatusDisplay('Loading names list');
    CallAjaxHandler(local.url + '/GetAll',
      {},
      function (info) {
        if (info.Error) {
          ShowStatusFailed(info.Error);
          return;
        }
        local.localNames = extendPeople(info.people);
        if (cb) {
          cb(info.lastVid);
        }
      }
    );
  }

  function extendPeople(arr) {
    arr.forEach(extendPerson);
    return arr;
  }

  function extendPerson(p) {
    // for searches, make lowercase
    p.CanReceiveVotes = !!p.V[0];
    p.CanVote = !!p.V[1];
    p.IneligibleReasonGuid = p.IRG; // backward compatible
    p.name = p.Name.toLowerCase().replace(/[\(\)\[\]]/ig, '');  // and remove brackets
    p.parts = p.name.split(local.nameSplitter);
    p.soundParts = p.parts.map(soundex.process);
    p.MatchType = 0;
    if (p.Area) {
      p.Name = p.Name + ' (' + p.Area + ')';
    }
    if (p.RowVersion > local.lastRowVersion) {
      local.lastRowVersion = p.RowVersion;
    }
  }

  function quickSearch(searchText, afterQuickSearch, usedPersonIds) {
    if (!afterQuickSearch) return;

    // look through chosen names
    var info = {
      People: []
    };
    var idsFound = {};
    var searchParts = searchText.toLowerCase().split(local.nameSplitter);

    // add soundex of each search term
    var searchSounds = [];
    var numParts = searchParts.length;
    for (var i = 0; i < numParts; i++) {
      //searchSounds.push(dropEndingZeros(soundex.process(searchParts[i])));
      searchSounds.push(soundex.process(searchParts[i]));
    }

    local.localNames.forEach(function (n) {
      addMatchedNames(info.People, idsFound, searchParts, searchSounds, n);
    });

    //// names recently loaded from the server
    //for (var p = 0; p < local.People.length; p++) {
    //  var person = local.People[p];
    //  if (idsFound[person.Id]) continue;
    //  var nameParts = $.map($.grep(person.Name.toLowerCase().split(local.nameSplitter), function (s) { return s; }), prepForSearching);

    //  addMatchedNames(info.People, idsFound, searchParts, person, nameParts);

    //  //        if (searchParts.length == 1) {
    //  //          // check the single search term in all name parts
    //  //          if (checkSearchInName(searchParts[0], nameParts)) {
    //  //            //person.Name = showMatchedLetters(searchParts, person, true);
    //  //            info.People.push(person);
    //  //            continue;
    //  //          }
    //  //        } else {
    //  //          // match each search and name part
    //  //          toMatch = searchParts.length;
    //  //          for (i = 0; i < searchParts.length; i++) {
    //  //            searchPart = searchParts[i];
    //  //            for (j = 0; j < nameParts.length; j++) {
    //  //              if (searchPart == nameParts[j].substr(0, searchPart.length)) {
    //  //                // matched in order
    //  //                toMatch--;
    //  //                break;
    //  //              }
    //  //            }
    //  //            if (toMatch <= 0) {
    //  //              info.People.push(Person);
    //  //              idsFound[Person.Id] = true;
    //  //              continue nameLoop2;
    //  //            }
    //  //          }
    //}

    info.People.sort(function (a, b) {
      if (a.MatchType < b.MatchType) return -1;
      if (a.MatchType > b.MatchType) return 1;
      return a.Name.toLowerCase().localeCompare(b.Name.toLowerCase());
    });
    afterQuickSearch(markUp(info, searchText, usedPersonIds, null, true), true);
  };

  function dropEndingZeros(s) {
    if (s === '000000') return '';
    while (s.slice(-1) === '0') {
      s = s.substring(0, s.length - 1);
    }
    return s;
  }

  //function loadStoredNames() {
  //  var obj = {};
  //  for (var key in localStorage) {
  //    if (localStorage.hasOwnProperty(key)) {
  //      if (key.substr(0, 5) === 'name_') {
  //        var cn = GetFromStorage(key);
  //        obj[cn.Person.Id] = cn;
  //      }
  //    }
  //  }
  //  return obj;
  //};

  function resetSearch() {
    //if (local.currentAjaxSearch) {
    //  local.currentAjaxSearch.abort();
    //  //console.log('aborted previous');
    //}
  };

  function startGettingPeople(search, onNamesReady, includeMatches, usedPersonIds, forBallot) {
    resetSearch();
    if (!search) {
      return;
    }

    //ShowStatusDisplay('Searching...', 500);

    //  local.currentAjaxSearch = CallAjaxHandler(local.url + '/GetPeople', {
    //    search: search,
    //    includeMatches: includeMatches,
    //    forBallot: forBallot
    //  }, onComplete, { callback: onNamesReady, search: search, usedIds: usedPersonIds }, onFail);
    //};

    //function onComplete(info, extra) {
    //  local.currentAjaxSearch = null;

    //  ResetStatusDisplay();
    //if (info && info.Error) {
    //  ShowStatusFailed(info.Error);
    //  return;
    //}
    //local.lastInfo = $.extend(true, {}, info);
    //local.People = info.People;
    //updateStoredPeople(info.People);

    var info = []; // find

    //extra.callback(
    markUp(info, extra.search, extra.usedIds)
    //);
  };

  function updateVoteCounts(info) {
    var updates = info.VoteUpdates;
    if (!updates || !updates.length) {
      return;
    }

    var toFind = updates.map(function (update) { return update.PersonGuid }).join(',');
    var numToFind = updates.length;

    local.localNames.forEach(function (person) {
      var guid = person.PersonGuid;
      if (toFind.indexOf(guid) !== -1) {
        // this person was updated
        if (numToFind === 1) {
          person.NumVotes = updates[0].Count;
        } else {
          var update = updates.find(function (update) {
            return update.PersonGuid = guid;
          });
          if (update) {
            person.NumVotes = update.Count;
          }
        }
      }
    });
  }

  function refreshListing(search, onNamesReady, usedPersonIds, info) {
    updateVoteCounts(info);
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
        //if (personInfo.MatchType !== currentType) { // && !inQuickSearch) {
        //  currentType = personInfo.MatchType;
        //  personInfo.Classes = ' class=First';
        //}
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

      //if (currentFocusId) {
      //  $.each(results, function (i, item) {
      //    if (item.Id == currentFocusId && item.MatchType === 1) {
      //      info.BestRowNum = i;
      //      foundBest = true;
      //      return false;
      //    }
      //  });
      //}
      //if (!foundBest) {
      //2018-Feb only look in type 1; only in 2 if none were in 1

      for (var matchType = 1; matchType <= 2; matchType++) {
        var foundInType = false;
        for (var targetMatch = highestNumVotes; !foundBest && targetMatch >= 0; targetMatch--) {
          $.each(results, function (i, item) {
            if (item.MatchType === matchType) {
              found = true;
            }
            if (item.MatchType === matchType && item.NumVotes === targetMatch && !item.InUse && !item.Ineligible) {
              info.BestRowNum = i;
              foundBest = true;
              return false;
            }
          });
        }
        if (matchType === 1 && foundInType) {
          break;
        }
      }
      //}
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

  //function updateStoredPeople(people) {
  //  // update our local copy if the NumVotes is different
  //  $.each(people, function (i, person) {
  //    var stored = local.localNames[person.Id];
  //    var save = false;
  //    if (!stored) {
  //      stored = {
  //        Person: person,
  //        nameParts: $.map($.grep(person.Name.toLowerCase().split(local.nameSplitter), function (s) { return s; }), prepForSearching)
  //      };
  //      save = true;
  //    } else if (stored.Person.RowVersion !== person.RowVersion) {
  //      //console.log('updated {Id} - {Name}'.filledWith(person));
  //      stored = {
  //        Person: person,
  //        nameParts: $.map($.grep(person.Name.toLowerCase().split(local.nameSplitter), function (s) { return s; }), prepForSearching)
  //      };
  //      save = true;
  //    } else if (stored.Person.NumVotes !== person.NumVotes) {
  //      //console.log('vote change {Id} - {Name}'.filledWith(person));
  //      stored.Person.NumVotes = person.NumVotes;
  //      save = true;
  //    }
  //    if (save) {
  //      // save in memory and echo into localStorage
  //      stored.MatchType = 1;
  //      //local.localNames[person.Id] = stored;
  //      //SetInStorage('name_' + person.Id, stored);
  //    }
  //  });
  //}

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
      if (names[j].startsWith(s)) {
        return true;
      }
    }
    return false;
  };

  function addMatchedNames(peopleList, idsFound, searchParts, searchSounds, person) {
    var nameParts = person.parts;

    // match each search and name part
    var matchedParts = [];
    var toMatch = searchParts.length;
    for (var i = 0; i < searchParts.length; i++) {
      var searchPart = searchParts[i];
      var matched = false;
      for (var j = 0; j < nameParts.length; j++) {
        if (nameParts[j].startsWith(searchPart)) {
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
        addMatchedSounds(peopleList, idsFound, searchSounds, person);
      }
      if (toMatch <= 0) {
        person.MatchType = 1;
        peopleList.push(person);
        idsFound[person.Id] = true;
        return;
      }
    }

  }

  function addMatchedSounds(peopleList, idsFound, searchSounds, person) {
    // match each search and name part
    var toMatch = searchSounds.length;
    if (toMatch === 1 && !searchSounds[0]) {
      return;
    }

    var matchedParts = [];
    var nameParts = person.soundParts;

    for (var i = 0; i < searchSounds.length; i++) {
      var searchPart = searchSounds[i];
      var matched = false;
      for (var j = 0; j < nameParts.length; j++) {
        if (nameParts[j].startsWith(searchPart)) {
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
        person.MatchType = 2;
        peopleList.push(person);
        idsFound[person.Id] = true;
        return;
      }
    }
  }


  var publicInterface = {
    Prepare: prepare,
    local: local,
    ResetSearch: resetSearch,
    SearchNames: function (searchText, onNamesReady, includeMatches, usedPersonIds, forBallot) {
      //startGettingPeople(searchText, onNamesReady, includeMatches, usedPersonIds, forBallot);
    },
    QuickSearch: function (searchText, onNamesReady, usedPersonIds) {
      quickSearch(searchText, onNamesReady, usedPersonIds);
    },
    RefreshListing: function (searchText, onNamesReady, usedPersonIds, info) {
      refreshListing(searchText, onNamesReady, usedPersonIds, info);
    }
    //AddToLocalNames: addToLocalNames,
    //AddGroupToLocalNames: addGroupToLocalNames
  };

  return publicInterface;
};


var DmSoundex = function () {
  // adapted from https://github.com/NaturalNode/natural/blob/master/lib/natural/phonetics/dm_soundex.js
  var codes = {
    a: {
      0: [0, -1, -1],
      i: [[0, 1, -1]],
      j: [[0, 1, -1]],
      y: [[0, 1, -1]],
      u: [[0, 7, -1]]
    },
    b: [[7, 7, 7]],
    c: {
      0: [5, 5, 5],
      z: { 0: [4, 4, 4], s: [[4, 4, 4]] },
      s: { 0: [4, 4, 4], z: [[4, 4, 4]] },
      k: [[5, 5, 5], [45, 45, 45]],
      h: { 0: [5, 5, 5], s: [[5, 54, 54]] }
    },
    d: {
      0: [3, 3, 3],
      t: [[3, 3, 3]],
      z: { 0: [4, 4, 4], h: [[4, 4, 4]], s: [[4, 4, 4]] },
      s: { 0: [4, 4, 4], h: [[4, 4, 4]], z: [[4, 4, 4]] },
      r: { s: [[4, 4, 4]], z: [[4, 4, 4]] }
    },
    e: {
      0: [0, -1, -1],
      i: [[0, 1, -1]],
      j: [[0, 1, -1]],
      y: [[0, 1, -1]],
      u: [[1, 1, -1]],
      w: [[1, 1, -1]]
    },
    f: {
      0: [7, 7, 7],
      b: [[7, 7, 7]]
    },
    g: [[5, 5, 5]],
    h: [[5, 5, -1]],
    i: {
      0: [0, -1, -1],
      a: [[1, -1, -1]],
      e: [[1, -1, -1]],
      o: [[1, -1, -1]],
      u: [[1, -1, -1]]
    },
    j: [[4, 4, 4]],
    k: {
      0: [5, 5, 5],
      h: [[5, 5, 5]],
      s: [[5, 54, 54]]
    },
    l: [[8, 8, 8]],
    m: {
      0: [6, 6, 6],
      n: [[66, 66, 66]]
    },
    n: {
      0: [6, 6, 6],
      m: [[66, 66, 66]]
    },
    o: {
      0: [0, -1, -1],
      i: [[0, 1, -1]],
      j: [[0, 1, -1]],
      y: [[0, 1, -1]]
    },
    p: {
      0: [7, 7, 7],
      f: [[7, 7, 7]],
      h: [[7, 7, 7]]
    },
    q: [[5, 5, 5]],
    r: {
      0: [9, 9, 9],
      z: [[94, 94, 94], [94, 94, 94]],
      s: [[94, 94, 94], [94, 94, 94]]
    },
    s: {
      0: [4, 4, 4],
      z: { 0: [4, 4, 4], t: [[2, 43, 43]], c: { z: [[2, 4, 4]], s: [[2, 4, 4]] }, d: [[2, 43, 43]] },
      d: [[2, 43, 43]],
      t: { 0: [2, 43, 43], r: { z: [[2, 4, 4]], s: [[2, 4, 4]] }, c: { h: [[2, 4, 4]] }, s: { h: [[2, 4, 4]], c: { h: [[2, 4, 4]] } } },
      c: { 0: [2, 4, 4], h: { 0: [4, 4, 4], t: { 0: [2, 43, 43], s: { c: { h: [[2, 4, 4]] }, h: [[2, 4, 4]] }, c: { h: [[2, 4, 4]] } }, d: [[2, 43, 43]] } },
      h: { 0: [4, 4, 4], t: { 0: [2, 43, 43], c: { h: [[2, 4, 4]] }, s: { h: [[2, 4, 4]] } }, c: { h: [[2, 4, 4]] }, d: [[2, 43, 43]] }
    },
    t: {
      0: [3, 3, 3],
      c: { 0: [4, 4, 4], h: [[4, 4, 4]] },
      z: { 0: [4, 4, 4], s: [[4, 4, 4]] },
      s: { 0: [4, 4, 4], z: [[4, 4, 4]], h: [[4, 4, 4]], c: { h: [[4, 4, 4]] } },
      t: { s: { 0: [4, 4, 4], z: [[4, 4, 4]], c: { h: [[4, 4, 4]] } }, c: { h: [[4, 4, 4]] }, z: [[4, 4, 4]] },
      h: [[3, 3, 3]],
      r: { z: [[4, 4, 4]], s: [[4, 4, 4]] }
    },
    u: {
      0: [0, -1, -1],
      e: [[0, -1, -1]],
      i: [[0, 1, -1]],
      j: [[0, 1, -1]],
      y: [[0, 1, -1]]
    },
    v: [[7, 7, 7]],
    w: [[7, 7, 7]],
    x: [[5, 54, 54]],
    y: [[1, -1, -1]],
    z: {
      0: [4, 4, 4],
      d: { 0: [2, 43, 43], z: { 0: [2, 4, 4], h: [[2, 4, 4]] } },
      h: { 0: [4, 4, 4], d: { 0: [2, 43, 43], z: { h: [[2, 4, 4]] } } },
      s: { 0: [4, 4, 4], h: [[4, 4, 4]], c: { h: [[4, 4, 4]] } }
    }
  };


  function process(word) {
    var codeLength = 6;
    var output = '';

    var pos = 0, lastCode = -1;
    while (pos < word.length) {
      var substr = word.slice(pos);
      var rules = findRules(substr);

      var code;
      if (pos == 0) {
        // at the beginning of the word
        code = rules.mapping[0];
      } else if (substr[rules.length] && findRules(substr[rules.length]).mapping[0] == 0) {
        // before a vowel
        code = rules.mapping[1];
      } else {
        // any other situation
        code = rules.mapping[2];
      }

      if ((code != -1) && (code != lastCode)) output += code;
      lastCode = code;
      pos += rules.length;
    }

    return normalizeLength(output, codeLength);
  }


  function findRules(str) {
    var state = codes[str[0]];
    var legalState = state || [[-1, -1, -1]],
      charsInvolved = 1;

    for (var offs = 1; offs < str.length; offs++) {
      if (!state || !state[str[offs]]) break;

      state = state[str[offs]];
      if (state[0]) {
        legalState = state;
        charsInvolved = offs + 1;
      }
    }

    return {
      length: charsInvolved,
      mapping: legalState[0]
    };
  }


  /**
   * Pad right with zeroes or cut excess symbols to fit length
   */
  function normalizeLength(token, length) {
    length = length || 6;
    if (token.length < length) {
      token += (new Array(length - token.length + 1)).join('0');
    }
    return token.slice(0, length);
  }

  return {
    process: process
  }
}
