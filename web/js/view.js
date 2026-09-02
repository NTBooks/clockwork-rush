/*
 * Clockwork Rush - board view (DOM + CSS)
 *
 * Port of GearMaker/GearMaker/GamePage.xaml.cs, keeping the original's
 * separation: the engine is the model and raises events, this file is the
 * view, and the handlers at the bottom are the controller.
 *
 * The original animated with XAML Storyboards - it never computed an
 * in-between frame itself, it declared a target and let the compositor get
 * there. This does the same with CSS transitions. There is no per-frame
 * render loop; the only repeating timer is the engine tick.
 *
 * Rotation leans on the fact the help screens spell out: "the game board is
 * in 4 states (0, 90, 180, and 270)". A gear's image is baked once and never
 * changes - a quarter turn is just `angle += 90` on its transform, and CSS
 * tweens it. That also means a gear's displayed colours are its base pattern
 * rotated by (angle / 90), which is what reconcile() checks against the model.
 *
 * Board metrics come straight from the original: a gear is 24px of cell plus
 * 3px of horizontal padding, drawn 48x48 so it spans two cells.
 */
(function (global) {
    'use strict';

    var GEAR_SIZE = 24;
    var H_SPACE = 3;
    var V_SPACE = 0;
    var CELL_W = GEAR_SIZE + H_SPACE;          // 27
    var CELL_H = GEAR_SIZE + V_SPACE;          // 24
    var GEAR_BOX = 48;
    var TILE = 64;
    var PAD = (TILE - GEAR_BOX) / 2;           // 8: the outline rings overhang

    // Durations, from the Storyboard calls in the original.
    var MOVE_MS = 80;                          // AddMovementAnimation
    var ROT_MS = 150;                          // RotateBlock
    var FADE_SLOW_MS = 1000;
    var FADE_FAST_MS = 100;
    var POP_MS = 3000;                         // score number fade
    var BAR_MS = 1000;                         // time bar descent

    /* Points the clock takes back every tick. GamePage.xaml.cs:510 did
       ModScore(-5, 0, 0) under the comment "Deduct points for slowness".
       Set to 0 to play without the drain. */
    var SCORE_PER_TICK = -5;
    var SCORE_PER_CLEAR = 200;

    /* Rows to lift the piece above a fingertip so it is not covered. A mouse
       pointer grabs the piece by its middle, as the original did. */
    var TOUCH_LIFT = 3;
    var MOUSE_LIFT = 1;

    /* Below this many CSS pixels a gear is too small to aim at by touch. */
    var MIN_TOUCH_GEAR_PX = 22;

    function randInt(n) { return n <= 0 ? 0 : Math.floor(Math.random() * n); }

    /* One clockwise quarter turn of a TL,TR,BL,BR pattern. */
    function rotPattern(p, n) {
        n = ((n % 4) + 4) % 4;
        for (var i = 0; i < n; i++) { p = p[2] + p[0] + p[3] + p[1]; }
        return p;
    }

    function el(tag, cls) {
        var e = document.createElement(tag);
        if (cls) { e.className = cls; }
        return e;
    }

    /*
     * Positions are stored as board units in custom properties, not as pixels.
     * The board's --s multiplies them in CSS, so resizing is one property write
     * instead of repositioning every gear, and nothing is texture-scaled.
     */
    function place(node, x, y, angle) {
        node.style.setProperty('--x', x - PAD);
        node.style.setProperty('--y', y - PAD);
        node.style.setProperty('--a', (angle || 0) + 'deg');
    }

    /* One vector image per colour combination; the browser re-rasterises it
       at whatever size the gear actually is. */
    function paintGear(node, pattern) {
        node.style.backgroundImage = 'url("' + global.Gear.gearSVGURL(pattern) + '")';
    }

    // ----------------------------------------------------------------- view

    function GameView(wrap, opts) {
        this.wrap = wrap;
        this.opts = opts || {};

        this.engine = null;
        this.rows = 0;
        this.cols = 0;
        this.boardW = 0;
        this.boardH = 0;
        this.scale = 1;

        this.statics = [];                     // {row, col, base, angle, node}
        this.pieces = {};                      // name -> {base, node}
        this.bgGears = [];

        this.trueScore = 0;
        this.shownScore = 0;
        this.tickResponded = 0;
        this.staticAdded = false;
        this.lastPieceAdded = null;

        this.gameOver = false;
        this.userPaused = false;

        this.timeText = '00:00:00';
        this.dragging = false;
        this.dragPiece = null;
        this.lastKeyTime = 0;

        this._timer = null;
        this._nextDue = 0;
        this._scoreTimer = null;
        this._bannerTimer = null;
        this.blocked = false;
        this.running = false;

        this._buildChrome();
        this._bind();
    }

    GameView.prototype._buildChrome = function () {
        this.wrap.innerHTML = '';

        this.board = el('div', 'board');
        this.layerBg = el('div', 'layer layer-bg');
        this.plate = el('div', 'board-plate');
        this.timeBar = el('div', 'time-bar');
        this.clock = el('div', 'ghost-clock');
        this.layerGears = el('div', 'layer layer-gears');
        this.layerFx = el('div', 'layer layer-fx');

        this.board.appendChild(this.layerBg);
        this.board.appendChild(this.timeBar);
        this.board.appendChild(this.clock);
        this.board.appendChild(this.plate);
        this.board.appendChild(this.layerGears);
        this.board.appendChild(this.layerFx);
        this.wrap.appendChild(this.board);
    };

    // ------------------------------------------------------------ lifecycle

    GameView.prototype.start = function (engine, fresh) {
        this.stop();

        this.engine = engine;
        this.rows = engine.Rows;
        this.cols = engine.Cols;
        this.boardW = CELL_W * this.cols;
        this.boardH = CELL_H * this.rows;

        this.statics = [];
        this.pieces = {};
        this.trueScore = 0;
        this.shownScore = 0;
        this.tickResponded = 0;
        this.staticAdded = false;
        this.gameOver = false;
        this.userPaused = false;
        this.timeText = '00:00:00';

        this.layerGears.innerHTML = '';
        this.layerFx.innerHTML = '';
        this.layerBg.innerHTML = '';
        this.board.classList.remove('is-over');

        this.board.style.setProperty('--bw', this.boardW);
        this.board.style.setProperty('--bh', this.boardH);
        this.plate.style.backgroundImage =
            'url("' + global.Gear.dotGridURL(this.boardW, this.boardH, CELL_W, CELL_H) + '")';
        this.clock.textContent = this.timeText;
        this.timeBar.style.setProperty('--y', 0);
        this.timeBar.classList.remove('is-alert', 'is-grabbing');

        this._buildBackground();
        this._wire(engine);
        this.resize();

        if (fresh) { engine.configAndStart(); }
        else { engine.forceStart(); }

        this.running = true;
        this._nextDue = performance.now() + engine.TickMs;
        this._schedule();
        this._startScoreChase();
    };

    GameView.prototype.stop = function () {
        this.running = false;
        if (this._timer) { clearTimeout(this._timer); this._timer = null; }
        if (this._scoreTimer) { clearInterval(this._scoreTimer); this._scoreTimer = null; }
        if (this._bannerTimer) { clearInterval(this._bannerTimer); this._bannerTimer = null; }
    };

    GameView.prototype._buildBackground = function () {
        this.bgGears = [];
        var count = randInt(5) + 1;

        for (var i = 0; i < count; i++) {
            var g = global.Gear.makeBackgroundGear(this.boardW, this.boardH);
            var node = el('div', 'bg-gear');
            var draw = g.size * 1.3;

            node.style.setProperty('--w', draw);
            node.style.setProperty('--x', g.left + g.size / 2 - draw / 2);
            node.style.setProperty('--y', g.top + g.size / 2 - draw / 2);
            node.style.backgroundImage = 'url(' + g.url + ')';
            node.style.opacity = Math.min(g.size / this.boardW, 1) * 0.9;

            // One slow, endless turn - the original nudged these a degree per
            // engine tick, which is the same thing without the per-tick work.
            node.style.animationDuration = (360 / (1000 / this.engine.TickMs)) + 's';
            node.style.animationDirection = g.spin > 0 ? 'normal' : 'reverse';

            this.layerBg.appendChild(node);
            this.bgGears.push(node);
        }
    };

    GameView.prototype._wire = function (e) {
        var self = this;
        e.on('staticAdded', function (ev) { self.onStaticAdded(ev); });
        e.on('staticRemoved', function (ev) { self.onStaticRemoved(ev); });
        e.on('pieceAdded', function (ev) { self.onPieceAdded(ev); });
        e.on('pieceMoved', function (ev) { self.onPieceMoved(ev); });
        e.on('pieceRemoved', function (ev) { self.onPieceRemoved(ev); });
        e.on('rotatedCW', function (ev) { self.onRotated(ev, true); });
        e.on('rotatedCCW', function (ev) { self.onRotated(ev, false); });
        e.on('gameTick', function (ev) { self.onGameTick(ev); });
        e.on('won', function () { self.onGameEnd(true); });
        e.on('lost', function () { self.onGameEnd(false); });
    };

    // ----------------------------------------------------------- tick clock

    GameView.prototype._schedule = function () {
        var self = this;
        var delay = Math.max(this._nextDue - performance.now(), 0);
        this._timer = setTimeout(function () { self._fire(); }, delay);
    };

    GameView.prototype._fire = function () {
        this._timer = null;
        if (!this.running || !this.engine) { return; }

        if (this.userPaused || this.blocked || document.hidden) {
            // Stay wound but do not advance, like the original's Pause().
            this._nextDue = performance.now() + this.engine.TickMs;
            this._schedule();
            return;
        }

        this._nextDue += this.engine.TickMs;
        // If the tab was throttled, resync rather than firing a burst.
        if (performance.now() - this._nextDue > this.engine.TickMs * 3) {
            this._nextDue = performance.now() + this.engine.TickMs;
        }

        this.engine.tick();

        if (this.running && this.engine.running) { this._schedule(); }
    };

    // -------------------------------------------------------------- helpers

    GameView.prototype.cellX = function (col) { return col * CELL_W; };
    GameView.prototype.cellY = function (row) { return row * CELL_H; };

    GameView.prototype.findStatic = function (row, col) {
        for (var i = 0; i < this.statics.length; i++) {
            if (this.statics[i].row === row && this.statics[i].col === col) {
                return this.statics[i];
            }
        }
        return null;
    };

    GameView.prototype._makeGear = function (pattern, cls) {
        var node = el('div', 'gear ' + cls);
        paintGear(node, pattern);
        return node;
    };

    // ---------------------------------------------------------- view events

    GameView.prototype.onStaticAdded = function (e) {
        this.staticAdded = true;

        var existing = this.findStatic(e.row, e.col);
        if (existing) {
            existing.base = e.contents;
            existing.angle = 0;
            paintGear(existing.node, e.contents);
            place(existing.node, this.cellX(e.col), this.cellY(e.row), 0);
            return;
        }

        var node = this._makeGear(e.contents, 'static');
        // Set the transform before insertion so no transition fires on birth.
        place(node, this.cellX(e.col), this.cellY(e.row), 0);
        this.layerGears.appendChild(node);

        if (this.gameOver) {
            node.animate([{ opacity: 0 }, { opacity: 1 }],
                         { duration: 2000, easing: 'linear', fill: 'both' });
        }

        this.statics.push({ row: e.row, col: e.col, base: e.contents, angle: 0, node: node });
    };

    GameView.prototype.onStaticRemoved = function (e) {
        var entry = this.findStatic(e.row, e.col);
        if (!entry) { return; }

        global.Sound.play('grind');

        this.statics.splice(this.statics.indexOf(entry), 1);
        this.throwDebris(entry.node, entry.base, entry.angle,
                         this.cellX(entry.col), this.cellY(entry.row));
        this.modScore(SCORE_PER_CLEAR, e.row, e.col);
    };

    GameView.prototype.onPieceAdded = function (e) {
        var node = this._makeGear(e.contents, 'falling');
        place(node, this.cellX(e.col), this.cellY(e.row), 0);
        this.layerGears.appendChild(node);

        this.pieces[e.name] = { base: e.contents, node: node,
                                row: e.row, col: e.col };
        this.lastPieceAdded = e.name;
    };

    GameView.prototype.onPieceMoved = function (e) {
        var p = this.pieces[e.name];
        if (!p) { return; }

        p.row = e.row;
        p.col = e.col;
        // A single transform change; CSS covers the 80ms in between.
        place(p.node, this.cellX(e.col), this.cellY(e.row), 0);
    };

    GameView.prototype.onPieceRemoved = function (e) {
        var p = this.pieces[e.name];
        if (!p) { return; }
        delete this.pieces[e.name];

        if (this.staticAdded) {
            // It landed and became a static gear - blink it out.
            var node = p.node;
            var anim = node.animate([{ opacity: 1 }, { opacity: 0 }],
                                    { duration: FADE_FAST_MS, easing: 'linear', fill: 'both' });
            anim.onfinish = function () { if (node.parentNode) { node.remove(); } };
        } else {
            // It matched - fade it and let it tumble away.
            this.throwDebris(p.node, p.base, 0, this.cellX(p.col), this.cellY(p.row));
        }

        this.staticAdded = false;
    };

    /*
     * A quarter turn. The model has already rotated; the element keeps its
     * image and just gains 90 degrees, which is all the original's
     * RotateTuple(oldRot + offset) did.
     */
    GameView.prototype.onRotated = function (e, clockwise) {
        var entry = this.findStatic(e.row, e.col);
        if (!entry) { return; }

        entry.angle += clockwise ? 90 : -90;
        place(entry.node, this.cellX(entry.col), this.cellY(entry.row), entry.angle);
    };

    GameView.prototype.onGameTick = function (e) {
        this.tickResponded = e.ticks;

        var seconds = Math.round(e.ticks * this.engine.TickMs / 1000.0);
        this.timeText = formatTime(seconds);
        if (!this.userPaused) { this.clock.textContent = this.timeText; }

        this.timeBar.style.setProperty('--y', this.engine.topRowAllowed * CELL_H);

        this.reconcile();
        this.modScore(SCORE_PER_TICK, 0, 0);
    };

    GameView.prototype.onGameEnd = function (won) {
        this.gameOver = true;
        this.board.classList.add('is-over');

        if (!won) {
            // Blow the board apart.
            var list = this.statics.slice();
            this.statics = [];
            for (var i = 0; i < list.length; i++) {
                this.throwDebris(list[i].node, list[i].base, list[i].angle,
                                 this.cellX(list[i].col), this.cellY(list[i].row));
            }
        }

        this._spinBanner();

        var self = this;
        setTimeout(function () {
            if (self.opts.onGameOver) { self.opts.onGameOver(won, self.trueScore); }
        }, 2400);
    };

    /*
     * Keep turning the WIN / LOSE banner once the game is over, at the same
     * rate the game ticked. The engine stops its own timer at game end but
     * leaves Running true and lets R and Q through while GameEnded, so the
     * board is still rotatable - this just keeps pressing R.
     */
    GameView.prototype._spinBanner = function () {
        if (this._bannerTimer) { clearInterval(this._bannerTimer); }

        var self = this;
        this._bannerTimer = setInterval(function () {
            if (!self.running || self.userPaused || self.blocked || document.hidden) { return; }
            self.engine.sendKey('R');
        }, this.engine.TickMs);
    };

    /*
     * The engine can clear cells the view never got a matching removal event
     * for. A gear's displayed face is its base pattern turned by angle/90, so
     * that is what gets compared against the model.
     */
    GameView.prototype.reconcile = function () {
        var m = this.engine.model;
        var keep = [];

        for (var i = 0; i < this.statics.length; i++) {
            var s = this.statics[i];
            if (s.row + 1 >= this.rows || s.col + 1 >= this.cols || s.row < 0 || s.col < 0) {
                keep.push(s);
                continue;
            }

            var quad = m[s.row][s.col] + m[s.row][s.col + 1] +
                       m[s.row + 1][s.col] + m[s.row + 1][s.col + 1];

            if (quad === '****') {
                var gone = s.node;
                var fade = gone.animate([{ opacity: 1 }, { opacity: 0 }],
                                        { duration: 250, easing: 'linear', fill: 'both' });
                fade.onfinish = function () { if (gone.parentNode) { gone.remove(); } };
                continue;
            }

            if (quad !== rotPattern(s.base, s.angle / 90)) {
                // Snap without animating - this is a correction, not a move.
                s.base = quad;
                s.angle = 0;
                s.node.style.transition = 'none';
                paintGear(s.node, quad);
                place(s.node, this.cellX(s.col), this.cellY(s.row), 0);
                void s.node.offsetWidth;
                s.node.style.transition = '';
            }

            keep.push(s);
        }

        this.statics = keep;
    };

    // -------------------------------------------------------------- effects

    /*
     * PhysicsObject from the original: screen-space kinematics, y down,
     * sampled into keyframes so the compositor runs it instead of a timer.
     */
    GameView.prototype.throwDebris = function (node, pattern, angle, x, y) {
        var vLeft = randInt(1000) - 500;
        var vTop = -1 * (randInt(200) + 100);
        var aTop = 500;
        var dur = FADE_SLOW_MS / 1000;

        node.classList.remove('static', 'falling');
        node.classList.add('debris');

        var k = this.scale;
        var frames = [];
        for (var i = 0; i <= 12; i++) {
            var t = (i / 12) * dur;
            var px = x + vLeft * t;
            var py = y + vTop * t + 0.5 * aTop * t * t;
            frames.push({
                transform: 'translate3d(' + ((px - PAD) * k) + 'px,' + ((py - PAD) * k) + 'px,0) rotate(' +
                           (angle + vLeft * t) + 'deg)',
                opacity: 1 - (i / 12)
            });
        }

        var anim = node.animate(frames, { duration: FADE_SLOW_MS, easing: 'linear', fill: 'both' });
        anim.onfinish = function () { if (node.parentNode) { node.remove(); } };
    };

    GameView.prototype.modScore = function (amount, row, col) {
        if (amount > 0) {
            amount += Math.floor(this.trueScore / (this.tickResponded + 1)) * 5;
            this.popScore(amount, row, col);
        }
        this.trueScore = Math.max(this.trueScore + amount, 0);
    };

    GameView.prototype.popScore = function (amount, row, col) {
        var node = el('div', 'score-pop');
        node.textContent = String(amount);

        var x = this.cellX(col);
        var y = this.cellY(row);
        var vLeft = row - this.rows / 2;
        var vTop = col;
        var aTop = -100;
        var dur = POP_MS / 1000;

        var k = this.scale;
        var frames = [];
        for (var i = 0; i <= 10; i++) {
            var t = (i / 10) * dur;
            frames.push({
                transform: 'translate3d(' + ((x + vLeft * t) * k) + 'px,' +
                           ((y + vTop * t + 0.5 * aTop * t * t) * k) + 'px,0)',
                opacity: 0.8 * (1 - i / 10)
            });
        }

        this.layerFx.appendChild(node);
        var anim = node.animate(frames, { duration: POP_MS, easing: 'linear', fill: 'both' });
        anim.onfinish = function () { if (node.parentNode) { node.remove(); } };
    };

    /* The header score chases the real one instead of snapping to it. */
    GameView.prototype._startScoreChase = function () {
        var self = this;
        if (this._scoreTimer) { clearInterval(this._scoreTimer); }

        this._scoreTimer = setInterval(function () {
            if (self.shownScore === self.trueScore) { return; }

            var diff = self.trueScore - self.shownScore;
            var step = Math.sign(diff) * Math.max(1, Math.floor(Math.abs(diff) / 10));
            self.shownScore += step;

            if (Math.sign(self.trueScore - self.shownScore) !== Math.sign(diff)) {
                self.shownScore = self.trueScore;
            }
            if (self.opts.onScore) { self.opts.onScore(self.shownScore); }
        }, 33);
    };

    // --------------------------------------------------------------- layout

    GameView.prototype.resize = function () {
        if (!this.boardW) { return; }

        var w = this.wrap.clientWidth;
        var h = this.wrap.clientHeight;
        if (!w || !h) { return; }

        this.scale = Math.min(w / this.boardW, h / this.boardH);

        // Suppress transitions so a resize does not animate every gear.
        this.board.classList.add('no-anim');

        this.board.style.setProperty('--s', this.scale);
        this.board.style.left = ((w - this.boardW * this.scale) / 2) + 'px';
        this.board.style.top = ((h - this.boardH * this.scale) / 2) + 'px';

        void this.board.offsetWidth;
        this.board.classList.remove('no-anim');

        if (this.opts.onLayout) { this.opts.onLayout(this.gearPx()); }
    };

    /* On-screen size of one gear, which is what decides touch playability. */
    GameView.prototype.gearPx = function () {
        return GEAR_BOX * this.scale;
    };

    GameView.prototype.setBlocked = function (blocked) {
        this.blocked = blocked;
        if (blocked) { global.Sound.pauseMusic(); }
        else {
            global.Sound.resumeMusic();
            if (this.engine) { this._nextDue = performance.now() + this.engine.TickMs; }
        }
    };

    // ---------------------------------------------------------------- input

    GameView.prototype._bind = function () {
        var self = this;

        this._onKeyDown = function (ev) { self.handleKey(ev); };
        window.addEventListener('keydown', this._onKeyDown);

        this.wrap.addEventListener('pointerdown', function (ev) {
            if (!self.engine || self.gameOver) { return; }
            global.Sound.unlock();

            self.dragging = true;
            self.dragPiece = self.lastPieceAdded;
            self.timeBar.classList.add('is-grabbing');

            try { self.wrap.setPointerCapture(ev.pointerId); } catch (err) { /* ok */ }
            self.dragTo(ev);
            ev.preventDefault();
        });

        this.wrap.addEventListener('pointermove', function (ev) {
            if (!self.dragging) { return; }
            self.dragTo(ev);
            ev.preventDefault();
        });

        var release = function (ev) {
            if (!self.dragging) { return; }
            self.dragging = false;
            self.dragPiece = null;
            self.timeBar.classList.remove('is-grabbing', 'is-alert');
            try { self.wrap.releasePointerCapture(ev.pointerId); } catch (err) { /* ok */ }
        };

        this.wrap.addEventListener('pointerup', release);
        this.wrap.addEventListener('pointercancel', release);

        document.addEventListener('visibilitychange', function () {
            if (document.hidden) { global.Sound.pauseMusic(); }
            else if (!self.userPaused) { global.Sound.resumeMusic(); }
        });
    };

    GameView.prototype.unbind = function () {
        window.removeEventListener('keydown', this._onKeyDown);
    };

    GameView.prototype.dragTo = function (ev) {
        if (!this.engine || this.dragPiece !== this.lastPieceAdded) { return; }
        if (!this.pieces[this.dragPiece]) { return; }

        var rect = this.board.getBoundingClientRect();
        var bx = (ev.clientX - rect.left) / this.scale;
        var by = (ev.clientY - rect.top) / this.scale;


        // A finger would cover the gear, so lift it clear; a mouse grabs it
        // by the middle the way the original did.
        var lift = (ev.pointerType === 'touch') ? TOUCH_LIFT : MOUSE_LIFT;
        var col = Math.floor(bx / CELL_W) - 1;
        var row = Math.floor(by / CELL_H) - lift;

        var ok = this.engine.moveUserBlock(row, col);
        this.timeBar.classList.toggle('is-alert', !ok);
    };

    GameView.prototype.handleKey = function (ev) {
        if (!this.engine) { return; }

        var now = performance.now();
        if (now - this.lastKeyTime < 90) { return; }

        var mapped = null;
        switch (ev.key) {
        case 'a': case 'A': case 'ArrowLeft':  mapped = 'A'; break;
        case 'd': case 'D': case 'ArrowRight': mapped = 'D'; break;
        case 's': case 'S': case 'ArrowDown':  mapped = 'S'; break;
        case 'w': case 'W': case 'ArrowUp':    mapped = 'W'; break;
        case 'q': case 'Q':                    mapped = 'Q'; break;
        case 'r': case 'R':                    mapped = 'R'; break;
        default: return;
        }

        this.lastKeyTime = now;
        global.Sound.unlock();
        this.engine.sendKey(mapped);

        if (this.opts.onRotateFlash && (mapped === 'R' || mapped === 'Q')) {
            this.opts.onRotateFlash(mapped);
        }
        ev.preventDefault();
    };

    GameView.prototype.rotate = function (which) {
        if (!this.engine) { return; }
        global.Sound.unlock();
        this.lastKeyTime = performance.now();
        this.engine.sendKey(which);
        if (this.opts.onRotateFlash) { this.opts.onRotateFlash(which); }
    };

    GameView.prototype.setPaused = function (paused) {
        this.userPaused = paused;
        this.clock.textContent = paused ? 'Paused' : this.timeText;
        if (paused) { global.Sound.pauseMusic(); }
        else {
            global.Sound.resumeMusic();
            this._nextDue = performance.now() + this.engine.TickMs;
        }
    };

    function formatTime(totalSeconds) {
        var h = Math.floor(totalSeconds / 3600);
        var m = Math.floor((totalSeconds % 3600) / 60);
        var s = totalSeconds % 60;
        return pad(h) + ':' + pad(m) + ':' + pad(s);
    }

    function pad(n) { return n < 10 ? '0' + n : String(n); }

    GameView.MIN_TOUCH_GEAR_PX = MIN_TOUCH_GEAR_PX;
    GameView.rotPattern = rotPattern;
    GameView.formatTime = formatTime;
    global.GameView = GameView;

}(window));
