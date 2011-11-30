/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />
/// <reference path="../../Scripts/highcharts.js" />

var AnalyzePage = function () {
    var settings = {
        rowTemplate: '',
        footTemplate: '',
        chart: null
    };

    var preparePage = function () {

        $('#btnRefesh').click(function () {
            runAnalysis(false);
        });

        var tableBody = $('#mainBody');
        settings.rowTemplate = tableBody.html();
        var tFoot = $('#mainFoot');
        settings.footTemplate = tFoot.html();
        tableBody.html('');
        tFoot.html('');


        var info = publicInterface.results;
        if (info) {
            showInfo(info, true);
        }
        else {
            runAnalysis(true);
        }
    };

    var runAnalysis = function (firstLoad) {
        ShowStatusDisplay('Analyzing...', 0);

        CallAjaxHandler(publicInterface.controllerUrl + '/RunAnalyze', null, showInfo);
    };

    var showInfo = function (info, firstLoad) {
        var tableBody = $('#mainBody');

        if (!firstLoad) {
            tableBody.animate({
                opacity: 0.5
            }, 100, function () {
                tableBody.animate({
                    opacity: 1
                }, 500);
            });
        }

        tableBody.html(settings.rowTemplate.filledWithEach(expand(info)));

        //TODO: add foot info

        setTimeout(function() {
            showChart(info);
        }, 100);
    };

    var showChart = function (info) {
        var maxToShow = 25; //TODO what is good limit?

        var getVoteCounts = function () {
            return $.map(info.slice(0, maxToShow), function (item, i) {
                return item.VoteCount;
            });
        };

        var getNames = function () {
            return $.map(info.slice(0, maxToShow), function (item, i) {
                return item.PersonName;
            });
        };


        settings.chart = new Highcharts.Chart({
            chart: {
                renderTo: 'chart',
                type: 'bar'
            },
            title: {
                text: 'Votes'
            },
            legend: {
                enabled: false
            },
            xAxis: {
                categories: getNames()
            },
            yAxis: {
                title: {
                    text: 'Votes'
                }
            },
            series: [{
                name: 'Votes',
                data: getVoteCounts()
            }]
        });
    };

    var expand = function (results) {
        $.each(results, function (i) {
            this.ClassName = (i % 2 == 0 ? 'Even' : 'Odd')
                + (this.IsTied ? ' Tied' : '');
            this.Comments = this.IsTied ? 'Tied' : '';
        });
        return results;
    };

    var publicInterface = {
        controllerUrl: '',
        results: null,
        PreparePage: preparePage
    };

    return publicInterface;
};

var analyzePage = AnalyzePage();

$(function () {
    analyzePage.PreparePage();
});