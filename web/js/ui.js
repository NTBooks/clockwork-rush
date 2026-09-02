/*
 * Clockwork Rush - screens, options and wiring
 *
 * Port of MainPage.xaml.cs, HelpRoot and the Help* pages. Options live in the
 * same singleton shape GameOptions.cs used, persisted to localStorage so they
 * survive a reload the way the Store app's roaming settings did.
 */
(function (global) {
    'use strict';

    var OPTIONS_KEY = 'clockworkrush.options';
    var SAVE_KEY = 'clockworkrush.save';

    // GameOptions.cs defaults.
    var options = {
        sfx: true,
        music: 'A',
        rows: 20,
        cols: 40,
        colors: 4,
        startingRows: 3,
        speed: 1000,
        difficulty: 'normal'
    };

    // SetDifficulty(sender, rows, cols, colors, speed, startRows) - slider units.
    var PRESETS = {
        beginner: { row: 12, col: 10, colors: 2, speed: 2, startRows: 1 },
        normal:   { row: 10, col: 20, colors: 4, speed: 3, startRows: 3 },
        expert:   { row: 12, col: 30, colors: 4, speed: 5, startRows: 4 }
    };

    var $ = function (id) { return document.getElementById(id); };

    // ------------------------------------------------------------- options

    function loadOptions() {
        try {
            var raw = localStorage.getItem(OPTIONS_KEY);
            if (raw) {
                var saved = JSON.parse(raw);
                Object.keys(options).forEach(function (k) {
                    if (saved[k] !== undefined) { options[k] = saved[k]; }
                });
            }
        } catch (err) { /* private mode, keep defaults */ }
    }

    function saveOptions() {
        try { localStorage.setItem(OPTIONS_KEY, JSON.stringify(options)); }
        catch (err) { /* nothing we can do */ }
    }

    function readSave() {
        try {
            var raw = localStorage.getItem(SAVE_KEY);
            return raw ? JSON.parse(raw) : null;
        } catch (err) { return null; }
    }

    function writeSave(state) {
        try { localStorage.setItem(SAVE_KEY, JSON.stringify(state)); }
        catch (err) { /* quota */ }
    }

    function clearSave() {
        try { localStorage.removeItem(SAVE_KEY); } catch (err) { /* ignore */ }
    }

    // ------------------------------------------------------------- screens

    var current = 'menu';

    function show(name) {
        ['menu', 'game', 'help'].forEach(function (s) {
            $('screen-' + s).classList.toggle('is-active', s === name);
        });
        current = name;
    }

    // ---------------------------------------------------------- menu setup

    var sliders = {
        row: $('sliderRow'),
        col: $('sliderCol'),
        startRows: $('sliderStartRows'),
        colors: $('sliderColors'),
        speed: $('sliderSpeed')
    };

    var outputs = {
        row: $('outRow'),
        col: $('outCol'),
        startRows: $('outStartRows'),
        colors: $('outColors'),
        speed: $('outSpeed')
    };

    function syncOutputs() {
        Object.keys(sliders).forEach(function (k) {
            outputs[k].textContent = sliders[k].value;
        });
    }

    function setSlidersEnabled(enabled) {
        Object.keys(sliders).forEach(function (k) {
            sliders[k].disabled = !enabled;
        });
    }

    function applyDifficulty(name) {
        options.difficulty = name;

        Array.prototype.forEach.call(
            $('difficulty').querySelectorAll('.btn'),
            function (b) { b.classList.toggle('is-selected', b.dataset.difficulty === name); }
        );

        setSlidersEnabled(name === 'custom');

        if (name !== 'custom') {
            var p = PRESETS[name];
            sliders.row.value = p.row;
            sliders.col.value = p.col;
            sliders.colors.value = p.colors;
            sliders.speed.value = p.speed;
            sliders.startRows.value = p.startRows;
        }

        syncOutputs();
        saveOptions();
    }

    function pushSlidersToOptions() {
        // StartGame(): rows and cols are stored doubled, speed is a tick period.
        options.rows = parseInt(sliders.row.value, 10) * 2;
        options.cols = parseInt(sliders.col.value, 10) * 2;
        options.colors = parseInt(sliders.colors.value, 10);
        options.speed = 1000 + ((3 - parseInt(sliders.speed.value, 10)) * 200);
        options.startingRows = parseInt(sliders.startRows.value, 10);
        saveOptions();
    }

    function pullOptionsToSliders() {
        sliders.row.value = options.rows >> 1;
        sliders.col.value = options.cols >> 1;
        sliders.colors.value = options.colors;
        sliders.speed.value = 3 - ((options.speed - 1000) / 200);
        sliders.startRows.value = options.startingRows;
        syncOutputs();
    }

    function selectIn(groupId, attr, value) {
        Array.prototype.forEach.call(
            $(groupId).querySelectorAll('.btn'),
            function (b) { b.classList.toggle('is-selected', b.dataset[attr] === value); }
        );
    }

    // ------------------------------------------------------------ menu art

    /* The four idle gears MainPage spun beside the options. */
    var menuGearState = null;

    function initMenuGears() {
        var canvas = $('menuGears');
        var ctx = canvas.getContext('2d');

        var gears = [
            { r: 0.13, cx: 0.34, cy: 0.16, spin: 14 },
            { r: 0.17, cx: 0.60, cy: 0.30, spin: -11 },
            { r: 0.26, cx: 0.34, cy: 0.56, spin: 9 },
            { r: 0.34, cx: 0.66, cy: 0.82, spin: -7 }
        ].map(function (g) {
            g.path = global.Gear.makeGear(43, 50, 12, 8 + Math.floor(Math.random() * 6));
            g.angle = Math.random() * 360;
            return g;
        });

        menuGearState = { canvas: canvas, ctx: ctx, gears: gears, last: 0 };

        function frame(now) {
            if (current !== 'menu') {
                menuGearState.last = now;
                requestAnimationFrame(frame);
                return;
            }

            var dt = Math.min((now - menuGearState.last) / 1000, 0.1);
            menuGearState.last = now;

            var dpr = window.devicePixelRatio || 1;
            var w = canvas.clientWidth;
            var h = canvas.clientHeight;

            if (w > 0 && h > 0) {
                if (canvas.width !== Math.round(w * dpr)) {
                    canvas.width = Math.round(w * dpr);
                    canvas.height = Math.round(h * dpr);
                }

                ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
                ctx.clearRect(0, 0, w, h);

                var base = Math.min(w, h);
                gears.forEach(function (g) {
                    g.angle += g.spin * dt;

                    var size = base * g.r * 2;
                    ctx.save();
                    ctx.globalAlpha = 0.5;
                    ctx.fillStyle = 'rgba(244,244,245,.30)';
                    ctx.translate(w * g.cx, h * g.cy);
                    ctx.rotate(g.angle * Math.PI / 180);
                    ctx.scale(size / 100, size / 100);
                    ctx.translate(-50, -50);
                    ctx.fill(g.path);
                    ctx.restore();
                });
            }

            requestAnimationFrame(frame);
        }

        requestAnimationFrame(function (t) {
            menuGearState.last = t;
            frame(t);
        });
    }

    // ---------------------------------------------------------------- game

    var view = null;
    var engine = null;

    /* Primary input is coarse - i.e. a finger, not a mouse. Overridable with
       ?touch=1 so the touch layout can be checked on a desktop browser. */
    var isTouch = window.matchMedia('(pointer: coarse)').matches ||
                  /[?&]touch=1/.test(location.search);
    var portraitOverride = false;
    var lastGearPx = 999;

    function startGame(restoreState) {
        var e;

        if (restoreState) {
            e = global.RotetrisEngine.deserialize(restoreState);
            if (!e) { restoreState = null; }
        }

        if (!restoreState) {
            pushSlidersToOptions();
            e = new global.RotetrisEngine(options.rows, options.cols,
                                          options.startingRows, options.speed,
                                          options.colors);
        }

        engine = e;

        $('endOverlay').hidden = true;
        $('pauseOverlay').hidden = true;
        $('btnPause').classList.remove('is-on');
        $('score').textContent = '0';

        document.body.classList.toggle('touch-play', isTouch);
        portraitOverride = false;
        $('orientGate').hidden = true;
        show('game');

        if (!view) {
            view = new global.GameView($('boardWrap'), {
                onScore: function (v) { $('score').textContent = v; },
                onRotateFlash: function (which) { flashRotate(which); },
                onLayout: function (gearPx) { lastGearPx = gearPx; updateOrientGate(); },
                onGameOver: function (won, score) { showEnd(won, score); }
            });
        }

        // show() already applied the layout, so the board measures correctly
        // here. Resize again next frame in case fonts or scrollbars shift it.
        view.start(engine, !restoreState);
        view.resize();
        requestAnimationFrame(function () { view.resize(); });

        global.Sound.sfxEnabled = options.sfx;
        global.Sound.playMusic(options.music);
    }

    function leaveGame() {
        if (view) { view.stop(); }

        if (engine && !engine.gameEnded) {
            writeSave(engine.serialize());
        } else {
            clearSave();
        }

        engine = null;
        $('orientGate').hidden = true;
        document.body.classList.remove('touch-play');
        global.Sound.stopMusic();
        refreshResumeButton();
        show('menu');
    }

    /*
     * The board is much wider than it is tall, so on a phone held upright it
     * scales down until the gears are too small to hit. Rather than blanket
     * blocking portrait, this checks the size the gears actually came out at -
     * a 20-column board is nearly square and plays fine upright, a 60-column
     * one does not.
     */
    function updateOrientGate() {
        var gate = $('orientGate');
        if (current !== 'game' || !view) { gate.hidden = true; return; }

        var portrait = window.innerHeight > window.innerWidth;
        var tooSmall = lastGearPx < global.GameView.MIN_TOUCH_GEAR_PX;
        var block = isTouch && portrait && tooSmall && !portraitOverride;

        if (block) {
            $('orientWhy').textContent =
                'This board is ' + engine.Cols + ' columns wide, so upright it has to ' +
                'shrink the gears to about ' + Math.round(lastGearPx) + ' pixels. ' +
                'Landscape gives them room.';
        }

        gate.hidden = !block;
        view.setBlocked(block);
    }

    function showEnd(won, score) {
        clearSave();
        $('endTitle').textContent = won ? 'You Win!' : 'Game Over';
        $('endScore').textContent = score;
        $('endTime').textContent = 'Time ' + (view ? view.timeText : '00:00:00');
        $('endOverlay').hidden = false;
    }

    /* Blink the on-screen rotate button, as AddDoubleAnimation did. */
    function flashRotate(which) {
        var btn = $(which === 'R' ? 'btnRotR' : 'btnRotQ');
        btn.classList.remove('is-flash');
        void btn.offsetWidth;
        btn.classList.add('is-flash');
    }

    function refreshResumeButton() {
        $('btnResume').hidden = !readSave();
    }

    // ---------------------------------------------------------------- help

    var HELP = global.HELP_CONTENT;

    var helpTopic = null;
    var helpIndex = 0;

    function openHelpTopic(key) {
        helpTopic = HELP[key];
        helpIndex = 0;
        $('helpMenu').hidden = true;
        $('helpTopic').hidden = false;
        renderHelpSlide();
    }

    function closeHelpTopic() {
        helpTopic = null;
        $('helpMenu').hidden = false;
        $('helpTopic').hidden = true;
    }

    function renderHelpSlide() {
        if (!helpTopic) { return; }

        var img = $('helpImage');
        var diagram = $('helpControlsDiagram');
        var count = helpTopic.captions.length;

        $('helpTopicTitle').textContent = helpTopic.title;

        if (helpTopic.diagram) {
            img.hidden = true;
            img.removeAttribute('src');
            diagram.hidden = false;
            diagram.innerHTML = keyboardDiagram();
        } else {
            img.src = 'assets/img/' + helpTopic.images[helpIndex];
            img.hidden = false;
            diagram.hidden = true;
        }

        var caption = $('helpCaption');
        caption.innerHTML = '';
        helpTopic.captions[helpIndex].forEach(function (para) {
            var p = document.createElement('p');
            p.textContent = para;
            caption.appendChild(p);
        });

        $('helpCount').textContent = (helpIndex + 1) + ' / ' + count;
        $('helpCount').hidden = count < 2;
        $('helpPrev').disabled = helpIndex === 0;
        $('helpNext').disabled = helpIndex >= count - 1;
        $('helpPrev').hidden = count < 2;
        $('helpNext').hidden = count < 2;
    }

    /* Mirrors HelpControls.xaml, including the keys it greyed out. */
    function keyboardDiagram() {
        return '' +
            '<div class="key-cluster">' +
              '<h3>Board Rotation</h3>' +
              '<div class="key-rows">' +
                '<div class="key-row">' +
                  '<div class="key">Q</div>' +
                  '<div class="key is-off">E</div>' +
                  '<div class="key">R</div>' +
                  '<div class="key is-off">F</div>' +
                '</div>' +
              '</div>' +
            '</div>' +
            '<div class="key-cluster">' +
              '<h3>Movement</h3>' +
              '<div class="key-rows">' +
                '<div class="key-row">' +
                  '<div class="key is-blank"></div>' +
                  '<div class="key is-off">W</div>' +
                  '<div class="key is-blank"></div>' +
                '</div>' +
                '<div class="key-row">' +
                  '<div class="key">A</div>' +
                  '<div class="key">S</div>' +
                  '<div class="key">D</div>' +
                '</div>' +
              '</div>' +
            '</div>';
    }

    // ------------------------------------------------------------- wire-up

    function init() {
        document.body.classList.toggle('touch', isTouch);

        /*
         * On touch, every control shares one row under the board. Flanking
         * rotate buttons cost 120px of width, which on a 390px phone is 31%
         * of the screen - far more than a 44px row costs in height, and the
         * board is the thing that needs the room.
         */
        if (isTouch) {
            var bar = $('touchBar');
            bar.insertBefore($('btnRotR'), bar.firstChild);
            bar.appendChild($('btnRotQ'));
        }

        loadOptions();

        applyDifficulty(options.difficulty);
        if (options.difficulty === 'custom') { pullOptionsToSliders(); }

        selectIn('musicGroup', 'music', options.music);
        selectIn('sfxGroup', 'sfx', options.sfx ? 'on' : 'off');
        global.Sound.sfxEnabled = options.sfx;

        refreshResumeButton();
        initMenuGears();

        $('difficulty').addEventListener('click', function (ev) {
            var btn = ev.target.closest('[data-difficulty]');
            if (btn) { applyDifficulty(btn.dataset.difficulty); }
        });

        Object.keys(sliders).forEach(function (k) {
            sliders[k].addEventListener('input', function () {
                syncOutputs();
                if (options.difficulty === 'custom') { pushSlidersToOptions(); }
            });
        });

        $('musicGroup').addEventListener('click', function (ev) {
            var btn = ev.target.closest('[data-music]');
            if (!btn) { return; }
            options.music = btn.dataset.music;
            selectIn('musicGroup', 'music', options.music);
            saveOptions();
        });

        $('sfxGroup').addEventListener('click', function (ev) {
            var btn = ev.target.closest('[data-sfx]');
            if (!btn) { return; }
            options.sfx = btn.dataset.sfx === 'on';
            global.Sound.sfxEnabled = options.sfx;
            selectIn('sfxGroup', 'sfx', btn.dataset.sfx);
            saveOptions();
        });

        $('btnPlay').addEventListener('click', function () {
            global.Sound.unlock();
            clearSave();
            startGame(null);
        });

        $('btnResume').addEventListener('click', function () {
            global.Sound.unlock();
            var state = readSave();
            if (state) { startGame(state); }
            else { refreshResumeButton(); }
        });

        $('btnHelp').addEventListener('click', function () {
            closeHelpTopic();
            show('help');
        });

        $('helpBack').addEventListener('click', function () {
            if (!$('helpTopic').hidden) { closeHelpTopic(); }
            else { show('menu'); }
        });

        $('helpMenu').addEventListener('click', function (ev) {
            var btn = ev.target.closest('[data-help]');
            if (btn) { openHelpTopic(btn.dataset.help); }
        });

        $('helpTopicBack').addEventListener('click', closeHelpTopic);

        $('helpPrev').addEventListener('click', function () {
            if (helpIndex > 0) { helpIndex--; renderHelpSlide(); }
        });

        $('helpNext').addEventListener('click', function () {
            if (helpIndex < helpTopic.captions.length - 1) { helpIndex++; renderHelpSlide(); }
        });

        $('gameBack').addEventListener('click', leaveGame);
        $('btnQuitToMenu').addEventListener('click', leaveGame);
        $('btnEndMenu').addEventListener('click', leaveGame);

        $('btnPlayAgain').addEventListener('click', function () {
            clearSave();
            startGame(null);
        });

        $('btnPause').addEventListener('click', function () { togglePause(); });
        $('btnUnpause').addEventListener('click', function () { togglePause(false); });

        $('orientAnyway').addEventListener('click', function () {
            portraitOverride = true;
            updateOrientGate();
        });

        // Nudge buttons: one cell per press, for placement drag cannot land.
        $('touchBar').addEventListener('pointerdown', function (ev) {
            var btn = ev.target.closest('[data-nudge]');
            if (!btn || !view) { return; }
            global.Sound.unlock();
            view.engine.sendKey(btn.dataset.nudge);
            ev.preventDefault();
        });

        $('btnRotR').addEventListener('click', function () { if (view) { view.rotate('R'); } });
        $('btnRotQ').addEventListener('click', function () { if (view) { view.rotate('Q'); } });

        window.addEventListener('keydown', function (ev) {
            if (current !== 'game') { return; }
            if (ev.key === 'Escape' || ev.key === 'p' || ev.key === 'P') {
                togglePause();
                ev.preventDefault();
            }
        });

        window.addEventListener('resize', function () {
            if (view) { view.resize(); }
            updateOrientGate();
        });

        if (window.screen && screen.orientation) {
            screen.orientation.addEventListener('change', function () {
                setTimeout(function () {
                    if (view) { view.resize(); }
                    updateOrientGate();
                }, 120);
            });
        }

        window.addEventListener('beforeunload', function () {
            if (engine && !engine.gameEnded && current === 'game') {
                writeSave(engine.serialize());
            }
        });

        show('menu');
    }

    function togglePause(force) {
        if (!view || !engine || engine.gameEnded) { return; }

        var paused = force === undefined ? !view.userPaused : force;
        view.setPaused(paused);
        $('pauseOverlay').hidden = !paused;
        $('btnPause').classList.toggle('is-on', paused);
    }

    // Handle for the console: inspect or drive a running game.
    global.ClockworkRush = {
        options: options,
        getEngine: function () { return engine; },
        getView: function () { return view; },
        screen: function () { return current; }
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

}(window));
