import gui_shared = require("./gui_shared");
import * as _ from "lodash";
export var signalHub: any = {};
export var hubConnector : any = {};
import * as signalR from "@microsoft/signalr";

$(document).ready(function () {
    gui_shared.renderNavBar("");
});
export function mapOptionType(p) {
	const fieldValue = $('#'+p.toLowerCase()).val();
	if(p==="CompanyName") { return fieldValue; }
	if(p==="FoundedAfter" || p==="FoundedBefore") {
		if(fieldValue.indexOf(".") > -1){
            const parts = fieldValue.split('.');
            const parsed = new Date(parts[2], parts[1]-1, parts[0]);
            return parsed;
        } else if (fieldValue.indexOf("/") > -1){
            const parts = fieldValue.split('/');
            const parsed = new Date(parts[2], parts[1]-1, parts[0]);
            return parsed;
        } else {
            const parsed = new Date(Date.parse(fieldValue));
            return parsed;
        }
    }
    return {Case: "Some", Fields: [fieldValue]};
    // Option Union-types would be: {Case: "Some", Fields: [{Case: fieldValue}]};
}

export function refreshResultList() {

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/signalhub")
        .withAutomaticReconnect([0, 0, 10000])
        .configureLogging(signalR.LogLevel.Information)
        .build();
    
    connection.onclose(error => {
        console.assert(connection.state === signalR.HubConnectionState.Disconnected);
    
        const li = document.createElement("li");
        li.textContent = `Connection closed due to error "${error}". Try refreshing this page to restart the connection.`;
        document.getElementById("messagesList").appendChild(li);
    });

    connection.on("NotifyDeal", (data) => {
        alert(data);
    });

	const ms = new Date().getTime() + 86400000;
	const tomorrow = new Date(ms);
	const searchObject = {
	 	FoundedAfter : new Date(0), // "1970-01-01T00:00:00.0000000+03:00"
	 	FoundedBefore : tomorrow, // "2015-07-12T15:26:13.7884528+03:00"
	 	CompanyName : "",
	 	CEOName: null // {"Case":"Nadela"}
	 };

	const keys = Object.keys(searchObject);
	const params = _.filter(keys, function(c){return ($('#'+c.toLowerCase()).val()!=="");});
	_.each(params, function(p){ searchObject[p] = mapOptionType(p);});

    $(document).foundation();
    gui_shared.renderNavBar("");
    $("#tinyLoader").show();
    connection.start().then(() => {
        connection.invoke("SearchCompanies", searchObject).then(function (data) {
                $("#tinyLoader").hide();
                gui_shared.renderAvailableCompanies(data, connection);
            }).catch(function(err) {
                    $("#tinyLoader").hide();
                    console.log('Response: ' + err);
            });
        }
    );
}
