import tools = require("./tools");
interface ICompanyHub extends SignalR { CompanyHub : any; }

export function initCompany(locale) {
	
    // UrlParameters: /company.html#/item/1
    $("#updatebtn").hide();
    $("#deletebtn").hide();
    $("#createbtn").hide();

    const parsed = tools.parseUrlPathParameters(window.location.href);

    const conn = <ICompanyHub> $.connection;
    const companyHub = conn.CompanyHub; // Hub class
    
    function setValuesToForm(data) {
        _.each(data, function(c:any){ $('#'+c.Item1).val(c.Item2); });
    }
    
    function parseFieldFromForm(){
       const nonbuttons = _.filter($(":input"), function(i) { return i.type !== "button";});
       const ids = _.map(nonbuttons, function(i) { return i.id;});
       const values = tools.getFormValues(ids);
       const keys = Object.keys(values);
       const tupleArray = _.map(keys, function(k) { return { Item1: k, Item2: values[k]};});
       // debugger;
       return tupleArray;
    }
    
    const compId = parsed.item;
    
    if(compId===undefined || compId === "undefined"){

        $("#profileInfo").text("Create a new company");

        $("#createbtn").show();
        $("#createbtn").click(function () {
            tools.validateForm($("#companyform"), function () {
                companyHub.server.create(parseFieldFromForm()).done(
                    function(data){ 
                        const id = _.filter(data, function(i:any){return i.Item1==="Id";});
                        const idval = _.map(id, function(i:any){return i.Item2;});
                        document.location.href = "company.html?i=" + idval[0] + "#/item/" + idval[0];
                    });
            });
            return false;
        });
        
    }else {
        $("#profileInfo").text("Update company");

        companyHub.server.read(compId).done(data => {
            setValuesToForm(data);
            const stempdate = $("#Founded").val().split("T")[0];
            $("#Founded").val(stempdate);
            return false;
        });

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