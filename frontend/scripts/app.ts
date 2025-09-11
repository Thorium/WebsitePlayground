
import tools = require('./tools');
import './gui_shared';
import signalhub = require("./signalhub");

import idx = require('./index');
// We can load modules eagerly or lazily, see end of this file.
// import company = require('./company');
// import results = require('./results');

// Here would be client-side routing and localization of html-content.

setTimeout(function(){
    if (typeof jQuery === 'undefined') {
        // ensure script loadings in FireFox.
        console.log("JQuery not loaded!");
        document.write('<script src="js/libs.min.js">\x3C/script>');
        setTimeout(function(){
            document.location.reload();
        }, 500);
    }
}, 500);

$(function() {
    try{
        Foundation.global.namespace = '';
    } catch(e) {
        console.log(e);
    }
    $(document).off('click.fndtn.magellan');
    try{
        let fn:any = $.fn;
        if (fn.button.noConflict) {
            fn.bootstrapBtn = fn.button.noConflict();
        }
    } catch(e) {
        console.log(e);
    }

    function doInit(locale) {
        idx.initIndex(locale);
        // If loaded eagerly above:
        // if(window.location.href.indexOf("/company.html") > 0){ company.initPage(locale); }
        // if(window.location.href.indexOf("/results.html") > 0){ results.initPage(locale); }
        // If loading lazily:
        if(window.location.href.indexOf("/company.html") > 0){ import('./company').then(company => company.initPage(locale)); }
        if(window.location.href.indexOf("/results.html") > 0){ import('./results').then(results => results.initPage(locale)); }
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
        $(document).off('click.fndtn.magellan');
        $(".hastip").click(function(){
            $("<div/>").attr("title", "Help").text(
                $(this).prop("title").split('\r\n').join('<br/>').split('\\r\\n').join('<br/>')).dialog();
            return false;
        });
    });
});
