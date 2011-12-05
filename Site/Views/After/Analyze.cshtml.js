/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />
/// <reference path="../../Scripts/highcharts.js" />

var AnalyzePage = function () {
    var settings = {
        rowTemplate: '',
        footTemplate: '',
        invalidsRowTemplate: '',
        chart: null
    };

    var preparePage = function () {

        $('#btnRefesh').click(function () {
            runAnalysis(false);
        });

        var tableBody = $('#mainBody');
        settings.rowTemplate = tableBody.html();
        tableBody.html('');

        var tFoot = $('#mainFoot');
        settings.footTemplate = tFoot.html();
        tFoot.html('');

        var invalidsBody = $('#invalidsBody');
        settings.invalidsRowTemplate = invalidsBody.html();
        invalidsBody.html('');

        setTimeout(function () {
            runAnalysis(true);
        }, 0);
    };

    var runAnalysis = function (firstLoad) {
        ShowStatusDisplay('Analyzing ballots...', 0);

        CallAjaxHandler(publicInterface.controllerUrl + '/RunAnalyze', null, showInfo, firstLoad);
    };

    var showInfo = function (info, firstLoad) {
        var votesTable = $('table.Main');
        var invalidsTable = $('table#invalids');
        var table;

        if (info.Votes) {
            votesTable.show();
            invalidsTable.hide();
            table = votesTable;

            $('#mainBody').html(settings.rowTemplate.filledWithEach(expand(info.Votes)));

            //TODO: add foot info

            setTimeout(function () {
                $('#chart').show();
                showChart(info.Votes);
            }, 100);
        }
        else {
            table = invalidsTable;
            votesTable.hide();
            $('#chart').hide();
            invalidsTable.show();

            $('#invalidsBody').html(settings.invalidsRowTemplate.filledWithEach(expandInvalids(info.NeedReview)));
        }

        $('#totalCounts').find('span[data-name]').each(function () {
            var span = $(this);
            var value = info[span.data('name')] || '';
            span.text(value);
        });

        if (!firstLoad) {
            table.animate({
                opacity: 0.5
            }, 100, function () {
                table.animate({
                    opacity: 1
                }, 500);
            });
        }


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
                return i + 1;
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

    var expandInvalids = function (needReview) {
        $.each(needReview, function () {
            this.Ballot = '<a href="../Ballots?l={LocationId}&b={BallotId}">{Ballot}</a>'.filledWith(this);
            this.Link = this.PositionOnBallot;
        });
        return needReview;
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
        PreparePage: preparePage
    };

    return publicInterface;
};

var analyzePage = AnalyzePage();

$(function () {
    analyzePage.PreparePage();
});