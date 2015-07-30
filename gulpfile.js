//Gulp tutorial: http://travismaynard.com/writing/getting-started-with-gulp
 'use strict';

//npm install --save-dev gulp gulp-jshint gulp-concat gulp-uglify gulp-plato gulp-rename gulp-sourcemaps gulp-less gulp-minify-css gulp-react gulp-autoprefixer gulp-flatten

var gulp = require('gulp'),
    jshint = require('gulp-jshint'),
    concat = require('gulp-concat'),
    uglify = require('gulp-uglify'),
//    plato = require('gulp-plato'),
    rename = require('gulp-rename'),
    autoprefixer = require('gulp-autoprefixer'),
    sourcemaps = require('gulp-sourcemaps'),
    less = require('gulp-less'),
    mincss = require('gulp-minify-css'),
    react = require('gulp-react'),
    flatten = require('gulp-flatten'),
    reactlint = require('jshint-jsx')
	;

var files = {
    targetPath: 'frontend/dist',
    javascripts: ['frontend/js/*.js'],
    templates: ['frontend/js/*.jsx'],
    jslibs: ['paket-files/ajax.aspnetcdn.com/jquery.min.js','paket-files/**/*.js'],  //TODO check if jquery included twice
    styles: ['frontend/styles/*.less'],
    csslibs: ['paket-files/**/*.css'],
    htmls: ['frontend/*.html'],
    statics: ['frontend/*.ico'],
    fonts: ['paket-files/**/*font*.svg',
        'frontend/fonts/*.*',
        'paket-files/**/*.eot',
        'paket-files/**/*.otf',
        'paket-files/**/*.ttf',
        'paket-files/**/*.woff',
        'paket-files/**/*.woff2'],
    images: 'frontend/img/**/*.*'
};

// Concatenate & Minify JS
function scripts(items, target) {
    return gulp.src(items)
        .pipe(flatten())
        .pipe(sourcemaps.init())
        .pipe(concat(target))
        .pipe(uglify())
        .pipe(sourcemaps.write('/.'))
        .pipe(gulp.dest(files.targetPath + '/js'));
}

function compilereact(items, target) {
    return gulp.src(items)
        .pipe(flatten())
        .pipe(sourcemaps.init())
        .pipe(react())
        .pipe(concat(target))
        .pipe(uglify())
        .pipe(sourcemaps.write('/.'))
        .pipe(gulp.dest(files.targetPath + '/js'));
}

function concatJavaScripts(items, target) {
    return gulp.src(items)
        .pipe(concat(target))
        .pipe(gulp.dest(files.targetPath + '/js'));
}

gulp.task('concatLibs', function () { concatJavaScripts(files.jslibs, '/libs.min.js');});
gulp.task('minifyApp', function () { scripts(files.javascripts, '/app.min.js');});
gulp.task('compileReact', function () { compilereact(files.templates, '/templates.min.js');});

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

function minifyless(items, target) {
    return gulp.src(items)
	  .pipe(sourcemaps.init())
	  .pipe(less())
      .pipe(autoprefixer('last 2 version'))
      .pipe(concat(target+'.css'))
      .pipe(rename({suffix: '.min'}))
      .pipe(mincss())
      .pipe(sourcemaps.write('/.'))
      .pipe(gulp.dest(files.targetPath + '/css'));
}

// Lint Task, JavaScript
gulp.task('lintjs', function () {
    var lintfiles = files.javascripts; //.concat(files.somemore);
    return gulp.src(lintfiles)
        .pipe(jshint())
        .pipe(jshint.reporter('default'))
// This takes about 30sec, so skipping it now:
//        .pipe(plato('./report', {
//            jshint: { options: {strict: true} },
//            complexity: { trycatch: true }
//        }))
        ;
});

gulp.task('lintjsx', function () {
    return gulp.src(files.templates)
        .pipe(jshint({ linter: reactlint.JSXHINT }))
        .pipe(jshint.reporter('default'));
});

gulp.task('styles', function () { minifyless(files.styles, 'app');});
gulp.task('styleslib', function () { minifycss(files.csslibs, 'libs');});

gulp.task('images', function () { return gulp.src(files.images).pipe(gulp.dest(files.targetPath + '/img/'));});
gulp.task('fonts', function () { return gulp.src(files.fonts).pipe(flatten()).pipe(gulp.dest(files.targetPath + '/fonts'));});
gulp.task('statics', function () { return gulp.src(files.statics).pipe(gulp.dest(files.targetPath));});
gulp.task('htmls', function () { 
    return gulp.src(files.htmls)
// Nice lint but gives wrong line numbers:
//               .pipe(jshint.extract('always'))    
//               .pipe(jshint())
//               .pipe(jshint.reporter('default'))
               .pipe(gulp.dest(files.targetPath));
});
gulp.task('deployStatic', ['htmls', 'fonts', 'images', 'statics']);


// Watch Files For Changes
gulp.task('watch', function () {
    gulp.watch(files.templates, ['lintjsx', 'compileReact']);
    gulp.watch(files.javascripts, ['lintjs', 'minifyApp']);
    gulp.watch(files.styles, ['styles']);
    gulp.watch(files.fonts, ['fonts']);
    gulp.watch(files.images, ['images']);
    gulp.watch(files.htmls, ['htmls']);
    gulp.watch(files.statics, ['statics']);
});

// Default Task
gulp.task('deploy', ['deployStatic', 'lintjs', 'lintjsx', 'compileReact', 'styles', 'styleslib', 'concatLibs', 'minifyApp']);
gulp.task('default', ['deploy', 'watch']);
