import tools = require("./tools");
import * as _ from "lodash";
import * as signalR from "@microsoft/signalr";
import companyGridControl = require("./grid")

export function initPage(locale) {
	
    // UrlParameters: /company.html#/item/1
    $("#updatebtn").hide();
    $("#deletebtn").hide();
    $("#createbtn").hide();
    $("#companyListDiv").hide();

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

            $("#companyListDiv").show();
            $("#tinyLoader").show();

            // Paging:
            // If you want to implement paging, send selected pageId to server side (SQL) to only pick the page you need, not all items.
            // Then return also totalcount from the method with the list of items.
            let pageId = parsed.pageId === undefined || parsed.pageId === null ? 0 : parsed.pageId;

            companyConnection.invoke("GetCompanyList").then(companyList => {
                
                // Render a React.js component template:
                // companyGridControl.renderList(companyList, pageId);
                // // tools.generatePagination(totalcount, pageId, pagesize);

                // Render an old-fashion jQuery html:
                $("#companyList").html("");
                companyList.forEach(company => {
                    let a = $("<a/>");
                    a.prop("href", "company.html#/item/" + company.id);
                    a.text(company.name);
                    $("<div/>").append(a).appendTo($("#companyList"));
                });

                $("#tinyLoader").hide();
            }).catch(function(err) {
                $("#tinyLoader").hide();
                console.log('Response: ' + err);
            });

            $("#profileInfo").text("Create a new company");

            $("#createbtn").show();
            $("#createbtn").off();
            $("#createbtn").click(function () {
                tools.validateForm($("#companyform"), function () {
                    $("#tinyLoader").show();
                    companyConnection.invoke("Create", parseFieldFromForm()).then(
                        function(data){ 
                            $("#tinyLoader").hide();
                            const id = _.filter(data, function(i:any){return i.item1==="Id";});
                            const idval = _.map(id, function(i:any){return i.item2;});
                            document.location.href = "company.html?i=" + idval[0] + "#/item/" + idval[0];
                        }).catch(function(err) {
                            $("#tinyLoader").hide();
                            console.log('Response: ' + err);
                        });
                });
                return false;
            });
            
        }else {
            $("#companyListDiv").hide();
            $("#profileInfo").text("Update company");
            $("#tinyLoader").show();
            companyConnection.invoke("Read", parseInt(compId, 10)).then(data => {
                $("#tinyLoader").hide();
                setValuesToForm(data);
                const stempdate = $("#Founded").val().split("T")[0];
                $("#Founded").val(stempdate);
                return false;
            }).catch(function(err) {
                $("#tinyLoader").hide();
                console.log('Response: ' + err);
            });

            $("#updatebtn").show();
            $("#deletebtn").show();

            $("#updatebtn").off();
            $("#updatebtn").click(function () {
                tools.validateForm($("#companyform"), function () {
                    $("#tinyLoader").show();
                    companyConnection.invoke("Update", parseInt(compId, 10), parseFieldFromForm())
                        .then(function(d){ 
                            $("#tinyLoader").hide();
                            alert("Company updated!"); 
                            setValuesToForm(d);
                        }).catch(function(err) {
                            $("#tinyLoader").hide();
                            console.log('Response: ' + err);
                        });
                });
                return false;
            });
            $("#deletebtn").off();
            $("#deletebtn").click(function () {
                if(confirm("Are you sure?")){
                    $("#tinyLoader").show();
                    companyConnection.invoke("Delete", parseInt(compId, 10)).then(
                        function(d){ 
                            $("#tinyLoader").hide();
                            alert("Deleted!");
                            document.location.href="company.html";
                        }).catch(function(err) {
                            $("#tinyLoader").hide();
                            console.log('Response: ' + err);
                        });
                }
                return false;
            });	
            $("#back").off();
            $("#back").click(function () {
                document.location.href="company.html"; 
            });

        }
    });
}
