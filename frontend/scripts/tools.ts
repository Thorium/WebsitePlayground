
'use strict';

export function emitUrlPathParameters(dict) {
    var keys = Object.keys(dict);
    function qparam(a, k){ return a + "/"+k+"/"+dict[k].replace("/", ""); }
    return _.reduce(keys, qparam, "");
}

	// eg app.html#/param1/value1
export function parseUrlPathParameters(url) {
    var ix = url.indexOf("#");
    if (ix < 0) { return {}; }

    var items1 = url.substring(ix+2);
    var items = items1.split("/");
    var res : any = {};
    for (var k = 0;k<items.length/2;k++) {
        res[items[2*k]] = items[2*k+1];
    }
    return res; 
}

export function setFormValues(params) {
    var keys = Object.keys(params);
    _.each(keys, function(x){ $('#'+x).val(params[x]); });
}

export function getFormValues(paramNames:Array<string>) {
    var res = {};
    var params = _.filter(paramNames, function(c){return $('#'+c).is(":visible");});
    _.each(params, function(p){ res[p] = $('#'+p).val();});
    return res;
}

export function onChangeInputs(inputs,callback) {
    _.each(inputs, function(i){ $('#'+i).change(callback); });
}