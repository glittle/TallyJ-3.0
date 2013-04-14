/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7.1.js" />
/// <reference path="../../Scripts/jquery-ui-1.8.16.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />

var ReconcilePageFunc = function () {
    var local = {
        ballotListTemplate: '<div id=B{Id}>{Code} - <span id=BallotStatus{Id}>{StatusCode}</span></div>',
        sortedBallots: {},
        currentLocation: -1,
        showingNames: false,
        ballotMethods: []
    };

    var preparePage = function () {
        $('#btnShowNames').click(function () {
            $(this).hide();
            $('.Names, #lists').fadeIn();
            local.showingNames = true;
        });
        $('#locations').change(changeLocation);

        processBallots(publicInterface.ballots);
    };

    var changeLocation = function () {
        var newLocation = $(this).val();
        if (newLocation != local.currentLocation) {
            ShowStatusDisplay('Loading ballot information');
            CallAjaxHandler(publicInterface.controllerUrl + '/BallotsForLocation', { id: newLocation }, function (info) {
                local.currentLocation = newLocation;
                processBallots(info.Ballots);
                ResetStatusDisplay();
            });
        }
    };

    var processBallots = function (ballots) {
        local.sortedBallots = {};
        local.ballotMethods = [];

        for (var i = 0; i < ballots.length; i++) {
            var ballot = ballots[i];
            var method = ballot.VotingMethod;

            var list = local.sortedBallots[method];
            if (!list) {
                list = local.sortedBallots[method] = [];
                local.ballotMethods.push(method);
            }

            list.push(ballot);
        }

        var methodInfos = {
            P: { name: 'In Person', count: 0 },
            D: { name: 'Dropped Off', count: 0 },
            M: { name: 'Mailed In', count: 0 },
            C: { name: 'Called In', count: 0 }
        };
        var methodList = ['P', 'D', 'M', 'C'];

        var host = $('#lists');
        host.html('');

        for (var j = 0; j < methodList.length; j++) {
            method = methodList[j];
            var methodInfo = methodInfos[method] || { name: '???', count: 0 };

            var methodName = methodInfo.name;

            var sortedBallots = local.sortedBallots[method] || null;
            if (sortedBallots) {
                var ballotList = '<div title="{Tellers}"><span>{C_FullName}</span><span class=When>{When}{#("{Tellers}"==""?"":" (T)")}</span>{#("{EnvNum}"=="") ? "" : "<span class=EnvNum>#{EnvNum}</span>"}</div>'.filledWithEach(extend(sortedBallots));
                host.append('<div data-method={0}><h3>{1}: {2}</h3><div class=Names>{^3}</div></div>'.filledWith(
                    method, methodName, sortedBallots.length, ballotList));

                methodInfo.count = sortedBallots.length;
            }
        }

        // show totals
        //        var html = [];
        //        var template = '<tr class="{className}"><td>{name}</td><td>{count}</td></tr>';
        //        html.push(template.filledWith(methodInfos['M']));
        //        html.push(template.filledWith(methodInfos['D']));
        //        html.push(template.filledWith(methodInfos['C']));

        var totals = {
            absent: methodInfos['M'].count + methodInfos['D'].count + methodInfos['C'].count,
            inPerson: methodInfos['P'].count
        };
        totals.total = totals.inPerson + totals.absent;

        $('#Totals').html([
                'Total: {total}'.filledWith(totals),
                'Absent: {absent}'.filledWith(totals),
                (methodInfos.P.name + ': {0}'.filledWith(methodInfos.P.count)).bold(),
                (methodInfos.D.name + ': {0}'.filledWith(methodInfos.D.count)).bold(),
                (methodInfos.M.name + ': {0}'.filledWith(methodInfos.M.count)).bold(),
                (methodInfos.C.count > 0 ? (methodInfos.C.name + ': {0}'.filledWith(methodInfos.C.count)) : '').bold()
            ].join(' &nbsp; &nbsp; '));

        //        html.push(template.filledWith({ className: 'SubTotal', name: 'Absentee Ballots', count: subTotal }));

        //        html.push(template.filledWith(methodInfos['P']));

        //        html.push(template.filledWith({ className: 'Total', name: 'Total', count: subTotal + methodInfos['P'].count }));

        //        $('#Totals').html('<table>{^0}</table>'.filledWith(html.join('')));


        if (local.showingNames) {
            $('.Names').fadeIn();
        }
    };

    var extend = function (ballots) {
        if (!ballots) return null;
        $.each(ballots, function () {
            //this.WhenText = FormatDate(this.When, ' ', true, true);
        });
        return ballots;
    };

    var publicInterface = {
        controllerUrl: '',
        preparePage: preparePage,
        ballots: []
    };

    return publicInterface;
};

var reconcilePage = ReconcilePageFunc();

$(function () {
    reconcilePage.preparePage();
});