
import './tools';
import './gui_shared';
import signalhub = require("./signalhub");

import idx = require('./index');
import company = require('./company');
import results = require('./results');

// Here would be client-side routing and localization of html-content.

if (typeof jQuery === 'undefined') {  
    // ensure script loadings in FireFox.
    document.write('<script src="js/libs.min.js">\x3C/script>');
    document.location.reload(false);
}

$(function() {
    signalhub.hubConnector.done(function () {
        window.onunload = undefined;
        window.onbeforeunload = undefined;
        const locale = "en"; // (navigator.language || navigator.userLanguage).substring(0,2);
        // const htmlBody = $(document.body).html();
        // const localized = translate(locale,htmlBody);
        // $(document.body).html(localized);
        idx.initIndex(locale);
        if(window.location.href.indexOf("company.html") > 0){ company.initCompany(locale); }
        if(window.location.href.indexOf("results.html") > 0){ results.initResults(locale); }
        
        $(".pageLoader").hide();
        $(".pageLoaded").show();
        $(document).foundation();

    });
});
