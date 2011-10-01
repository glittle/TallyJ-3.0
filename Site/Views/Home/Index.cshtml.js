/// <reference path=".././../Scripts/site.js" />
/// <reference path=".././../Scripts/jquery-1.6.4-vsdoc.js" />

var HomeIndexPage = function () {
    var localSettings = {
    };
    var publicInterface = {
        PreparePage: function () {
            $('section.feature').live('click', function () {
                location.href = $(this).find('h3 a').eq(0).attr('href');
            });
        }
    };

    return publicInterface;
};

var homeIndexPage = HomeIndexPage();

$(function () {
    homeIndexPage.PreparePage();
});