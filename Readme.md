

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
- SQL-Server: SQLProvider supports any: MS-SQL/Postgres/Oracle/MySql/MariaDB/SQLite/MS-Access. Instructions for MariaDB is included, MySql should work as well. Also some for SQLite database and MS-SQL scripts if you want to use those.
- Backend: F# (FSharp), Paket, TypeProviders, OWIN, SignalR (WebSockets/Long-Polling), Logary
- Frontend: Gulp, React.js, Less/Sass, Foundation.css, FontAwesome, Lodash, jQuery / jQuery-UI, TypeScript / ES6
- Maybe in the future: Crossroads.js, Rx/Rx.Js, FsUnit.xUnit, Canopy 

Documentation:

- [More about the Technology-stack](specifications/Technologies.md)
- [How to install the environment](specifications/Deployment.md)


There are two screens: 1) You can search companies. 2) CRUD-operations for companies.

![](specifications/ui.jpg)
