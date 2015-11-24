import tools = require("./tools");

export function initIndex(locale) {
    $(document).ready(function () {
        $("#search").click(function () {
			// Do search			
			var options = ["companyname", "foundedafter", "foundedbefore", "ceoname"];
			var qry2 = tools.emitUrlPathParameters(tools.getFormValues(options));
			document.location.href = "results.html#" + qry2;
            return false;
        });        
		
		$("#foundedafter").datepicker({ dateFormat: 'dd.mm.yy' });
		$("#foundedbefore").datepicker({ dateFormat: 'dd.mm.yy' });
				
		function doToggleMore(speed) {
			$( "#toggleMoreIcon" ).toggleClass( "fa fa-angle-double-up", speed );
			$( "#moreOptions" ).toggle( "blind", {}, speed );
        }
		
        $("#toggleMore").click(function () {
			doToggleMore(100);
        });
    });
}