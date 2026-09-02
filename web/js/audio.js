/*
 * Clockwork Rush - sound
 *
 * The original used a MediaElement per SFX hit and one root MediaElement for
 * music (A TYPE = NickMade.mp3, B TYPE = PowerhouseHouse.mp3). Browsers will
 * not start audio before a gesture, so playback is armed on first input.
 */
(function (global) {
    'use strict';

    var SFX_SRC = {
        grind: 'assets/sound/grind.wav',
        merge: 'assets/sound/Merge.wav'
    };

    var MUSIC_SRC = {
        A: 'assets/sound/NickMade.mp3',
        B: 'assets/sound/PowerhouseHouse.mp3'
    };

    var pools = {};
    var POOL_SIZE = 5;
    var unlocked = false;

    var music = new Audio();
    music.loop = true;
    music.volume = 0.45;

    var musicTrack = 'A';
    var musicWanted = false;

    function pool(name) {
        if (pools[name]) { return pools[name]; }

        var voices = [];
        for (var i = 0; i < POOL_SIZE; i++) {
            var a = new Audio(SFX_SRC[name]);
            a.volume = 0.5;
            a.preload = 'auto';
            voices.push(a);
        }
        pools[name] = { voices: voices, next: 0 };
        return pools[name];
    }

    var Sound = {
        sfxEnabled: true,

        /* Called from the first real user gesture. */
        unlock: function () {
            if (unlocked) { return; }
            unlocked = true;
            if (musicWanted) { Sound.playMusic(musicTrack); }
        },

        play: function (name) {
            if (!Sound.sfxEnabled || !unlocked || !SFX_SRC[name]) { return; }

            var p = pool(name);
            var voice = p.voices[p.next];
            p.next = (p.next + 1) % POOL_SIZE;

            try {
                voice.currentTime = 0;
                var r = voice.play();
                if (r && r.catch) { r.catch(function () { /* autoplay blocked */ }); }
            } catch (err) { /* not ready yet */ }
        },

        /* track is 'A', 'B' or '' for none. */
        playMusic: function (track) {
            musicTrack = track;
            musicWanted = !!MUSIC_SRC[track];

            if (!musicWanted) {
                music.pause();
                music.removeAttribute('src');
                music.load();
                return;
            }

            if (!unlocked) { return; }

            var src = MUSIC_SRC[track];
            if (music.getAttribute('src') !== src) {
                music.setAttribute('src', src);
                music.load();
            }

            var r = music.play();
            if (r && r.catch) { r.catch(function () { /* autoplay blocked */ }); }
        },

        pauseMusic: function () {
            music.pause();
        },

        resumeMusic: function () {
            if (musicWanted && unlocked) {
                var r = music.play();
                if (r && r.catch) { r.catch(function () {}); }
            }
        },

        stopMusic: function () {
            music.pause();
            try { music.currentTime = 0; } catch (err) { /* not seekable yet */ }
        }
    };

    global.Sound = Sound;

}(window));
