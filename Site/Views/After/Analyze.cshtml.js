/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7.1.js" />
/// <reference path="../../Scripts/highcharts/highcharts.js" />

var AnalyzePage = function () {
    var settings = {
        rowTemplate: '',
        info: {},
        footTemplate: '',
        invalidsRowTemplate: '',
        tieResultRowTemplate: '',
        chart: null,
        hasCloseVote: false,
        hasTie: false
    };

    var preparePage = function () {

        $('#btnRefresh').click(function () {
            runAnalysis(false);
        });
        //        $('#chkShowAll').on('click change', function () {
        //            ShowStatusDisplay('Updating...', 0);
        //            CallAjaxHandler(publicInterface.controllerUrl + '/UpdateElectionShowAll', {
        //                showAll: $(this).prop('checked')
        //            }, function () {
        //                ShowStatusDisplay('Updated', 0, 3000, false, true);
        //            });
        //        });

        $('#body').on('change', '#ddlElectionStatus', function () {
            ShowStatusDisplay('Updating...', 0);
            CallAjaxHandler(publicInterface.controllerUrl + '/UpdateElectionStatus', {
                status: $(this).val()
            }, function () {
                ShowStatusDisplay('Updated', 0, 3000, false, true);
            });
        });

        $('#body').on('click', '.btnSaveTieCounts', saveTieCounts);

        var tableBody = $('#mainBody');
        settings.rowTemplate = tableBody.html();
        tableBody.html('');

        var tFoot = $('#mainFoot');
        settings.footTemplate = tFoot.html();
        tFoot.html('');

        var invalidsBody = $('#invalidsBody');
        settings.invalidsRowTemplate = invalidsBody.html();
        invalidsBody.html('');


        var tieResultsRowTemplate = $('#tieResultsBody');
        settings.tieResultRowTemplate = tieResultsRowTemplate.html();
        tieResultsRowTemplate.html('');

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
        $('.LeftHalf, .RightHalf').fadeOut();

        CallAjaxHandler(publicInterface.controllerUrl + '/RunAnalyze', null, showInfo, firstLoad);
    };

    var showInfo = function (info, firstLoad) {
        var votesTable = $('table.Main');
        var invalidsTable = $('table#invalids');
        var table;

        settings.info = info;

        $('#InitialMsg').hide();
        $('#tieResults').hide();
        $('#HasCloseVote').hide();

        ResetStatusDisplay();

        if (info.Votes) {
            table = votesTable;
            votesTable.show();
            invalidsTable.hide();

            $('#mainBody').html(settings.rowTemplate.filledWithEach(expand(info.Votes)));
            showTies(info);

            $('#HasCloseVote').toggle(settings.hasCloseVote);
            $('.HasTie').toggle(settings.hasTie);
            //LogMessage(settings.hasTie);

            if (info.Votes.length != 0) {
                var max = info.Votes[0].VoteCount;

                $('.ChartLine').each(function() {
                    var item = $(this);
                    item.animate({
                            width: (item.data('value') / max * 100) + '%'
                        }, {
                            duration: 2000
                        });
                });
            }

            //            setTimeout(function () {
            //                $('#chart').show();
            //                showChart(info.Votes);
            //            }, 100);
        }
        else {
            table = invalidsTable;
            votesTable.hide();
            invalidsTable.show();
            $('#chart').hide();

            $('#invalidsBody').html(settings.invalidsRowTemplate.filledWithEach(expandInvalids(info.NeedReview)));
        }

        $('#totalCounts').find('span[data-name]').each(function () {
            var span = $(this);
            var value = info[span.data('name')];
            span.text(value);
        });

        table.show();
        $('.LeftHalf, .RightHalf').fadeIn();

    };

    var showChart = function (votes) {
        var numToElect = settings.info.NumToElect;
        var numExtra = settings.info.NumExtra;

        var maxToShow = (numToElect + numExtra) * 1.5;

        var getVoteCounts = function () {
            var determineColor = function (item, i) {
                //                if (item.TieBreakRequired) {
                //                    return '#F' + item.TieBreakGroup + item.TieBreakGroup; // works for groups A-F
                //                }
                //                else 
                if (i < numToElect) {
                    return 'green';
                }
                else if (i < numToElect + numExtra) {
                    return 'orange';
                }
                else {
                    return '#ccc';
                }
            };


            return $.map(votes.slice(0, maxToShow), function (item, i) {
                return {
                    name: (i + 1), //TODO: name not showing?
                    color: determineColor(item, i),
                    y: item.VoteCount
                };
            });
        };

        var getNames = function () {
            return $.map(votes.slice(0, maxToShow), function (item, i) {
                return item.Rank;
            });
        };


        settings.chart = new Highcharts.Chart({
            chart: {
                renderTo: 'chart',
                type: 'bar'
            },
            title: {
                text: 'Votes Received'
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
                    var s = 'Position {0}: {1} vote{2}'.filledWith(this.x, this.y, this.y == 1 ? '' : 's');
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
        settings.hasCloseVote = false;
        $.each(results, function (i) {
            this.ClassName = 'Section{0} {1} {2} {3}'.filledWith(
                this.Section,
                this.Section == 'O' && this.ForceShowInOther ? 'Force' : '',
                (i % 2 == 0 ? 'Even' : 'Odd'),
                (this.IsTied && this.TieBreakRequired ? (this.IsTieResolved ? 'Resolved' : 'Tied') : ''));
            this.TieVote = this.IsTied ? (this.TieBreakRequired ? ('Tie Break ' + this.TieBreakGroup) : '(Tie ' + this.TieBreakGroup + ' Okay)') : '';
            if (this.CloseToNext) {
                this.CloseUpDown = this.CloseToPrev ? '&#8597;' : '&#8595;';
            } else if (this.CloseToPrev) {
                this.CloseUpDown = '&#8593;';
            }
            if ((this.Section == 'T' || this.Section == 'E')
                && (this.CloseToNext || this.CloseToPrev)) {
                settings.hasCloseVote = true;
            }
            this.VoteDisplay = this.VoteCount + (this.TieBreakCount ? ', ' + this.TieBreakCount : '');
        });
        return results;
    };

    var showTies = function (info) {
        var votes = info.Votes;
        var groups = info.Ties;

        var addConclusions = function (items) {
            $.each(items, function () {
                var tie = this;
                if (!tie.TieBreakRequired) {
                    tie.Conclusion = 'This tie does not need to be resolved, as it has no impact on the results of the election.';
                }
                else {
                    var firstPara;
                    if (tie.IsResolved) {
                        firstPara = '<p>This tie has been resolved.</p>';
                    }
                    else {
                        tie.rowClass = 'TieBreakNeeded';
                        firstPara = '<p>A tie-break election is required to break this tie.</p>';
                    }
                    tie.Conclusion = firstPara
                        + '<p>Voters must vote for <span class=Needed>{0}</span> {1} from this list of {2}. If the tie-break vote has been completed, enter the number of votes recieved by each person below.</p>'
                            .filledWith(tie.NumToElect, tie.NumToElect == 1 ? 'person' : 'people', tie.NumInTie);
                    var list = $.map(votes, function (v) {
                        return v.TieBreakGroup == tie.TieBreakGroup ? v : null;
                    });
                    tie.People = '<div><input data-rid="{rid}" class=TieBreakCount type=number value="{TieBreakCount}">{PersonName}</div>'.filledWithEach(list.sort(function (a, b) {
                        if (a.PersonName < b.PersonName) return -1;
                        if (a.PersonName > b.PersonName) return 1;
                        return 0;
                    }));
                    tie.Buttons = '<button type=button class=btnSaveTieCounts>Save Counts & Re-run Analysis</button>';
                }
            });
            return items;
        };

        if (groups.length == 0) {
            $('#tieResults').hide();
            settings.hasTie = false;
        } else {
            $('#tieResults').show();
            var tbody = $('#tieResultsBody');
            tbody.html(settings.tieResultRowTemplate.filledWithEach(addConclusions(groups)));
            settings.hasTie = true;
        }

    };

    var saveTieCounts = function () {
        var btn = $(this);
        var counts = btn.parent().find('input');
        var needed = +btn.parent().find('.Needed').text();
        var dups = [];
        var foundDup = false;
        var foundOkay = 0;
        var foundNegative = false;

        var values = $.map(counts, function (item) {
            var $item = $(item);
            var value = +$item.val();
            if (value > 0) {
                if (dups[value]) {
                    foundDup = true;
                }
                else {
                    foundOkay++;
                }
                dups[value] = (dups[value] ? dups[value] : 0) + 1;
            }
            if (value < 0) {
                foundNegative = true;
            }
            return $item.data('rid') + '_' + value;
        });
        if (foundNegative) {
            alert('All vote counts must be a positive number.');
            return;
        }
        if (foundDup) {
            var foundBeforeDup = 0;
            for (var i = dups.length - 1; i >= 0; i--) {
                var dup = dups[i];
                if (dup > 1) break;
                foundBeforeDup = foundBeforeDup + dup; // will be 1 or 0
            }
            if (foundBeforeDup < needed) {
                alert('A tie has been entered within the top {0} vote counts.\n\nWhen the tie-breaking vote is done, tied results cannot be accepted. Please resolve those tied votes.'.filledWith(needed));
            }
        }
        if (foundOkay < needed) {
            alert('Please ensure that {0} or more votes are entered.'.filledWith(needed));
        }
        var form = {
            counts: values
        };
        ShowStatusDisplay("Saving...", 0);
        CallAjaxHandler(publicInterface.controllerUrl + '/SaveTieCounts', form, function (info) {
            ShowStatusDisplay("Saved", 0, 3000, false, true);
            runAnalysis(false);
        });
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