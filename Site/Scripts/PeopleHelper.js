/// <reference path="site.js" />
/// <reference path="jquery-1.7-vsdoc.js" />

var PeopleHelper = function (url) {
    var local = {
        url: url,
        lastInfo: null
    };
    var startGettingPeople = function (search, onNamesReady, includeMatches, usedPersonIds) {
        ShowStatusDisplay('searching...', 500);
        CallAjaxHandler(local.url + '/GetPeople', {
            search: search,
            includeIneligible: true,
            includeMatches: includeMatches
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
        var foundFuzzy = false;
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
            $.each(info.People, function (i, personInfo) {
                var foundHit = false;
                var classes = [];
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

                if (usedIds && $.inArray(personInfo.Id, usedIds) != -1) {
                    classes.push('InUse');
                }
                if (personInfo.Ineligible) {
                    classes.push('InvalidName');
                    personInfo.IneligibleData = ' data-ineligible="{0}"'.filledWith(personInfo.Ineligible);
                }
                if (classes.length != 0) {
                    personInfo.Name = '<span class="{0}">{1}</span>'.filledWith(classes.join(' '), personInfo.Name);
                }
                results.push(personInfo);
            });
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
        SearchNames: function (searchText, onNamesReady, includeMatches, usedPersonIds) {
            startGettingPeople(searchText, onNamesReady, includeMatches, usedPersonIds);
        },
        RefreshListing: function (searchText, onNamesReady, usedPersonIds) {
            refreshListing(searchText, onNamesReady, usedPersonIds);
        }
    };

    return publicInterface;
};

