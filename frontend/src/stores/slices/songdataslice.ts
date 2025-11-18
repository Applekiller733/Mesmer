import { createSlice } from '@reduxjs/toolkit';
import type { Song } from '../../models/song';
import type { RootState } from '../store';
import { fetchSongById, fetchSongIds, fetchSongs, flipLike } from '../thunks/songthunks';

const getInitialState = (): {
    songs: Song[],
    allSongIds: number[],
    loadedSongs: Record<number, Song>,
    currentSongIndex: number,
    status: string, errormsg: string
} => {

    return {
        songs: [],
        allSongIds: [],
        loadedSongs: {},
        currentSongIndex: 0,
        status: 'idle',
        errormsg: '',
    };
};


export const songdataSlice = createSlice(
    {
        name: 'songdata',
        initialState: getInitialState(),
        reducers: {
            
            loadSong: (state, action) => {
                if (action.payload) {
                    state.loadedSongs[action.payload.id] = action.payload;
                }
            },
            unloadSong: (state, action) => {
                if (action.payload) {
                    delete state.loadedSongs[action.payload];
                }
            },
            setIndex: (state, action) => {
                if (action.payload !== undefined) {
                    state.currentSongIndex = action.payload;
                }
            },
        },
        extraReducers(builder) {
            builder.addCase(fetchSongs.pending, (state) => {
                state.status = 'loading';
            })
                .addCase(fetchSongs.fulfilled, (state, action) => {
                    state.status = 'succeeded';
                    const loadedSongs = action.payload?.map((song: Song) => {
                        return song;
                    });
                    if (loadedSongs !== undefined) {
                        state.songs = state.songs.concat(loadedSongs);
                        // state.songs = loadedSongs;
                    }
                })
                .addCase(fetchSongs.rejected, (state, action) => {
                    state.status = 'failed';
                    state.errormsg = action.error.message || 'failed to load error';
                })
                .addCase(fetchSongIds.fulfilled, (state, action) => {
                    state.status = 'succeeded';
                    const loadedSongIds = action.payload;

                    if (loadedSongIds !== undefined) {
                        state.allSongIds = loadedSongIds;
                    }
                })
                .addCase(fetchSongIds.rejected, (state, action) => {
                    state.status = 'failed';
                    state.errormsg = action.error.message || 'failed to load error';
                })
                .addCase(flipLike.fulfilled, (state, action) => {
                    state.status = 'succeeded';
                    state.loadedSongs[action.payload.id].upvotes = action.payload.upvotes;
                })
        },
    }
);

export const selectAllSongs = (store: RootState) => store.songdata.songs; // Can add further validation
export const selectAllSongIds = (store: RootState) => store.songdata.allSongIds;
export const selectLoadedSongs = (store: RootState) => store.songdata.loadedSongs;
export const selectCurrentSongIndex = (store: RootState) => store.songdata.currentSongIndex;
export const selectCurrentSong = (store: RootState) =>
    store.songdata.loadedSongs[store.songdata.allSongIds[store.songdata.currentSongIndex]];
export const selectPreviousSong = (store: RootState) =>
    store.songdata.currentSongIndex > 0
        ?
        store.songdata.loadedSongs[store.songdata.allSongIds[store.songdata.currentSongIndex - 1]]
        :
        null;
export const selectNextSong = (store: RootState) =>
    store.songdata.currentSongIndex < store.songdata.allSongIds.length - 1
        ?
        store.songdata.loadedSongs[store.songdata.allSongIds[store.songdata.currentSongIndex + 1]]
        :
        null;

export const selectSongsStatus = (store: RootState) => store.songdata.status;
export const selectSongsError = (store: RootState) => store.songdata.errormsg;

export const { loadSong, unloadSong, setIndex } = songdataSlice.actions;

export default songdataSlice.reducer;