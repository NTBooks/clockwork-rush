# Clockwork Rush

A gear-matching puzzle game. Land a falling gear so its colours line up with the
gear beneath it and both grind away — then rotate the whole board a quarter turn
and do it again.

**Play it: https://clockwork-rush.ping6163.workers.dev**

---

## History

I wrote Clockwork Rush in 2013 as a Windows 8 Store app — WinRT and XAML, C#,
with the gears drawn as procedural vector `PathGeometry` rather than sprites. It
shipped on the Windows Store under my publisher name, Dinetopia.

I'm the original author. Windows 8, the Store as it existed then, and WinRT
itself have all long since moved on, and the game went with them — so in 2026 I
had it ported to the browser. The original C# is still in this repo, untouched,
next to the port.

The port is deliberately faithful. It's the same engine logic, the same
staggered gear lattice, the same collision and match rules, the same procedural
gear geometry, and the same block-letter WIN / LOSE banners — just expressed in
JavaScript, DOM and CSS instead of C# and XAML. Even the tutorial screenshots
are the 2013 originals.

## How to play

Gears sit on a staggered lattice. Each gear covers a 2×2 block of cells, and
each cell is one coloured quadrant of it.

- A new gear drops in whenever the last one settles. Land it so its **bottom two
  colours** match the **top two colours** of the gear underneath, and both grind
  away.
- Rows are offset, so a gear can straddle **two** gears at once. Match that way
  and you clear both — worth three times as much.
- Gears left with nothing under them crumble on their own. That's where the
  combos come from: a clear propagates along the diagonals.
- The clock drains points the whole time, so speed pays.
- Clear the entire board to win. Let the pile reach the top and you lose.

The board is always in one of four rotation states — 0°, 90°, 180°, 270°.
Switching quickly between them looking for matches is the whole game.

## Controls

| | |
| --- | --- |
| `A` `S` `D` or arrow keys | Move the falling gear |
| `Q` / `R` | Rotate every gear a quarter turn — neighbouring rows turn opposite ways, like meshed gears |
| Click or touch and drag | Move the falling gear directly, anywhere below the timer line |
| `P` or `Esc` | Pause |

On a phone all the controls sit in one row under the board. The game is happiest
in landscape; upright it will suggest you turn the device, but only when the
board is genuinely too wide to be playable at that size.

`W`, `E` and `F` show greyed out on the Controls help page because they were
never wired up in 2013 either.

## What's in here

| | |
| --- | --- |
| `web/` | The browser port. Plain HTML, CSS and JavaScript — no build step, no dependencies. |
| `GearMaker/` | The original 2013 WinRT / XAML source, as it was. |
| `Rotetris Test/` | An earlier WPF prototype, kept for the record. |
| `wrangler.jsonc` | Cloudflare Workers static-asset config for the deployed site. |

`web/README.md` has the technical detail — what maps to what, how the view
animates, and the handful of places the port deliberately departs from the
original.

## Running it

It's a folder of static files; any static server will do.

```bash
npx wrangler dev
```

Then open <http://127.0.0.1:8787>.

## No license

**No license is granted.** This code is published for reference and for the
history of the thing, not for reuse. All rights reserved by the author.

The audio and artwork are mine as well and are not licensed for reuse.
