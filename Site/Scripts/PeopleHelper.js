var PeopleHelper = function(url, forBallotEntry) {
    var local = {
        url: url,
        nameSplitter: /[\s\-']/,
        localNames: []
    };

    var maxToShow = 60;
    var soundex = new Metaphone();

    function prepare(cb) {
        startLoadingAllNames(cb);
        site.onbroadcast(site.broadcastCode.personSaved, personSaved);
    }

    function startLoadingAllNames(cb) {
        ShowStatusDisplay('Loading names list');
        CallAjaxHandler(local.url + '/GetAll',
            {},
            function(info) {
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
        var i = local.localNames.findIndex(function(person) {
            return person.Id === editedPerson.C_RowId;
        });

        if (i === -1) {
            // new person, adjust to fit
            editedPerson.Name = editedPerson.C_FullName;
            editedPerson.Id = editedPerson.C_RowId;
            editedPerson.NumVotes = 0;

            extendPersonCore(editedPerson);

            local.localNames.push(editedPerson);
        }
        else {
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
        info.PersonLines.forEach(function(editedPerson) {

            // find this person
            var i = local.localNames.findIndex(function(person) {
                return person.Id === editedPerson.PersonId;
            });

            if (i === -1) {
                // new person, adjust to fit
                editedPerson.Name = editedPerson.FullName;
                editedPerson.Id = editedPerson.PersonId;
                editedPerson.NumVotes = 0;

                extendPersonCore(editedPerson);

                local.localNames.push(editedPerson);
            }
            else {
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
        arr.forEach(extendPerson);
        return arr;
    }

    function extendPerson(p) {
        // decode compressed info from the server
        //if (p.Id === 25068) debugger;

        p.CanReceiveVotes = p.V[0] === '1';
        p.CanVote = p.V[1] === '1';
        p.Ineligible = p.IRG;

        extendPersonCore(p);
    }

    function extendPersonCore(p) {
        // for searches, make lowercase
        p.name = p.Name.toLowerCase().replace(/[\(\)\[\]]/ig, '');  // and remove brackets
        p.parts = p.name.split(local.nameSplitter);

        p.soundParts = p.parts.map(soundex.process);
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

        //console.log(searchParts, searchSounds);

        local.localNames.forEach(function(n) {
            if (result.People.length < maxToShow) {
                addMatchedNames(n, result.People, searchParts, searchSounds);
            }
        });

        sortResults(result);

        var info = markUp(result, searchParts, usedPersonIds);

        cbAfterSearch(info, true);
    }

    function addMatchedNames(person, matchedPeople, searchParts, searchSounds) {
        var nameParts = person.parts;
        var nameSounds = person.soundParts; // same length as nameParts

        // match each search and name part
        person.matchedParts = nameParts.map(function() { return 0; }); // fill array of correct length with 0
        var toMatch = searchParts.length;
        var found = 0;

        for (var i = 0; i < searchParts.length; i++) {

            var searchPart = searchParts[i];
            var searchSound = searchSounds[i];

            for (var j = 0; j < nameParts.length; j++) {

                if (nameParts[j].startsWith(searchPart) && !person.matchedParts[j]) {
                    person.matchedParts[j] = 2; // high level match
                    found++;
                    break;
                }
                else if (nameSounds[j].startsWith(searchSound) && !person.matchedParts[j]) {
                    person.matchedParts[j] = 1; // low level
                    found++;
                    break;
                }
            }
        }

        person.MatchType = toMatch !== found ? 0 : person.matchedParts.reduce(function(acc, p) { return p > acc ? p : acc; }, 0);

        person.Parts2 = person.matchedParts.reduce(function(acc, p) { return p === 2 ? 1 + acc : acc; }, 0);
        person.Parts1 = person.matchedParts.reduce(function(acc, p) { return p === 1 ? 1 + acc : acc; }, 0);

        if (person.MatchType) {
            // something matched
            matchedPeople.push(person);
        }
    }

    function sortResults(result) {
        if (forBallotEntry) {
            result.People.sort(function(a, b) {
                if (a.Parts2 < b.Parts2) return 1;
                if (a.Parts2 > b.Parts2) return -1;

                if (a.Parts1 < b.Parts1) return 1;
                if (a.Parts1 > b.Parts1) return -1;

                if (a.NumVotes < b.NumVotes) return 1;
                if (a.NumVotes > b.NumVotes) return -1;

                return a.Name.toLowerCase().localeCompare(b.Name.toLowerCase());
            });
        } else {
            result.People.sort(function(a, b) {
                if (a.Parts2 < b.Parts2) return 1;
                if (a.Parts2 > b.Parts2) return -1;

                if (a.Parts1 < b.Parts1) return 1;
                if (a.Parts1 > b.Parts1) return -1;

                return a.Name.toLowerCase().localeCompare(b.Name.toLowerCase());
            });
        }
    }

    function markUp(info, searchParts, usedIds) {
        var results = [];

        var currentFocus = $('#nameList > li.selected');
        var rawId = currentFocus.attr('id');
        var currentFocusId = rawId ? +rawId.substr(1) : 0;

        if (info && typeof info.People !== 'undefined') {
            var highestNumVotes = 0;

            $.each(info.People, function(i, personInfo) {

                //if (personInfo.Id === 25068) debugger;

                if (personInfo.NumVotes > highestNumVotes) {
                    highestNumVotes = personInfo.NumVotes;
                }

                var liClasses = [];
                var spanClasses = [];

                if (forBallotEntry) {
                    liClasses.push(personInfo.NumVotes ? 'HasVotes' : 'NoVotes');
                }
                if (personInfo.Parts1 && !personInfo.Parts2) liClasses.push('Match1');
                if (personInfo.Parts2 && !personInfo.Parts1) liClasses.push('Match2Only');

                spanClasses.push('Match' + personInfo.MatchType);

                showMatchedLetters(searchParts, personInfo);

                if (personInfo.Area) {
                    personInfo.DisplayName = personInfo.DisplayName + ' (' + personInfo.Area + ')';
                }

                if (usedIds && $.inArray(personInfo.Id, usedIds) !== -1) {
                    spanClasses.push('InUse');
                    personInfo.InUse = true;
                }
                if (personInfo.Ineligible) {
                    if (!personInfo.CanReceiveVotes) {
                        spanClasses.push('CannotReceiveVotes');
                    }
                    // only add if the only restriction
                    if (!personInfo.CanVote) {
                        spanClasses.push('CannotVote');
                    }

                    personInfo.IneligibleData = ' data-ineligible="{Ineligible}" data-canVote={CanVote} data-canReceiveVotes={CanReceiveVotes}'.filledWith(personInfo);
                }
                if (spanClasses.length) {
                    personInfo.HtmlName = '<span class="{0}">{^1}</span>'.filledWith(spanClasses.join(' '), personInfo.DisplayName);
                }
                if (liClasses.length) {
                    personInfo.Classes = ' class="{0}"'.filledWith(liClasses.join(' '));
                }
                results.push(personInfo);
            });

            var foundBest = false;
            info.BestRowNum = 0;

            //2018-Feb only look in type 2; only in 1 if none were in 2

            for (var matchType = 2; matchType >= 1; matchType--) {
                var foundInType = false;
                for (var targetMatch = highestNumVotes; !foundBest && targetMatch >= 0; targetMatch--) {
                    $.each(results, function(i, item) {
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
            info.People = results;
        }
        return info;
    }

    function updateVoteCounts(info) {
        var updates = info.VoteUpdates;
        if (!updates || !updates.length) {
            return;
        }

        var toFind = updates.map(function(update) { return update.PersonGuid; }).join(',');
        var numToFind = updates.length;

        local.localNames.forEach(function(person) {
            var guid = person.PersonGuid;
            if (toFind.indexOf(guid) !== -1) {
                // this person was updated
                if (numToFind === 1) {
                    person.NumVotes = updates[0].Count;
                } else {
                    var update = updates.find(function(update) {
                        return update.PersonGuid = guid;
                    });
                    if (update) {
                        person.NumVotes = update.Count;
                    }
                }
            }
        });
    }

    function refreshListing(searchTerm, onNamesReady, usedPersonIds, info) {
        updateVoteCounts(info);
        search(searchTerm, onNamesReady, usedPersonIds);
    }

    function showMatchedLetters(searchParts, personInfo) {
        var name = personInfo.Name;

        searchParts.forEach(function(searchPart) {
            if ($.trim(searchPart) === '') return;
            var searchReg = new RegExp('[\\s\\-\\\'\\[\\(]({0})|(^{0})'.filledWith(searchPart), 'ig');
            name = name.replace(searchReg, function(a, b, c) {
                return '##1' + arguments[0] + '##2';
            });
        });

        if (personInfo.Parts1) {
            // find the soundex match
            var nameSplit = name.match(/(.*?)([\s\-\']|$)/g);

            personInfo.matchedParts.forEach(function(p, i) {
                if (p !== 1) return;

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

        personInfo.DisplayName = name
            .replace(/##1/g, '<b>')
            .replace(/##2/g, '</b>')
            .replace(/##3/g, '<i>')
            .replace(/##4/g, '</i>');

    }

    var publicInterface = {
        Prepare: prepare,
        local: local,
        Search: function(searchText, onNamesReady, usedPersonIds) {
            search(searchText, onNamesReady, usedPersonIds);
        },
        UpdatePeople: function(info) {
            updatePeople(info);
        },
        Special: function(searchText, onNamesReady) {
            special(searchText, onNamesReady);
        },
        RefreshListing: function(searchText, onNamesReady, usedPersonIds, info) {
            refreshListing(searchText, onNamesReady, usedPersonIds, info);
        }
        //AddToLocalNames: addToLocalNames,
        //AddGroupToLocalNames: addGroupToLocalNames
    };

    return publicInterface;
};


var Metaphone = function() {


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

        if (token.length >= maxLength)
            token = token.substring(0, maxLength);

        return token;
    }

    return {
        process: process
    };
};


