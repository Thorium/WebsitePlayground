import tools = require("./tools");
import signalhub = require("./signalhub");
import * as _ from "lodash";

export function initPage(locale) {
    tools.setFormValues(tools.parseUrlPathParameters(window.location.href));

    $(".pagination").hide();
    $("#foundedafter").datepicker({ dateFormat: 'yy-mm-dd' });
    $("#foundedbefore").datepicker({ dateFormat: 'yy-mm-dd' });

    signalhub.refreshResultList();				
    tools.onChangeInputs(["companyname", "foundedafter", "foundedbefore", "ceoname"],signalhub.refreshResultList);
    $('#companyname').keyup(_.throttle(function() {signalhub.refreshResultList();},300));
    $('#ceoname').keyup(_.throttle(function() {signalhub.refreshResultList();},300));    
}