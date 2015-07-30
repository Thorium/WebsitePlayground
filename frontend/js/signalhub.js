/// <reference path="../../paket-files/aFarkas/html5shiv/dist/html5shiv.min.js" /> 
/// <reference path="../../paket-files/ajax.aspnetcdn.com/jquery.min.js" /> 
/// <reference path="../../paket-files/cdnjs.cloudflare.com/knockout-min.js" /> 
/// <reference path="../../paket-files/code.jquery.com/jquery-ui.min.js" /> 
/// <reference path="../../paket-files/reactjs/react-bower/react.js" /> 
/// <reference path="../../paket-files/SignalR/bower-signalr/jquery.signalR.js" /> 
/// <reference path="../../paket-files/underscorejs.org/underscore-min.js" /> 
/// <reference path="../../paket-files/zurb/bower-foundation/js/foundation.min.js" /> 

var signalHub = {};
var hubConnector = {};
$(document).ready(function () {

	//SignalR Hub:
//	var signalHub;
	$.connection.hub.url = "/signalr";
	signalHub = $.connection.SignalHub; //Hub class
	var connection = !signalHub?$.hubConnection():$.connection.hub;
	if (!signalHub) {
	   console.log("hub not found");
	}
	
	signalHub.client.listCompanies = function (data) {
		renderAvailableCompanies(data);
	};

	signalHub.client.notifyDeal = function (data) {
		alert(data);
	};

	connection.error(function (error) {
		console.log('SignalR error: ' + error);
	});

	connection.logging = true;
	hubConnector = connection.start({ transport: 'longPolling' });
	hubConnector.done(function () {
        // more functions could be here...
        // signalHub.server.doThings(...);
    }).fail(function(){ console.log('Could not Connect!'); });

    $(document).foundation();
    renderNavBar("");
});
function mapOptionType(p) {
	var fieldValue = $('#'+p.toLowerCase()).val();
	if(p==="CompanyName") { return fieldValue; }
	if(p==="FoundedAfter" || p==="FoundedBefore") {
		var parts = fieldValue.split('.');
		var parsed = new Date(parts[2], parts[1]-1, parts[0]);
		return parsed;
	}
	return {Case: "Some", Fields: [fieldValue]};
	// Option Union-types would be: {Case: "Some", Fields: [{Case: fieldValue}]};
}

function refreshResultList() {
	var ms = new Date().getTime() + 86400000;
	var tomorrow = new Date(ms);
	var searchObject = { 
		FoundedAfter : new Date(0), // "1970-01-01T00:00:00.0000000+03:00"
		FoundedBefore : tomorrow, // "2015-07-12T15:26:13.7884528+03:00"
		CompanyName : "",
		CEOName: null // {"Case":"Nadela"}
	};
	
	var keys = Object.keys(searchObject);
	var params = _.filter(keys, function(c){return ($('#'+c.toLowerCase()).val()!=="");});
	_.each(params, function(p){ searchObject[p] = mapOptionType(p);});
	signalHub.server.searchCompanies(searchObject);
}
