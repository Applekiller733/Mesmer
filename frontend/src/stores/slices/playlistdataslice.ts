import { createSlice } from '@reduxjs/toolkit';
import type { Playlist } from '../../models/playlist';
import { PlaylistVisibility } from '../../models/playlist';
import type { RootState } from '../store';
import { fetchLoadedPlaylist, fetchPlaylistById, fetchPlaylists, fetchPlaylistsCreatedByAccountId, fetchPlaylistsSavedByAccountId } from '../thunks/playlistthunks';

const getInitialState = (): { playlists: Playlist[], savedplaylists: Playlist[], loadedplaylist:Playlist, status: string, errormsg: string } => {

    return {
        playlists: [], // should be 'generally loaded' playlists, like on a discovery page or smth
        savedplaylists: [], //should be personally saved playlists
        // Visibility added to the initial loadedplaylist now that
        // Playlist requires it. Defaulting to Private here is a safe
        // sentinel — the value is replaced as soon as a real playlist
        // is fetched, and Private would never accidentally widen
        // access if a stray render somehow read this default.
        loadedplaylist: {id: '', name: '', createdAt: '', updatedAt: '', visibility: PlaylistVisibility.Private, songs:[]},
        status: 'idle',
        errormsg: '',
    };
};


export const playlistdataSlice = createSlice(
    {
        name: 'playlistdata',
        initialState: getInitialState(),
        reducers: {
        },
        extraReducers(builder) {
            builder.addCase(fetchPlaylists.pending, (state) => {
                state.status = 'loading';
            })
                .addCase(fetchPlaylists.fulfilled, (state, action) => {
                    state.status = 'succeeded';
                    // console.log(action);
                    const loadedPlaylists = action.payload?.map((playlist: Playlist) => {
                        return playlist;
                    });
                    if (loadedPlaylists !== undefined) {
                        state.playlists = state.playlists.concat(loadedPlaylists);
                        // state.songs = loadedSongs;
                    }
                })
                .addCase(fetchPlaylists.rejected, (state, action) => {
                    state.status = 'failed';
                    state.errormsg = action.error.message || 'failed to load error';
                })
                .addCase(fetchPlaylistsSavedByAccountId.pending, (state) => {
                    state.status = 'loading';
                })
                .addCase(fetchPlaylistsSavedByAccountId.fulfilled, (state, action) => {
                    state.status = 'succeeded';
                    console.log(action);
                    const loadedPlaylists = action.payload?.map((playlist: Playlist) => {
                        return playlist;
                    });
                    if (loadedPlaylists !== undefined) {
                        // state.savedplaylists = state.savedplaylists.concat(loadedPlaylists);
                        state.savedplaylists = loadedPlaylists;
                        // state.songs = loadedSongs;
                    }
                })
                .addCase(fetchPlaylistsSavedByAccountId.rejected, (state, action) => {
                    state.status = 'failed';
                    state.errormsg = action.error.message || 'failed to load error';
                })
                .addCase(fetchLoadedPlaylist.fulfilled, (state, action) => {
                    state.status = 'succeeded';
                    const loadedPlaylist = action.payload;
                    if (loadedPlaylist !== undefined){
                        state.loadedplaylist = loadedPlaylist;
                    }
                })
        },
    }
);

export const selectAllPlaylists = (store: RootState) => store.playlistdata.playlists; // Can add further validation
export const selectSavedPlaylists = (store: RootState) => store.playlistdata.savedplaylists;
export const selectLoadedPlaylist = (store: RootState) => store.playlistdata.loadedplaylist;
export const selectUsersStatus = (store: RootState) => store.playlistdata.status;
export const selectUsersError = (store: RootState) => store.playlistdata.errormsg;

// export const { updateUpvotes } = playlistdataSlice.actions;

export default playlistdataSlice.reducer;