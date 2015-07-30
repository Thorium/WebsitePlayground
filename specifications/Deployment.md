
## Install the Environment ##

### Database ###

- First install [MariaDB](https://downloads.mariadb.org/).
- Ensure that the server is in the path: mysql -command should work. 
- (Ensure that the server is running. To start server e.g. on Mac, run: `mysql.server start`.) 
- Create a user and a database. To login to server: `mysql -u root -p`

        CREATE USER 'webuser'@'localhost' IDENTIFIED BY 'p4ssw0rd';
        CREATE DATABASE companyweb;
        GRANT ALL PRIVILEGES ON companyweb.* TO 'webuser'@'localhost';
        FLUSH PRIVILEGES;

(This will also available as a script file: backend/sql/createuser.sql)

### Version control ###

1. Install [Git](https://git-scm.com/) version control tool. If you want, you may install also a graphical user interface. There are a lot of different ones, e.g. [Git Extensions](http://sourceforge.net/projects/gitextensions/) for Windows or [GitX](http://gitx.frim.nl/) for Mac. 
2. Clone this repository, e.g.:

        git clone http://github.org/Thorium/WebsitePlayground.git

    This will make a new folder under your current folder called WebsitePlayground.

### Runtime environment ###

1. Install npm and Gulp. Npm should be in the path. 
   [Here are short instructions how!](https://gist.github.com/Thorium/b74c7e3a70e6d20bf705)

2. If you Mac/Linux, install F# (and Mono). Here are the instructions:
   - [Windows](http://fsharp.org/use/windows/)
   - [Linux](http://fsharp.org/use/linux/)
   - [Mac](http://fsharp.org/use/mac/)

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
