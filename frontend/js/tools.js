/// <reference path="../../paket-files/aFarkas/html5shiv/dist/html5shiv.min.js" /> 
/// <reference path="../../paket-files/ajax.aspnetcdn.com/jquery.min.js" /> 
/// <reference path="../../paket-files/cdnjs.cloudflare.com/knockout-min.js" /> 
/// <reference path="../../paket-files/code.jquery.com/jquery-ui.min.js" /> 
/// <reference path="../../paket-files/reactjs/react-bower/react.js" /> 
/// <reference path="../../paket-files/SignalR/bower-signalr/jquery.signalR.js" /> 
/// <reference path="../../paket-files/underscorejs.org/underscore-min.js" /> 
/// <reference path="../../paket-files/zurb/bower-foundation/js/foundation.min.js" /> 

	function emitUrlPathParameters(dict) {
		var keys = Object.keys(dict);
		function qparam(a, k){ return a + "/"+k+"/"+dict[k].replace("/", ""); }
		return _.reduce(keys, qparam, "");
	}

	//eg app.html#/param1/value1
	function parseUrlPathParameters(url) {
		var ix = url.indexOf("#");
		if (ix < 0) { return {}; }

		var items1 = url.substring(ix+2);
		var items = items1.split("/");
		var res = {};
		for (var k = 0;k<items.length/2;k++) {
			res[items[2*k]] = items[2*k+1];
		}
		return res; 
	}

	function setFormValues(params) {
		var keys = Object.keys(params);
		_.each(keys, function(x){ $('#'+x).val(params[x]); });
	}

	function getFormValues(paramNames) {
		var res = {};
		var params = _.filter(paramNames, function(c){return $('#'+c).is(":visible");});
		_.each(params, function(p){ res[p] = $('#'+p).val();});
		return res;
	}

	function onChangeInputs(inputs,callback) {
		_.each(inputs, function(i){ $('#'+i).change(callback); });
	}