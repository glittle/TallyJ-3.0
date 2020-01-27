var ShowTies = function () {
  //  var local = {
  //  };

  var preparePage = function () {

    if (showTies.ties.length === 1) {
      displayNamesInfoAndNames(0, showTies.tieInfo);
    } else {
      $(document.body).on('keypress', function(ev) {
        var key = +ev.key;
        if (!isNaN(key)) {
          $('#btn' + key).click();
        }
      });
    }

    //    $('#selTieBreaks').change(function () {
    //      this.size = 1;
    //      var tieBreakGroup = $(this).val();
    //      if (tieBreakGroup) {
    //        var index = this.selectedIndex - 1;
    //        showTie(tieBreakGroup, index);
    //      } else {
    //        $('#names').html('');
    //      }
    //    });

    $('#btnReturn').click(clickReturn);


    if (!$('#Wait').is(':visible')) {
      setTimeout(function () {
        $('.Nav').animate({ opacity: 0 }, 2500, null, function () {
          $('.Nav').removeClass('Show').css({
            opacity: ''
          });
        });
      }, 1500);
    } else {
      clickReturn();
    }
  };

  function showTie(tieBreakGroup, index) {
    CallAjaxHandler(showTies.getTies,
      { tieBreakGroup: tieBreakGroup },
      function (info) {
        displayNamesInfoAndNames(index, info);
      });

    $('.tieBtn').removeClass('btn-success');
    $('.tieBtn#btn' + (index + 1)).addClass('btn-success');
    $('.tieBtnSelect').hide();
  }

  function clickReturn() {
    var isShowing = $('header').is(':visible');
    $('header').toggle(!isShowing);

    isShowing = !isShowing;
    $('#btnReturn').text(isShowing ? 'Hide Menu' : 'Show Menu');
    $('.Nav').toggleClass('Show', isShowing);
    return false;
  };

  function displayNamesInfoAndNames(tieIndex, info) {
    var tie = showTies.ties[tieIndex];
    var normal = true;
    var num = tie.NumToElect;
    $.each(info, function (i, el) {
      el.html = '<div class=name>{0}</div>'.filledWith(el.name);
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

    $('#TieIntro').html('Vote for {0} of these individuals'.filledWith(num));

    var numPerColumn = Math.ceil(info.length / 2);
    var manualColumns = '<div>{^0}</div><div>{^1}</div>'.filledWith(
      info.slice(0, numPerColumn).map(i => i.html).join(''),
      info.slice(numPerColumn).map(i => i.html).join(''));

    $('#names')
      .html(manualColumns)
      .toggleClass('manyNames', info.length > 10);
  }

  var publicInterface = {
    preparePage: preparePage,
    showTie: showTie
  };

  return publicInterface;
};

var showTies = ShowTies();

$(function () {
  showTies.preparePage();
});