import { createAsyncThunk } from "@reduxjs/toolkit";
import {
    apigetincominginvitations,
    apigetoutgoinginvitations,
    apigetincominginvitationscount,
} from "../api/playlistinvitationapi";
import {
    setIncoming,
    setOutgoing,
    setIncomingCount,
    setLoadingIncoming,
    setLoadingOutgoing,
} from "../slices/playlistinvitationslice";


export const fetchIncomingInvitations = createAsyncThunk(
    "playlistinvitation/fetchIncoming",
    async (_: void, thunkAPI) => {
        thunkAPI.dispatch(setLoadingIncoming(true));
        try {
            const data = await apigetincominginvitations();
            thunkAPI.dispatch(setIncoming(data));
        } catch (e) {
            // Clear the loading flag on failure so the UI doesn't get
            // stuck on a spinner. Re-throw so callers can react.
            thunkAPI.dispatch(setLoadingIncoming(false));
            throw e;
        }
    },
);

export const fetchOutgoingInvitations = createAsyncThunk(
    "playlistinvitation/fetchOutgoing",
    async (_: void, thunkAPI) => {
        thunkAPI.dispatch(setLoadingOutgoing(true));
        try {
            const data = await apigetoutgoinginvitations();
            thunkAPI.dispatch(setOutgoing(data));
        } catch (e) {
            thunkAPI.dispatch(setLoadingOutgoing(false));
            throw e;
        }
    },
);


export const fetchIncomingInvitationsCount = createAsyncThunk(
    "playlistinvitation/fetchIncomingCount",
    async (_: void, thunkAPI) => {
        try {
            const count = await apigetincominginvitationscount();
            thunkAPI.dispatch(setIncomingCount(count));
        } catch {
            /* silent: best-effort poll */
        }
    },
);