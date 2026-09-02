/*
 * Clockwork Rush - help text
 *
 * The tutorial screenshots in assets/img are the originals from the Windows
 * Store build, and several of them carry their explanation burned into the
 * image. Those captions are transcribed verbatim below and marked; the rest
 * describe what the frame shows.
 *
 * Each caption is an array of paragraphs.
 */
(function (global) {
    'use strict';

    global.HELP_CONTENT = {

        controls: {
            title: 'Controls',
            // HelpControls.xaml drew this as a keyboard diagram, not a bitmap.
            diagram: true,
            captions: [[
                "Move the active gear with A, S and D, or the arrow keys. Rotate the whole board with Q and R.",
                "Click and drag on the game board to move the active piece. It must be moved to a position below the timer line.",
                "W, E and F are shown greyed out because they were never wired up."
            ]]
        },

        rotation: {
            title: 'Board Rotation',
            images: ['Rotation_f01.png', 'Rotation_f02.png', 'Rotation_f03.png',
                     'Rotation_f04.png', 'Rotation_f05.png'],
            captions: [
                [   // verbatim, Rotation_f01
                    "The direction isn't as important as realizing that the game board is in 4 states (0, 90, 180, and 270.)",
                    "Switching quickly through these states and looking for matches is what the game is all about!"
                ],
                [   // verbatim, Rotation_f02
                    "Pieces rotate clockwise or counterclockwise when you click the rotate button or hit the rotate key.",
                    "The direction is reversed for each gear on top of another gear (by row.)",
                    "Follow the colored dots in the following screenshots as the Rotate Clockwise key was pressed."
                ],
                [
                    "Rotation: 180 degrees. The red and magenta dots travel opposite ways round their gears, because the two rows are meshed."
                ],
                [
                    "Rotation: 270 degrees. One more press and every gear is back where it started."
                ],
                [   // verbatim, Rotation_f05
                    "That's all there is to it!",
                    "Four states, and back to 0."
                ]
            ]
        },

        matching: {
            title: 'Making Matches',
            images: ['RegularMatch_f01.png', 'RegularMatch_f02.png', 'RegularMatch_f03.png',
                     'RegularMatch_f04.png', 'RegularMatch_f05.png', 'RegularMatch_f06.png',
                     'RegularMatch_f07.png'],
            captions: [
                [
                    "A new gear drops in above the pile. Steer it toward a gear whose top colors you can match."
                ],
                [   // verbatim, RegularMatch_f02
                    "Here, the orange and red match vertically."
                ],
                [   // verbatim, RegularMatch_f03
                    "Both Gears are Removed, but no Combo can happen."
                ],
                [
                    "A clean match is worth 200 points."
                ],
                [
                    "Line the falling gear up between two gears and you can match against both at once."
                ],
                [   // verbatim, RegularMatch_f06
                    "Here, the reds match vertically to the red corners of the adjacent blocks.",
                    "All four don't have to be the same, just the colors in each highlighted column."
                ],
                [   // verbatim, RegularMatch_f07
                    "That match removed all 3 blocks!"
                ]
            ]
        },

        combos: {
            title: 'Combos',
            images: ['Combo1.png', 'Combo2.png', 'Combo3.png', 'Combo4.png', 'Combo5.png'],
            captions: [
                [
                    "A green gear falls toward a notch in the stack."
                ],
                [
                    "It drops into place against the gears below it."
                ],
                [   // verbatim, Combo3
                    "Match results in a combo removing pieces along the diagonals."
                ],
                [   // verbatim, Combo4
                    "Bonus points are awarded for combo matches."
                ],
                [
                    "The whole diagonal comes apart, and the bonus climbs with every gear that goes.",
                    "The clock is draining points the whole time, so speed pays."
                ]
            ]
        }
    };

}(window));
