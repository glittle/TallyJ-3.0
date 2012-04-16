/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7.1.js" />

var dashboardIndex = function () {
    var localSettings = {
    };
    var publicInterface = {
        elections: [],
        electionsUrl: '',
        PreparePage: function () {
            site.onbroadcast(site.broadcastCode.electionStatusChanged, function (ev, info) {
                if (info.QuickLinks) {
                    location.reload();
                }
            });
        }
    };

    return publicInterface;
};

var dashboardIndexPage = dashboardIndex();

$(function () {
  dashboardIndexPage.PreparePage();
});