import * as _ from "lodash";

declare global {
    interface Number {
        toLocaleFixed(): any;
        toLocaleFixedN(x:Number): any;
    }
}

export function setCookie(cname, cvalue, hours) {
    try {
        let d = new Date();
        d.setTime(d.getTime() + (hours*60*60*1000));
        const expires = "expires="+d.toUTCString();
        document.cookie = cname + "=" + cvalue + "; " + expires;
    } catch (e) {
        console.log("Setting cookie failed: " + e);
    }
}

export function getCookie(cname) {
    try {
        const name = cname + "=";
        const ca = document.cookie.split(';');
        for(let i=0; i<ca.length; i++) {
            let c = ca[i];
            while (c.charAt(0)===' ') {c = c.substring(1);}
            if (c.indexOf(name) === 0) {
                return c.substring(name.length, c.length);
            }
        }
        return "";
    } catch (e) {
        console.log("Getting cookie failed: " + e);
        return "";
    }
}
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
    for (let k = 0;k<items.length/2;k++) {
        res[items[2*k]] = decodeURIComponent(items[2*k+1]);
    }
    return res;
}


export function getTraditionalUrlParameterByName(name) {
    let searchstr =
        window.location.search !== "" ? (window.location.search +
            (window.location.search.indexOf("%23") > 0 ? "" : window.location.hash)) :
        window.location.href.indexOf("?") > 0 ? window.location.href.split("?")[1] : "";

    var match = RegExp('[?&]' + name + '=([^&]*)').exec(searchstr);
    return match && decodeURIComponent(match[1].replace(/\+/g, ' '));
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
    const invalidcallback = function () {
        const invalid_fields = jqForm.find('[data-invalid]');
        const fieldnames = _.reduce(invalid_fields, (acc, f:any) => {
            return  f === null || f.id === null ? acc : acc + ", " + f.id;
        });
        console.log( fieldnames.toString() );
    };
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
export function setItemValue(jQControl: JQuery, parval) {
    if (jQControl.is(':checkbox') || jQControl.is(':radio')) {
        jQControl.prop('checked', (parval === 'true' || parval === true));
    } else if (jQControl.hasClass('hasDatepicker')) {
        jQControl.datepicker("setDate", parval == null ? '' : new Date(parval));
    } else if (jQControl.attr('type') === 'date') {
        jQControl.val(new Date(parval).toISOString().split('T')[0]);
    } else if (jQControl.is('span') || jQControl.is('p')) {
        jQControl.html(parval);
    } else {
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

	_.each(params, (p:any) => { res[p] = getItemValue($('#'+p)); });
	return res;
}

export function getFormValuesFrom(form, paramNames:Array<string>) {
	let res = {};
    const paramNamesf = _.filter(paramNames, c => c !== "");
	const params = _.filter(paramNamesf, c => form.find('#'+c).is(":visible") || $('#'+c).attr('type') === 'hidden'
                                           || form.find('#'+c).hasClass("containsInput")
                                           || form.find('#'+c).prop('checked') );
	_.each(params, (p:any) => {
        res[p] = getItemValue(form.find('#'+p));
    });
	return res;
}

export function setValuesToFormName(formIdName, data) {
    _.each(data, (c:any) => {
        setItemValue($('#' + formIdName + ' #'+c.item1), c.item2);
    });
}

export function setValuesToForm(data) {
    _.each(data, (c:any) => {
        setItemValue($('#'+c.item1), c.item2);
    });
}

export function parseTuplesToDictionary(listOfItems) {
    const all = _.map(listOfItems, function(item) {
        let dict : any = {};
        _.each(item, (c:any) => { dict[c.item1] = c.item2; });
        return dict;
    });
    return all;
}

export function parseTuplesToObject(listOfItems) {
	let res = {};
    _.each(listOfItems, function(item:any) {
        res[item.item1] = item.item2;
    });
    return res;
}

export function parseFieldsFromForm(form){
   const nonbuttons = _.filter(form.find(":input"), (i:any) => i.type !== "button");
   const ids = _.map(nonbuttons, (i:any) => i.id);
   const values = getFormValuesFrom(form, ids);
   const keys = Object.keys(values);
   const tupleArray = _.map(keys, k => {
       return k.indexOf("Date")>-1 && values[k] != null ?
           { item1: k, item2: new Date(values[k])} :
           { item1: k, item2: values[k]};});

   return tupleArray;
}
export function onChangeInputs(inputs,callback) {
    _.each(inputs, i => { $('#'+i).change(callback); });
}
export function generatePagination(totalCount = 0, pageId = 1, pageSize = 30){

    const parsed = parseUrlPathParameters(window.location.href);
    const pagename =
        (parsed.pageId !== undefined) ?
            window.location.href.replace("/pageId/"+ parsed.pageId, "/pageId/")
        :
            window.location.href.indexOf("#") > -1 ?
            window.location.href + "/pageId/":
            window.location.href + "#/pageId/";

    const pageCount=Math.ceil(totalCount/pageSize);

    $("#pager").html("");
    let liprev = $("<li/>").appendTo($("#pager"));
    if(pageId>1){
        liprev.addClass("arrow");
    } else {
        liprev.addClass("arrow unavailable");
    }

    let startpage = pageId > 30 && pageCount > 40 ? (pageId - 20):1;
    let endpage = pageCount > 30 ?
        (pageCount - pageId > 30 ? (pageId + 20): pageCount)
            : pageCount;

    $("<a/>")
        .attr("href", pagename + (pageId-1))
        .addClass("pagerItem")
        .text("<<").appendTo(liprev);

    for(let i = startpage;i<=endpage;i++){
        let li = $("<li/>")
                 .appendTo($("#pager"));
        $("<a/>")
            .attr("href", pagename + i)
            .addClass("pagerItem")
            .text((i)).appendTo(li);
        if(i===pageId) { li.addClass("current"); }
    }

    let linext = $("<li/>").appendTo($("#pager"));
    $("<a/>")
        .attr("href", pagename + (pageId+1))
        .addClass("pagerItem")
        .text(">>").appendTo(linext);

    if(pageId<pageCount){
        linext.addClass("arrow");
    } else {
        linext.addClass("arrow unavailable");
    }
}
export function renderIfSome(optvalue){
    return (optvalue === undefined || optvalue === null) ? "" :
           (optvalue.fields === undefined || optvalue.fields === null) ? "" :
            optvalue.fields.length > 0 ? optvalue.fields[0] : "";
}

Number.prototype.toLocaleFixed = function() {
    if(this === undefined || this === null){
        return "0";
    } else {
        return (Math.round(this * 10000) / 10000).toLocaleString(undefined, {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }
};

Number.prototype.toLocaleFixedN = function(d) {
    if(this === undefined || this === null){
        return "0";
    } else {
        let r = (d as number)<=2?(Math.round(this * 10000) / 10000):this;
        return r.toLocaleString(undefined, {
            minimumFractionDigits: d,
            maximumFractionDigits: d
        });
    }
};

// filename = "data";
// textdata = "Name, Price\nApple, 2\nOrange, 3";
// filetype = "csv"; // or html or xml or txt
export function downloadDocument(filename, textdata, fileend) {
    var exportFilename = filename + "." + fileend;
    var csvData = new Blob([textdata], {type: 'text/csv;charset=utf-8;'});
    // IE11 & Edge
    if ((navigator as any).msSaveBlob) {
      (navigator as any).msSaveBlob(csvData, exportFilename);
    } else {
      // In FF link must be added to DOM to be clicked
      var link = document.createElement('a');
      link.href = window.URL.createObjectURL(csvData);
      link.setAttribute('download', exportFilename);
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
    }
}

export function printDivContent(htmlcont,pageName){
    if(htmlcont === undefined || htmlcont === null){
        return false;
    }
    $("#printFrame").remove();
    var ifrm = document.createElement("iframe");
    ifrm.style.width = "0px";
    ifrm.style.height = "0px";
    ifrm.style.border = "none 0px;";
    ifrm.id="printFrame";
    ifrm.name="printFrame";
    ifrm.scrolling = "no";
    ifrm.title = pageName;
    document.title = pageName;
    document.body.appendChild(ifrm);
    let title = pageName!==undefined && pageName !== null && pageName !== "" ? "<title>" + pageName + "</title>" : "";

    $("#printFrame").contents().find('html').html(
        `<head>` + title + `
            <link href="//maxcdn.bootstrapcdn.com/font-awesome/4.1.0/css/font-awesome.min.css" rel="stylesheet"/>
            <link rel='stylesheet' type='text/css' href='/css/libs.min.css'
                onerror="this.href='css/libs.min.css';" />
            <link rel='stylesheet' type='text/css' href='css/app.min.css' />
            <style>
            .termsprint {
                white-space: pre-wrap;       /* CSS 3 */
                white-space: -moz-pre-wrap;  /* Mozilla, since 1999 */
                white-space: -pre-wrap;      /* Opera 4-6 */
                white-space: -o-pre-wrap;    /* Opera 7 */
                word-wrap: break-word;       /* Internet Explorer 5.5+ */
                padding: 5px;
            }
            @media print {
                .divTable {
                    white-space: unset;
                    word-wrap: unset;
                }
            }
            </style>
        </head>` +
        "<body><div class='termsprint'>" + htmlcont + "</div></body>");

    setTimeout(function(){
        /* tslint:disable:no-string-literal */
        window.frames["printFrame"].focus();
        window.frames["printFrame"].print();
        /* tslint:enable:no-string-literal */
    }, 2000);
}
