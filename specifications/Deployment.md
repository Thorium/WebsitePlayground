
## Install the Development Environment ##

This document describes detailed instructions how to set up a development environment 

### Database ###

- First install [MariaDB](https://downloads.mariadb.org/). Version 10.0.20 or later. (MySQL should work as well; it's just a non-free version of MariaDB.) For different databases, see the end of this file.
- Ensure that the server is in the path: mysql -command should work. 
- (Ensure that the server is running. To start the server, e.g. on Mac, run: `mysql.server start`.) 
- Create a user and a database. To login to the server: `mysql -u root -p`

        CREATE USER 'webuser'@'localhost' IDENTIFIED BY 'p4ssw0rd';
        CREATE DATABASE companyweb;
        GRANT ALL PRIVILEGES ON companyweb.* TO 'webuser'@'localhost';
        FLUSH PRIVILEGES;

(This will also be available as a script file: backend/sql/createuser.sql)
- If you want a graphical user interface for DB management, you may use MySQL Workbench: http://dev.mysql.com/downloads/workbench/

You can select to use SQLProvider.MySql or SQLProvider.MySqlConnector depending which driver you want.
MySql is the official driver, meanwhile MySqlConnector is a community-driven open source driver.

Other databases are supported as well, more details follow near the bottom of this page.



### Version control ###

1. Install [Git](https://git-scm.com/) version control tool. If you want, you may also install a graphical user interface. There are a lot of different ones, e.g. [Git Extensions](http://sourceforge.net/projects/gitextensions/) for Windows or [GitX](http://gitx.frim.nl/) for Mac. 
2. Clone this repository, e.g.:

        git clone http://github.org/Thorium/WebsitePlayground.git

    This will make a new folder under your current folder called WebsitePlayground.

### Runtime environment ###

1. Install Node.js to get npm-package manager. 
   Install npm and Gulp. Npm should be in the path. 
   [Here are short instructions how!](https://gist.github.com/Thorium/b74c7e3a70e6d20bf705)
   Node.js should be new (version 4.x) , otherwise you will get errors about missing Promise support during build
   Gulp locates in the npm folder, so it has to be in the path. Typically this is correct by default if the Windows user stays the same.
   In Windows, the npm is probably located something like C:\Users\(User)\AppData\Roaming\npm

   (Install Visual Studio Code (https://code.visualstudio.com/) TypeScript editor, for *.ts and *.tsx files)

2. If you install to Mac/Linux, install F# (and Mono). Here are the instructions:
   - [Windows](http://fsharp.org/use/windows/)
   - [Linux](http://fsharp.org/use/linux/)
   - [Mac](http://fsharp.org/use/mac/)
   
   - After the basic install, run this to update HTTPS certificates

     mozroots --import --sync

2. If you install it on Windows, install Visual Studio 2015 (Community-edition is ok and free for some use).
   - [Visual Studio 2015](https://www.visualstudio.com/downloads/download-visual-studio-vs) From installer options select F# to be installed also.
   - If the online installer is not working, the page also has a downloads section for the full image.
   - [F# on Windows](http://fsharp.org/use/windows/)

3. Then you should run FAKE script to do various environment setups like fetch used 3rd party components, setup database tables, and add demo data. This is easy, just run from the root folder:

  - On Windows machine: `build.cmd`
  - On Mac/Linux: `sh ./build.sh`

(This will drop modifications to the database. So edit the backend/sql folder scripts if you want to modify the database structures.)
Npm module restore will take some time. If you want to avoid that, add the parameter: project

### Start the server ###

1. Optional: Install [Fiddler](http://www.telerik.com/fiddler). This is a tool for web-debugging.

2. You should have admin rights. Now you should be able to start the server from the root folder:

  - On Windows machine: `run.cmd`
  - On Mac/Linux: `sh ./run.sh`

   This will start the server, and now you can open your web-browser to the address told in the console: http://localhost:7050/

3. On Windows, run.cmd will also start Gulp. In Mac/Linux, please start another shell in root-folder and run: `gulp`

That's it. You should have the environment up and running.
If you modify HTML files, just press F5 on the browser, and it should refresh.
Gulp-window should display any Javascript errors.

You can also run code from the F#-interactive (or debug the program with Visual Studio F5)

  - On Windows machine: `fsi backend\Program.fsx`
  - On Mac/Linux (SignalR won't work): `fsharpi backend/Program.fsx`

##### Some Troubleshooting #####

If you have problems, you may miss something on the path. Try running the following commands from the shell (with admin rights):

- `mysql` to see that database is installed
- `npm` to see that Node.js and its package manager are working. Also, `build npm` should do.
- `gulp` to see that Gulp is working. If not, try installing with or without -g parameter. Also `build gulp` should do.
- `git` to see that Git is installed
- `fsc` on Windows, `fsharpc` on Mac/Linux, to check that F#-compiler is ok.

Installers should add paths. But if not, In Windows the path is modified from:
Control Panel -> System -> Advanced System Settings -> Advanced -> Environment Variables -> PATH. Do not replace the existing one, just add with semicolon separated the new ones.

##### How to run on a different database? #####

- It's recommend to then change to SQLProvider dependency to e.g. SQLProvider.MySQLite or other SQLProvider.PostgreSql. That can be done with `dotnet paket add SQLProvider.MySQLite` and then change the SqlDataProvider type namespace, e.g. FSharp.Data.Sql.SQLite.SqlDataProvider and the connection string.
- There are some other options for databases in the folder `backend/sql/other`: MS-SQL script files or SQLite database file. You could also use PostgreSQL, Oracle or MsAccess, but then you have to do your own database by looking at the simple database structure provided.
- Create this simple database for your system.
- From `build.fsx` end of the file, comment/remove the two lines: `==> "database"` and `==> "demodata"`. This will disable the auto-installation of the MariaDB database.
- Modify the design time SqlDataProvider parameters from `backend/Domain.fs`. MS-SQL connectionstring is like this: `@"Data Source=localhost; Initial Catalog=companyweb; Integrated Security=True"` and for SQLite like: `@"Data Source=C:\git\WebsitePlayground\backend\sql\other\database-sqlite.db;Version=3"`
- Correspondingly modify the RuntimeDBConnectionString from `backend/app.config`.
- Maybe some code changes are needed; your editor intellisense should help here. Try to build.

For Microsoft SQL Server, you can also use SSDT (dacpac-files) as the source of design-time, to avoid design-time/compile-time database calls. There will be a separate branch example in this repo.


## Install the Production Environment ##

First, you have created the release-folder with the deployment package from the development environment by command: `build package Configuration=Release`

It's recommended to use [Farmer](https://compositionalit.github.io/farmer/tutorials/traditional-ea/) to setup the infrastructure.

### Business Logic Server ###

Install [Visual F# Tools 4.0](https://www.microsoft.com/en-us/download/details.aspx?id=48179) (and [.Net Framework Runtime](https://dotnet.microsoft.com/en-us/download/dotnet-framework/net481)), and [Dotnet runtime](https://dotnet.microsoft.com/en-us/download).

Then, get the files from the development environment `release` folder. Extract those to some path, e.g. `c:\WebsitePlayground\wwwroot` and `c:\WebsitePlayground\server`. (If there is already a "playground" service running, stop that first...)

Modify the `WebsitePlayground.exe.config` file under the server folder. You can also create a Deployment.fsx script to modify it automatically.

To be fixed:
Extra step for now: Copy the MySQL.Data.dll to the same path that you have it in the development environment.

Install SSL. You can get one for free from Let's Encrypt but then you need to setup auto-renewal.
[Some example to automate SSL.](https://www.compositional-it.com/news-blog/virtual-machines-and-ssl-with-azure-and-farmer/)

On Windows, you can register WebsitePlayground as a service:

    sc create playground binPath= "c:\WebsitePlayground\server\WebsitePlayground.exe"
    sc start playground
    sc query playground

Go check that the service is configured to start automatically.

