/// <reference path="site.js" />
/// <reference path="jquery-1.7-vsdoc.js" />

var PeopleHelper = function (url) {
    var local = {
        url: url
    };
    var startGettingPeople = function (search, onNamesReady, includeMatches) {
        ShowStatusDisplay('searching...', 500);
        CallAjaxHandler(local.url + '/GetPeople', {
            search: search,
            includeInelligible: true,
            includeMatches: includeMatches
        }, onComplete, { callback: onNamesReady, search: search }, onFail);
    };

    var onComplete = function (info, extra) {
        ResetStatusDisplay();
        if (info && info.Error) {
            ShowStatusFailed(info.Error);
            return;
        }
        extra.callback(markUp(info, extra.search));
    };

    var markUp = function (info, search) {
        var results = [];
        var searchParts = [];
        var parts = search.split(' ');
        var foundFuzzy = false;
        $.each(parts, function (i, part) {
            if (part) {
                searchParts.push(new RegExp(part, "ig"));
            }
        });
        $.each(info && info.People, function (i, personInfo) {
            var foundHit = false;
            if (personInfo.SoundMatch) {
                personInfo.Name = '<i{0}>'.filledWith(foundFuzzy ? '' : ' class=First') + personInfo.Name + '</i>';
                foundFuzzy = true;
            }
            else {
                $.each(searchParts, function (k, searchPart) {
                    personInfo.Name = personInfo.Name.replace(searchPart, function () {
                        return '<b>' + arguments[0] + '</b>';
                    });
                });
            }
            //            if (!foundHit) {
            //                // must be soundex
            //                personInfo.Name = '<i{0}>'.filledWith(foundFuzzy ? '' : ' class=First') + personInfo.Name + '</i>';
            //                foundFuzzy = true;
            //            }
            if (personInfo.Inelligible) {
                personInfo.Name = '<span class=Inelligible>' + personInfo.Name + '</span>';
            }
            results.push(personInfo);
        });
        info.People = results;
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
        SearchNames: function (searchText, onNamesReady, includeMatches) {
            startGettingPeople(searchText, onNamesReady, includeMatches);
        }
    };

    return publicInterface;
};

