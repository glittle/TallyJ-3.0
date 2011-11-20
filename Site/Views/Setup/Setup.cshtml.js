﻿/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />

var SetupIndexPage = function () {
    var cachedRules = {
        // temporary cache of rules, for the life of this page
    };

    var preparePage = function () {

        $('#ddlType').live('change keyup', startToAdjustByType);
        $('#ddlMode').live('change keyup', startToAdjustByType);

        $('#btnSave').live('click', saveChanges);

        $("#txtDate").datepicker({
            dateFormat: 'd MM yy'
        });

        applyValues(publicInterface.Election);

        $('#txtName').focus();
    };

    var applyValues = function (election) {
        if (election == null) {
            return;
        };

        $(':input["data-name"]').each(function () {
            var input = $(this);
            var value = election[input.data('name')] || '';
            if (input.attr('type') == 'date') {
                input.datepicker('setDate', ('' + value).parseJsonDate());
            }
            else {
                input.val(value);
            }
        });

        startToAdjustByType();
    };

    var saveChanges = function () {
        var form = {
            C_RowId: publicInterface.Election ? publicInterface.Election.C_RowId : 0
        };

        $(':input["data-name"]').each(function () {
            var input = $(this);
            form[input.data('name')] = input.val();
        });

        ShowStatusDisplay("Saving...");
        CallAjaxHandler(publicInterface.setupUrl + '/SaveElection', form, function (info) {
            if (info.Election) {
                var election = adjustElection(info.Election);
                applyValues(election);
            }
            ShowStatusDisplay(info.Status, 0);
        });
    };

    var startToAdjustByType = function () {

        var type = $('#ddlType').val();
        var mode = $('#ddlMode').val();

        if (type == 'Con') {
            if (mode == "B") {
                mode = "N";
                $("#ddlMode").val("N");
            }
            $("#modeB").attr("disabled", "disabled");
        }
        else {
            $("#modeB").removeAttr("disabled");
        }
        var combined = type + '.' + mode;
        var cachedRule = cachedRules[combined];
        if (cachedRule) {
            applyRules(cachedRule);
            return;
        }

        var form = {
            type: type,
            mode: mode
        };

        CallAjaxHandler(publicInterface.setupUrl + '/DetermineRules', form, applyRules, combined);
    };

    function applyRules(info, combined) {
        $('#txtNames').prop('disabled', info.NumLocked).val(info.Num);
        $('#txtExtras').prop('disabled', info.ExtraLocked).val(info.Extra);
        $('#ddlCanVote').prop('disabled', info.CanVoteLocked).val(info.CanVote);
        $('#ddlCanReceive').prop('disabled', info.CanReceiveLocked).val(info.CanReceive);

        cachedRules[combined] = info;
    }

    //  var buildPage = function () {
    //    $('#editArea').html(site.templates.ElectionEditScreen.filledWith(local.Election));
    //  };

    var publicInterface = {
        setupUrl: '',
        Election: null,
        initialRules: function (type, mode, info) {
            var combined = type + '.' + mode;
            cachedRules[combined] = info;
        },
        PreparePage: preparePage
    };

    return publicInterface;
};

var setupIndexPage = SetupIndexPage();

$(function () {
    setupIndexPage.PreparePage();
});