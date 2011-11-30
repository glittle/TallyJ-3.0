/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />
/// <reference path="../../Scripts/highcharts.js" />

var BallotPageFunc = function () {
    var settings = {
    };

    var preparePage = function () {

    };

    var publicInterface = {
        controllerUrl: '',
        PreparePage: preparePage
    };

    return publicInterface;
};

var ballotPage = BallotPageFunc();

$(function () {
    ballotPage.PreparePage();
});