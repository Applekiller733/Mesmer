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

/**
 * Thin wrappers that fetch and dispatch into the slice. Mirror the
 * friendshipthunk pattern: the thunk's job is the network call plus
 * loading-flag bookkeeping; mutations are pushed through the slice's
 * setX/removeX/addX reducers from the call site (so optimistic updates
 * stay flexible without re-fetching).
 *
 * State changes (Accept / Decline / Cancel / Invite) are NOT thunks —
 * they're called directly from the components, which then dispatch
 * the appropriate slice action to update local state. Same convention
 * the friend-system uses (see friendrequestsbadge.tsx for the model).
 */

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

/**
 * Polled by the navbar badge. Swallows errors silently — a transient
 * network failure shouldn't fire a toast at the user; the next poll
 * will refresh. Same swallow pattern friendrequestsbadge uses.
 */
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