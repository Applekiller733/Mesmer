import { createAsyncThunk } from "@reduxjs/toolkit";
import type { CreatePlaylistRequest, DeletePlaylistRequest, Playlist, UpdatePlaylistRequest } from "../../models/playlist";
import {
    apicreateplaylist, apideleteplaylist, apifetchplaylistbyid, apifetchplaylists,
    apifetchplaylistscreatedbyaccount, apifetchplaylistssavedbyaccount, apiupdateplaylist
} from "../api/playlistapi";

export const fetchPlaylists = createAsyncThunk('playlists/fetchPlaylists', async (_, thunkAPI) => {
    try {
        return await apifetchplaylists();
    }
    catch (err: any) {
        return thunkAPI.rejectWithValue(err.message);
    }
})

export const fetchPlaylistById = createAsyncThunk('playlists/fetchPlaylistById', async (id: number, thunkAPI) => {
    try {
        return await apifetchplaylistbyid(id);
    }
    catch (err: any) {
        return thunkAPI.rejectWithValue(err.message);
    }
})

export const fetchLoadedPlaylist = createAsyncThunk('playlists/fetchLoadedPlaylist', async (id: number, thunkAPI) => {
    try {
        return await apifetchplaylistbyid(id);
    }
    catch (err: any) {
        return thunkAPI.rejectWithValue(err.message);
    }
})

export const fetchPlaylistsCreatedByAccountId = createAsyncThunk('playlists/fetchPlaylistsCreatedByAccountId',
    async (accountid: number, thunkAPI) => {
        try {
            return await apifetchplaylistscreatedbyaccount(accountid);
        }
        catch (err: any) {
            return thunkAPI.rejectWithValue(err.message);
        }
    })

export const fetchPlaylistsSavedByAccountId = createAsyncThunk('playlists/fetchPlaylistsSavedByAccountId',
    async (accountid: number, thunkAPI) => {
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