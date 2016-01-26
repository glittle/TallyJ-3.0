var ShowTies = function () {
  var local = {
  };

  var preparePage = function () {

    if (showTies.ties.length == 1) {
      displayNamesInfoAndNames(0, showTies.tieInfo);
    }

    $('#selTieBreaks').change(function () {
      var index = this.selectedIndex - 1;
      var tieBreakGroup = $(this).val();

      CallAjaxHandler(showTies.getTies, { tieBreakGroup: tieBreakGroup }, function (info) {
        displayNamesInfoAndNames(index, info);
      });
    });

    $('#btnReturn').click(clickReturn);


    if (!$('#Wait').is(':visible')) {
      setTimeout(function () {
        $('.Nav').animate({ opacity: 0 }, 1500, null, function () {
          $('.Nav').removeClass('Show').css({
            opacity: ''
          });
        });
      }, 1000);
    } else {
      clickReturn();
    }
  };

  var clickReturn = function () {
    var isShowing = $('header').is(':visible');
    $('header').toggle(!isShowing);

    isShowing = !isShowing;
    $('#btnReturn').text(isShowing ? 'Hide Menu' : 'Show Menu');
    $('.Nav').toggleClass('Show', isShowing);
    return false;
  };
  var displayNamesInfoAndNames = function (tieIndex, info) {
    var tie = showTies.ties[tieIndex];
    var normal = true;
    var num = tie.NumToElect;
    $.each(info, function (i, el) {
      if (el.isResolved) {
        normal = false;
        return false;
      }
    });

    // if this tie-break has been voted on, and some results have been entered, only show those that have not been resolved yet.
    // this should only be two or three, so ask people to vote for only one of them, regardless of how many are in the Top or Extra section.
    if (!normal) {
      num = 1;
      info = $.grep(info, function (el) {
        return !el.isResolved;
      });
    }

    $('#TieIntro').html('Vote for any {0} of these individuals:'.filledWith(num));

    $('#names')
      .html('<div class=name>{name}</div>'.filledWithEach(info))
      .toggleClass('manyNames', info.length > 10);
  }

  var publicInterface = {
    preparePage: preparePage
  };

  return publicInterface;
};

var showTies = ShowTies();

$(function () {
  showTies.preparePage();
});