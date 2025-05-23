import tools = require("./tools");
import * as _ from "lodash";
interface ICompanyHub extends SignalR { CompanyHub: any; }

export function initPage(locale) {
	
    // UrlParameters: /company.html#/item/1
    $("#updatebtn").hide();
    $("#deletebtn").hide();
    $("#createbtn").hide();
    $("#companyListDiv").hide();

    const parsed = tools.parseUrlPathParameters(window.location.href);

    const conn = <ICompanyHub> $.connection;
    const companyHub = conn.CompanyHub; // Hub class
    
    function setValuesToForm(data) {
        _.each(data, function(c:any){ $('#'+c.Item1).val(c.Item2); });
    }
    
    function parseFieldFromForm(){
       const nonbuttons = _.filter($(":input"), i => (<HTMLInputElement>i).type !== "button");
       const ids = _.map(nonbuttons, function(i) { return i.id;});
       const values = tools.getFormValues(ids);
       const keys = Object.keys(values);
       const tupleArray = _.map(keys, function(k) { return { Item1: k, Item2: values[k]};});
       // debugger;
       return tupleArray;
    }
    
    const compId = parsed.item;
    
    if(compId===undefined || compId === "undefined"){

        $("#companyListDiv").show();
        $("#tinyLoader").show();
        companyHub.server.getCompanyList().done(function (res) {
            $("#companyList").html("");
            res.forEach(data => {
                let a = $("<a/>");
                a.prop("href", "company.html#/item/" + data.Item1);
                a.text(data.Item2);
                $("<div/>").append(a).appendTo($("#companyList"));
            });
            $("#tinyLoader").hide();
        }).fail(function (xhr, textStatus, errorThrown) { 
            $("#tinyLoader").hide();
            console.log('Response: ' + xhr);
        });

        $("#profileInfo").text("Create a new company");

        $("#createbtn").show();
        $("#createbtn").off();
        $("#createbtn").click(function () {
            tools.validateForm($("#companyform"), function () {
                $("#tinyLoader").show();
                companyHub.server.create(parseFieldFromForm()).done(
                    function(data){ 
                        $("#tinyLoader").hide();
                        const id = _.filter(data, function(i:any){return i.Item1==="Id";});
                        const idval = _.map(id, function(i:any){return i.Item2;});
                        document.location.href = "company.html?i=" + idval[0] + "#/item/" + idval[0];
                    }).fail(function(xhr, textStatus, errorThrown) { 
                        console.log('Response: ' + textStatus);
                        $("#tinyLoader").hide();
                    });
            });
            return false;
        });
        
    }else {
        $("#companyListDiv").hide();
        $("#profileInfo").text("Update company");
        $("#tinyLoader").show();
        companyHub.server.read(compId).done(data => {
            $("#tinyLoader").hide();
            setValuesToForm(data);
            const stempdate = $("#Founded").val().split("T")[0];
            $("#Founded").val(stempdate);
            return false;
        }).fail(function(xhr, textStatus, errorThrown) { 
            console.log('Response: ' + textStatus);
            $("#tinyLoader").hide();
        });

        $("#updatebtn").show();
        $("#deletebtn").show();

        $("#updatebtn").off();
        $("#updatebtn").click(function () {
            tools.validateForm($("#companyform"), function () {
                $("#tinyLoader").show();
                companyHub.server.update(compId, parseFieldFromForm()).done(function(d){
                    $("#tinyLoader").hide();
                    alert("Company updated!"); 
                    setValuesToForm(d);
                }).fail(function(xhr, textStatus, errorThrown) { 
                    console.log('Response: ' + textStatus);
                    $("#tinyLoader").hide();
                });

            });
            return false;
        });
        $("#deletebtn").off();
        $("#deletebtn").click(function () {
            if(confirm("Are you sure?")){
                $("#tinyLoader").show();
                companyHub.server.delete(compId).done(function(d){
                    $("#tinyLoader").hide();
                    alert("Deleted!");
                    document.location.href="company.html";
                }).fail(function(xhr, textStatus, errorThrown) { 
                    console.log('Response: ' + textStatus);
                    $("#tinyLoader").hide();
                });
            }
            return false;
        });
        $("#back").off();
        $("#back").click(function () {
            document.location.href = "company.html";
        });

    }
}
