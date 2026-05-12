import { createAsyncThunk } from "@reduxjs/toolkit";
import {
    apigetfriends,
    apigetincoming,
    apigetoutgoing,
    apigetblocked,
    apigetincomingcount,
} from "../api/friendshipapi";
import {
    setFriends,
    setIncoming,
    setOutgoing,
    setBlocked,
    setIncomingCount,
    setLoadingFriends,
    setLoadingIncoming,
    setLoadingOutgoing,
    setLoadingBlocked,
} from "../slices/friendshipslice";

export const fetchFriends = createAsyncThunk(
    "friendship/fetchFriends",
    async (_: void, thunkAPI) => {
        thunkAPI.dispatch(setLoadingFriends(true));
        try {
            const data = await apigetfriends();
            thunkAPI.dispatch(setFriends(data));
        } catch (e) {
            thunkAPI.dispatch(setLoadingFriends(false));
            throw e;
        }
    }
);

export const fetchIncoming = createAsyncThunk(
    "friendship/fetchIncoming",
    async (_: void, thunkAPI) => {
        thunkAPI.dispatch(setLoadingIncoming(true));
        try {
            const data = await apigetincoming();
            thunkAPI.dispatch(setIncoming(data));
        } catch (e) {
            thunkAPI.dispatch(setLoadingIncoming(false));
            throw e;
        }
    }
);

export const fetchOutgoing = createAsyncThunk(
    "friendship/fetchOutgoing",
    async (_: void, thunkAPI) => {
        thunkAPI.dispatch(setLoadingOutgoing(true));
        try {
            const data = await apigetoutgoing();
            thunkAPI.dispatch(setOutgoing(data));
        } catch (e) {
            thunkAPI.dispatch(setLoadingOutgoing(false));
            throw e;
        }
    }
);

export const fetchBlocked = createAsyncThunk(
    "friendship/fetchBlocked",
    async (_: void, thunkAPI) => {
        thunkAPI.dispatch(setLoadingBlocked(true));
        try {
            const data = await apigetblocked();
            thunkAPI.dispatch(setBlocked(data));
        } catch (e) {
            thunkAPI.dispatch(setLoadingBlocked(false));
            throw e;
        }
    }
);

export const fetchIncomingCount = createAsyncThunk(
    "friendship/fetchIncomingCount",
    async (_: void, thunkAPI) => {
        try {
            const count = await apigetincomingcount();
            thunkAPI.dispatch(setIncomingCount(count));
        } catch {
        }
    }
);