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

export const savePlaylist = createAsyncThunk(
    'playlists/savePlaylist',
    async (playlistId: string, thunkAPI) => {
        try {
            return await apisaveplaylist(playlistId);
        }
        catch (err: any) {
            return thunkAPI.rejectWithValue(err.message);
        }
    }
);


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


export const updatePlaylistVisibility = createAsyncThunk(
    'playlists/updatePlaylistVisibility',
    async (
        args: { playlistId: string; visibility: PlaylistVisibility },
        thunkAPI,
    ) => {
        try {
            return await apiupdateplaylistvisibility(args.playlistId, args.visibility);
        }
        catch (err: any) {
            return thunkAPI.rejectWithValue(err.message);
        }
    }
);