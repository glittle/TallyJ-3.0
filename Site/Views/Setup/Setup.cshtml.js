/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/jquery-1.7.1.js" />

var SetupIndexPage = function () {
    var cachedRules = {
        // temporary cache of rules, for the life of this page
    };
    var settings = {
        locationTemplate: '<div><input data-id={C_RowId} type=text value="{Name}">  <span class="ui-icon ui-icon-arrow-2-n-s" title="Drag to sort"></span></div>'
    };
    var preparePage = function () {

        $('#ddlType').live('change keyup', startToAdjustByType);
        $('#ddlMode').live('change keyup', startToAdjustByType);

        $('#btnResetPeople').live('click', resetVoteStatuses);

        $('#btnSave').live('click', saveChanges);
        $('#btnAddLocation').live('click', addLocation);

        $('#locationList').live('change', 'input', locationChanged);

        $("#txtDate").datepicker({
            dateFormat: 'd MM yy'
        });


        $('#btnResetList').click(function () {
            ShowStatusDisplay('Resetting...');
            CallAjaxHandler(publicInterface.controllerUrl + '/ResetAll', null, function (info) {
                ResetStatusDisplay();
            });
        });

        applyValues(publicInterface.Election);
        showLocations(publicInterface.Locations);

        $('#txtName').focus();
    };

    var resetVoteStatuses = function () {
        ShowStatusDisplay('Updating...', 0);
        CallAjaxHandler(publicInterface.controllerUrl + '/ResetAll', null, function () {
            ShowStatusDisplay('Updated', 0, 3000, false, true);
        });
    };

    var showLocations = function (locations) {
        if (locations == null) {
            $('#locationList').html('[None]');
            return;
        }

        $('#locationList').html(settings.locationTemplate.filledWithEach(locations));

        setupLocationSortable();
    };

    var locationChanged = function (ev) {
        var input = $(ev.target);
        var form = {
            id: input.data('id'),
            text: input.val()
        };
        ShowStatusDisplay("Saving...");
        CallAjaxHandler(publicInterface.controllerUrl + '/EditLocation', form, function (info) {
            ShowStatusDisplay(info.Status, 0, 3000, false, true);

            if (info.Id == 0) {
                input.parent().remove();
            } else {
                input.val(info.Text);
                if (info.Id != form.id) {
                    input.data('id', info.Id);
                }
            }
        });
    };

    var setupLocationSortable = function () {
        $('#locationList').sortable({
            handle: '.ui-icon',
            stop: orderChanged
        });
    };
    var orderChanged = function (ev, ui) {
        var ids = [];
        $('#locationList input').each(function () {
            var id = $(this).data('id');
            if (+id < 1) {
                // an item not saved yet!?
            }
            ids.push(id);
        });
        var form = {
            ids: ids
        };
        ShowStatusDisplay("Saving...");
        CallAjaxHandler(publicInterface.controllerUrl + '/SortLocations', form, function (info) {
            ShowStatusDisplay("Saved", 0, 3000, false, true);
        });
    };

    var addLocation = function () {
        var location = {
            C_RowId: -1
        };
        var line = $(settings.locationTemplate.filledWith(location));
        line.appendTo('#locationList').find('input').focus();

        setupLocationSortable();
    };

    var applyValues = function (election) {
        if (election == null) {
            return;
        };

        $('.Demographics :input["data-name"]').each(function () {
            var input = $(this);
            var value = election[input.data('name')] || '';
            switch (input.attr('type')) {
                case 'date':
                    input.datepicker('setDate', ('' + value).parseJsonDate());
                    break;
                case 'checkbox':
                    input.prop('checked', value);
                    break;
                default:
                    input.val(value);
                    break;
            }
        });

        $('.CurrentElectionName').text(election.Name);

        startToAdjustByType();
    };

    var saveChanges = function () {
        var form = {
            C_RowId: publicInterface.Election ? publicInterface.Election.C_RowId : 0
        };

        $(':input["data-name"]').each(function () {
            var input = $(this);
            var value;
            switch (input.attr('type')) {
                case 'checkbox':
                    value = input.prop('checked');
                    break;
                default:
                    value = input.val();
                    break;
            }
            form[input.data('name')] = value;
        });

        ShowStatusDisplay("Saving...");
        CallAjaxHandler(publicInterface.controllerUrl + '/SaveElection', form, function (info) {
            if (info.Election) {
                applyValues(info.Election);
            }
            ResetStatusDisplay();
            ShowStatusDisplay(info.Status, 0, 3000, false, true);
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

        CallAjaxHandler(publicInterface.controllerUrl + '/DetermineRules', form, applyRules, combined);
    };

    function applyRules(info, combined) {
        var setRule = function (target, locked, value) {
            target.prop('disabled', locked);
            if (locked) {
                target.val(value);
            }
        };

        setRule($('#txtNames'), info.NumLocked, info.Num);
        setRule($('#txtExtras'), info.ExtraLocked, info.Extra);
        setRule($('#ddlCanVote'), info.CanVoteLocked, info.CanVote);
        setRule($('#ddlCanReceive'), info.CanReceiveLocked, info.CanReceive);

        cachedRules[combined] = info;
    }

    //  var buildPage = function () {
    //    $('#editArea').html(site.templates.ElectionEditScreen.filledWith(local.Election));
    //  };

    var publicInterface = {
        controllerUrl: '',
        Election: null,
        Locations: null,
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