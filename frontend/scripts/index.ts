import tools = require("./tools");

export function initIndex(locale) {
    $("#search").off();
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
        if ($('#toggleMoreIcon').attr('class') === 'fa fa-angle-double-down') {
          $('#toggleMoreIcon').removeClass('fa fa-angle-double-down');
          $('#toggleMoreIcon').addClass('fa fa-angle-double-up');
        } else {
          $('#toggleMoreIcon').removeClass('fa fa-angle-double-up');
          $('#toggleMoreIcon').addClass('fa fa-angle-double-down');
        }
        $( "#moreOptions" ).toggle( "blind", {}, speed );
    }

    $("#toggleMore").off();
    $("#toggleMore").click(function () {
        doToggleMore(100);
        return false;
    });
}