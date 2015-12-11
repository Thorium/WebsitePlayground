
'use strict';

export function emitUrlPathParameters(dict) {
    let keys = Object.keys(dict);
    function qparam(a, k){ return a + "/"+k+"/"+dict[k].replace("/", ""); }
    return _.reduce(keys, qparam, "");
}

// eg app.html#/param1/value1
export function parseUrlPathParameters(url) {
    let ix = url.indexOf("#");
    if (ix < 0) { return {}; }

    let items1 = url.substring(ix+2);
    let items = items1.split("/");
    let res : any = {};
    for (var k = 0;k<items.length/2;k++) {
        res[items[2*k]] = items[2*k+1];
    }
    return res; 
}

export function setFormValues(params) {
    let keys = Object.keys(params);
	_.each(keys, x => { 
        if($('#'+x).is(':checkbox') || $('#'+x).is(':radio')){
            $('#'+x).prop('checked', params[x] === 'true');
        }else{
            $('#'+x).val(params[x]); 
        }
    });
}

export function getFormValues(paramNames:Array<string>) {
    let res = {};
    let params = _.filter(paramNames, c => $('#'+c).is(":visible") || $('#'+c).hasClass("containsInput"));
	_.each(params, p => { 
        if($('#'+p).is(':checkbox') || $('#'+p).is(':radio')){
            res[p] = $('#'+p).prop('checked').toString();
        }else{
            res[p] = $('#'+p).val();
        }
    });
	return res;
}

export function getFormValuesFrom(form, paramNames:Array<string>) {
	let res = {};
	let params = _.filter(paramNames, c => form.find('#'+c).is(":visible") || form.find('#'+c).hasClass("containsInput"));
	_.each(params, p => { res[p] = form.find('#'+p).val();});
	return res;
}

export function setValuesToForm(data) {
    _.each(data, (c:any) => { $('#'+c.Item1).val(c.Item2); });
}

export function parseTuplesToDictionary(listOfItems) {
    let all = _.map(listOfItems, function(item) {
        let dict : any = {};
        _.each(item, (c:any) => { dict[c.Item1] = c.Item2; });
        return dict;
    });
    return all;
}

export function parseFieldsFromForm(form){
   let nonbuttons = _.filter(form.find(":input"), (i:any) => i.type !== "button");
   let ids = _.map(nonbuttons, (i:any) => i.id);
   let values = getFormValuesFrom(form, ids);
   let keys = Object.keys(values);
   let tupleArray = _.map(keys, k => { return { Item1: k, Item2: values[k]};});
   return tupleArray;
}
export function onChangeInputs(inputs,callback) {
    _.each(inputs, function(i){ $('#'+i).change(callback); });
}