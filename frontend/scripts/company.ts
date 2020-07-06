import tools = require("./tools");
import * as _ from "lodash";
import * as signalR from "@microsoft/signalr";

export function initCompany(locale) {
	
    // UrlParameters: /company.html#/item/1
    $("#updatebtn").hide();
    $("#deletebtn").hide();
    $("#createbtn").hide();

    const parsed = tools.parseUrlPathParameters(window.location.href);

    const companyConnection = new signalR.HubConnectionBuilder()
        .withUrl("/companyhub")
        .withAutomaticReconnect([0, 0, 10000])
        .configureLogging(signalR.LogLevel.Information)
        .build();

    function setValuesToForm(data) {
        _.each(data, function(c:any){ $('#'+c.item1).val(c.item2); });
    }
    
    function parseFieldFromForm(){
       const nonbuttons = _.filter($(":input"), i => (<HTMLInputElement>i).type !== "button");
       const ids = _.map(nonbuttons, function(i) { return i.id;});
       const values = tools.getFormValues(ids);
       const keys = Object.keys(values);
       const tupleArray = _.map(keys, function(k) { return { item1: k, item2: values[k]};});
       // debugger;
       return tupleArray;
    }
    
    const compId = parsed.item;
    companyConnection.start().then(function(){
        
        if(compId===undefined || compId === "undefined"){

            $("#profileInfo").text("Create a new company");

            $("#createbtn").show();
            $("#createbtn").off();
            $("#createbtn").click(function () {
                tools.validateForm($("#companyform"), function () {
                    companyConnection.invoke("Create", parseFieldFromForm()).then(
                        function(data){ 
                            const id = _.filter(data, function(i:any){return i.item1==="Id";});
                            const idval = _.map(id, function(i:any){return i.item2;});
                            document.location.href = "company.html?i=" + idval[0] + "#/item/" + idval[0];
                        });
                });
                return false;
            });
            
        }else {
            $("#profileInfo").text("Update company");
            companyConnection.invoke("Read", parseInt(compId, 10)).then(data => {
                setValuesToForm(data);
                const stempdate = $("#Founded").val().split("T")[0];
                $("#Founded").val(stempdate);
                return false;
            });

            $("#updatebtn").show();
            $("#deletebtn").show();

            $("#updatebtn").off();
            $("#updatebtn").click(function () {
                tools.validateForm($("#companyform"), function () {
                    companyConnection.invoke("Update", parseInt(compId, 10), parseFieldFromForm())
                        .then(function(d){ alert("Company updated!"); setValuesToForm(d);});
                });
                return false;
            });
            $("#deletebtn").off();
            $("#deletebtn").click(function () {
                if(confirm("Are you sure?")){
                    companyConnection.invoke("Delete", parseInt(compId, 10)).then(
                        function(d){ alert("Deleted!"); document.location.href="company.html"; });
                }
                return false;
            });	
        }
    });
}