var vueVoterHome = null;
var vueOptions = {
    el: '#body',
    props: {
    },
    data: function () {
        return {
            elections: [],
            test: 'Hello'
        };
    },
    watch: {
    },
    mounted: function () {
        this.elections.push({ id: 1, name: 'Test 1' });
        this.elections.push({ id: 2, name: 'Test 2' });
    }
};

$(function() {
    vueVoterHome = new Vue(vueOptions);
});
