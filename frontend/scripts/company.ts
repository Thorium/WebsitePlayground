import tools = require("./tools");
interface ICompanyHub extends SignalR { CompanyHub : any; }

export function initCompany(locale) {
	
    // UrlParameters: /company.html#/item/1

    var parsed = tools.parseUrlPathParameters(window.location.href);

    var conn = <ICompanyHub> $.connection;
    var companyHub = conn.CompanyHub; // Hub class
    
    function setValuesToForm(data) {
        _.each(data, function(c:any){ $('#'+c.Item1).val(c.Item2); });
    }
    
    function parseFieldFromForm(){
       var nonbuttons = _.filter($(":input"), function(i) { return i.type !== "button";});
       var ids = _.map(nonbuttons, function(i) { return i.id;});
       var values = tools.getFormValues(ids);
       var keys = Object.keys(values);
       var tupleArray = _.map(keys, function(k) { return { Item1: k, Item2: values[k]};});
       // debugger;
       return tupleArray;
    }
    
    var compId = parsed.item;

    $("#updatebtn").hide();
    $("#deletebtn").hide();
    $("#createbtn").hide();
    
    if(compId===undefined){

        $("#profileInfo").text("Create a new company");

        $("#createbtn").show();
        $("#createbtn").click(function () {
            tools.validateForm($("#companyform"), function () {
                companyHub.server.create(parseFieldFromForm()).done(
                    function(data){ 
                        var id = _.filter(data, function(i:any){return i.Item1==="Id";});
                        var idval = _.map(id, function(i:any){return i.Item2;});
                        document.location.href = "company.html?i=" + idval[0] + "#/item/" + idval[0];
                    });
            });
            return false;
        });
        
    }else {
        $("#profileInfo").text("Update company");

        companyHub.server.read(compId).done(setValuesToForm);

        $("#updatebtn").show();
        $("#deletebtn").show();

        $("#updatebtn").click(function () {
            tools.validateForm($("#companyform"), function () {
                companyHub.server.update(compId, parseFieldFromForm()).done(function(d){ alert("Company updated!"); setValuesToForm(d);});
            });
            return false;
        });
        $("#deletebtn").click(function () {
            if(confirm("Are you sure?")){
                companyHub.server.delete(compId).done(function(d){ alert("Deleted!"); document.location.href="company.html"; });
            }
            return false;
        });	
    }
}