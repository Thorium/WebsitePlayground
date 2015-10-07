import tools = require("./tools");
import signalhub = require("./signalhub");
if(window.location.href.indexOf("results") > 0){
         $(function() {
			tools.setFormValues(tools.parseUrlPathParameters(window.location.href));

            $("#foundedafter").datepicker({ dateFormat: 'dd.mm.yy' });
            $("#foundedbefore").datepicker({ dateFormat: 'dd.mm.yy' });

			signalhub.hubConnector.done(function () {
				signalhub.refreshResultList();				
				tools.onChangeInputs(["companyname", "foundedafter", "foundedbefore", "ceoname"],signalhub.refreshResultList);
			});	
		
			
        });
}