/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />
/// <reference path="../../Scripts/highcharts.js" />

var PresenterPage = function () {
    var settings = {
        rowTemplate: '',
        footTemplate: '',
        invalidsRowTemplate: '',
        refreshTimeout: null,
        chart: null
    };

    var preparePage = function () {

        $('#btnRefesh').click(function () {
            getReportData(false);
        });

        var tableBody = $('#mainBody');
        settings.rowTemplate = tableBody.html();
        tableBody.html('');

        var tFoot = $('#mainFoot');
        settings.footTemplate = tFoot.html();
        tFoot.html('');

        setTimeout(function () {
            getReportData(true);
        }, 0);
    };

    var getReportData = function (firstLoad) {
        ShowStatusDisplay('Getting results...', 0);
        clearTimeout(settings.refreshTimeout);
        CallAjaxHandler(publicInterface.controllerUrl + '/GetReport', null, showInfo, firstLoad);
    };

    var showInfo = function (info, firstLoad) {
        var votesTable = $('table.Main');

        ResetStatusDisplay();

        // refresh again...
        settings.refreshTimeout = setTimeout(function () {
            getReportData();
        }, 1000 * 60);

        if (info.Status != 'Report') {
            $('#Results').hide();
            $('#Wait').show();
            $('#Status').text(info.StatusText);

            return;
        }

        $('#Results').show();
        $('#Wait').hide();

        if (info.ReportVotes) {

            votesTable.show();

            $('#mainBody').html(settings.rowTemplate.filledWithEach(expand(info.ReportVotes)));

            //TODO: add foot info

            setTimeout(function () {
                $('#chart').show();
                showChart(info.ChartVotes);
            }, 100);
        }
        else {
        }

        $('#totalCounts').find('span[data-name]').each(function () {
            var span = $(this);
            var value = info[span.data('name')];
            span.text(value);
        });

    };

    var showChart = function (info) {
        var maxToShow = 10; //TODO what is good limit?

        var getVoteCounts = function () {
            return $.map(info.slice(0, maxToShow), function (item, i) {
                return item.VoteCount;
            });
        };

        var getNames = function () {
            return $.map(info.slice(0, maxToShow), function (item, i) {
                return item.Rank;
            });
        };


        settings.chart = new Highcharts.Chart({
            chart: {
                renderTo: 'chart',
                type: 'bar'
            },
            title: {
                text: 'Top Vote Counts'
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

var presenterPage = PresenterPage();

$(function () {
    presenterPage.PreparePage();
});