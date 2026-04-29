import { createAsyncThunk } from "@reduxjs/toolkit";
import { apifetchrecommendationsforplaylist } from "../api/recommendationapi";

export const setModePlaylist = createAsyncThunk(
    "recommendationmode/setModePlaylist",
    async (playlistId: string, thunkAPI) => {
        try {
            const ids = await apifetchrecommendationsforplaylist(playlistId, 50);
            return { playlistId, ids };
        } catch (err: any) {
            return thunkAPI.rejectWithValue(err.message);
        }
    }
);