// frontend/src/stores/slices/playbackslice.ts
//
// Holds playback-level UI state that's global rather than per-song.
// Right now: volume (0-1) and muted (bool). Designed to grow into things
// like shuffle, repeat-mode, and playback speed if you add them later.
//
// Volume is persisted to localStorage immediately so it survives reloads
// without needing a backend round-trip. When user-settings save/load is
// wired up later, that thunk just hydrates this slice on login.

import { createSlice, type PayloadAction } from '@reduxjs/toolkit';
import type { RootState } from '../store';

const STORAGE_KEY = 'playback.volume.v1';

interface PlaybackState {
    volume: number;   // 0.0 – 1.0
    muted: boolean;
}

function readPersistedVolume(): number {
    if (typeof window === 'undefined') return 1.0;
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        if (raw === null) return 1.0;
        const parsed = parseFloat(raw);
        // Clamp to [0, 1] in case the stored value was tampered with.
        if (Number.isFinite(parsed)) return Math.min(1, Math.max(0, parsed));
    } catch {
        // localStorage can throw in private mode / quota issues — fall through.
    }
    return 1.0;
}

function persistVolume(v: number) {
    if (typeof window === 'undefined') return;
    try {
        localStorage.setItem(STORAGE_KEY, String(v));
    } catch {
        // Same defense as above — never let persistence break playback.
    }
}

const getInitialState = (): PlaybackState => ({
    volume: readPersistedVolume(),
    muted: false,
});

export const playbackSlice = createSlice({
    name: 'playback',
    initialState: getInitialState(),
    reducers: {
        // Accepts 0–1. UI components are responsible for converting from 0–100.
        setVolume: (state, action: PayloadAction<number>) => {
            const v = Math.min(1, Math.max(0, action.payload));
            state.volume = v;
            // Adjusting volume implicitly un-mutes — matches every audio app
            // people are used to (YouTube, Spotify, system mixers).
            if (v > 0) state.muted = false;
            persistVolume(v);
        },
        setMuted: (state, action: PayloadAction<boolean>) => {
            state.muted = action.payload;
        },
        toggleMuted: (state) => {
            state.muted = !state.muted;
        },
    },
});

export const selectVolume = (store: RootState) => store.playback.volume;
export const selectMuted = (store: RootState) => store.playback.muted;
// Convenience selector: what should ReactPlayer's `volume` prop actually be?
// When muted, ReactPlayer wants 0; otherwise the configured volume.
export const selectEffectiveVolume = (store: RootState) =>
    store.playback.muted ? 0 : store.playback.volume;

export const { setVolume, setMuted, toggleMuted } = playbackSlice.actions;
export default playbackSlice.reducer;