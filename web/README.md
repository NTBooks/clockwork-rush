# Clockwork Rush — web port

A browser port of the Windows Store / WinRT game in `../GearMaker`. Plain HTML,
CSS and DOM — no build step, no dependencies, no frameworks.

## Running it

There is no build step and no server code — it is a folder of static files.

```bash
npx wrangler dev
```

Then open <http://127.0.0.1:8787>. Any other static server works just as well
(`npx serve web`, `python -m http.server`, nginx). Opening `index.html` straight
off disk works too, but some browsers block the audio over `file://`.

## Deploying to Cloudflare

`wrangler.jsonc` in the repo root configures Workers Static Assets: it points at
`./web` and declares no `main`, so Cloudflare serves the files directly with no
Worker script in front of them.

```bash
npx wrangler deploy
```

That publishes to `clockwork-rush.<your-subdomain>.workers.dev`. It needs a
Cloudflare account — run `npx wrangler login` once first, or set
`CLOUDFLARE_API_TOKEN`. Use `npx wrangler versions upload` to stage a version
without putting it live.

All paths in the page are relative, so it also works served from a subdirectory
behind a custom domain or any other static host.

Asset budget: 31 files, 5.4 MB, largest 2.1 MB — comfortably inside the
20,000-file and 25 MiB-per-file limits. The two MP3s are most of that weight;
delete them and the **None** music option still works.

## What maps to what

| Original | Port |
| --- | --- |
| `Rotetris.cs` (`Rotetris_Engine`) | `js/engine.js` |
| `UIGear.cs` (`PathGeometry` gears) | `js/gear.js` |
| `GamePage.xaml.cs` (board, animation, physics) | `js/view.js` |
| `MainPage.xaml.cs`, `HelpRoot`, `Help*.xaml` | `js/ui.js`, `index.html` |
| `GameOptions.cs` singleton | `options` object in `js/ui.js`, persisted to `localStorage` |
| `RecentState.cs` (`SaveState.xml`) | `Engine.serialize()` / `Engine.deserialize()`, persisted to `localStorage` |
| `Help*.xaml` caption text | `js/help-content.js` (transcribed from the screenshots) |
| `Assets/*.png`, `Sounds/*` | `assets/img`, `assets/sound` (copied unchanged) |

The engine is a direct port: same char grid, same `'*'` empty cell, same
staggered 2×2 gear lattice, same collision strings, same match and glance rules,
same orphan sweep, same block-letter win/lose banners. It is headless and raises
events; `js/view.js` draws them.

## How the view animates

The original never computed an in-between frame. It declared a target on a XAML
Storyboard and let the compositor get there. `js/view.js` does the same with CSS
transitions, so **there is no render loop** — the only repeating timer in the
whole game is the engine tick.

The rotation trick is the one the help screens spell out:

> "The direction isn't as important as realizing that the game board is in 4
> states (0, 90, 180, and 270.)" — `Rotation_f01.png`

So a gear's image is rasterised once, at birth, and never changes. A quarter
turn is `angle += 90` on its `transform`, and CSS tweens the 150ms. Nothing is
re-coloured mid-animation, and a gear's *displayed* face is just its baked
pattern turned by `angle / 90` — which is exactly the invariant `reconcile()`
checks against the model.

| | main-thread cost |
| --- | --- |
| Idle, no input | nothing but the one-per-second engine tick |
| One board rotation, 354 gears | 7.9 ms once, then the compositor owns the tween |
| Falling piece step | one `transform` write, 80 ms transition |
| Resize | one `--s` write; CSS repositions everything |

Gears are **vector**, not bitmaps. Each colour combination is one SVG data URI
(the two dark rings plus four quadrant paths, from the same `gearPathData()`
that feeds the canvas `Path2D`), so the browser re-rasterises it at whatever
size the gear actually is. The board is sized in real pixels via a `--s`
custom property rather than a `transform: scale()`, because a scaled layer is
rasterised once and stretched — which softened everything inside it.

Two approaches were measured and rejected:

| approach | rotation, 354 gears | crisp? |
| --- | --- | --- |
| PNG tiles + `transform: scale()` | 4.2 ms | no — magnified 113-120% on common displays |
| SVG mask + `conic-gradient` per gear | 27.2 ms | yes, but 163% of a frame budget |
| **one composed SVG per pattern** | **7.9 ms** | **yes** |

Debris and score numbers are Web Animations with sampled ballistic keyframes, so
the original's kinematics survive without a physics timer; each removes itself
on `finish`.

## Rules

Gears sit on a staggered lattice — each one is 2×2 cells, and each cell is a
coloured quadrant. A new gear falls from the top whenever the last one settles.

- Land a gear so its **bottom two colours** match the **top two colours** of the
  gear beneath it, and both grind away.
- Rows are offset, so a gear can straddle **two** lattice gears at once. Match
  that way and you clear both — worth three times as much.
- Gears left with nothing under them crumble on their own, which is where the
  chains come from.
- The clock drains points continuously, so speed pays.
- Clear the whole board to win. Let the pile reach the top and you lose.

## Controls

| | |
| --- | --- |
| `A` `S` `D` or arrow keys | Move the falling gear |
| `Q` / `R` | Rotate every gear on the board a quarter turn (rows turn in opposite directions, like meshed gears) |
| Click / touch and drag | Move the falling gear directly — but never above the timer line |
| `P` or `Esc` | Pause |

On touch, every control shares one row under the board: rotate, left, down,
right, rotate. Flanking rotate buttons cost 120px of width, which on a 390px
phone is 31% of the screen — far more than a 44px row costs in height, and the
board is what needs the room. Dragging also lifts the gear three rows clear of
the fingertip so your thumb isn't covering it; a mouse still grabs it by the
middle the way the original did.

The board is much wider than it is tall, so upright it has to shrink. Rather
than blanket-blocking portrait, the game checks the size the gears actually
came out at and only asks you to turn the device when they fall below 22px —
a 20-column board is nearly square and plays fine upright (34px gears), a
60-column one does not (11px). There is a "play anyway" either way, and the
clock is paused while the prompt is up.

`W`, `E` and `F` are shown greyed out on the Controls help page because they
were never wired up in the original either.

The board keeps rotating after the game ends — that easter egg is in the
original's `SendKeyUpperCase`, which lets `R` and `Q` through while `GameEnded`.
The WIN / LOSE banner also keeps turning on its own at the game's tick rate.

## Deliberate changes

Everything else is a faithful port; these are the exceptions.

- **Save and resume actually work.** `RecentState.cs` existed but `MainPage`
  hard-collapsed the Resume button, so the feature was dead. Here the board,
  falling piece, clock and timer line all round-trip through `localStorage`.
- **Display is reconciled against the model each tick.** In the original, a gear
  that settled off the lattice kept its sprite while board rotation scrambled the
  cells underneath it, so the picture could drift from the model. `reconcile()`
  in `js/view.js` re-reads the model and corrects any gear that disagrees.
- **Two leaks fixed.** Floating score numbers were never removed (their removal
  test could not fire for objects moving upward), and the score readout stalled
  short of the true value because its approach step used integer division.
- **The clock's point drain is a named constant.** `SCORE_PER_TICK` at the top
  of `js/view.js` is the `ModScore(-5, 0, 0)` from `GamePage.xaml.cs:510`
  ("Deduct points for slowness"). Set it to `0` to play without the drain.
- **`ensureMatch()` has a guard.** The original indexed one rotation preview with
  another's length, which could throw on an empty board; it now falls back to a
  random colour pair.
- **Arrow keys** work alongside `A`/`S`/`D`, and there are pause and game-over
  overlays, since a browser page has no Back button or app bar.
- **The win/lose banner spins by itself.** `Rotetris.cs:1009` stops the engine
  timer at game end, so in the original the banner only moved if you pressed
  R or Q. It never set `Running = false` though, so the board stayed rotatable —
  the view just keeps pressing `R` at the game's tick rate.
- **A mobile layout**, which the Store build had no need for: it locked itself
  to landscape via `DisplayProperties.AutoRotationPreferences`.
- **Help captions were transcribed from the tutorial screenshots**, several of
  which carry their text burned into the image. `js/help-content.js` marks which
  lines are verbatim.
- Gear bitmaps are cached per colour combination, and the board scales to the
  window with a CSS transform instead of a XAML `Viewbox`.

## Console handle

`window.ClockworkRush` exposes `getEngine()`, `getView()`, `options` and
`screen()` for poking at a running game.
