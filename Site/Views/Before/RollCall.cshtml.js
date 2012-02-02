/// <reference path="../../Scripts/site.js" />
/// <reference path="../../Scripts/PeopleHelper.js" />
/// <reference path="../../Scripts/jquery-1.7-vsdoc.js" />

var RollCallPage = function () {
    var local = {
        currentNameNum: 0,
        nameDivs: []
    };
    var preparePage = function () {

        var main = $('.Main');
        local.nameDivs = main.children('div.Voter');

        scrollToMe(local.nameDivs[0]);

        $(document).keydown(keyPressed);

        //        main.animate({
        //            marginTop: '0%'
        //        }, 5000, 'linear', function () {
        //            // Animation complete.
        //        });

        $('#btnReturn').click(function () {
            location.href = site.rootUrl + 'Dashboard';
            return false;
        });
    };

    var keyPressed = function (ev) {
        var delta = 1;
        switch (ev.which) {
            case 75: // k
            case 38: // up
                delta = -1;
                ev.preventDefault();
                break;

            case 33: // page up
                delta = -4;
                ev.preventDefault();
                break;

            case 32: // space
            case 74: // j
            case 40: // down
                delta = 1;
                ev.preventDefault();
                break;

            case 34: // page down
                delta = 4;
                ev.preventDefault();
                break;

            default:
                LogMessage(ev.which);
                return;
        }
        var num = local.currentNameNum;

        if (num + delta >= 0 && num + delta < local.nameDivs.length) {
            local.currentNameNum += delta;
            scrollToMe(local.nameDivs[local.currentNameNum]);

            //            if (local.currentNameNum > 0) {
            //                $(local.nameDivs[local.currentNameNum]).animate({ opacity: delta < 0 ? 0 : 100 }, 200);
            //            }
        }
    };

    var scrollToMe = function (nameDiv) {
        var top = $(nameDiv).offset().top;
        var fudge = 84;
        $('html,body').animate({
            scrollTop: top + fudge
        }, 800);
    };

    //    var goFullScreen = function (div) {
    //        if (div.webkitRequestFullScreen) {
    //            div.webkitRequestFullScreen(Element.ALLOW_KEYBOARD_INPUT);
    //        }

    //        if (div.mozRequestFullScreen) {
    //            div.mozRequestFullScreen();
    //        }
    //    };

    var publicInterface = {
        controllerUrl: '',
        PreparePage: preparePage
    };
    return publicInterface;
};

var rollCallPage = RollCallPage();

$(function () {
    rollCallPage.PreparePage();
});