var HomeIndexPage = function () {
  //  var local = {
  //    uploader: null
  //  };
  var clearElectionRelatedStorageItems = function () {
    for (var key in localStorage) {
      if (key.substr(0, 5) == 'name_') {
        localStorage.removeItem(key);
      } else if (key == 'ActiveUploadRowId') {
        localStorage.removeItem(key);
      }
    }

  };
  var preparePage = function () {
    $(document).on('click', '.btnSelectElection', null, selectElection);

    showElections(publicInterface.elections);

    if (!publicInterface.isGuest) {
      //            $(document).on('click', '.btnCopyElection', null, copyElection);
      $(document).on('click', '.btnExport', null, exportElection);
      $(document).on('click', '.btnDelete', null, deleteElection);
      $(document).on('click', '#btnLoad', null, loadElection);
      $(document).on('click', '#file-uploader', null, loadElection2);
      $(document).on('click', '#btnCreate', null, createElection);
      $(document).on('change', '#loadFile', null, upload2);

      $(document).on('click', '#btnUpload2', null, upload2);

      //            $('#loadFile2').iframePostForm({
      //                iframeID: 'glen1',
      //                json: true,
      //                post: function() {
      //                    LogMessage('in post');
      //                },
      //                complete: function(response) {
      //                    LogMessage('in complete');
      //                    LogMessage(response);
      //                    debugger;
      //                }
      //            });

      //            local.uploader = new qq.FileUploader({
      //                element: $('#file-uploader')[0],
      //                action: publicInterface.controllerUrl + '/LoadElection',
      //                allowedExtensions: ['xml'],
      //                template: '<div class="qq-uploader">' +
      //                    '<div class="qq-upload-drop-area"><span>Drop files here to upload</span></div>' +
      //                    '<button type=button class="qq-upload-button" title="Load an election from a previously saved file">Load from a File</button>' +
      //                    '<ul class="qq-upload-list"></ul>' +
      //                    '</div>',
      //                fileTemplate: '<li style="display:none">' +
      //                    '<span class="qq-upload-file"></span>' +
      //                    '<span class="qq-upload-spinner"></span>' +
      //                    '<span class="qq-upload-size"></span>' +
      //                    '<a class="qq-upload-cancel" href="#">Cancel</a>' +
      //                    '<span class="qq-upload-failed-text">Failed</span>' +
      //                    '</li>',
      //                onSubmit: function(id, fileName) {
      //                    ShowStatusDisplay('Loading...');
      //                },
      //                onProgress: function(id, fileName, loaded, total) {
      //                    return false;
      //                },
      //                onComplete: function(id, fileName, info) {
      //                    ResetStatusDisplay();
      //                    if (info.success) {
      //                        getUploadsList();
      //                        if (info.rowId) {
      //                        }
      //                    } else {
      //                        if (info && info.messages) {
      //                            ShowStatusFailed(info.messages);
      //                        } else {
      //                            ShowStatusFailed('unknown error<select onfocus="this.blur();">');
      //                        }
      //                    }
      //                },
      //                onCancel: function(id, fileName) {
      //                    ResetStatusDisplay();
      //                },
      //                showMessage: function(message) { ShowStatusFailed(message); }
      //            });

      connectToImportHub();

    } else {
      $('.btnExport, .btnDelete, #btnLoad, #btnCreate').hide();
    }
  };

  var connectToImportHub = function () {
    var hub = $.connection.importHubCore;

    hub.client.loaderStatus = function (msg, isTemp) {
      if (isTemp) {
        $('#tempLog').html(msg);
      } else {
        $('#tempLog').html('');
        $('#log').append('<div>' + msg + '</div>');
      }
    };

    activateHub(hub, function () {
      LogMessage('Join import Hub');
      CallAjaxHandler(chooseElectionPage.importHubUrl, { connId: site.signalrConnectionId }, function (info) {

      });
    });
  };



  var upload2 = function () {
    var $input = $('#loadFile');
    if ($input.val() == '') {
      return;
    }

    ShowStatusDisplay("Loading election...");

    $('#loadingLog').show();
    $('#log').html('');
    $('#tempLog').html('Uploading the file to the server');

    var form = $('#formLoadFile');
    var frameId = 'tempUploadFrame';
    var frame = $('#' + frameId);
    if (!frame.length) {
      $('body').append('<iframe id=' + frameId + ' name=' + frameId + ' style="display:none" />');
      frame = $('#' + frameId);
    }
    form.attr({
      target: frameId,
      action: publicInterface.loadElectionUrl,
      enctype: 'multipart/form-data',
      method: 'post'
    });

    var frameObject = frame.load(function () {
      frameObject.unbind('load');
      $input.val(''); // blank out file name

      var response = frameObject.contents().text();
      var info;
      try {
        info = $.parseJSON(response);
      } catch (e) {
        info = { Success: false, Message: "Unexpected server message" };
      }

      if (info.Success) {
        showElections(info.Elections);
        ShowStatusSuccess('Loaded');

        var newRow = $('div.Election[data-guid="{0}"]'.filledWith(info.ElectionGuid));
        scrollToMe(newRow);

        //                newRow.addClass('justloaded');
        //                setTimeout(function () {
        //                    newRow.removeClass('justloaded');
        //                }, 6000);


        //                var form2 =
        //                    {
        //                        guid: info.ElectionGuid
        //                    };
        //                ShowStatusDisplay("Selecting election...");
        //                CallAjaxHandler(publicInterface.electionsUrl + '/SelectElection', form2, afterSelectElection);
      }
      else {
        ShowStatusFailed(info.Message);
      }
    });

    form.submit();
  };

  var showElections = function (info) {
    $.each(info, function () {
      if (this.Locations) {
        this.Locations = site.templates.LocationSelectItem.filledWithEach(this.Locations);
      }
      this.TestClass = this.IsTest ? ' TestElection' : '';
    });

    $('#ElectionList').html(site.templates.ElectionListItem.filledWithEach(info));

    if (publicInterface.isGuest) {
      $('#ElectionList').find('.Detail button').each(function () {
        $(this).prop('disabled', true);
      });
    }

    /* - old template:
    <div class="SelectLocation">
    Select the location you are at...</div>
    <div class="Locations">
    {Locations}
    </div>
    */
  };

  var selectElection = function () {
    if (publicInterface.isGuest) return;

    var btn = $(this);
    var row = btn.parents('.Election');
    var guid = row.data('guid');
    var form =
        {
          guid: guid,
          oldComputerGuid: GetFromStorage('compcode_' + guid, null)
        };

    ShowStatusDisplay("Opening election...");

    clearElectionRelatedStorageItems();

    CallAjaxHandler(publicInterface.electionsUrl + '/SelectElection', form, afterSelectElection);
  };

  var afterSelectElection = function (info) {
    //    if (info.Pulse) {
    //      ProcessPulseResult(info.Pulse);
    //    }
    ResetStatusDisplay();

    if (info.Selected) {
      //TODO: store computer Guid
      SetInStorage('compcode_' + info.ElectionGuid, info.CompGuid);

      location.href = site.rootUrl + 'Dashboard';

      //            $('.Election.true').removeClass('true');
      //            row.addClass('true');

      //            $('.CurrentElectionName').text(info.ElectionName);
      //            $('.CurrentLocationName').text('[No location selected]');

      //            showLocations(info.Locations, row);


      //            site.heartbeatActive = true;
      //            ActivateHeartbeat(true);
    }
    else {
      ShowStatusFailed("Unable to select");
    }
  };

  //    var showLocations = function (list, row) {
  //        var host = row.find('.Locations');
  //        host.html(site.templates.LocationSelectItem.filledWithEach(list));
  //    };

  //    var selectLocation = function () {
  //        var btn = $(this);
  //        var form =
  //        {
  //            id: btn.data('id')
  //        };

  //        ShowStatusDisplay('Selecting location...');

  //        CallAjaxHandler(publicInterface.electionsUrl + '/SelectLocation', form, afterSelectLocation);
  //    };

  //    var afterSelectLocation = function (info) {
  //        if (info.Selected) {
  //            location.href = site.rootUrl + 'Dashboard';
  //            return;
  //        }
  //    };

  var createElection = function () {
    if (publicInterface.isGuest) return;

    // get the server to make an election, then go see it
    CallAjaxHandler(publicInterface.electionsUrl + '/CreateElection', null, function (info) {
      //var row = $(site.templates.ElectionListItem.filledWith(info.Election)).prependTo($('#ElectionList'));
      //afterSelectElection(info, row);
      if (info.Success) {
        location.href = site.rootUrl + 'Setup';
        return;
      }
    });
  };

  var exportElection = function () {
    if (publicInterface.isGuest) return;

    var btn = $(this);
    var guid = btn.parents('.Election').data('guid');

    ShowStatusDisplay("Preparing file...");

    //var oldText = btn.text();

    btn.addClass('active');
    var iframe = $('body').append('<iframe style="display:none" src="{0}/ExportElection?guid={1}"></iframe>'.filledWith(publicInterface.electionsUrl, guid));
    iframe.ready(function () {
      setTimeout(function () {
        ResetStatusDisplay();
        btn.removeClass('active');
      }, 1000);
    });
  };

  var deleteElection = function () {
    if (publicInterface.isGuest) return;


    var btn = $(this);
    var row = btn.parents('.Election');
    var name = btn.parents('.Detail').find('b').text();

    btn.addClass('active');
    row.addClass('deleting');

    ShowStatusDisplay('Deleting...', 0);

    if (!confirm('Completely delete election this election?\n\n  {0}\n\n'.filledWith(name))) {
      btn.removeClass('active');
      row.removeClass('deleting');
      ResetStatusDisplay();
      return;
    }


    var form =
        {
          guid: row.data('guid')
        };

    CallAjaxHandler(publicInterface.electionsUrl + '/DeleteElection', form, function (info) {
      btn.removeClass('active');
      if (info.Deleted) {
        row.slideUp(1000, 0, function () {
          row.remove();
          ShowStatusSuccess('Deleted.');
        });
      } else {
        row.removeClass('deleting');
        ShowStatusFailed(info.Message);
      }
    });
  };

  var loadElection = function () {
    $('#fileName').show();
  };

  var loadElection2 = function () {
    var name = $('#fileName').val();
    LogMessage(name);
  };

  //  var copyElection = function () {
  //    if (publicInterface.isGuest) return;
  //
  //    var btn = $(this);
  //    var form =
  //        {
  //          guid: btn.parents('.Election').data('guid')
  //        };
  //
  //    if (!confirm('Are you sure you want to make a new election based on this one?')) {
  //      return;
  //    }
  //
  //    CallAjaxHandler(publicInterface.electionsUrl + '/CopyElection', form, function (info) {
  //
  //      if (info.Success) {
  //        location.href = '.';
  //        return;
  //      }
  //
  //      alert(info.Message);
  //
  //      site.heartbeatActive = true;
  //      ActivateHeartbeat(true);
  //    });
  //  };

  var scrollToMe = function (nameDiv) {
    var target = $(nameDiv);
    target.addClass('justloaded');

    var top = target.offset().top;
    var fudge = -83;
    var time = 800;

    try {
      $(document).animate({
        scrollTop: top + fudge
      }, time);
    } catch (e) {
      // ignore error
      LogMessage(e.message);
    }

    setTimeout(function () {
      target.toggleClass('justloaded', 'slow');
    }, 10000);
  };



  var publicInterface = {
    elections: [],
    isGuest: false,
    electionsUrl: '',
    loadElectionUrl: '',
    PreparePage: preparePage
  };

  return publicInterface;
};

var chooseElectionPage = HomeIndexPage();

$(function () {
  chooseElectionPage.PreparePage();
});


///**
//* jQuery plugin for posting form including file inputs.
//* 
//* Copyright (c) 2010 - 2011 Ewen Elder
//*
//* Licensed under the MIT and GPL licenses:
//* http://www.opensource.org/licenses/mit-license.php
//* http://www.gnu.org/licenses/gpl.html
//*
//* @author: Ewen Elder <ewen at jainaewen dot com> <glomainn at yahoo dot co dot uk>
//* @version: 1.1.1 (2011-07-29)
//**/
//(function($) {
//    $.fn.iframePostForm = function(options) {
//        var response,
//            returnReponse,
//            element,
//            status = true,
//            iframe;

//        options = $.extend({ }, $.fn.iframePostForm.defaults, options);

//        // Add the iframe.
//        if (!$('#' + options.iframeID).length) {
//            $('body').append('<iframe id="' + options.iframeID + '" name="' + options.iframeID + '" style="display:none" />');
//        }

//        return $(this).each(function() {
//            element = $(this);

//            // Target the iframe.
//            element.attr('target', options.iframeID);

//            // Submit listener.
//            element.submit(function() {
//                // If status is false then abort.
//                status = options.post.apply(this);

//                if (status === false) {
//                    return status;
//                }

//                iframe = $('#' + options.iframeID).load(function() {
//                    response = iframe.contents().find('body');

//                    if (options.json) {
//                        returnReponse = $.parseJSON(response.html());
//                    } else {
//                        returnReponse = response.html();
//                    }

//                    options.complete.apply(this, [returnReponse]);

//                    iframe.unbind('load');

//                    setTimeout(function() {
//                        response.html('');
//                    }, 1);
//                });
//                return status;
//            });
//        });
//    };

//    $.fn.iframePostForm.defaults =
//        {
//            iframeID: 'iframe-post-form',       // Iframe ID.
//            json: false,                        // Parse server response as a json object.
//            post: function() {
//            },               // Form onsubmit.
//            complete: function(response) {
//            }    // After response from the server has been received.
//        };
//})(jQuery);