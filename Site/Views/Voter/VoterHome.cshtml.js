var vueVoterHome = null;
var vueOptions = {
  el: '#body',
  data: function () {
    return {
      elections: [],
      loading: true
  };
  },
  watch: {
  },
  mounted: function () {
    this.getElectionList();
  },
  methods: {
    getElectionList: function () {
      var vue = this;

      CallAjaxHandler(GetRootUrl() + 'Voter/GetVoterElections', null, function (info) {
        vue.loading = false;
        if (info.list) {
          vue.elections = info.list;
        }
        else {
          ShowStatusFailed(info.Error);
        }
      });
    }
  }
};

$(function () {
  vueVoterHome = new Vue(vueOptions);
});
