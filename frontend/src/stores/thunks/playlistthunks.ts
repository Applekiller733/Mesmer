import { createAsyncThunk } from "@reduxjs/toolkit";
import type {
    CreatePlaylistRequest,
    DeletePlaylistRequest,
    Playlist,
    PlaylistVisibility,
    UpdatePlaylistRequest,
} from "../../models/playlist";
import {
    apicreateplaylist,
    apideleteplaylist,
    apifetchplaylistbyid,
    apifetchplaylists,
    apifetchplaylistscreatedbyaccount,
    apifetchplaylistssavedbyaccount,
    apisaveplaylist,
    apiunsaveplaylist,
    apiupdateplaylist,
    apiupdateplaylistvisibility,
} from "../api/playlistapi";

export const fetchPlaylists = createAsyncThunk('playlists/fetchPlaylists', async (_, thunkAPI) => {
    try {
        return await apifetchplaylists();
    }
    catch (err: any) {
        return thunkAPI.rejectWithValue(err.message);
    }
})

export const fetchPlaylistById = createAsyncThunk('playlists/fetchPlaylistById', async (id: string, thunkAPI) => {
    try {
        return await apifetchplaylistbyid(id);
    }
    catch (err: any) {
        return thunkAPI.rejectWithValue(err.message);
    }
})

export const fetchLoadedPlaylist = createAsyncThunk('playlists/fetchLoadedPlaylist', async (id: string, thunkAPI) => {
    try {
        return await apifetchplaylistbyid(id);
    }
    catch (err: any) {
        return thunkAPI.rejectWithValue(err.message);
    }
})

export const fetchPlaylistsCreatedByAccountId = createAsyncThunk('playlists/fetchPlaylistsCreatedByAccountId',
    async (accountid: string, thunkAPI) => {
        try {
            return await apifetchplaylistscreatedbyaccount(accountid);
        }
        catch (err: any) {
            return thunkAPI.rejectWithValue(err.message);
        }
    })

export const fetchPlaylistsSavedByAccountId = createAsyncThunk('playlists/fetchPlaylistsSavedByAccountId',
    async (accountid: string, thunkAPI) => {
        try {
            return await apifetchplaylistssavedbyaccount(accountid);
        }
        catch (err: any) {
            return thunkAPI.rejectWithValue(err.message);
        }
    })

//check if this type of implementation is fine?
//should only be called with self id
// export const fetchPlaylistsOwned = createAsyncThunk('playlists/fetchPlaylistsOwned', async (accountid: number) => {
//     try {
//         const response:Playlist[] = await apifetchplaylistscreatedbyaccount(accountid);

//         if (!response){
//             throw new Error("Fetching Owned Playlists failed");
//         }
//         return response;
//     }
//     catch(err:any){
//         return err.message;
//     }
// })

export const createPlaylist = createAsyncThunk('playlists/createPlaylist', async (request: CreatePlaylistRequest, thunkAPI) => {
    try {
        return await apicreateplaylist(request);
    }
    catch (err: any) {
        return thunkAPI.rejectWithValue(err.message);
    }
})

export const updatePlaylist = createAsyncThunk('playlists/updatePlaylist', async (request: UpdatePlaylistRequest, thunkAPI) => {
    try {
        return await apiupdateplaylist(request);
    }
    catch (err: any) {
        return thunkAPI.rejectWithValue(err.message);
    }
})

export const deletePlaylist = createAsyncThunk('playlists/deletePlaylist', async (request: DeletePlaylistRequest, thunkAPI) => {
    try {
        return await apideleteplaylist(request);
    }
    catch (err: any) {
        return thunkAPI.rejectWithValue(err.message);
    }
})

// ---------------------------------------------------------------------------
// Save / unsave / visibility-change thunks
// ---------------------------------------------------------------------------

/**
 * Save a Public or Unlisted playlist to the current user's library.
 * Idempotent at the API layer — re-saving is a no-op success.
 *
 * Note: most consumers will want to dispatch fetchPlaylistsSavedByAccountId
 * after a successful save to refresh the sidebar list. Doing that inside
 * the thunk would require pulling the current user id; keeping the
 * post-action refresh at the call site mirrors how createPlaylist /
 * deletePlaylist are used elsewhere.
 */
export const savePlaylist = createAsyncThunk(
    'playlists/savePlaylist',
    async (playlistId: string, thunkAPI): Promise<Playlist> => {
        try {
            return await apisaveplaylist(playlistId);
        }
        catch (err: any) {
            return thunkAPI.rejectWithValue(err.message);
        }
    }
);

/**
 * Remove a playlist from the current user's library. The backend
 * rejects owners trying to unsave their own playlists; callers should
 * surface the resulting error so the user knows they need Delete instead.
 */
export const unsavePlaylist = createAsyncThunk(
    'playlists/unsavePlaylist',
    async (playlistId: string, thunkAPI) => {
        try {
            await apiunsaveplaylist(playlistId);
            // Echo the id back as the fulfilled payload so slice
            // reducers can do optimistic removal without a follow-up
            // fetch if they want to.
            return playlistId;
        }
        catch (err: any) {
            return thunkAPI.rejectWithValue(err.message);
        }
    }
);

/**
 * Owner-only visibility change. Takes the playlist id and the new
 * visibility; returns the updated Playlist. Use the typed
 * PlaylistVisibility members (PlaylistVisibility.Public etc.) rather
 * than raw integers to keep the call sites self-documenting.
 */
export const updatePlaylistVisibility = createAsyncThunk(
    'playlists/updatePlaylistVisibility',
    async (
        args: { playlistId: string; visibility: PlaylistVisibility },
        thunkAPI,
    ): Promise<Playlist> => {
        try {
            return await apiupdateplaylistvisibility(args.playlistId, args.visibility);
        }
        catch (err: any) {
            return thunkAPI.rejectWithValue(err.message);
        }
    }
);