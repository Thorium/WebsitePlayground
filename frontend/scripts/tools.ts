
'use strict';

export function emitUrlPathParameters(dict) {
    let keys = Object.keys(dict);
	function qparam(a, k){ 
       if(dict[k]===null) { 
           return a; 
       } else if(dict[k] instanceof Date){
		   dict[k].setHours(0, -dict[k].getTimezoneOffset(), 0, 0);
           return a + "/"+k+"/"+dict[k].toISOString().replace("/", ""); 
       } else {
           return a + "/"+k+"/"+dict[k].replace("/", ""); 
       }
    }
    return _.reduce(keys, qparam, "");
}

// eg app.html#/param1/value1
export function parseUrlPathParameters(url) {
    let ix = url.indexOf("#");
    if (ix < 0) { return {}; }

    let items1 = url.substring(ix+2);
    if(items1 === "=_") {return {};}
    let items = items1.split("/");
    let res : any = {};
    for (var k = 0;k<items.length/2;k++) {
        res[items[2*k]] = items[2*k+1];
    }
    return res; 
}

function getItemValue(jQControl){
    if(jQControl.is(':checkbox') || jQControl.is(':radio')){
        return jQControl.prop('checked').toString();
    }else if(jQControl.hasClass('hasDatepicker')){
        return jQControl.datepicker('getDate');
    }else if(jQControl.is('span') || jQControl.is('p')){
        return jQControl.html();
    }else{
        return jQControl.val();
    }    
} 
function setItemValue(jQControl, parval){
    if(jQControl.is(':checkbox') || jQControl.is(':radio')){
        jQControl.prop('checked', parval === 'true');
    }else if(jQControl.hasClass('hasDatepicker')){
        jQControl.datepicker("setDate", new Date(parval) );
    }else if(jQControl.is('span') || jQControl.is('p')){
        jQControl.html(parval); 
    }else{
        jQControl.val(parval); 
    }
}

export function setFormValues(params) {
    let keys = Object.keys(params);
	_.each(keys, x => { setItemValue($('#'+x), params[x]); });
}

export function getFormValues(paramNames:Array<string>) {
    let res = {};
    let params = _.filter(paramNames, c => $('#'+c).is(":visible") || $('#'+c).hasClass("containsInput"));
	_.each(params, p => { res[p] = getItemValue($('#'+p)); });
	return res;
}

export function getFormValuesFrom(form, paramNames:Array<string>) {
	let res = {};
	let params = _.filter(paramNames, c => form.find('#'+c).is(":visible") || form.find('#'+c).hasClass("containsInput"));
	_.each(params, p => { 
        res[p] = getItemValue(form.find('#'+p));
    });
	return res;
}

export function setValuesToForm(data) {
    _.each(data, (c:any) => { 
        setItemValue($('#'+c.Item1), c.Item2);
    });
}

export function parseTuplesToDictionary(listOfItems) {
    let all = _.map(listOfItems, function(item) {
        let dict : any = {};
        _.each(item, (c:any) => { dict[c.Item1] = c.Item2; });
        return dict;
    });
    return all;
}

export function parseTuplesToObject(listOfItems) {
	let res = {};
    _.each(listOfItems, function(item) {
        res[item.Item1] = item.Item2;
    });
    return res;
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