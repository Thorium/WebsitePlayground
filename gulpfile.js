//Gulp tutorial: http://travismaynard.com/writing/getting-started-with-gulp
 'use strict';

//npm install

var gulp = require('gulp'),
    jshint = require('gulp-jshint'),
    concat = require('gulp-concat'),
    uglify = require('gulp-uglify'),
//    plato = require('gulp-plato'),
    rename = require('gulp-rename'),
    autoprefixer = require('gulp-autoprefixer'),
    sourcemaps = require('gulp-sourcemaps'),
    less = require('gulp-less'),
    sass = require('gulp-sass'),
    gulpMerge = require('gulp-merge'),
    mincss = require('gulp-minify-css'),
    flatten = require('gulp-flatten'),
    gulpif = require('gulp-if'),
    gutil = require('gulp-util'),
    browserify = require('browserify'),
    tsify = require('tsify'),
    source = require('vinyl-source-stream'),
    streamify = require('gulp-streamify'),
    reactify = require('reactify'),
    tslint = require('gulp-tslint'),
    stylishts = require('gulp-tslint-stylish'),
    htmlhint = require("gulp-htmlhint"),
	htmlmin = require('gulp-htmlmin')
	;

// Set to true for production build. gulp deploy --release ok
var isRelease = gutil.env.release ? true : false;
process.env.NODE_ENV = isRelease ? 'production' : 'development';

var excludeReact = isRelease? "!paket-files/clientside/unpkg.com/react.development.js" : "!paket-files/clientside/unpkg.com/react.production.min.js"
var excludeFoundation = isRelease? "!paket-files/**/foundation.css" : "!paket-files/**/foundation.min.css"
var excludeSignalR = isRelease? "!paket-files/**/jquery.signalR.js" : "!paket-files/**/jquery.signalR.min.js";
var excludePaketGithubBinaries = "!paket-files/github.com/**/*.*"

var files = {
    targetPath: 'frontend/dist',
    typescripts: ['frontend/scripts/*.ts', 'frontend/scripts/*.tsx'],
    jslibs: ['paket-files/clientside/ajax.aspnetcdn.com/jquery.min.js',
             'paket-files/clientside/lodash/lodash/dist/lodash.min.js',
             'paket-files/clientside/cdnjs.cloudflare.com/modernizr.min.js',
             'paket-files/clientside/unpkg.com/react.production.min.js',
             'paket-files/clientside/unpkg.com/react.development.js',
             'paket-files/clientside/npmcdn.com/tether.min.js',
             'paket-files/clientside/**/*.js',
              excludeReact, excludeSignalR, excludePaketGithubBinaries], // Gulp is intelligent enough to not include same twice

    lessstyles: ['frontend/styles/*.less'],
    sassstyles: ['frontend/styles/*.scss'],
    cssstyles: ['frontend/styles/*.css'],
    csslibs: ['paket-files/**/*.css', excludeFoundation, excludePaketGithubBinaries],
    htmls: ['frontend/**/*.html', '!frontend/dist/**/*.html'],
    txts: ['frontend/**/*.txt', '!frontend/dist/**/*.txt'],
    statics: ['frontend/*.ico'],
    fonts: ['paket-files/**/*font*.svg',
        'frontend/fonts/*.*',
        'paket-files/**/*.eot',
        'paket-files/**/*.otf',
        'paket-files/**/*.ttf',
        'paket-files/**/*.woff',
        'paket-files/**/*.woff2', excludePaketGithubBinaries],
    images: 'frontend/img/**/*.*',
    jqueryImages: 'frontend/jqueryImages/**/*.*'
};

var errorHandler = function(title) {
  return function(err) {
    gutil.log(gutil.colors.red('[' + title + ']'), err.toString());
    this.emit('end');
  };
};

gulp.task('typeScripts', function () {
    return browserify({
            entries: ['./frontend/scripts/_references.d.ts','frontend/scripts/app.ts'],
            transform: [reactify],
            debug: !isRelease})
        .plugin('tsify', {
            /* noImplicitAny: true, */
            jsx: 'react',
            target: 'ES5' })
        .bundle()
        .on('error', function (error) { gutil.log(gutil.colors.red('[TypeScript]'), error.toString()); this.emit('end'); })
        //.pipe(process.stdout)
        .pipe(source('app.min.js'))
        .pipe(gulpif(isRelease, streamify(uglify())))
        .pipe(gulp.dest(files.targetPath + '/js'));
});

function concatJavaScripts(items, target) {
    return gulp.src(items)
        .pipe(concat(target, {newLine: ';\n'}))
        .pipe(gulp.dest(files.targetPath + '/js'));
}

gulp.task('concatLibs', function () { concatJavaScripts(files.jslibs, '/libs.min.js');});

// concatenate & minify css
function minifycss(items, target) {
    return gulp.src(items)
      .pipe(flatten())
      .pipe(autoprefixer('last 2 version'))
      .pipe(concat(target+'.css'))
      .pipe(rename({suffix: '.min'}))
      .pipe(mincss())
      .pipe(gulp.dest(files.targetPath + '/css'));
}

function minifystyles(target) {
    var lessfile = gulp.src(files.lessstyles)
	  .pipe(gulpif(!isRelease, sourcemaps.init()))
	  .pipe(less()).on('error', errorHandler('Less'))
      .pipe(autoprefixer('last 2 version'));

	var sassfile = gulp.src(files.sassstyles)
	  .pipe(gulpif(!isRelease, sourcemaps.init()))
	  .pipe(sass()).on('error', errorHandler('Sass'))
      .pipe(autoprefixer('last 2 version'));

	var cssfile = gulp.src(files.cssstyles)
	  .pipe(gulpif(!isRelease, sourcemaps.init()))
      .pipe(autoprefixer('last 2 version'));
	  
    return gulpMerge(cssfile, gulpMerge(lessfile, sassfile))
      .pipe(concat(target+'.css'))
      .pipe(rename({suffix: '.min'}))
      .pipe(mincss())
      .pipe(gulpif(!isRelease, sourcemaps.write({includeContent: true, sourceRoot: '/css/'})))
      .pipe(gulp.dest(files.targetPath + '/css'));
}

// Lint Task, JavaScript
function lintjs(lintfiles) {
    return gulp.src(lintfiles)
        .pipe(jshint())
        .pipe(jshint.reporter('jshint-stylish'))
// This takes about 30sec, so skipping it now:
//        .pipe(plato('./report', {
//            jshint: { options: {strict: true} },
//            complexity: { trycatch: true }
//        }))
        ;
}

gulp.task('tslint', function(){
      return gulp.src(files.typescripts)
        .pipe(tslint())
        .pipe(tslint.report(stylishts, {
            emitError: true,
            sort: true,
            bell: true
        }));
});

gulp.task('styles', function () { minifystyles('app');});
gulp.task('styleslib', function () { minifycss(files.csslibs, 'libs');});

gulp.task('images', function () { return gulp.src(files.images).pipe(gulp.dest(files.targetPath + '/img/'));});
gulp.task('jqueryImages', function () { return gulp.src(files.jqueryImages).pipe(gulp.dest(files.targetPath + '/css/images/'));});
gulp.task('fonts', function () { return gulp.src(files.fonts).pipe(flatten()).pipe(gulp.dest(files.targetPath + '/fonts'));});
gulp.task('statics', function () { return gulp.src(files.statics).pipe(gulp.dest(files.targetPath));});
gulp.task('maps', function () { return gulp.src('paket-files/**/*.map').pipe(flatten()).pipe(gulpif(!isRelease, gulp.dest(files.targetPath + '/js')));});
gulp.task('htmls', function () {
    return gulp.src(files.htmls)
               .pipe(htmlhint('.htmlhintrc'))
			   .pipe(htmlhint.reporter("htmlhint-stylish"))
			   .pipe(htmlhint.failReporter({ suppress: true }))
			   .on('error', errorHandler('HTML'))
// Nice lint but gives wrong line numbers:
//               .pipe(jshint.extract('always'))
//               .pipe(jshint())
//               .pipe(jshint.reporter('default'))
               .pipe(gulpif(isRelease, htmlmin({collapseWhitespace: true})))
               .pipe(gulp.dest(files.targetPath));
});
gulp.task('txts', function () {
    return gulp.src(files.txts)
               .pipe(gulp.dest(files.targetPath));
});
gulp.task('deployStatic', ['htmls', 'txts', 'fonts', 'jqueryImages', 'images', 'statics']);


// Watch Files For Changes
gulp.task('watch', function () {
    gulp.watch(files.lessstyles, ['styles']);
    gulp.watch(files.sassstyles, ['styles']);
    gulp.watch(files.fonts, ['fonts']);
    gulp.watch(files.images, ['images']);
    gulp.watch(files.htmls, ['htmls']);
    gulp.watch(files.statics, ['statics']);
    gulp.watch(files.typescripts, ['tslint', 'typeScripts']);
});

// Default Task
gulp.task('deploy', ['deployStatic', 'tslint', 'typeScripts', 'styles', 'styleslib', 'concatLibs', 'maps']);
gulp.task('default', ['deploy', 'watch']);
