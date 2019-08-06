function lookBusy() {
  document.body.style.cursor = 'wait';
}
//$(function() {
//  var x = $('form').validate({
//    ignore: 'test',
//    debug: true,
//    submitHandler: function(form) {
//      // do other things for a valid form
//      debugger;
//      form.submit();
//    },
//    invalidHandler: function (event, validator) {
//      // 'this' refers to the form
//      debugger;
//      var errors = validator.numberOfInvalids();
//      if (errors) {
//        var message = errors == 1
//          ? 'You missed 1 field. It has been highlighted'
//          : 'You missed ' + errors + ' fields. They have been highlighted';
//        $("div.error span").html(message);
//        $("div.error").show();
//      } else {
//        $("div.error").hide();
//      }
//    }
//  });
//})