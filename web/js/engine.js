/*
 * Clockwork Rush - game engine
 *
 * Direct port of GearMaker/GearMaker/Rotetris.cs (Rotetris_Engine).
 * The model is a Rows x Cols char grid; '*' is empty, 'A'..'D' are gear
 * colours and 'Z' is the black filler used by the win/lose banners.
 *
 * Gears occupy 2x2 cells. Row pair `realRow` (model row 2*realRow) sits on a
 * lattice whose first column is (realRow + 1) % 2, which gives the staggered
 * brickwork that board rotation depends on.
 *
 * The engine is headless: it raises events and the view layer draws them.
 * The host drives it by calling tick() every tickMs.
 */
(function (global) {
    'use strict';

    var EMPTY = '*';

    function randInt(n) {
        return n <= 0 ? 0 : Math.floor(Math.random() * n);
    }

    // ---------------------------------------------------------------- block

    var blockId = 1;

    function ActiveBlock(pattern, rows, cols) {
        this.posRow = 0;
        this.posCol = 0;
        this.rows = rows;
        this.cols = cols;
        this.setPattern(pattern);
        this.name = 'blk' + (blockId++);
    }

    ActiveBlock.prototype.setPattern = function (p) {
        this.block = [[p[0], p[1]], [p[2], p[3]]];
    };

    ActiveBlock.prototype.getPattern = function () {
        return this.block[0][0] + this.block[0][1] + this.block[1][0] + this.block[1][1];
    };

    ActiveBlock.prototype.move = function (offsetRow, offsetCol) {
        if ((this.posCol + offsetCol < this.cols - 1) && (this.posCol + offsetCol >= 0)) {
            this.posCol += offsetCol;
        }
        if ((this.posRow + offsetRow < this.rows - 1) && (this.posRow + offsetRow >= 0)) {
            this.posRow += offsetRow;
        }
    };

    // --------------------------------------------------------------- engine

    function Engine(rows, cols, startRows, tickMs, colorCt) {
        this.Rows = rows;
        this.Cols = cols;
        this.StartRows = startRows;
        this.TickMs = tickMs;
        this.NumColorsUsed = colorCt;
        this.ColorAlphabet = 'ABCDEFGHIJKLMNOPQSTUVWXY';

        this.Ticks = 0;
        this.matches = 0;
        this.gameEnded = false;
        this.running = false;
        this.topRowAllowed = 0;

        this.fallingBlocks = [];
        this.userBlock = null;

        this.model = [];
        for (var i = 0; i < rows; i++) {
            var row = [];
            for (var j = 0; j < cols; j++) { row.push(EMPTY); }
            this.model.push(row);
        }

        this._handlers = {};
    }

    /* Events, payload {row, col, contents, ticks, name}:
       pieceAdded, pieceMoved, pieceRemoved, staticAdded, staticRemoved,
       rotatedCW, rotatedCCW, boardChanged, gameTick, won, lost            */

    Engine.prototype.on = function (name, fn) {
        (this._handlers[name] || (this._handlers[name] = [])).push(fn);
        return this;
    };

    Engine.prototype.fire = function (name, row, col, contents, blockName) {
        var list = this._handlers[name];
        if (!list) { return; }
        var e = {
            row: row,
            col: col,
            contents: contents || null,
            ticks: this.Ticks,
            name: blockName || ''
        };
        for (var i = 0; i < list.length; i++) { list[i](e); }
    };

    // ------------------------------------------------------------- helpers

    Engine.prototype.quadAt = function (row, col) {
        return this.model[row][col] + this.model[row][col + 1] +
               this.model[row + 1][col] + this.model[row + 1][col + 1];
    };

    Engine.prototype.addBlock = function (row, col, pattern) {
        this.model[row][col] = pattern[0];
        this.model[row][col + 1] = pattern[1];
        this.model[row + 1][col] = pattern[2];
        this.model[row + 1][col + 1] = pattern[3];
        this.fire('staticAdded', row, col, pattern);
        this.fire('boardChanged', row, col, pattern);
    };

    Engine.prototype.randomBlock = function (set) {
        return set[randInt(set.length)] + set[randInt(set.length)] +
               set[randInt(set.length)] + set[randInt(set.length)];
    };

    Engine.prototype.randomPair = function () {
        var set = this.ColorAlphabet.substr(0, this.NumColorsUsed);
        return set[randInt(set.length)] + set[randInt(set.length)];
    };

    Engine.prototype.printModel = function () {
        return this.model.map(function (r) { return r.join(''); }).join('\n');
    };

    Engine.prototype.flatModel = function () {
        return this.model.map(function (r) { return r.join(''); }).join('');
    };

    // --------------------------------------------------------------- start

    Engine.prototype.configAndStart = function () {
        if (this.running) { return; }
        this.running = true;

        var i, j;
        for (i = 0; i < this.Rows; i++) {
            for (j = 0; j < this.Cols; j++) { this.model[i][j] = EMPTY; }
        }

        var set = this.ColorAlphabet.substr(0, this.NumColorsUsed);
        for (j = 0; (j / 2) < this.StartRows; j += 2) {
            var off = ((j / 2) % 2);
            for (i = off; i < this.Cols - off; i += 2) {
                this.addBlock(this.Rows - 2 - j, i, this.randomBlock(set));
            }
        }
    };

    /* Re-announce a restored board so the view can rebuild itself. */
    Engine.prototype.forceStart = function () {
        this.running = true;
        this.gameEnded = false;

        for (var b = 0; b < this.fallingBlocks.length; b++) {
            var blk = this.fallingBlocks[b];
            this.fire('pieceAdded', blk.posRow, blk.posCol, blk.getPattern(), blk.name);
        }

        for (var i = 0; i < this.Rows; i += 2) {
            for (var j = 0; j <= this.Cols - 2; j += 2) {
                var xMod = (i / 2) % 2 === 0 ? 1 : 0;
                var col = j + xMod;
                if (xMod === 1 && j === this.Cols - 2) { continue; }

                var possible = this.quadAt(i, col);
                if (possible !== '****') {
                    if (this.model[i][col] === EMPTY) { break; }
                    this.fire('staticAdded', i, col, possible);
                }
            }
        }
    };

    Engine.prototype.pause = function () { this.running = false; };

    Engine.prototype.resume = function () {
        if (!this.gameEnded) { this.running = true; }
    };

    // -------------------------------------------------------------- bounds

    Engine.prototype.checkBounds = function (row, col) {
        return !(col >= this.Cols || col < 0 || row >= this.Rows || row < 0);
    };

    Engine.prototype.checkCollision = function (offsetRow, offsetCol, c) {
        if (!c) { return false; }
        for (var i = 0; i < 2; i++) {
            for (var j = 0; j < 2; j++) {
                if (!this.checkBounds(i + c.posRow + offsetRow, j + c.posCol + offsetCol)) {
                    return true;
                }
                if (this.model[i + c.posRow + offsetRow][j + c.posCol + offsetCol] !== EMPTY) {
                    return true;
                }
            }
        }
        return false;
    };

    Engine.prototype.getCollision = function (offsetRow, offsetCol, block) {
        var out = ['*', '*', '*', '*'];
        if (block) {
            for (var i = 0; i < 2; i++) {
                for (var j = 0; j < 2; j++) {
                    var index = 2 * i + j;
                    if (!this.checkBounds(i + block.posRow + offsetRow, j + block.posCol + offsetCol)) {
                        out[index] = '@';
                        continue;
                    }
                    out[index] = this.model[i + block.posRow + offsetRow][j + block.posCol + offsetCol];
                }
            }
        }
        return out.join('');
    };

    // ----------------------------------------------------------- user move

    Engine.prototype.moveUserBlock = function (row, col) {
        if (!this.running || !this.userBlock) { return false; }

        if (this.checkBounds(row, col)) {
            if (col < this.Cols - 1 && row < this.Rows - 1 &&
                    this.model[row][col] === EMPTY && this.model[row + 1][col] === EMPTY &&
                    this.model[row][col + 1] === EMPTY && this.model[row + 1][col + 1] === EMPTY) {
                if (row >= this.topRowAllowed) {
                    this.userBlock.posRow = row;
                    this.userBlock.posCol = col;
                    this.fire('pieceMoved', row, col, this.userBlock.getPattern(), this.userBlock.name);
                    return true;
                }
            }
        }
        return false;
    };

    Engine.prototype.sendKey = function (c) {
        if (!this.running) {
            // After the game ends R and Q still work - the original's easter egg.
            if (!(this.gameEnded === true && (c === 'R' || c === 'Q'))) { return; }
        }

        var i;
        switch (c) {
        case 'R':
            for (i = this.Rows - 2; i >= 0; i -= 2) {
                this.rotateRow(i, ((i >> 1) % 2) === 1);
            }
            this.fire('boardChanged', -1, -1, null);
            break;
        case 'Q':
            for (i = this.Rows - 2; i >= 0; i -= 2) {
                this.rotateRow(i, ((i >> 1) % 2) === 0);
            }
            this.fire('boardChanged', -1, -1, null);
            break;
        case 'A':
            if (this.userBlock && !this.checkCollision(0, -1, this.userBlock)) {
                this.userBlock.move(0, -1);
                this.fire('pieceMoved', this.userBlock.posRow, this.userBlock.posCol,
                          this.userBlock.getPattern(), this.userBlock.name);
            }
            break;
        case 'S':
            if (this.userBlock && !this.checkCollision(1, 0, this.userBlock)) {
                this.userBlock.move(1, 0);
                this.fire('pieceMoved', this.userBlock.posRow, this.userBlock.posCol,
                          this.userBlock.getPattern(), this.userBlock.name);
            }
            break;
        case 'D':
            if (this.userBlock && !this.checkCollision(0, 1, this.userBlock)) {
                this.userBlock.move(0, 1);
                this.fire('pieceMoved', this.userBlock.posRow, this.userBlock.posCol,
                          this.userBlock.getPattern(), this.userBlock.name);
            }
            break;
        default:
            break;
        }
    };

    // ---------------------------------------------------------------- tick

    Engine.prototype.tick = function () {
        this.topRowAllowed++;

        var live = this.fallingBlocks.slice();
        for (var i = 0; i < live.length; i++) {
            var cur = live[i];
            if (this.fallingBlocks.indexOf(cur) === -1) { continue; }

            if (!this.checkCollision(1, 0, cur)) {
                cur.move(1, 0);
                this.fire('pieceMoved', cur.posRow, cur.posCol, cur.getPattern(), cur.name);
                continue;
            }

            var coll = this.getCollision(1, 0, cur);
            var pattern = cur.getPattern();
            var sameRow = (Math.floor(cur.posRow / 2) % 2) === (cur.posCol % 2);

            if (coll.substr(2) === '@@') {
                this.hitBottom(cur);
            } else if (coll.indexOf('***') === 0) {          // blocked under the right side
                this.glanceRight(cur);
            } else if (coll.indexOf('**') === 0 && coll.charAt(3) === '*') {
                this.glanceLeft(cur);                        // blocked under the left side
            } else if (sameRow && pattern.substr(2) === coll.substr(2)) {
                this.fuseBlocks(cur);                        // aligned match
            } else if (pattern.substr(2) === coll.substr(2)) {
                this.oppositeRowMatch(cur);                  // straddling match
            } else if (sameRow) {
                this.sameRowNonMatch(cur);
            } else if (coll.indexOf('**') === 0) {
                this.nonMatch(cur);
            }
        }

        this.scanForGaps();

        if (this.fallingBlocks.length === 0) {
            var occupied = 0;
            for (var r = 0; r < this.Rows; r++) {
                for (var c = 0; c < this.Cols; c++) {
                    if (this.model[r][c] !== EMPTY) { occupied++; }
                }
            }

            if (occupied === 0) {
                this.fire('won', -1, -1, null);
                this.running = false;
                this.writeWin();
                this.gameEnded = true;
                return;
            }

            var newBlock = new ActiveBlock(this.ensureMatch() + this.ensureMatch(),
                                           this.Rows, this.Cols);
            newBlock.posCol = Math.max(randInt(this.Cols) - 2, 0);

            this.fallingBlocks.push(newBlock);
            this.userBlock = newBlock;

            if (this.checkCollision(0, 0, newBlock)) {
                this.fire('lost', newBlock.posRow, newBlock.posCol,
                          newBlock.getPattern(), newBlock.name);
                this.running = false;
                this.writeLose();
                this.gameEnded = true;
            } else {
                this.topRowAllowed = newBlock.posRow;
                this.fire('pieceAdded', newBlock.posRow, newBlock.posCol,
                          newBlock.getPattern(), newBlock.name);
            }
        }

        // The original fired with the pre-increment value: Ticks++ inside the
        // event args was evaluated before Fire() ran.
        this.fire('gameTick', -1, -1, null);
        this.Ticks++;
    };

    // ---------------------------------------------------------- resolution

    Engine.prototype.removeFallingBlock = function (block) {
        var idx = this.fallingBlocks.indexOf(block);
        if (idx >= 0) { this.fallingBlocks.splice(idx, 1); }
        if (this.userBlock === block) { this.userBlock = null; }
    };

    Engine.prototype.hitBottom = function (block) {
        if (block.posCol % 2 === 1) { block.posCol++; }
        this.addBlock(block.posRow, block.posCol, block.getPattern());
        this.fire('pieceRemoved', block.posRow, block.posCol, block.getPattern(), block.name);
        this.removeFallingBlock(block);
    };

    Engine.prototype.nonMatch = function (block) {
        this.addBlock(block.posRow, block.posCol, block.getPattern());
        this.fire('pieceRemoved', block.posRow, block.posCol, block.getPattern(), block.name);
        this.removeFallingBlock(block);
    };

    Engine.prototype.sameRowNonMatch = function (block) {
        if (block.posCol < this.Cols - 1) { this.glanceLeft(block); }
        else { this.glanceRight(block); }
    };

    Engine.prototype.glanceLeft = function (block) {
        if (block.posCol < this.Cols - 2) {
            block.posCol++;
            if (!this.checkCollision(1, 0, block)) { block.posRow++; }
            this.fire('pieceMoved', block.posRow, block.posCol, block.getPattern(), block.name);
        } else if (block.posCol === this.Cols - 2 && (Math.floor(block.posRow / 2) % 2 === 0)) {
            block.posCol--;
            if (!this.checkCollision(1, 0, block)) { block.posRow++; }
            this.fire('pieceMoved', block.posRow, block.posCol, block.getPattern(), block.name);
        } else {
            this.addBlock(block.posRow, block.posCol, block.getPattern());
            this.fire('pieceRemoved', block.posRow, block.posCol, block.getPattern(), block.name);
            this.removeFallingBlock(block);
        }
    };

    Engine.prototype.glanceRight = function (block) {
        if (block.posCol > 1) {
            block.posCol--;
            if (!this.checkCollision(1, 0, block)) { block.posRow++; }
            this.fire('pieceMoved', block.posRow, block.posCol, block.getPattern(), block.name);
        } else {
            this.addBlock(block.posRow, block.posCol, block.getPattern());
            this.fire('pieceRemoved', block.posRow, block.posCol, block.getPattern(), block.name);
            this.removeFallingBlock(block);
        }
    };

    /* The falling piece's bottom row matches the gear directly under it. */
    Engine.prototype.fuseBlocks = function (block) {
        this.matches++;
        this.fire('pieceRemoved', block.posRow, block.posCol, block.getPattern(), block.name);

        for (var r = block.posRow + 2; r < block.posRow + 4; r++) {
            for (var c = block.posCol; c < block.posCol + 2; c++) {
                if (this.checkBounds(r, c)) { this.model[r][c] = EMPTY; }
            }
        }

        this.removeFallingBlock(block);
        this.fire('staticRemoved', block.posRow + 2, block.posCol, null);
    };

    /* The piece straddles two lattice gears - clear both. Worth 3 matches. */
    Engine.prototype.oppositeRowMatch = function (block) {
        this.matches += 3;

        for (var i = block.posRow + 2; i < block.posRow + 4; i++) {
            for (var j = Math.max(block.posCol - 1, 0); j < Math.min(block.posCol + 3, this.Cols); j++) {
                if (!this.checkBounds(i, j)) { continue; }
                this.model[i][j] = EMPTY;

                if (j === block.posCol - 1 && i === block.posRow + 2) {
                    this.fire('staticRemoved', i, j, null);
                }
                if (j === block.posCol + 1 && i === block.posRow + 2) {
                    this.fire('staticRemoved', i, j, null);
                }
            }
        }

        this.fire('pieceRemoved', block.posRow, block.posCol, block.getPattern(), block.name);
        this.removeFallingBlock(block);
    };

    /* Gears left hanging with nothing under them crumble away. */
    Engine.prototype.scanForGaps = function () {
        var found = true;
        while (found) {
            found = false;

            for (var i = 1; i < this.Rows - 1; i++) {
                for (var j = 1; j < this.Cols - 1; j++) {
                    if (this.model[i][j] === EMPTY) { continue; }

                    var bRow = -1;
                    var bCol = -1;

                    if (this.model[i][j + 1] === EMPTY && this.model[i + 1][j] === EMPTY &&
                            this.model[i + 1][j + 1] === EMPTY) {
                        bRow = i - 1;                        // lower right edge
                        bCol = j - 1;
                    } else if (this.model[i][j - 1] === EMPTY && this.model[i + 1][j] === EMPTY &&
                            this.model[i + 1][j - 1] === EMPTY) {
                        bRow = i - 1;                        // lower left edge
                        bCol = j;
                    }

                    if (bRow > 0) {
                        found = true;
                        this.model[bRow][bCol] = EMPTY;
                        this.model[bRow][bCol + 1] = EMPTY;
                        this.model[bRow + 1][bCol] = EMPTY;
                        this.model[bRow + 1][bCol + 1] = EMPTY;
                        this.fire('staticRemoved', bRow, bCol, null);
                    }
                }
            }
        }
    };

    // ------------------------------------------------------------ rotation

    Engine.prototype.rotateRow = function (rowNum, clockwise) {
        var realRow = Math.floor(rowNum / 2);
        var m = this.model;

        for (var j = (realRow + 1) % 2; j < this.Cols - 1; j += 2) {
            var c = m[rowNum][j];
            if (clockwise) {
                m[rowNum][j] = m[rowNum + 1][j];
                m[rowNum + 1][j] = m[rowNum + 1][j + 1];
                m[rowNum + 1][j + 1] = m[rowNum][j + 1];
                m[rowNum][j + 1] = c;
                this.fire('rotatedCW', rowNum, j, this.quadAt(rowNum, j));
            } else {
                m[rowNum][j] = m[rowNum][j + 1];
                m[rowNum][j + 1] = m[rowNum + 1][j + 1];
                m[rowNum + 1][j + 1] = m[rowNum + 1][j];
                m[rowNum + 1][j] = c;
                this.fire('rotatedCCW', rowNum, j, this.quadAt(rowNum, j));
            }
        }
    };

    Engine.prototype.previewRotate = function (model, clockwise) {
        var f = model.map(function (r) { return r.slice(); });

        for (var r = this.Rows - 2; r >= 0; r -= 2) {
            var realRow = Math.floor(r / 2);
            for (var j = (realRow + 1) % 2; j < this.Cols - 1; j += 2) {
                var c = f[r][j];
                if (clockwise) {
                    f[r][j] = f[r + 1][j];
                    f[r + 1][j] = f[r + 1][j + 1];
                    f[r + 1][j + 1] = f[r][j + 1];
                    f[r][j + 1] = c;
                } else {
                    f[r][j] = f[r][j + 1];
                    f[r][j + 1] = f[r + 1][j + 1];
                    f[r + 1][j + 1] = f[r + 1][j];
                    f[r + 1][j] = c;
                }
            }
        }
        return f;
    };

    /* Two colours sitting side by side at the same height are always
       reachable as a match, in this rotation or one of the other three. */
    Engine.prototype.topRowFromPreview = function (m) {
        var sb = '';
        var lastMatch = null;
        var lastHeight = 0;

        for (var i = 0; i < this.Cols; i++) {
            for (var j = 0; j < this.Rows; j++) {
                if (m[j][i] !== EMPTY) {
                    if (j === lastHeight && lastMatch !== null) {
                        sb += lastMatch + m[j][i];
                    }
                    lastMatch = m[j][i];
                    lastHeight = j;
                    break;
                }
            }
        }
        return sb;
    };

    Engine.prototype.ensureMatch = function () {
        var rot1 = this.previewRotate(this.model, true);
        var rot2 = this.previewRotate(rot1, true);
        var rot3 = this.previewRotate(rot2, true);

        var lines = [
            this.topRowFromPreview(rot1),
            this.topRowFromPreview(rot2),
            this.topRowFromPreview(rot3),
            this.topRowFromPreview(this.model)
        ].filter(function (l) { return l.length >= 2; });

        if (lines.length === 0) { return this.randomPair(); }

        var pos = randInt(Math.floor(lines[0].length / 2)) * 2;
        var pick = lines[randInt(lines.length)];
        var out = pick.substr(pos, 2);

        return out.length === 2 ? out : pick.substr(0, 2);
    };

    // ------------------------------------------------------- win/lose text

    Engine.prototype.setModel = function (row, col, val) {
        if (row < this.Rows && col < this.Cols && row >= 0 && col >= 0) {
            this.model[row][col] = val;
        }
    };

    Engine.prototype._wipeBanner = function () {
        for (var i = Math.max(this.Rows - 6, 0); i < this.Rows; i++) {
            for (var j = 0; j < this.Cols; j++) {
                var xOffset = (Math.floor(i / 2) % 2 === 1) ? 0 : 1;
                if (j + xOffset < this.Cols) { this.model[i][j + xOffset] = 'Z'; }
            }
        }
    };

    Engine.prototype._announceBanner = function () {
        for (var row = Math.max(this.Rows - 6, 0); row < this.Rows; row += 2) {
            for (var col = 0; col <= this.Cols - 2; col += 2) {
                var xOffset = (Math.floor(row / 2) % 2 === 1) ? 0 : 1;
                if (xOffset + col + 1 < this.Cols) {
                    this.fire('staticAdded', row, col + xOffset, this.quadAt(row, col + xOffset));
                }
            }
        }
    };

    Engine.prototype.writeWin = function () {
        this._wipeBanner();
        var top = this.Rows - 6;

        this.writeChar('W', top, 2, 'A');
        this.writeChar('I', top, 8, 'B');
        this.writeChar('N', top, 10, 'C');

        if (this.Cols >= 30) {
            this.writeChar('N', top, 16, 'D');
            this.writeChar('E', top, 22, 'A');
            this.writeChar('R', top, 27, 'B');
            this.writeChar('!', top, 32, 'C');
            this.writeChar('!', top, 36, 'D');
        } else {
            this.writeChar('!', top, 16, 'D');
            this.writeChar('!', top, 20, 'C');
            this.writeChar('!', top, 24, 'D');
            this.writeChar('!', top, 28, 'D');
        }

        this._announceBanner();
    };

    Engine.prototype.writeLose = function () {
        this._wipeBanner();
        var top = this.Rows - 6;

        this.writeChar('L', top, 2, 'A');
        this.writeChar('O', top, 7, 'B');
        this.writeChar('S', top, 12, 'C');
        this.writeChar('E', top, 17, 'D');
        this.writeChar('!', top, 22, 'C');
        this.writeChar('!', top, 26, 'D');

        this._announceBanner();
    };

    /* 6-row block letters, as drawn by the WriteChar_* helpers. */
    Engine.prototype.writeChar = function (ch, topRow, topCol, color) {
        var i, j;
        var set = this.setModel.bind(this);

        switch (ch) {
        case 'W':
            for (i = 0; i < 6; i++) {
                set(topRow + i, topCol, color);
                set(topRow + i, topCol + 4, color);
            }
            set(topRow + 4, topCol + 1, color);
            set(topRow + 3, topCol + 2, color);
            set(topRow + 4, topCol + 3, color);
            break;
        case 'I':
            for (i = 0; i < 6; i++) { set(topRow + i, topCol, color); }
            break;
        case 'N':
            for (i = 0; i < 6; i++) {
                set(topRow + i, topCol, color);
                set(topRow + i, topCol + 4, color);
            }
            set(topRow + 1, topCol + 1, color);
            set(topRow + 2, topCol + 2, color);
            set(topRow + 3, topCol + 3, color);
            break;
        case 'L':
            for (i = 0; i < 6; i++) { set(topRow + i, topCol, color); }
            for (j = 1; j < 4; j++) { set(topRow + 5, topCol + j, color); }
            break;
        case 'O':
            for (i = 0; i < 6; i++) {
                set(topRow + i, topCol, color);
                set(topRow + i, topCol + 3, color);
            }
            for (j = 1; j < 4; j++) {
                set(topRow, topCol + j, color);
                set(topRow + 5, topCol + j, color);
            }
            break;
        case 'S':
            for (i = 0; i < 2; i++) {
                set(topRow, topCol + 1 + i, color);
                set(topRow + 2, topCol + 1 + i, color);
                set(topRow + 5, topCol + 1 + i, color);
            }
            set(topRow + 1, topCol, color);
            set(topRow + 4, topCol, color);
            set(topRow + 3, topCol + 3, color);
            set(topRow + 4, topCol + 3, color);
            break;
        case 'E':
            for (i = 0; i < 6; i++) { set(topRow + i, topCol, color); }
            for (j = 1; j < 4; j++) {
                set(topRow + 5, topCol + j, color);
                if (j < 3) { set(topRow + 2, topCol + j, color); }
                set(topRow, topCol + j, color);
            }
            break;
        case 'R':
            for (i = 0; i < 6; i++) {
                set(topRow + i, topCol, color);
                if (i !== 0 && i !== 2) { set(topRow + i, topCol + 3, color); }
            }
            for (j = 1; j < 3; j++) {
                set(topRow, topCol + j, color);
                set(topRow + 2, topCol + j, color);
            }
            break;
        case '!':
            for (i = 0; i < 6; i++) {
                if (i !== 3) {
                    set(topRow + i, topCol, color);
                    set(topRow + i, topCol + 1, color);
                }
            }
            break;
        default:
            break;
        }
    };

    // ----------------------------------------------------- save / restore

    Engine.prototype.serialize = function () {
        return {
            v: 1,
            rows: this.Rows,
            cols: this.Cols,
            startRows: this.StartRows,
            tickMs: this.TickMs,
            colors: this.NumColorsUsed,
            ticks: this.Ticks,
            matches: this.matches,
            topRowAllowed: this.topRowAllowed,
            gameEnded: this.gameEnded,
            flat: this.flatModel(),
            blocks: this.fallingBlocks.map(function (b) {
                return { row: b.posRow, col: b.posCol, pattern: b.getPattern(), name: b.name };
            }),
            userBlock: this.userBlock ? this.userBlock.name : null
        };
    };

    Engine.deserialize = function (s) {
        if (!s || s.v !== 1) { return null; }

        var e = new Engine(s.rows, s.cols, s.startRows, s.tickMs, s.colors);
        e.Ticks = s.ticks;
        e.matches = s.matches || 0;
        e.topRowAllowed = s.topRowAllowed;
        e.gameEnded = s.gameEnded;

        for (var i = 0; i < s.rows; i++) {
            for (var j = 0; j < s.cols; j++) {
                e.model[i][j] = s.flat[i * s.cols + j];
            }
        }

        s.blocks.forEach(function (b) {
            var blk = new ActiveBlock(b.pattern, s.rows, s.cols);
            blk.posRow = b.row;
            blk.posCol = b.col;
            blk.name = b.name;
            e.fallingBlocks.push(blk);
            if (b.name === s.userBlock) { e.userBlock = blk; }
        });

        return e;
    };

    Engine.EMPTY = EMPTY;
    Engine.ActiveBlock = ActiveBlock;

    global.RotetrisEngine = Engine;

}(window));
