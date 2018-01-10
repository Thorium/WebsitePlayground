import * as _ from "lodash";

export function emitUrlPathParameters(dict) {
    const keys = Object.keys(dict);
	function qparam(a, k){
       if(dict[k]===null) {
           return a;
       } else if(dict[k] instanceof Date){
		   dict[k].setHours(dict[k].getHours(), -dict[k].getTimezoneOffset(), 0, 0);
           return a + "/"+k+"/"+encodeURIComponent(dict[k].toISOString());
       } else {
           return a + "/"+k+"/"+encodeURIComponent(dict[k]);
       }
    }
    return _.reduce(keys, qparam, "");
}

// eg app.html#/param1/value1
export function parseUrlPathParameters(url) {
    const ix = url.indexOf("#");
    if (ix < 0) { return {}; }

    const items1 = url.substring(ix+2);
    if(items1 === "=_") {return {};}
    const items = items1.split("/");
    let res : any = {};
    for (var k = 0;k<items.length/2;k++) {
        res[items[2*k]] = decodeURIComponent(items[2*k+1]);
    }
    return res;
}

var formCache = {};
export function validateFormWithInvalid(jqForm, callback, invalidcallback) {
    function onValid(){ callback(); }
    function onInvalid(){ invalidcallback(); }
    $(document).foundation();
    if(jqForm.selector === undefined || !formCache[document.location.href + "_" + jqForm.selector]){
        jqForm
            .on('invalid.fndtn.abide', function () { onInvalid(); })
            .on('valid.fndtn.abide', function () { onValid(); })
            .on('invalid', function () { onInvalid(); })
            .on('valid', function () { onValid(); })
            .on('submit', Foundation.utils.debounce(function(e){
                jqForm.off('submit');
                formCache[document.location.href + "_" + jqForm.selector] = false;
                return false;
            }, 300, true));
        formCache[document.location.href + "_" + jqForm.selector] = true;
    }
    jqForm.submit();
    return false;
}

const warning = "Please correct invalid fields!";
export function validateForm(jqForm, callback) {
    const invalidcallback = function () { /*alert(warning);*/ };
    return validateFormWithInvalid(jqForm, callback, invalidcallback);
}

export function validateFormWithMsg(jqForm, callback) {
    const invalidcallback = function (){
        const invalid_fields = jqForm.find('[data-invalid]');
        const fieldnames = _.reduce(invalid_fields, (acc, f:any) => {
            return  f === null || f.id === null ? acc : acc + ", " + f.id;
        });

        const message =
              fieldnames !== null && fieldnames.toString().length > 0 ?
              warning + " ("+ fieldnames.toString() + ")" :
              warning;
        alert(message);
    };
    return validateFormWithInvalid(jqForm, callback, invalidcallback);
}

function getItemValue(jQControl){
    if(jQControl.is(':checkbox') || jQControl.is(':radio')){
        return jQControl.prop('checked').toString();
    }else if(jQControl.hasClass('hasDatepicker')){
        let dt = jQControl.datepicker('getDate');
        if(dt!==null) { dt.setHours(dt.getHours(), -dt.getTimezoneOffset(), 0, 0); }
        return dt;
    }else if(jQControl.is('span') || jQControl.is('p')){
        return jQControl.html();
    }else{
        let x = jQControl.val();
        return (x !== undefined && x !== null && (typeof x === 'string' || x instanceof String))? x.trim() : x;
    }
}
function setItemValue(jQControl, parval){
    if(jQControl.is(':checkbox') || jQControl.is(':radio')){
        jQControl.prop('checked', (parval === 'true' || parval === true));
    }else if(jQControl.hasClass('hasDatepicker')){
        jQControl.datepicker("setDate", new Date(parval) );
    }else if(jQControl.is('span') || jQControl.is('p')){
        jQControl.html(parval);
    }else{
        jQControl.val(parval);
    }
}

export function setFormValues(params) {
    const keys = Object.keys(params);
	_.each(keys, x => { setItemValue($('#'+x), params[x]); });
}

export function getFormValues(paramNames:Array<string>) {
    let res = {};
	const params = _.filter(paramNames, c => $('#'+c).is(":visible") || $('#'+c).attr('type') === 'hidden'
                                           || $('#'+c).hasClass("containsInput")
                                           || $('#'+c).prop('checked') );

	_.each(params, p => { res[p] = getItemValue($('#'+p)); });
	return res;
}

export function getFormValuesFrom(form, paramNames:Array<string>) {
	let res = {};
    const paramNamesf = _.filter(paramNames, c => c !== "");
	const params = _.filter(paramNamesf, c => form.find('#'+c).is(":visible") || $('#'+c).attr('type') === 'hidden'
                                           || form.find('#'+c).hasClass("containsInput")
                                           || form.find('#'+c).prop('checked') );
	_.each(params, p => {
        res[p] = getItemValue(form.find('#'+p));
    });
	return res;
}

export function setValuesToFormName(formIdName, data) {
    _.each(data, (c:any) => {
        setItemValue($('#' + formIdName + ' #'+c.Item1), c.Item2);
    });
}

export function setValuesToForm(data) {
    _.each(data, (c:any) => {
        setItemValue($('#'+c.Item1), c.Item2);
    });
}

export function parseTuplesToDictionary(listOfItems) {
    const all = _.map(listOfItems, function(item) {
        let dict : any = {};
        _.each(item, (c:any) => { dict[c.Item1] = c.Item2; });
        return dict;
    });
    return all;
}

export function parseTuplesToObject(listOfItems) {
	let res = {};
    _.each(listOfItems, function(item:any) {
        res[item.Item1] = item.Item2;
    });
    return res;
}

export function parseFieldsFromForm(form){
   const nonbuttons = _.filter(form.find(":input"), (i:any) => i.type !== "button");
   const ids = _.map(nonbuttons, (i:any) => i.id);
   const values = getFormValuesFrom(form, ids);
   const keys = Object.keys(values);
   const tupleArray = _.map(keys, k => {
       return k.indexOf("Date")>-1 ?
           { Item1: k, Item2: new Date(values[k])} :
           { Item1: k, Item2: values[k]};});

   return tupleArray;
}
export function onChangeInputs(inputs,callback) {
    _.each(inputs, function(i){ $('#'+i).change(callback); });
}