/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />
/// <reference path="../../Scripts/highcharts.js" />

var AnalyzePage = function () {
    var settings = {
        rowTemplate: '',
        chart: null
    };

    var preparePage = function () {
        var tableBody = $('#mainBody');
        settings.rowTemplate = tableBody.html();

        showInfo(publicInterface.results, true);

        showChart(publicInterface.results);
    };


    var showInfo = function (info, firstLoad) {
        var table = $('#mainBody');
        table.html(settings.rowTemplate.filledWithEach(expand(info)));
    };

    var showChart = function (info) {

        var getVoteCounts = function () {
            return $.map(info, function (item, i) {
                return item.VoteCount;
            });
        };
        
        var getNames = function () {
            return $.map(info, function (item, i) {
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