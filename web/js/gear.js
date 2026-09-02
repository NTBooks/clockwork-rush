/*
 * Clockwork Rush - gear geometry
 *
 * Port of GearMaker/GearMaker/UIGear.cs. The original built WPF PathGeometry
 * objects on a 100x100 canvas centred at (50,50); the same construction is
 * reproduced here with Path2D so it can be rasterised once and blitted.
 *
 * A gear is drawn as `teeth` wedges. Each wedge runs:
 *     hole -> inner -> outer (tooth rises) -> arc across the tooth ->
 *     back to inner -> arc along the root -> back to hole
 *
 * Tooth i covers angles [i*360/teeth, (i+1)*360/teeth). With teeth = 8 the
 * gear splits into four 2-tooth quadrants that line up with the 2x2 cell
 * pattern of a board piece:
 *     quadrant 0 =   0-90  = bottom right    quadrant 2 = 180-270 = top left
 *     quadrant 1 =  90-180 = bottom left     quadrant 3 = 270-360 = top right
 */
(function (global) {
    'use strict';

    var GEAR_TEETH = 8;
    var GEAR_INNER = 43;
    var GEAR_OUTER = 50;
    var GEAR_HOLE = 0;
    var OUTLINE_PCT = 0.15;

    // Colour map from GamePage.ConfigEnv's LetterColorMap.
    var COLORS = {
        'A': 'rgb(109,126,255)',
        'B': 'rgb(130,204,109)',
        'C': 'rgb(224,163,62)',
        'D': 'rgb(233,119,95)',
        'Z': 'rgb(0,0,0)',
        '*': 'rgb(255,105,180)'      // hot pink: a piece built with a hole in it
    };

    function colorFor(ch) {
        return COLORS[ch] || COLORS['*'];
    }

    /*
     * One tooth range of a gear, as SVG path data in 100x100 space.
     * gearLeftAngleStop / gearTopStop / gearRightAngleStop are the fractions
     * of the tooth pitch at which the flank rises, the crown ends and the
     * flank falls back to the root circle.
     *
     * SVG path data rather than direct Path2D calls, so the exact same
     * geometry can be handed to `new Path2D(d)` for canvas or dropped into an
     * <svg> for the DOM. Sweep flag 1 is clockwise on a y-down surface, which
     * is what SweepDirection.Clockwise meant in the original ArcSegments.
     */
    function gearPathData(innerRadius, outerRadius, holeRadius, teeth,
                          gearLeftAngleStop, gearTopStop, gearRightAngleStop,
                          startTooth, endTooth) {

        if (gearLeftAngleStop === undefined) { gearLeftAngleStop = 0.1; }
        if (gearTopStop === undefined) { gearTopStop = 0.4; }
        if (gearRightAngleStop === undefined) { gearRightAngleStop = 0.5; }
        if (startTooth === undefined) { startTooth = 0; }
        if (endTooth === undefined || endTooth === -1) { endTooth = teeth; }

        var d = [];
        var n = function (v) { return Math.round(v * 1000) / 1000; };
        var px = function (r, a) { return n(50 + Math.cos(a) * r) + ',' + n(50 + Math.sin(a) * r); };

        for (var i = startTooth; i < endTooth; i++) {
            var angle = (i * 360.0 / teeth) * Math.PI / 180.0;
            var endAngle = ((i + 1) * 360.0 / teeth) * Math.PI / 180.0;
            var span = endAngle - angle;

            var q1 = span * gearLeftAngleStop;
            var q2 = span * gearTopStop;
            var q3 = span * gearRightAngleStop;

            d.push('M' + px(holeRadius, angle));
            d.push('L' + px(innerRadius, angle));
            d.push('L' + px(outerRadius, angle + q1));
            d.push('A' + n(outerRadius) + ',' + n(outerRadius) + ' 0 0 1 ' + px(outerRadius, angle + q2));
            d.push('L' + px(innerRadius, angle + q3));
            d.push('A' + n(innerRadius) + ',' + n(innerRadius) + ' 0 0 1 ' + px(innerRadius, endAngle));

            if (holeRadius > 0) {
                d.push('L' + px(holeRadius, endAngle));
                d.push('A' + n(holeRadius) + ',' + n(holeRadius) + ' 0 0 0 ' + px(holeRadius, angle));
            }

            d.push('Z');
        }

        return d.join('');
    }

    function makeGear(innerRadius, outerRadius, holeRadius, teeth,
                      gearLeftAngleStop, gearTopStop, gearRightAngleStop,
                      startTooth, endTooth) {
        return new Path2D(gearPathData(innerRadius, outerRadius, holeRadius, teeth,
                                       gearLeftAngleStop, gearTopStop, gearRightAngleStop,
                                       startTooth, endTooth));
    }

    /*
     * The four coloured quadrants plus the two oversized dark rings the
     * original stacked underneath them to fake an outline.
     */
    function buildGearPaths(inner, outer, hole, teeth, outlinePercent,
                            leftStop, topStop, rightStop) {
        var quarter = teeth >> 2;
        var quads = [];

        for (var i = 0; i < 4; i++) {
            quads.push(makeGear(inner, outer, hole, teeth,
                                leftStop, topStop, rightStop,
                                i * quarter, i < 3 ? (i + 1) * quarter : teeth));
        }

        var outline = null;
        var outline2 = null;

        if (outlinePercent > 0) {
            outline = makeGear(inner * (1 + outlinePercent), outer * (1 + outlinePercent),
                               hole * (1 - outlinePercent), teeth,
                               leftStop, topStop, rightStop, 0, teeth);
            outline2 = makeGear(inner * (1 + outlinePercent * 2), outer * (1 + outlinePercent * 2),
                                hole * (1 - outlinePercent * 2), teeth,
                                leftStop, topStop, rightStop, 0, teeth);
        }

        return { quads: quads, outline: outline, outline2: outline2 };
    }

    // Board gears never change shape, so their paths are built once.
    var BOARD_PATHS = buildGearPaths(GEAR_INNER, GEAR_OUTER, GEAR_HOLE, GEAR_TEETH,
                                     OUTLINE_PCT, 0.1, 0.4, 0.5);

    /*
     * Draw a board gear into the current transform, in 100x100 gear space.
     * `pattern` is the 4-char cell content, TL TR BL BR.
     */
    function paintBoardGear(ctx, pattern) {
        ctx.fillStyle = 'rgb(22,22,22)';
        ctx.fill(BOARD_PATHS.outline2);

        ctx.fillStyle = 'rgb(0,0,0)';
        ctx.fill(BOARD_PATHS.outline);

        // GearPaths[0..3] were bound to Fills..Fills3 = pattern[3],[2],[0],[1]
        ctx.fillStyle = colorFor(pattern[3]);          // bottom right
        ctx.fill(BOARD_PATHS.quads[0]);

        ctx.fillStyle = colorFor(pattern[2]);          // bottom left
        ctx.fill(BOARD_PATHS.quads[1]);

        ctx.fillStyle = colorFor(pattern[0]);          // top left
        ctx.fill(BOARD_PATHS.quads[2]);

        ctx.fillStyle = colorFor(pattern[1]);          // top right
        ctx.fill(BOARD_PATHS.quads[3]);
    }

    /*
     * Rasterised gear cache. Gears are drawn at 48x48 inside a 64x64 tile,
     * because the fake outline rings overflow the nominal 100x100 box.
     */
    var TILE = 64;
    var GEAR_BOX = 48;
    var tileCache = {};
    var tileQuality = 2;

    function setTileQuality(q) {
        q = Math.max(1, Math.min(3, q));
        if (Math.abs(q - tileQuality) < 0.25) { return; }
        tileQuality = q;
        tileCache = {};
        clearURLCache();
    }

    function gearTile(pattern) {
        var key = pattern + '@' + tileQuality;
        if (tileCache[key]) { return tileCache[key]; }

        var px = Math.round(TILE * tileQuality);
        var cv = document.createElement('canvas');
        cv.width = px;
        cv.height = px;

        var ctx = cv.getContext('2d');
        ctx.scale(tileQuality, tileQuality);
        ctx.translate(TILE / 2, TILE / 2);
        ctx.scale(GEAR_BOX / 100, GEAR_BOX / 100);
        ctx.translate(-50, -50);
        paintBoardGear(ctx, pattern);

        tileCache[key] = cv;
        return cv;
    }

    /*
     * Blit a cached gear. (x, y) is the top-left of its 48x48 box, matching
     * the Canvas.SetLeft / Canvas.SetTop the original used.
     */
    function drawGear(ctx, pattern, x, y, angleDeg, alpha) {
        if (alpha <= 0.004) { return; }

        var tile = gearTile(pattern);
        var cx = x + GEAR_BOX / 2;
        var cy = y + GEAR_BOX / 2;

        ctx.save();
        ctx.globalAlpha = alpha;
        ctx.translate(cx, cy);
        if (angleDeg) { ctx.rotate(angleDeg * Math.PI / 180); }
        ctx.drawImage(tile, -TILE / 2, -TILE / 2, TILE, TILE);
        ctx.restore();
    }

    /*
     * ---------------------------------------------------------------- vector
     *
     * The board draws gears as real vector, not bitmaps: the silhouette is an
     * SVG mask and the four quadrant colours are a conic-gradient behind it.
     * That keeps them resolution independent the way the original XAML Paths
     * were, and it means a gear needs no per-colour image at all - the colours
     * are four CSS custom properties.
     *
     * The viewBox matches the 64px tile geometry: gear space 0..100 maps to
     * the middle 48px, so the box spans 133.333 units centred on (50,50).
     */
    var VIEWBOX = '-16.6667 -16.6667 133.3333 133.3333';

    function svgURL(body) {
        var svg = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="' + VIEWBOX + '">' +
                  body + '</svg>';
        return 'data:image/svg+xml,' + encodeURIComponent(svg);
    }

    /*
     * A whole gear as one SVG data URI: the two dark rings, then the four
     * coloured quadrants. One flat image per colour combination.
     *
     * SVG rather than PNG because the browser re-rasterises it at whatever
     * size the element actually is, so it stays sharp at any board scale the
     * way the original XAML Paths did. And one plain background-image rather
     * than a CSS mask + conic-gradient, because a masked layer has to be
     * re-composited on every rotation - measured at 27ms for a full board
     * versus 4ms for a flat image.
     */
    var QUAD_D = null;
    var RING_D = null;
    var svgCache = {};

    function ensurePaths() {
        if (QUAD_D) { return; }
        var quarter = GEAR_TEETH >> 2;
        QUAD_D = [];
        for (var i = 0; i < 4; i++) {
            QUAD_D.push(gearPathData(GEAR_INNER, GEAR_OUTER, GEAR_HOLE, GEAR_TEETH,
                                     0.1, 0.4, 0.5,
                                     i * quarter, i < 3 ? (i + 1) * quarter : GEAR_TEETH));
        }
        RING_D = [
            gearPathData(GEAR_INNER * (1 + OUTLINE_PCT * 2), GEAR_OUTER * (1 + OUTLINE_PCT * 2),
                         0, GEAR_TEETH, 0.1, 0.4, 0.5, 0, GEAR_TEETH),
            gearPathData(GEAR_INNER * (1 + OUTLINE_PCT), GEAR_OUTER * (1 + OUTLINE_PCT),
                         0, GEAR_TEETH, 0.1, 0.4, 0.5, 0, GEAR_TEETH)
        ];
    }

    /*
     * The board's dot grid as one SVG sized in board units, stretched to the
     * board's pixel box. Both the dots and the gears then derive from the same
     * coordinate system, so they cannot drift apart the way a tiled CSS
     * background can when the tile size is fractional.
     *
     * The original drew a 2x2 rectangle with its top-left ON each cell corner
     * (GamePage.xaml.cs, "Add grid dots in background").
     */
    function dotGridURL(boardW, boardH, cellW, cellH) {
        var svg = '<svg xmlns="http://www.w3.org/2000/svg" ' +
                  'viewBox="0 0 ' + boardW + ' ' + boardH + '" ' +
                  'preserveAspectRatio="none">' +
                  '<defs><pattern id="d" width="' + cellW + '" height="' + cellH + '" ' +
                  'patternUnits="userSpaceOnUse">' +
                  '<rect width="2" height="2" fill="rgba(128,128,128,0.5)"/>' +
                  '</pattern></defs>' +
                  '<rect width="' + boardW + '" height="' + boardH + '" fill="url(%23d)"/>' +
                  '</svg>';
        return 'data:image/svg+xml,' + encodeURIComponent(svg).replace(/%2523/g, '%23');
    }

    function gearSVGURL(pattern) {
        if (svgCache[pattern]) { return svgCache[pattern]; }
        ensurePaths();

        // Quadrant 0..3 are the BR, BL, TL and TR corners of the 2x2 cell,
        // exactly as GearPaths[0..3] bound to Fills..Fills3 in the original.
        var order = [3, 2, 0, 1];
        var body = '<path fill="#161616" d="' + RING_D[0] + '"/>' +
                   '<path fill="#000000" d="' + RING_D[1] + '"/>';

        for (var i = 0; i < 4; i++) {
            body += '<path fill="' + colorFor(pattern[order[i]]) + '" d="' + QUAD_D[i] + '"/>';
        }

        svgCache[pattern] = svgURL(body);
        return svgCache[pattern];
    }

    /*
     * Same tile as a data URL, for use as a CSS background-image. A gear's
     * image never changes once it exists - board rotation is a CSS transform -
     * so each pattern is rasterised at most once.
     */
    var urlCache = {};

    function gearURL(pattern) {
        var key = pattern + '@' + tileQuality;
        if (!urlCache[key]) {
            urlCache[key] = gearTile(pattern).toDataURL('image/png');
        }
        return urlCache[key];
    }

    function clearURLCache() { urlCache = {}; }

    // ------------------------------------------------- background gears

    function randInt(n) { return n <= 0 ? 0 : Math.floor(Math.random() * n); }

    /*
     * The slow black cogs behind the board. AddBackgroundGear used random
     * radii, tooth counts and flank stops for each one.
     */
    function makeBackgroundGear(boardW, boardH) {
        var size = randInt(Math.max(Math.round(boardW), 1)) + 200;

        var left = randInt(3) / 10.0 + 0.1;
        var top = randInt(4) / 10.0 + left;
        var right = randInt(4) / 10.0 + top;

        var paths = buildGearPaths(randInt(10) + 39, 50, randInt(20), randInt(50) + 1,
                                   0.001, left, top, right);

        var px = Math.min(Math.round(size * 1.3), 1400);
        var cv = document.createElement('canvas');
        cv.width = px;
        cv.height = px;

        var ctx = cv.getContext('2d');
        var scale = px / 130;                 // 100 units of gear + outline slop
        ctx.translate(px / 2, px / 2);
        ctx.scale(scale, scale);
        ctx.translate(-50, -50);

        ctx.fillStyle = 'rgb(0,0,0)';
        if (paths.outline2) { ctx.fill(paths.outline2); }
        if (paths.outline) { ctx.fill(paths.outline); }
        for (var i = 0; i < 4; i++) { ctx.fill(paths.quads[i]); }

        return {
            url: cv.toDataURL('image/png'),
            canvas: cv,
            size: size,
            left: randInt(Math.max(Math.round(boardW / 2), 1)),
            top: randInt(Math.max(Math.round(boardH / 2), 1)),
            angle: randInt(360),
            spin: (size % 2 === 0 ? 1 : -1)   // matches the alternating spin direction
        };
    }

    function drawBackgroundGear(ctx, g, alpha) {
        var half = g.size / 2;
        ctx.save();
        ctx.globalAlpha = alpha;
        ctx.translate(g.left + half, g.top + half);
        ctx.rotate(g.angle * Math.PI / 180);
        ctx.drawImage(g.canvas, -half * 1.3, -half * 1.3, g.size * 1.3, g.size * 1.3);
        ctx.restore();
    }

    global.Gear = {
        COLORS: COLORS,
        colorFor: colorFor,
        GEAR_BOX: GEAR_BOX,
        makeGear: makeGear,
        drawGear: drawGear,
        gearURL: gearURL,
        gearTile: gearTile,
        gearPathData: gearPathData,
        gearSVGURL: gearSVGURL,
        dotGridURL: dotGridURL,
        setTileQuality: setTileQuality,
        makeBackgroundGear: makeBackgroundGear,
        drawBackgroundGear: drawBackgroundGear
    };

}(window));
