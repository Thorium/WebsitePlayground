
import './tools';
import './gui_shared';

import idx = require('./index');
import company = require('./company');
import results = require('./results');

// Here would be client-side routing and localization of html-content.
$(function() {
    window.onunload = undefined;
    window.onbeforeunload = undefined;
    var locale = "en"; // (navigator.language || navigator.userLanguage).substring(0,2);
    // var htmlBody = $(document.body).html();
    // var localized = translate(locale,htmlBody);
    // $(document.body).html(localized);
    idx.initIndex(locale);
    if(window.location.href.indexOf("company") > 0){ company.initCompany(locale); }
    if(window.location.href.indexOf("results") > 0){ results.initResults(locale); }
});
