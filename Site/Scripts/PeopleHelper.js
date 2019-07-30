var PeopleHelper = function (url, forBallotEntry, forVoter, customExtendPerson) {
  var debugSearch = false;
  var local = {
    url: url,
    nameSplitter: /[\s\-']/,
    localNames: [],
    maxVotes: 0
  };

  var maxToShow = 1000;
  var soundex = new Metaphone();
  var removeAccents = new RemoveAccents();

  function prepare(cb) {
    startLoadingAllNames(cb);
    site.onbroadcast(site.broadcastCode.personSaved, personSaved);
  }

  function startLoadingAllNames(cb) {
    ShowStatusDisplay('Loading names list');
    CallAjaxHandler(local.url + '/' + (forVoter ? 'GetForVoter' : 'GetAll'),
      {},
      function (info) {
        if (info.Error) {
          ShowStatusFailed(info.Error);
          return;
        }
        ShowStatusDisplay('Preparing names');

        local.localNames = extendPeople(info.people);
        if (cb) {
          cb(info.lastVid);
        }

        ResetStatusDisplay();
      }
    );
  }

  function personSaved(ev, info) {
    var editedPerson = info.Person;

    // find this person
    var i = local.localNames.findIndex(function (person) {
      return person.Id === editedPerson.C_RowId;
    });

    if (i === -1) {
      // new person, adjust to fit
      editedPerson.Name = editedPerson.C_FullName;
      editedPerson.Id = editedPerson.C_RowId;
      editedPerson.NumVotes = 0;

      extendPersonCore(editedPerson);

      local.localNames.push(editedPerson);
    } else {
      var old = local.localNames[i];

      old.Name = editedPerson.C_FullName;
      old.CanVote = editedPerson.CanVote;
      old.CanReceiveVotes = editedPerson.CanReceiveVotes;
      old.Ineligible = editedPerson.IneligibleReasonGuid;

      extendPersonCore(old);
    }
  }

  function updatePeople(info) {
    // from changes when a person's info changes elsewhere
    info.PersonLines.forEach(function (editedPerson) {

      // find this person
      var i = local.localNames.findIndex(function (person) {
        return person.Id === editedPerson.PersonId;
      });

      if (i === -1) {
        // new person, adjust to fit
        editedPerson.Name = editedPerson.FullName;
        editedPerson.Id = editedPerson.PersonId;
        editedPerson.NumVotes = editedPerson.NumVotes || 0;
        editedPerson.Ineligible = editedPerson.IneligibleReasonGuid;

        extendPersonCore(editedPerson);

        local.localNames.push(editedPerson);
      } else {
        var old = local.localNames[i];

        old.Name = editedPerson.FullName;
        old.CanVote = editedPerson.CanVote;
        old.CanReceiveVotes = editedPerson.CanReceiveVotes;
        old.Ineligible = editedPerson.IneligibleReasonGuid;

        extendPersonCore(old);
      }
    });
  }

  function extendPeople(arr) {
    local.maxVotes = 0;
    arr.forEach(extendPerson);
    return arr;
  }

  function extendPerson(p) {
    // decode compressed info from the server
    //if (p.Id === 25068) debugger;

    p.Ineligible = p.IRG;
    p.classesList = [];

    if (forVoter) {
      p.CanReceiveVotes = !p.Ineligible;
    } else {
      p.CanReceiveVotes = p.V[0] === '1';
      p.CanVote = p.V[1] === '1';
    }

    if (p.NumVotes > local.maxVotes) local.maxVotes = p.NumVotes;

    extendPersonCore(p);
  }

  function extendPersonCore(p) {
    p.NameArea = p.Name + (p.Area ? ' (' + p.Area + ')' : '');
    // for searches, make lowercase
    p.name = removeAccents.process(p.NameArea.toLowerCase());
    p.namePlain = p.name.replace(/[\(\)\[\]]/ig, ''); // and remove brackets 
    p.parts = p.namePlain.split(local.nameSplitter);

    p.soundParts = p.parts.map(soundex.process);

    if (customExtendPerson) {
      customExtendPerson(p);
    }
  }

  function special(code, cbAfter) {
    switch (code) {
      case 'All':
        return '';
    }
  }

  function search(searchText, cbAfterSearch, usedPersonIds) {
    if (!cbAfterSearch) return;
    var trimmed = searchText.trim();
    if (!trimmed) {
      return;
    }

    // look through chosen names
    var result = {
      People: []
    };

    // get search terms and soundex of each search term
    var searchParts = trimmed.toLowerCase().split(local.nameSplitter);
    var searchSounds = [];
    for (var i = 0; i < searchParts.length; i++) {
      searchSounds.push(soundex.process(searchParts[i]));
    }

    if (debugSearch) console.log('sound:', searchSounds);

    local.localNames.forEach(function (n) {
      if (result.People.length < maxToShow) {
        addMatchedNames(n, result.People, searchParts, searchSounds);
      }
    });

    sortResults(result);

    var info = markUp(result, searchParts, usedPersonIds);

    cbAfterSearch(info, true, true);
  }

  function addMatchedNames(person, matchedPeople, searchParts, searchSounds) {
    var nameParts = person.parts;
    var nameSounds = person.soundParts; // same length as nameParts

    // match each search and name part
    person.matchedParts = nameParts.map(function () { return 0; }); // fill array of correct length with 0
    var toMatch = searchParts.length;
    var found = 0;

    for (var i = 0; i < searchParts.length; i++) {

      var searchPart = searchParts[i];
      var searchSound = searchSounds[i];
      var partFound = false;

      for (var j = 0; j < nameParts.length; j++) {

        if (nameParts[j].startsWith(searchPart)) { // } && !person.matchedParts[j]) {
          person.matchedParts[j] = 5; // high level match
          if (!partFound) found++;
          partFound = true;
        } else if (nameSounds[j] === searchSound) { // && !person.matchedParts[j]) {
          person.matchedParts[j] = 4; // low level
          if (!partFound) found++;
          partFound = true;
        } else if (nameParts[j].indexOf(searchPart) !== -1) { // && !person.matchedParts[j]) {
          person.matchedParts[j] = 3; // lowest level
          if (!partFound) found++;
          partFound = true;
        }
      }
    }

    // 5 = highest - exact match of start of a word
    // 4 = sound
    // 3 = text in word

    // get max match type
    //        person.MatchType = toMatch !== found ? 0 : person.matchedParts.reduce(function (acc, p) { return p > acc ? p : acc; }, 0);
    person.MatchType =
      toMatch > found ? 0 : person.matchedParts.reduce(function (acc, p) { return p > acc ? p : acc; }, 0);

    if (person.MatchType) {
      // something matched

      person.Parts5 = adjustSearch(person.matchedParts.findIndex(function (p) { return p === 5; }));
      person.Parts4 = adjustSearch(person.matchedParts.findIndex(function (p) { return p === 4; }));
      person.Parts3 = adjustSearch(person.matchedParts.findIndex(function (p) { return p === 3; }));

      person.Sort1 = person.Parts5 * 10000 +
        (person.Parts5 === 0 ? person.Parts4 * 100 : 0) +
        (person.Parts5 === 0 && person.Parts4 === 0 ? person.Parts3 : 0);

      //      person.Parts3 = person.matchedParts.reduce(function (acc, p) { return p === 3 ? 1 + acc : acc; }, 0);
      //      person.PartFL = person.

      matchedPeople.push(person);
    }
  }

  function adjustSearch(index) {
    if (index === -1) return 0;
    return 20 - index;
  }

  function sortResults(result) {
    if (forBallotEntry) {
      result.People.sort(function (a, b) {
        if (a.Sort1 < b.Sort1) return 1;
        if (a.Sort1 > b.Sort1) return -1;

        //        if (a.Parts5 < b.Parts5) return 1;
        //        if (a.Parts5 > b.Parts5) return -1;
        //
        //        if (a.Parts5 === 0) {
        //          if (a.Parts4 < b.Parts4) return 1;
        //          if (a.Parts4 > b.Parts4) return -1;
        //
        //          if (a.Parts4 === 0) {
        //            if (a.Parts3 < b.Parts3) return 1;
        //            if (a.Parts3 > b.Parts3) return -1;
        //          }
        //        }

        if (a.NumVotes < b.NumVotes) return 1;
        if (a.NumVotes > b.NumVotes) return -1;

        return a.name.localeCompare(b.name);
      });
    } else {
      result.People.sort(function (a, b) {
        //        console.log(a.Sort1, b.Sort1);
        if (a.Sort1 < b.Sort1) return 1;
        if (a.Sort1 > b.Sort1) return -1;
        //
        //        if (a.Parts5 < b.Parts5) return 1;
        //        if (a.Parts5 > b.Parts5) return -1;
        //
        //        if (a.Parts4 < b.Parts4) return 1;
        //        if (a.Parts4 > b.Parts4) return -1;
        //
        //        if (a.Parts3 < b.Parts3) return 1;
        //        if (a.Parts3 > b.Parts3) return -1;

        return a.name.localeCompare(b.name);
      });
    }
  }

  function markUp(info, searchParts, usedIds) {
    var results = [];

    if (info && typeof info.People !== 'undefined') {
      var currentFocus = $('#nameList > li.selected');
      var currentFocusId;
      if (currentFocus.length) {
        var rawId = currentFocus.attr('id');
        currentFocusId = rawId ? +rawId.substr(1) : 0;
      } else {
        currentFocusId = info.People.length ? info.People[0].Id : 0;
      }
      info.currentFocusRow = info.People.findIndex(function (p) { return p.Id === currentFocusId; });

      var highestNumVotes = 0;

      $.each(info.People,
        function (i, personInfo) {
          if (debugSearch) console.log('Matched', i, personInfo.DisplayName)

          if (personInfo.NumVotes > highestNumVotes) {
            highestNumVotes = personInfo.NumVotes;
          }

          // determine % with max at 80%
          personInfo.VoteSize = Math.round(personInfo.NumVotes / (0.01 + local.maxVotes) * 80);

          var liClasses = [];
          var spanClasses = [];

          if (forBallotEntry) {
            liClasses.push(personInfo.NumVotes ? 'HasVotes' : 'NoVotes');
          }

          if ((personInfo.Parts4 || personInfo.Parts3) && !personInfo.Parts5) liClasses.push('Match4');
          if (personInfo.Parts5 && !personInfo.Parts4 && !personInfo.Parts3) liClasses.push('Match5Only');

          spanClasses.push('Match' + personInfo.MatchType);

          showMatchedLetters(searchParts, personInfo);

          //        if (personInfo.Area) {
          //          personInfo.DisplayName = personInfo.DisplayName + ' (' + personInfo.Area + ')';
          //        }

          if (usedIds && $.inArray(personInfo.Id, usedIds) !== -1) {
            spanClasses.push('InUse');
            personInfo.InUse = true;
          } else {
            personInfo.InUse = false; // clear from last usage
          }
          if (personInfo.Ineligible) {
            if (!personInfo.CanReceiveVotes) {
              spanClasses.push('CannotReceiveVotes');
            }
            // only add if the only restriction
            if (!personInfo.CanVote) {
              spanClasses.push('CannotVote');
            }

            personInfo.IneligibleData =
              ' data-ineligible="{Ineligible}" data-canVote={CanVote} data-canReceiveVotes={CanReceiveVotes}'
                .filledWith(personInfo);
          }
          if (spanClasses.length) {
            personInfo.HtmlName =
              '<span class="{0}">{^1}</span>'.filledWith(spanClasses.join(' '), personInfo.DisplayName);
          }
          if (liClasses.length) {
            personInfo.classesList = liClasses;
            personInfo.Classes = ' class="{0}"'.filledWith(liClasses.join(' '));
          }
          results.push(personInfo);
        });

      var foundBest = false;
      info.BestRowNum = 0;

      //2018-Feb look in type ; only in 4 if none were in 5

      // rseults are sorted, so 0 is best... if it is not already in use

      for (var matchType = 5; matchType >= 3; matchType--) {
        var foundInType = false;
        for (var targetMatch = highestNumVotes; !foundBest && targetMatch >= 0; targetMatch--) {
          $.each(results,
            function (i, item) {
              if (item.MatchType === matchType && item.NumVotes === targetMatch && !item.InUse && !item.Ineligible) {
                info.BestRowNum = i;
                foundBest = true;
                return false;
              }
            });
        }
      }
      info.People = results;
      info.maxVotes = local.maxVotes;
    }
    return info;
  }

  function updateVoteCounts(info) {
    var updates = info.VoteUpdates;
    if (!updates || !updates.length) {
      return;
    }

    updates.forEach(function (update) {
      var person = local.localNames.find(function (p) {
        return p.Id === update.Id;
      });
      if (person) {
        person.NumVotes = update.Count;
        if (update.Count > local.maxVotes) local.maxVotes = update.Count;
      }
    })
  }

  function refreshListing(searchTerm, onNamesReady, usedPersonIds, info) {
    updateVoteCounts(info);
    search(searchTerm, onNamesReady, usedPersonIds);
  }

  function showMatchedLetters(searchParts, personInfo) {
    var name = personInfo.NameArea;
    //        console.log(personInfo)
    searchParts.forEach(function (searchPart) {
      if ($.trim(searchPart) === '') return;
      //            var searchReg = new RegExp('[\\s\\-\\\'\\[\\(]({0})|(^{0})'.filledWith(searchPart), 'ig');
      var searchReg = new RegExp('({0})|(^{0})'.filledWith(searchPart), 'ig');
      name = name.replace(searchReg,
        function (a, b, c) {
          return '##1' + arguments[0] + '##2';
        });
    });

    if (personInfo.Parts4) {
      // find the soundex match

      var nameSplit = name.match(/(.*?)([\s\-\']|$)/g);

      personInfo.matchedParts.forEach(function (p, i) {
        if (!p) return;

        // there is a sound match in this position
        // this maybe 90% likely to mark the correct text...
        // need to better distinguish spaces and other splitters
        var part = nameSplit[i];
        var space1 = part[0] === ' ' ? ' ' : '';
        var space2 = part.slice(-1) === ' ' ? ' ' : '';

        nameSplit[i] = space1 + '##3' + part.trim() + '##4' + space2;
      });

      name = nameSplit.join('');
    }

    if (debugSearch) console.log(personInfo);

    personInfo.DisplayName = name
      .replace(/##1/g, '<b>')
      .replace(/##2/g, '</b>')
      .replace(/##3/g, '<i>')
      .replace(/##4/g, '</i>');

    if (debugSearch) {
      personInfo.DisplayName += `<u>${personInfo.soundParts.join('-')},${personInfo.MatchType},${
        personInfo.matchedParts.join('')},${personInfo.classesList.join('/')}</u>`;
    }

  }

  var publicInterface = {
    Prepare: prepare,
    local: local,
    Search: function (searchText, onNamesReady, usedPersonIds) {
      search(searchText, onNamesReady, usedPersonIds);
    },
    UpdatePeople: function (info) {
      updatePeople(info);
    },
    Special: function (searchText, onNamesReady) {
      special(searchText, onNamesReady);
    },
    RefreshListing: function (searchText, onNamesReady, usedPersonIds, info) {
      refreshListing(searchText, onNamesReady, usedPersonIds, info);
    }
    //AddToLocalNames: addToLocalNames,
    //AddGroupToLocalNames: addGroupToLocalNames
  };

  return publicInterface;
};


var Metaphone = function () {


  function dedup(token) {
    return token.replace(/([^c])\1/g, '$1');
  }

  function dropInitialLetters(token) {
    if (token.match(/^(kn|gn|pn|ae|wr)/))
      return token.substr(1, token.length - 1);

    return token;
  }

  function dropBafterMAtEnd(token) {
    return token.replace(/mb$/, 'm');
  }

  function cTransform(token) {


    token = token.replace(/([^s]|^)(c)(h)/g, '$1x$3').trim();


    token = token.replace(/cia/g, 'xia');
    token = token.replace(/c(i|e|y)/g, 's$1');
    token = token.replace(/c/g, 'k');

    return token;
  }

  function dTransform(token) {
    token = token.replace(/d(ge|gy|gi)/g, 'j$1');
    token = token.replace(/d(z|v)/g, 's'); //glen
    token = token.replace(/d/g, 't');

    return token;
  }

  function dropG(token) {
    token = token.replace(/gh(^$|[^aeiou])/g, 'h$1');
    token = token.replace(/g(n|ned)$/g, '$1');

    return token;
  }

  function transformG(token) {
    token = token.replace(/gh/g, 'f');
    token = token.replace(/([^g]|^)(g)(i|e|y)/g, '$1j$3');
    token = token.replace(/gg/g, 'g');
    token = token.replace(/g/g, 'k');

    return token;
  }

  function dropH(token) {
    return token.replace(/([aeiou])h([^aeiou]|$)/g, '$1$2');
  }

  function transformCK(token) {
    return token.replace(/ck/g, 'k');
  }

  function transformPH(token) {
    return token.replace(/ph/g, 'f');
  }

  function transformQ(token) {
    return token.replace(/q/g, 'k');
  }

  function transformS(token) {
    //return token.replace(/s(h|io|ia)/g, 'x$1');
    return token.replace(/s(io|ia)/g, 'x$1'); //glen
  }

  function transformT(token) {
    token = token.replace(/t(ia|io)/g, 'x$1');
    token = token.replace(/th/, '0');

    return token;
  }

  function dropT(token) {
    return token.replace(/tch/g, 'ch');
  }

  function transformV(token) {
    return token.replace(/v/g, 'f');
  }

  function transformWH(token) {
    return token.replace(/^wh/, 'w');
  }

  function dropW(token) {
    token = token.replace(/rw/g, 'rf'); //glen
    return token.replace(/w([^aeiou]|$)/g, '$1');
  }

  function transformX(token) {
    token = token.replace(/^x/, 's');
    token = token.replace(/x/g, 'ks');
    return token;
  }

  function glenAdjust(token) {
    // other reasonable adjustments
    token = token.replace(/^ou/, 'u'); // Sousan
    token = token.replace(/aa/g, 'a'); // Jaan
    return token;
  }

  function dropY(token) {
    return token.replace(/y([^aeiou]|$)/g, '$1');
  }

  function transformZ(token) {
    token = token.replace(/z/, 's');
    return token.replace(/ss/g, 's'); // glen - drop double s
  }

  function dropVowels(token) {
    return token.charAt(0) + token.substr(1, token.length).replace(/[aeiou]/g, '');
  }

  function process(token) {
    var maxLength = 10;
    token = token.toLowerCase();
    token = dedup(token);
    token = dropInitialLetters(token);
    token = dropBafterMAtEnd(token);
    token = transformCK(token);
    token = cTransform(token);
    token = dTransform(token);
    token = dropG(token);
    token = transformG(token);
    token = dropH(token);
    token = transformPH(token);
    token = transformQ(token);
    token = transformS(token);
    token = transformX(token);
    token = transformT(token);
    token = dropT(token);
    token = transformV(token);
    token = transformWH(token);
    token = dropW(token);
    token = dropY(token);
    token = transformZ(token);
    token = dropVowels(token);
    token = glenAdjust(token);

    if (token.length >= maxLength)
      token = token.substring(0, maxLength);

    return token;
  }

  return {
    process: process
  };
};

var RemoveAccents = function () {
  // adapted from https://stackoverflow.com/a/9667752/32429
  var map = JSON.parse(decodeURIComponent(escape(atob(
    'eyLDgSI6IkEiLCLEgiI6IkEiLCLhuq4iOiJBIiwi4bq2IjoiQSIsIuG6sCI6IkEiLCLhurIiOiJBIiwi4bq0IjoiQSIsIseNIjoiQSIsIsOCIjoiQSIsIuG6pCI6IkEiLCLhuqwiOiJBIiwi4bqmIjoiQSIsIuG6qCI6IkEiLCLhuqoiOiJBIiwiw4QiOiJBIiwix54iOiJBIiwiyKYiOiJBIiwix6AiOiJBIiwi4bqgIjoiQSIsIsiAIjoiQSIsIsOAIjoiQSIsIuG6oiI6IkEiLCLIgiI6IkEiLCLEgCI6IkEiLCLEhCI6IkEiLCLDhSI6IkEiLCLHuiI6IkEiLCLhuIAiOiJBIiwiyLoiOiJBIiwiw4MiOiJBIiwi6pyyIjoiQUEiLCLDhiI6IkFFIiwix7wiOiJBRSIsIseiIjoiQUUiLCLqnLQiOiJBTyIsIuqctiI6IkFVIiwi6py4IjoiQVYiLCLqnLoiOiJBViIsIuqcvCI6IkFZIiwi4biCIjoiQiIsIuG4hCI6IkIiLCLGgSI6IkIiLCLhuIYiOiJCIiwiyYMiOiJCIiwixoIiOiJCIiwixIYiOiJDIiwixIwiOiJDIiwiw4ciOiJDIiwi4biIIjoiQyIsIsSIIjoiQyIsIsSKIjoiQyIsIsaHIjoiQyIsIsi7IjoiQyIsIsSOIjoiRCIsIuG4kCI6IkQiLCLhuJIiOiJEIiwi4biKIjoiRCIsIuG4jCI6IkQiLCLGiiI6IkQiLCLhuI4iOiJEIiwix7IiOiJEIiwix4UiOiJEIiwixJAiOiJEIiwixosiOiJEIiwix7EiOiJEWiIsIseEIjoiRFoiLCLDiSI6IkUiLCLElCI6IkUiLCLEmiI6IkUiLCLIqCI6IkUiLCLhuJwiOiJFIiwiw4oiOiJFIiwi4bq+IjoiRSIsIuG7hiI6IkUiLCLhu4AiOiJFIiwi4buCIjoiRSIsIuG7hCI6IkUiLCLhuJgiOiJFIiwiw4siOiJFIiwixJYiOiJFIiwi4bq4IjoiRSIsIsiEIjoiRSIsIsOIIjoiRSIsIuG6uiI6IkUiLCLIhiI6IkUiLCLEkiI6IkUiLCLhuJYiOiJFIiwi4biUIjoiRSIsIsSYIjoiRSIsIsmGIjoiRSIsIuG6vCI6IkUiLCLhuJoiOiJFIiwi6p2qIjoiRVQiLCLhuJ4iOiJGIiwixpEiOiJGIiwix7QiOiJHIiwixJ4iOiJHIiwix6YiOiJHIiwixKIiOiJHIiwixJwiOiJHIiwixKAiOiJHIiwixpMiOiJHIiwi4bigIjoiRyIsIsekIjoiRyIsIuG4qiI6IkgiLCLIniI6IkgiLCLhuKgiOiJIIiwixKQiOiJIIiwi4rGnIjoiSCIsIuG4piI6IkgiLCLhuKIiOiJIIiwi4bikIjoiSCIsIsSmIjoiSCIsIsONIjoiSSIsIsSsIjoiSSIsIsePIjoiSSIsIsOOIjoiSSIsIsOPIjoiSSIsIuG4riI6IkkiLCLEsCI6IkkiLCLhu4oiOiJJIiwiyIgiOiJJIiwiw4wiOiJJIiwi4buIIjoiSSIsIsiKIjoiSSIsIsSqIjoiSSIsIsSuIjoiSSIsIsaXIjoiSSIsIsSoIjoiSSIsIuG4rCI6IkkiLCLqnbkiOiJEIiwi6p27IjoiRiIsIuqdvSI6IkciLCLqnoIiOiJSIiwi6p6EIjoiUyIsIuqehiI6IlQiLCLqnawiOiJJUyIsIsS0IjoiSiIsIsmIIjoiSiIsIuG4sCI6IksiLCLHqCI6IksiLCLEtiI6IksiLCLisakiOiJLIiwi6p2CIjoiSyIsIuG4siI6IksiLCLGmCI6IksiLCLhuLQiOiJLIiwi6p2AIjoiSyIsIuqdhCI6IksiLCLEuSI6IkwiLCLIvSI6IkwiLCLEvSI6IkwiLCLEuyI6IkwiLCLhuLwiOiJMIiwi4bi2IjoiTCIsIuG4uCI6IkwiLCLisaAiOiJMIiwi6p2IIjoiTCIsIuG4uiI6IkwiLCLEvyI6IkwiLCLisaIiOiJMIiwix4giOiJMIiwixYEiOiJMIiwix4ciOiJMSiIsIuG4viI6Ik0iLCLhuYAiOiJNIiwi4bmCIjoiTSIsIuKxriI6Ik0iLCLFgyI6Ik4iLCLFhyI6Ik4iLCLFhSI6Ik4iLCLhuYoiOiJOIiwi4bmEIjoiTiIsIuG5hiI6Ik4iLCLHuCI6Ik4iLCLGnSI6Ik4iLCLhuYgiOiJOIiwiyKAiOiJOIiwix4siOiJOIiwiw5EiOiJOIiwix4oiOiJOSiIsIsOTIjoiTyIsIsWOIjoiTyIsIseRIjoiTyIsIsOUIjoiTyIsIuG7kCI6Ik8iLCLhu5giOiJPIiwi4buSIjoiTyIsIuG7lCI6Ik8iLCLhu5YiOiJPIiwiw5YiOiJPIiwiyKoiOiJPIiwiyK4iOiJPIiwiyLAiOiJPIiwi4buMIjoiTyIsIsWQIjoiTyIsIsiMIjoiTyIsIsOSIjoiTyIsIuG7jiI6Ik8iLCLGoCI6Ik8iLCLhu5oiOiJPIiwi4buiIjoiTyIsIuG7nCI6Ik8iLCLhu54iOiJPIiwi4bugIjoiTyIsIsiOIjoiTyIsIuqdiiI6Ik8iLCLqnYwiOiJPIiwixYwiOiJPIiwi4bmSIjoiTyIsIuG5kCI6Ik8iLCLGnyI6Ik8iLCLHqiI6Ik8iLCLHrCI6Ik8iLCLDmCI6Ik8iLCLHviI6Ik8iLCLDlSI6Ik8iLCLhuYwiOiJPIiwi4bmOIjoiTyIsIsisIjoiTyIsIsaiIjoiT0kiLCLqnY4iOiJPTyIsIsaQIjoiRSIsIsaGIjoiTyIsIsiiIjoiT1UiLCLhuZQiOiJQIiwi4bmWIjoiUCIsIuqdkiI6IlAiLCLGpCI6IlAiLCLqnZQiOiJQIiwi4rGjIjoiUCIsIuqdkCI6IlAiLCLqnZgiOiJRIiwi6p2WIjoiUSIsIsWUIjoiUiIsIsWYIjoiUiIsIsWWIjoiUiIsIuG5mCI6IlIiLCLhuZoiOiJSIiwi4bmcIjoiUiIsIsiQIjoiUiIsIsiSIjoiUiIsIuG5niI6IlIiLCLJjCI6IlIiLCLisaQiOiJSIiwi6py+IjoiQyIsIsaOIjoiRSIsIsWaIjoiUyIsIuG5pCI6IlMiLCLFoCI6IlMiLCLhuaYiOiJTIiwixZ4iOiJTIiwixZwiOiJTIiwiyJgiOiJTIiwi4bmgIjoiUyIsIuG5oiI6IlMiLCLhuagiOiJTIiwixaQiOiJUIiwixaIiOiJUIiwi4bmwIjoiVCIsIsiaIjoiVCIsIsi+IjoiVCIsIuG5qiI6IlQiLCLhuawiOiJUIiwixqwiOiJUIiwi4bmuIjoiVCIsIsauIjoiVCIsIsWmIjoiVCIsIuKxryI6IkEiLCLqnoAiOiJMIiwixpwiOiJNIiwiyYUiOiJWIiwi6pyoIjoiVFoiLCLDmiI6IlUiLCLFrCI6IlUiLCLHkyI6IlUiLCLDmyI6IlUiLCLhubYiOiJVIiwiw5wiOiJVIiwix5ciOiJVIiwix5kiOiJVIiwix5siOiJVIiwix5UiOiJVIiwi4bmyIjoiVSIsIuG7pCI6IlUiLCLFsCI6IlUiLCLIlCI6IlUiLCLDmSI6IlUiLCLhu6YiOiJVIiwixq8iOiJVIiwi4buoIjoiVSIsIuG7sCI6IlUiLCLhu6oiOiJVIiwi4busIjoiVSIsIuG7riI6IlUiLCLIliI6IlUiLCLFqiI6IlUiLCLhuboiOiJVIiwixbIiOiJVIiwixa4iOiJVIiwixagiOiJVIiwi4bm4IjoiVSIsIuG5tCI6IlUiLCLqnZ4iOiJWIiwi4bm+IjoiViIsIsayIjoiViIsIuG5vCI6IlYiLCLqnaAiOiJWWSIsIuG6giI6IlciLCLFtCI6IlciLCLhuoQiOiJXIiwi4bqGIjoiVyIsIuG6iCI6IlciLCLhuoAiOiJXIiwi4rGyIjoiVyIsIuG6jCI6IlgiLCLhuooiOiJYIiwiw50iOiJZIiwixbYiOiJZIiwixbgiOiJZIiwi4bqOIjoiWSIsIuG7tCI6IlkiLCLhu7IiOiJZIiwixrMiOiJZIiwi4bu2IjoiWSIsIuG7viI6IlkiLCLIsiI6IlkiLCLJjiI6IlkiLCLhu7giOiJZIiwixbkiOiJaIiwixb0iOiJaIiwi4bqQIjoiWiIsIuKxqyI6IloiLCLFuyI6IloiLCLhupIiOiJaIiwiyKQiOiJaIiwi4bqUIjoiWiIsIsa1IjoiWiIsIsSyIjoiSUoiLCLFkiI6Ik9FIiwi4bSAIjoiQSIsIuG0gSI6IkFFIiwiypkiOiJCIiwi4bSDIjoiQiIsIuG0hCI6IkMiLCLhtIUiOiJEIiwi4bSHIjoiRSIsIuqcsCI6IkYiLCLJoiI6IkciLCLKmyI6IkciLCLKnCI6IkgiLCLJqiI6IkkiLCLKgSI6IlIiLCLhtIoiOiJKIiwi4bSLIjoiSyIsIsqfIjoiTCIsIuG0jCI6IkwiLCLhtI0iOiJNIiwiybQiOiJOIiwi4bSPIjoiTyIsIsm2IjoiT0UiLCLhtJAiOiJPIiwi4bSVIjoiT1UiLCLhtJgiOiJQIiwiyoAiOiJSIiwi4bSOIjoiTiIsIuG0mSI6IlIiLCLqnLEiOiJTIiwi4bSbIjoiVCIsIuKxuyI6IkUiLCLhtJoiOiJSIiwi4bScIjoiVSIsIuG0oCI6IlYiLCLhtKEiOiJXIiwiyo8iOiJZIiwi4bSiIjoiWiIsIsOhIjoiYSIsIsSDIjoiYSIsIuG6ryI6ImEiLCLhurciOiJhIiwi4bqxIjoiYSIsIuG6syI6ImEiLCLhurUiOiJhIiwix44iOiJhIiwiw6IiOiJhIiwi4bqlIjoiYSIsIuG6rSI6ImEiLCLhuqciOiJhIiwi4bqpIjoiYSIsIuG6qyI6ImEiLCLDpCI6ImEiLCLHnyI6ImEiLCLIpyI6ImEiLCLHoSI6ImEiLCLhuqEiOiJhIiwiyIEiOiJhIiwiw6AiOiJhIiwi4bqjIjoiYSIsIsiDIjoiYSIsIsSBIjoiYSIsIsSFIjoiYSIsIuG2jyI6ImEiLCLhupoiOiJhIiwiw6UiOiJhIiwix7siOiJhIiwi4biBIjoiYSIsIuKxpSI6ImEiLCLDoyI6ImEiLCLqnLMiOiJhYSIsIsOmIjoiYWUiLCLHvSI6ImFlIiwix6MiOiJhZSIsIuqctSI6ImFvIiwi6py3IjoiYXUiLCLqnLkiOiJhdiIsIuqcuyI6ImF2Iiwi6py9IjoiYXkiLCLhuIMiOiJiIiwi4biFIjoiYiIsIsmTIjoiYiIsIuG4hyI6ImIiLCLhtawiOiJiIiwi4baAIjoiYiIsIsaAIjoiYiIsIsaDIjoiYiIsIsm1IjoibyIsIsSHIjoiYyIsIsSNIjoiYyIsIsOnIjoiYyIsIuG4iSI6ImMiLCLEiSI6ImMiLCLJlSI6ImMiLCLEiyI6ImMiLCLGiCI6ImMiLCLIvCI6ImMiLCLEjyI6ImQiLCLhuJEiOiJkIiwi4biTIjoiZCIsIsihIjoiZCIsIuG4iyI6ImQiLCLhuI0iOiJkIiwiyZciOiJkIiwi4baRIjoiZCIsIuG4jyI6ImQiLCLhta0iOiJkIiwi4baBIjoiZCIsIsSRIjoiZCIsIsmWIjoiZCIsIsaMIjoiZCIsIsSxIjoiaSIsIsi3IjoiaiIsIsmfIjoiaiIsIsqEIjoiaiIsIsezIjoiZHoiLCLHhiI6ImR6Iiwiw6kiOiJlIiwixJUiOiJlIiwixJsiOiJlIiwiyKkiOiJlIiwi4bidIjoiZSIsIsOqIjoiZSIsIuG6vyI6ImUiLCLhu4ciOiJlIiwi4buBIjoiZSIsIuG7gyI6ImUiLCLhu4UiOiJlIiwi4biZIjoiZSIsIsOrIjoiZSIsIsSXIjoiZSIsIuG6uSI6ImUiLCLIhSI6ImUiLCLDqCI6ImUiLCLhursiOiJlIiwiyIciOiJlIiwixJMiOiJlIiwi4biXIjoiZSIsIuG4lSI6ImUiLCLisbgiOiJlIiwixJkiOiJlIiwi4baSIjoiZSIsIsmHIjoiZSIsIuG6vSI6ImUiLCLhuJsiOiJlIiwi6p2rIjoiZXQiLCLhuJ8iOiJmIiwixpIiOiJmIiwi4bWuIjoiZiIsIuG2giI6ImYiLCLHtSI6ImciLCLEnyI6ImciLCLHpyI6ImciLCLEoyI6ImciLCLEnSI6ImciLCLEoSI6ImciLCLJoCI6ImciLCLhuKEiOiJnIiwi4baDIjoiZyIsIselIjoiZyIsIuG4qyI6ImgiLCLInyI6ImgiLCLhuKkiOiJoIiwixKUiOiJoIiwi4rGoIjoiaCIsIuG4pyI6ImgiLCLhuKMiOiJoIiwi4bilIjoiaCIsIsmmIjoiaCIsIuG6liI6ImgiLCLEpyI6ImgiLCLGlSI6Imh2Iiwiw60iOiJpIiwixK0iOiJpIiwix5AiOiJpIiwiw64iOiJpIiwiw68iOiJpIiwi4bivIjoiaSIsIuG7iyI6ImkiLCLIiSI6ImkiLCLDrCI6ImkiLCLhu4kiOiJpIiwiyIsiOiJpIiwixKsiOiJpIiwixK8iOiJpIiwi4baWIjoiaSIsIsmoIjoiaSIsIsSpIjoiaSIsIuG4rSI6ImkiLCLqnboiOiJkIiwi6p28IjoiZiIsIuG1uSI6ImciLCLqnoMiOiJyIiwi6p6FIjoicyIsIuqehyI6InQiLCLqna0iOiJpcyIsIsewIjoiaiIsIsS1IjoiaiIsIsqdIjoiaiIsIsmJIjoiaiIsIuG4sSI6ImsiLCLHqSI6ImsiLCLEtyI6ImsiLCLisaoiOiJrIiwi6p2DIjoiayIsIuG4syI6ImsiLCLGmSI6ImsiLCLhuLUiOiJrIiwi4baEIjoiayIsIuqdgSI6ImsiLCLqnYUiOiJrIiwixLoiOiJsIiwixpoiOiJsIiwiyawiOiJsIiwixL4iOiJsIiwixLwiOiJsIiwi4bi9IjoibCIsIsi0IjoibCIsIuG4tyI6ImwiLCLhuLkiOiJsIiwi4rGhIjoibCIsIuqdiSI6ImwiLCLhuLsiOiJsIiwixYAiOiJsIiwiyasiOiJsIiwi4baFIjoibCIsIsmtIjoibCIsIsWCIjoibCIsIseJIjoibGoiLCLFvyI6InMiLCLhupwiOiJzIiwi4bqbIjoicyIsIuG6nSI6InMiLCLhuL8iOiJtIiwi4bmBIjoibSIsIuG5gyI6Im0iLCLJsSI6Im0iLCLhta8iOiJtIiwi4baGIjoibSIsIsWEIjoibiIsIsWIIjoibiIsIsWGIjoibiIsIuG5iyI6Im4iLCLItSI6Im4iLCLhuYUiOiJuIiwi4bmHIjoibiIsIse5IjoibiIsIsmyIjoibiIsIuG5iSI6Im4iLCLGniI6Im4iLCLhtbAiOiJuIiwi4baHIjoibiIsIsmzIjoibiIsIsOxIjoibiIsIseMIjoibmoiLCLDsyI6Im8iLCLFjyI6Im8iLCLHkiI6Im8iLCLDtCI6Im8iLCLhu5EiOiJvIiwi4buZIjoibyIsIuG7kyI6Im8iLCLhu5UiOiJvIiwi4buXIjoibyIsIsO2IjoibyIsIsirIjoibyIsIsivIjoibyIsIsixIjoibyIsIuG7jSI6Im8iLCLFkSI6Im8iLCLIjSI6Im8iLCLDsiI6Im8iLCLhu48iOiJvIiwixqEiOiJvIiwi4bubIjoibyIsIuG7oyI6Im8iLCLhu50iOiJvIiwi4bufIjoibyIsIuG7oSI6Im8iLCLIjyI6Im8iLCLqnYsiOiJvIiwi6p2NIjoibyIsIuKxuiI6Im8iLCLFjSI6Im8iLCLhuZMiOiJvIiwi4bmRIjoibyIsIserIjoibyIsIsetIjoibyIsIsO4IjoibyIsIse/IjoibyIsIsO1IjoibyIsIuG5jSI6Im8iLCLhuY8iOiJvIiwiyK0iOiJvIiwixqMiOiJvaSIsIuqdjyI6Im9vIiwiyZsiOiJlIiwi4baTIjoiZSIsIsmUIjoibyIsIuG2lyI6Im8iLCLIoyI6Im91Iiwi4bmVIjoicCIsIuG5lyI6InAiLCLqnZMiOiJwIiwixqUiOiJwIiwi4bWxIjoicCIsIuG2iCI6InAiLCLqnZUiOiJwIiwi4bW9IjoicCIsIuqdkSI6InAiLCLqnZkiOiJxIiwiyqAiOiJxIiwiyYsiOiJxIiwi6p2XIjoicSIsIsWVIjoiciIsIsWZIjoiciIsIsWXIjoiciIsIuG5mSI6InIiLCLhuZsiOiJyIiwi4bmdIjoiciIsIsiRIjoiciIsIsm+IjoiciIsIuG1syI6InIiLCLIkyI6InIiLCLhuZ8iOiJyIiwiybwiOiJyIiwi4bWyIjoiciIsIuG2iSI6InIiLCLJjSI6InIiLCLJvSI6InIiLCLihoQiOiJjIiwi6py/IjoiYyIsIsmYIjoiZSIsIsm/IjoiciIsIsWbIjoicyIsIuG5pSI6InMiLCLFoSI6InMiLCLhuaciOiJzIiwixZ8iOiJzIiwixZ0iOiJzIiwiyJkiOiJzIiwi4bmhIjoicyIsIuG5oyI6InMiLCLhuakiOiJzIiwiyoIiOiJzIiwi4bW0IjoicyIsIuG2iiI6InMiLCLIvyI6InMiLCLJoSI6ImciLCLhtJEiOiJvIiwi4bSTIjoibyIsIuG0nSI6InUiLCLFpSI6InQiLCLFoyI6InQiLCLhubEiOiJ0IiwiyJsiOiJ0IiwiyLYiOiJ0Iiwi4bqXIjoidCIsIuKxpiI6InQiLCLhuasiOiJ0Iiwi4bmtIjoidCIsIsatIjoidCIsIuG5ryI6InQiLCLhtbUiOiJ0IiwixqsiOiJ0IiwiyogiOiJ0IiwixaciOiJ0Iiwi4bW6IjoidGgiLCLJkCI6ImEiLCLhtIIiOiJhZSIsIsedIjoiZSIsIuG1tyI6ImciLCLJpSI6ImgiLCLKriI6ImgiLCLKryI6ImgiLCLhtIkiOiJpIiwiyp4iOiJrIiwi6p6BIjoibCIsIsmvIjoibSIsIsmwIjoibSIsIuG0lCI6Im9lIiwiybkiOiJyIiwiybsiOiJyIiwiyboiOiJyIiwi4rG5IjoiciIsIsqHIjoidCIsIsqMIjoidiIsIsqNIjoidyIsIsqOIjoieSIsIuqcqSI6InR6Iiwiw7oiOiJ1Iiwixa0iOiJ1Iiwix5QiOiJ1Iiwiw7siOiJ1Iiwi4bm3IjoidSIsIsO8IjoidSIsIseYIjoidSIsIseaIjoidSIsIsecIjoidSIsIseWIjoidSIsIuG5syI6InUiLCLhu6UiOiJ1IiwixbEiOiJ1IiwiyJUiOiJ1Iiwiw7kiOiJ1Iiwi4bunIjoidSIsIsawIjoidSIsIuG7qSI6InUiLCLhu7EiOiJ1Iiwi4burIjoidSIsIuG7rSI6InUiLCLhu68iOiJ1IiwiyJciOiJ1IiwixasiOiJ1Iiwi4bm7IjoidSIsIsWzIjoidSIsIuG2mSI6InUiLCLFryI6InUiLCLFqSI6InUiLCLhubkiOiJ1Iiwi4bm1IjoidSIsIuG1qyI6InVlIiwi6p24IjoidW0iLCLisbQiOiJ2Iiwi6p2fIjoidiIsIuG5vyI6InYiLCLKiyI6InYiLCLhtowiOiJ2Iiwi4rGxIjoidiIsIuG5vSI6InYiLCLqnaEiOiJ2eSIsIuG6gyI6InciLCLFtSI6InciLCLhuoUiOiJ3Iiwi4bqHIjoidyIsIuG6iSI6InciLCLhuoEiOiJ3Iiwi4rGzIjoidyIsIuG6mCI6InciLCLhuo0iOiJ4Iiwi4bqLIjoieCIsIuG2jSI6IngiLCLDvSI6InkiLCLFtyI6InkiLCLDvyI6InkiLCLhuo8iOiJ5Iiwi4bu1IjoieSIsIuG7syI6InkiLCLGtCI6InkiLCLhu7ciOiJ5Iiwi4bu/IjoieSIsIsizIjoieSIsIuG6mSI6InkiLCLJjyI6InkiLCLhu7kiOiJ5IiwixboiOiJ6Iiwixb4iOiJ6Iiwi4bqRIjoieiIsIsqRIjoieiIsIuKxrCI6InoiLCLFvCI6InoiLCLhupMiOiJ6IiwiyKUiOiJ6Iiwi4bqVIjoieiIsIuG1tiI6InoiLCLhto4iOiJ6IiwiypAiOiJ6IiwixrYiOiJ6IiwiyYAiOiJ6Iiwi76yAIjoiZmYiLCLvrIMiOiJmZmkiLCLvrIQiOiJmZmwiLCLvrIEiOiJmaSIsIu+sgiI6ImZsIiwixLMiOiJpaiIsIsWTIjoib2UiLCLvrIYiOiJzdCIsIuKCkCI6ImEiLCLigpEiOiJlIiwi4bWiIjoiaSIsIuKxvCI6ImoiLCLigpIiOiJvIiwi4bWjIjoiciIsIuG1pCI6InUiLCLhtaUiOiJ2Iiwi4oKTIjoieCJ9'
  ))));

  function process(s) {
    return s.replace(/[^A-Za-z\[\] ]/g,
      function (c) {
        return map[c] || c;
      });
  }

  return {
    process: process
  }
};