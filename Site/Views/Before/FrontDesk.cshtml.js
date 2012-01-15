/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />

var FrontDeskPage = function () {
    var local = {
    };
    var preparePage = function () {
        $('#Main').on('click', '.Btn', voteBtnClicked);
    };

    var voteBtnClicked = function (ev) {
        var btn = $(ev.target);
        var row = btn.parent();
        var pid = row.attr('id').substr(1);

        var btnType = btn.hasClass('InPerson') ? 'P'
            : btn.hasClass('DroppedOff') ? 'D' : 'M';


        var form = {
            id: pid,
            type: btnType,
            last: publicInterface.lastRowVersion || 0
        };

        ShowStatusDisplay("Saving...");
        CallAjaxHandler(publicInterface.controllerUrl + '/RegisterVote', form, updatePeople, pid);
    };

    var updatePeople = function (info, pid) {
        ResetStatusDisplay();
        if (info) {
            if (info.PersonLines) {
                $.each(info.PersonLines, function () {
                    var selector = '#P' + this.PersonId;
                    $(selector).replaceWith(site.templates.FrontDeskLine.filledWith(this));
                    if (this.PersonId != pid) {
                        $(selector).effect('highlight', {}, 2000);
                    }
                });
            }
            if (info.LastRowVersion) {
                publicInterface.lastRowVersion = info.LastRowVersion;
            }
        }
    };

    var publicInterface = {
        controllerUrl: '',
        lastRowVersion: 0,
        PreparePage: preparePage
    };
    return publicInterface;
};

var frontDeskPage = FrontDeskPage();

$(function () {
    frontDeskPage.PreparePage();
});