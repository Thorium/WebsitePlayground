
## Install the Environment ##

### Database ###

- First install [MariaDB](https://downloads.mariadb.org/). Version 10.0.20 or later. (MySQL should work as well, it's just a non-free version of MariaDB.) For different databases see the end of this file.
- Ensure that the server is in the path: mysql -command should work. 
- (Ensure that the server is running. To start server e.g. on Mac, run: `mysql.server start`.) 
- Create a user and a database. To login to server: `mysql -u root -p`

        CREATE USER 'webuser'@'localhost' IDENTIFIED BY 'p4ssw0rd';
        CREATE DATABASE companyweb;
        GRANT ALL PRIVILEGES ON companyweb.* TO 'webuser'@'localhost';
        FLUSH PRIVILEGES;

(This will also available as a script file: backend/sql/createuser.sql)
- If you want graphical user interface for DB management, you may use MySQL Workbench: http://dev.mysql.com/downloads/workbench/


### Version control ###

1. Install [Git](https://git-scm.com/) version control tool. If you want, you may install also a graphical user interface. There are a lot of different ones, e.g. [Git Extensions](http://sourceforge.net/projects/gitextensions/) for Windows or [GitX](http://gitx.frim.nl/) for Mac. 
2. Clone this repository, e.g.:

        git clone http://github.org/Thorium/WebsitePlayground.git

    This will make a new folder under your current folder called WebsitePlayground.

### Runtime environment ###

1. Install Node.js (the new version 4.x) to get npm-package manager. 
   Install npm and Gulp. Npm should be in the path. 
   [Here are short instructions how!](https://gist.github.com/Thorium/b74c7e3a70e6d20bf705)

   (Install Visual Studio Code (https://code.visualstudio.com/) TypeScript editor, for *.ts and *.tsx files)

2. If you install to Mac/Linux, install F# (and Mono). Here are the instructions:
   - [Linux](http://fsharp.org/use/linux/)
   - [Mac](http://fsharp.org/use/mac/)
   
   - After basic install run this to update HTTPS certificates

     mozroots --import --sync

2. If you install to Windows, install Visual Studio 2015 (Community-edition is ok and it's free).
   - [Visual Studio 2015](https://www.visualstudio.com/downloads/download-visual-studio-vs) From installer options select F# to be installed also.
   - If on-line installer is not working, the page has also downloads-section for the full image.
   - [F# on Windows](http://fsharp.org/use/windows/)

3. Then you should run FAKE script to do various environment setups like fetch used 3rd party components, setup database tables, add demo data. This is easy, just run from the root folder:

  - On Windows machine: `build.cmd`
  - On Mac/Linux: `sh ./build.sh`

(This will drop modifications to the database. So edit the backend/sql folder scripts if you want to modify the database structures.)
Npm module restore will take some time. If you want to avoid that, add parameter: project

### Start the server ###

1. Optional: Install [Fiddler](http://www.telerik.com/fiddler). This is a tool for web-debugging.

2. You should have admin rights. Now you should be able to start the server from the root folder:

  - On Windows machine: `run.cmd`
  - On Mac/Linux: `sh ./run.sh`

   This will start the server and now you can open your web-browser to the address told in the console: http://localhost:7050/

3. On Windows run.cmd will start also Gulp. In Mac/Linux, please start another shell in root-folder and run: `gulp`

That's it. You should have environment up and running.
If you modify html-files just pressing F5 on the browser should do a refresh.
Gulp-window should display any Javascript errors.

You can also run code from the F#-interactive (or debug the program with Visual Studio F5)

  - On Windows machine: `fsi backend\Program.fsx`
  - On Mac/Linux (SignalR won't work): `fsharpi backend/Program.fsx`

##### Some Troubleshooting #####

If you have problems, you may miss somethig from the path. Try running the following commands from the shell (with admin rights):

- `mysql` to see that database is installed
- `npm` to see that Node.js and its package manager is working. Also `build npm` should do.
- `gulp` to see that Gulp is working. If not, try installing with or without -g parameter. Also `build gulp` should do.
- `git` to see that Git is installed
- `fsc` on Windows, `fsharpc` on Mac/Linux, to check that F#-compiler is ok.

Installers should add paths. But if not, In Windows the path is modified from:
Control Panel -> System -> Advanced System Settings -> Advanced -> Environment Variables -> PATH. Do not replace the existing one, just add with semicolon separated the new ones.

##### How to run on a different database? #####

- There are some other options for databases on folder `backend/sql/other`: MS-SQL script files or SQLite database file. You could also use PostgreSQL, Oracle or MsAccess but then you have to do your own database by looking the simple database structure provided.
- Create this simple database to your system.
- From `build.fsx` end of the file, comment/remove the two lines : `==> "database"` and `==> "demodata"`. This will disable auto-installation of MariaDB database.
- Modify the design time SqlDataProvider parameters from `backend/Domain.fs`. MS-SQL connectionstring is like this: `@"Data Source=localhost; Initial Catalog=companyweb; Integrated Security=True"` and for SQLite like: `@"Data Source=C:\git\WebsitePlayground\backend\sql\other\database-sqlite.db;Version=3"`
- Also modify the RuntimeDBConnectionString from `backend/app.config` correspondingly.
- Maybe some code changes needed, your editor intellisense should help here. Try to build.