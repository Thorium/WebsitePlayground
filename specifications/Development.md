
# General Instructions for Developers #

Here are some instructions on how to do general development tasks with this technology stack.

* [Frontend](#frontend)
* [Backend](#backend)
* [Package Management and 3rd Party References](#package-management-and-3rd-party-references)


### Frontend ###

While you do frontend development, you should have Gulp running (by `run.cmd` or just `gulp`) so that you can
observe any Html or JavaScript mistakes directly. The gulp window should beep if there are errors. Meanwhile, 
there are errors; the frontend site (in the dist folder) may not be updated, so fix them.

#### Debugging Frontend ####

As there are map-files you should be able to debug (and attach a breakpoint to the script) your browser script 
with the browser's developer tools. If your browser console window shows a failure, add a debug-point to that
position and reload the page: Now that you have a debug point in a failing script, see the stacktrace from browser
developer tools to observe more information.

#### Adding a New Page ####

Adding a new HTML page: You can add a new page, e.g. `some.html`, or copy some existing as new. If you make it totally new, please copy the content 
inside `<head>...</head>` tag from an existing page: the existing `link` and `script` tags at least.

Then also, add or copy the corresponding TypeScript file from the `frontend/scripts` folder to, e.g. `some.ts`. This TypeScript file contains all the Javascripts 
(and TypeScripts) for the page. It has this `export function initSomething(locale)` ("Something" may vary.), which is called after the jQuery 
has been loaded, and SignalR-connection is running. So you don't need to check jQuery loading manually anymore in your scripts.

For this file, the possible `import`s at the beginning of the file are imports from other TypeScript components, e.g.
`import tools = require("./tools");` would import `tools.ts`. 

Next, you need to modify `app.ts`. App.ts is the script that loads first. Add a new `import idx = require('./some');` to import your `some.ts`.
Then locate `if(window.location.href.indexOf...`s and add correspondingly your new line, e.g.:

```
if(window.location.href.indexOf("some.html") > 0){ some.initSomething(locale); }
```

This will call the init function when the page loads. It's not the world's most sophisticated frontend-router, but it will get you started. 

`tools.ts` are some common scripts. Other files to include would be other common functionality like SingalR-communication,
or React.js components. React.js components, `*.tsx`, contain either common HTML templates for multiple pages, or a component 
of producing some kind of list of some template components, like "a custom view of search results".

Pages have div id "pageLoader" and div id "pageLoaded". The idea is that pageLoader is the loading-progress-circle, and pageLoaded has the page content. pageLoaded is initially not visible, but `app.ts` will hide pageLoader and show pageLoaded when the init-call has been done. 
Now, if your page has errors, it might be that the browser won't execute JavaScript to the end, and the pageLoaded stays invisible: It seems like the page is loading forever, even if nothing is actually loading.

Now, check the errors from Gulp-window and correct them!
You can access your new page from the webserver like all the other pages (i.e. direct url call at least before you make the links to other pages). Include all the page JavaScripts in these .ts-files and not in script-tags.

#### Calling Serverside Methods via SignalR API Method ####

In your `*.ts`-file you call corresponding server functions with SignalR methods with the hub class
(e.g. if you use SignalHub class, there is already `import signalhub = require("./signalhub");` and then
you call:

```
signalhub.signalHub.server.myMethod(parameter).done(function (data) {
    // do something with the response data...
}).fail(function () {
    alert("myMethod failed.");
});
```

Things to know:

- Serverside uppercase `MyMethod` starts with lowercase in JavaScript `myMethod`.
- You can transfer complex objects if you want (they are serialized as JSON).
- The return types: There are already some helper functions in `tools.ts`.
- But still, From F#, it's often easier to say `|> Map.ofSeq` than to return a list of tuples.
- If you try to call these outside your page's init method (why would you?), ensure that SignalR connection is loaded.
- Explicitly handle the fail-case, or it's swollen.

#### React.js Components ####

We are using React.js for common HTML templates/components. 
The files are located under `frontend/script/*.tsx`.

The simplest example of a common component would be like this:

```
var MyFooter = React.createClass({
  render: function() {
    const rem06 = { height: '0.6rem'};
    return (<div className="footer" style={rem06}></div>);
  }
});

export function renderFooter() {
    const footer = document.getElementById('footerPlace');
    ReactDOM.render(<MyFooter />, footer);
}
```

Now you just call a `myComponent.renderFooter()` from another `.ts` script where 
`import myComponent = require("./myComponent");` and your html-page is having the `<div id="footerPlace"></div>`.

The main differences to "normal" HTML: 

- `class` attribute has to be `className`
- You can't use `style` attribute directly
- Constants are marked as double quotes `"footer"` but variables with curly brackets `{rem06}` (without quotes!)
- Be careful with JavaScript this-keyword: Create temp-variables at the beginning of the function to avoid using `this` inside inner functions / lambdas. 

To make a dynamic list of templates, you can create another react component inside your react component:

```
interface IMyItemData { key : string; myParameter : any; }
let MyItem = React.createClass<IMyItemData, any>({ /* ... rendering ... */ });

// ...
// And then in another component:
_.map(myCollection, function(item:any, idx) { 
              // Maybe some data processing...
              return (<MyItem key={idx} myParameter={(idx+1)} />);
```


### Backend ###

If you do backend modifications, you have to run `build` in one way or another: This can be done from the development
IDE (e.g. Visual Studio), or you can run `build project` from the command line. If you want to build the full package
in release mode instead of development, you will execute `build package Configuration=Release`

Namespaces:

- Domain - Main business domain and logics
- Scheduler - Scheduled maintenance tasks
- SignalHubs - Business logics communication to frontend
- OwinStart - Web-server and WebAPI

#### Debugging Backend ####

If you want to debug the system, you can just attach your debugger to the backend webserver executable process
or you can kill the server, keep the gulp running, and start the server from your development IDE (Visual Studio, `F5` button).

#### Configuration ####

Runtime configuration changes can be done to app.config items without a new build: `App.config` will be your `*.exe.config`
file in production. These values are accessible from the source code via `ConfigurationManager.AppSettings.["..."]`.

##### Creating SignalR API Method ####

To add a SignalR class member, it's usually better to explicitly state the input parameters, e.g.:
`member __.MyMethod(input:string, input2:int) =`. Even F# is usually able to know the results, but the problem may
occur if you are trying to expose too many generic parameters from the SignalR API.

SignalR supports `Task<...>` output parameter for asynchrony. 
This means that if you use F# `async { ... }` you have to convert F# `Async<...>` to `Task<...>` like this:

```fsharp
member __.MyMethod(input:string, input2:int) =
    async {
        // do your stuff here...
        // return something
    } |> Async.StartAsTask
```

#### Transactions And SQLProvider ####

It's a best practice to separate read and write operations from each other (`command query separation`).
If you use only read operation to your database, you may not have a transaction. Transaction is a way to say 
that "either all the operations succeed or fail, not half of them". If you need a transaction
it's best practice to wrap the whole operation (e.g. SignalR call) into a single transaction but 
keep your transactions small to avoid database locks.

If you don't use write operations in your method and don't need a transaction, you can just use `dbReadContext`.
If you need a transaction, you want to create a new database connection to commit to this created transaction.
There is a helper method called `writeWithDbContext`, which takes a function that takes the database
context as a parameter. 

You can use it like this:

```fsharp
member __.MyMethod(input:string, input2:int) =
    writeWithDbContext <| fun (dbContext:DataContext) ->
        async {
            // do your dbContext-stuff here...
            do! dbContext.SubmitUpdates2()
            // return something
        } |> Async.StartAsTask
```

Mono has some problems with Async transactions so that's why Windows server (or virtual machine) is recommended for 
the production environment.


### Package Management and 3rd Party References ###

Don't commit the file directly to the source control for multiple reasons:

- We want to update the components easily, e.g. know the security fixes.
- We don't want to have binary files in our Git version control as it handles binary file changes poorly and increasing repository size will slow down Git.
- We don't want to maintain component code that is not ours

So please use package management, Paket, and here is how.
I also recommend package changes be done in separate commits so don't mix those into other commits:
See that you don't have many changes before the modifications, and after done, commit the changes to Git.

#### Updating Components for Backend ####

First, you check what packages would be available for updates. Write to console:

```
.paket\paket.exe outdated
```

Or with Mono:

```
mono .paket\paket.exe outdated
```

This will tell you the outdated packages, for example:

```
...
Outdated packages found:
  Group: Build
    * FAKE 4.39 -> 4.39.1
  Group: Main
    * Hopac 0.3.13 -> 0.3.15
    * Logary 4.0.101 -> 4.0.112
    * SQLProvider 1.0.31 -> 1.0.32
```

Now you can update all packages by saying `.paket\paket.exe update` or a single 
NuGet package from group Main by saying e.g.: `.paket\paket.exe update group Main NuGet SQLProvider`.
This will update the `paket.lock` file, which will keep track of the versions that the system uses. That's it.

#### Adding New Component to Backend ####

You have some package from (NuGet)[http://www.nuget.org] package id, and you want to add it to the system.
First, edit the `paket.dependencies` to contain the package id in a group you like. Then go to your folder
containing the project file (`*.fsproj`, e.g. `backend`) and edit the `paket.references` file to contain 
the package id. Then run on the console:

```
.paket\paket.exe install
```

Or with Mono:

```
mono .paket\paket.exe install
```

And that's it. Paket should modify the `paket.lock` file and project files `*.fsproj`.

#### Adding New Component to Frontend ####

You have some files, e.g. JavaScript, from the internet, maybe from GitHub, and you want to add them to the infra. 
First, add a component to the `paket.dependencies` file, like there are already some others.

If your component is a JavaScript component, you can also search (from Google) the GitHub project 
borisyankov/DefinitelyTyped for the corresponding TypeDefinitions and add those to the 
`paket.dependencies` file. Then run on the console:

```
.paket\paket.exe install
```

or with Mono:

```
mono .paket\paket.exe install
```

Now your file has been added to the `paket-files` folder, which content is merged to deployment automatically. 
Then you have to run `gulp` or `gulp deploy` once. So that's it. But if you also have the 
type definitions, there is still one more step: Edit file `frontend/scripts/_references.d.ts` to contain 
the reference to your new type definitions. This is just for JavaScript editor intellisense.
