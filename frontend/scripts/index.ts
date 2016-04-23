import tools = require("./tools");

export function initIndex(locale) {
    $("#search").click(function () {
        // Do search			
        const options = ["companyname", "foundedafter", "foundedbefore", "ceoname"];
        const qry2 = tools.emitUrlPathParameters(tools.getFormValues(options));
        document.location.href = "results.html#" + qry2;
        return false;
    });        
    
    $("#foundedafter").datepicker({ dateFormat: 'yy-mm-dd' });
    $("#foundedbefore").datepicker({ dateFormat: 'yy-mm-dd' });
            
    function doToggleMore(speed) {
        $( "#toggleMoreIcon" ).toggleClass( "fa fa-angle-double-up", speed );
        $( "#moreOptions" ).toggle( "blind", {}, speed );
    }
    
    $("#toggleMore").click(function () {
        doToggleMore(100);
        return false;
    });
}