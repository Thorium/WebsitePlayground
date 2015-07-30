

# Website-playground #

This is yet another template for web-sites (aka. playing with some technologies).
What are the key points:

- The same web-server works on Mac / Windows / Ubuntu.
- No O/R-mapping, no n-tiers, no huge amount of manual object-to-object-mapping
- No direct SQL-clauses, no SQL-injection problems.
- No APIs inside one software. Not SOAP nor REST.
- Real-time communication between server and clients.

Technology-stack:

- General: Git, Markdown
- Backend: MariaDB (MySQL), Paket, F# (FSharp), TypeProviders, OWIN, SignalR (WebSockets/Long-Polling)
- Frontend: Gulp, React.js, Less, Foundation.css, FontAwesome, Underscore, jQuery / jQuery-UI
- Not yet: Crossroads.js, Rx/Rx.Js, 

Documentation:

- [More about the Technology-stack](specifications/Technologies.md)
- [How to install the environment](specifications/Deployment.md)


There are two screens: 1) You can search companies. 2) CRUD-operations for companies.

![](specifications/ui.jpg)
