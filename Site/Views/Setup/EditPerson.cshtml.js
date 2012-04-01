/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />
/// <reference path="../../Scripts/jquery-1.7.1.js" />

var EditPersonPage = function () {
    var local = {
        inputField: null,
        hostPanel: null
    };

    var applyValues = function (panel, person) {
        if (panel && panel != null) {
            local.hostPanel = panel;
        }
        else {
            panel = local.hostPanel;
        }
        panel.find(':input[data-name]').each(function () {
            var input = $(this);
            var value = person[input.data('name')] || '';
            switch (input.attr('type')) {
                case 'checkbox':
                    input.prop('checked', value);
                    break;
                default:
                    input.val(value);
                    break;
            }
        });

        panel.find('[data-name="FirstName"]').focus();

        //TODO...
        //        if (publicInterface.defaultRules.CanVoteLocked) {
        //            $('[data-name="CanVote"]').prop('enabled', false).propertyIsEnumerable('checked', publicInterface.defaultRules.CanVote=='A');
        //        }
        //        if (publicInterface.defaultRules.CanReceiveLocked) {
        //            $('[data-name="CanReceiveVote"]').prop('enabled', false).propertyIsEnumerable('checked', publicInterface.defaultRules.CanReceive=='A');
        //        }

        //        panel.fadeIn();
        //        panel.find('[data-name="FirstName"]').focus();
    };

    var saveChanges = function () {
        var form = {};
        $(':input[data-name]').each(function () {
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
        CallAjaxHandler(publicInterface.controllerUrl + '/SavePerson', form, function (info) {
            if (info.Person) {
                applyValues(null, info.Person);

                site.broadcast(site.broadcastCode.personSaved);
            }
            ShowStatusDisplay(info.Status, 0, null, false, true);
        });
    };


    var preparePage = function () {
        $('#btnSave').live('click', saveChanges);
        $('#ddlIneligible').html(prepareReasons());
    };

    var prepareReasons = function () {
        var html = ['<option value="">Ineligible reasons...</option>'];
        var group = '';
        $.each(publicInterface.invalidReasons, function () {
            var reasonGroup = this.Group;
            if (reasonGroup != group) {
                if (group) {
                    html.push('</optgroup>');
                }
                html.push('<optgroup label="{0}">'.filledWith(reasonGroup));
                group = reasonGroup;
            }
            html.push('<option value="{Guid}">{Desc}</option>'.filledWith(this));
        });
        html.push('</optgroup>');
        return html.join('\n');
    };

    var publicInterface = {
        peopleUrl: '',
        invalidReasons: [],
        defaultRules: null,
        applyValues: applyValues,
        PreparePage: preparePage
    };
    return publicInterface;
};

var editPersonPage = EditPersonPage();

$(function () {
    editPersonPage.PreparePage();
});