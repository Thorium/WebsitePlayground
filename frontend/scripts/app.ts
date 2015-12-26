
import './tools';
import './gui_shared';
import signalhub = require("./signalhub");

import idx = require('./index');
import company = require('./company');
import results = require('./results');

// Here would be client-side routing and localization of html-content.
$(function() {
    signalhub.hubConnector.done(function () {
        window.onunload = undefined;
        window.onbeforeunload = undefined;
        var locale = "en"; // (navigator.language || navigator.userLanguage).substring(0,2);
        // var htmlBody = $(document.body).html();
        // var localized = translate(locale,htmlBody);
        // $(document.body).html(localized);
        idx.initIndex(locale);
        if(window.location.href.indexOf("company.html") > 0){ company.initCompany(locale); }
        if(window.location.href.indexOf("results.html") > 0){ results.initResults(locale); }
    });
});
