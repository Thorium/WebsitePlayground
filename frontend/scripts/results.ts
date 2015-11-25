import tools = require("./tools");
import signalhub = require("./signalhub");

export function initResults(locale) {
    tools.setFormValues(tools.parseUrlPathParameters(window.location.href));

    $("#foundedafter").datepicker({ dateFormat: 'dd.mm.yy' });
    $("#foundedbefore").datepicker({ dateFormat: 'dd.mm.yy' });

    signalhub.refreshResultList();				
    tools.onChangeInputs(["companyname", "foundedafter", "foundedbefore", "ceoname"],signalhub.refreshResultList);
    $('#companyname').keyup(signalhub.refreshResultList);
    $('#ceoname').keyup(signalhub.refreshResultList);    
}