/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />
/// <reference path="../../Scripts/highcharts/highcharts.js" />

var AnalyzePage = function () {
    var settings = {
        rowTemplate: '',
        footTemplate: '',
        invalidsRowTemplate: '',
        chart: null
    };

    var preparePage = function () {

        $('#btnRefresh').click(function () {
            runAnalysis(false);
        });
        $('#chkShowAll').on('click change', function () {
            ShowStatusDisplay('Updating...', 0);
            CallAjaxHandler(publicInterface.controllerUrl + '/UpdateElectionShowAll', {
                showAll: $(this).prop('checked')
            }, function () {
                ShowStatusDisplay('Updated', 0, 3000, false, true);
            });
        });

        $('#ddlElectionStatus').on('change', function () {
            ShowStatusDisplay('Updating...', 0);
            CallAjaxHandler(publicInterface.controllerUrl + '/UpdateElectionStatus', {
                status: $(this).val()
            }, function () {
                ShowStatusDisplay('Updated', 0, 3000, false, true);
            });
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

        if (publicInterface.results) {
            showInfo(publicInterface.results, true);
        }
        else {
            setTimeout(function () {
                runAnalysis(true);
            }, 0);
        }
    };

    var runAnalysis = function (firstLoad) {
        ShowStatusDisplay('Analyzing ballots...', 0);

        CallAjaxHandler(publicInterface.controllerUrl + '/RunAnalyze', null, showInfo, firstLoad);
    };

    var showInfo = function (info, firstLoad) {
        var votesTable = $('table.Main');
        var invalidsTable = $('table#invalids');
        var table;

        $('#InitialMsg').hide();
        ResetStatusDisplay();

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
            var value = info[span.data('name')];
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
                type: 'column'
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
            }],
            tooltip: {
                formatter: function () {
                    var s = 'Rank {0}: {1} vote{2}'.filledWith(this.x, this.y, this.y == 1 ? '' : 's');

                    return s;
                }
            }
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
            this.ClassName = 'Section{0} {1} {2} {3}'.filledWith(
                this.Section,
                this.Section=='O' && this.ForceShowInOther ? 'Force' : '',
                (i % 2 == 0 ? 'Even' : 'Odd'),
                (this.IsTied && this.TieBreakRequired && !this.IsTieResolved ? 'Tied' : ''));
            this.TieVote = this.IsTied ? (this.TieBreakRequired ? ('Tie Break ' + this.TieBreakGroup) : '(Tie Okay)') : '';
        });
        return results;
    };

    var publicInterface = {
        controllerUrl: '',
        PreparePage: preparePage,
        results: null
    };

    return publicInterface;
};

var analyzePage = AnalyzePage();

$(function () {
    analyzePage.PreparePage();
});