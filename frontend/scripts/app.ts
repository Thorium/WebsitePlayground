
import './tools';
import './gui_shared';
import signalhub = require("./signalhub");

import idx = require('./index');
import company = require('./company');
import results = require('./results');

// Here would be client-side routing and localization of html-content.

setTimeout(function(){
    if (typeof jQuery === 'undefined') {
        // ensure script loadings in FireFox.
        console.log("JQuery not loaded!");
        document.write('<script src="js/libs.min.js">\x3C/script>');
        setTimeout(function(){
            document.location.reload(false);
        }, 500);
    }
}, 500);

$(function() {
    function doInit(locale) {
        idx.initIndex(locale);
        if(window.location.href.indexOf("/company.html") > 0){ company.initCompany(locale); }
        if(window.location.href.indexOf("/results.html") > 0){ results.initResults(locale); }
        $(document).foundation();
    }

    signalhub.hubConnector.done(function () {
        // window.onunload = undefined;
        // window.onbeforeunload = undefined;
        // const nav:any = navigator;
        const locale = "en"; // (nav.language || nav.userLanguage).substring(0,2);
        // const htmlBody = $(document.body).html();
        // const localized = translate(locale,htmlBody);
        // $(document.body).html(localized);

        doInit(locale);

        // Refresh page if url hash part changes:
        window.onhashchange = function() { doInit(locale); };


        $(".pageLoader").hide();
        $(".pageLoaded").show();
        $(document).foundation();

    });
});
