import { createSlice, type PayloadAction } from "@reduxjs/toolkit";
import type { RootState } from "../store";
import type { PlaylistInvitation } from "../../models/playlistinvitation";

/**
 * Local store for the playlist-invitation inbox/outbox. Mirrors
 * friendshipslice in shape so the navbar popper and Socials → Playlists
 * tab can be built with the same patterns as their friendship
 * counterparts.
 *
 * No "accepted" or "blocked" state here — accepted invitations are
 * deleted from the table (acceptance is represented by membership in
 * the playlist's SavedByAccounts list, not by a state transition on
 * the invitation row). The whole slice is therefore about pending
 * inbox/outbox only.
 */
interface PlaylistInvitationState {
    incoming: PlaylistInvitation[];
    outgoing: PlaylistInvitation[];
    // Cached count for the navbar badge. Updated by the polling thunk
    // (cheap) and kept in sync by setIncoming/removeIncoming so the
    // badge stays consistent even when the user has the popper open.
    incomingCount: number;
    loadingIncoming: boolean;
    loadingOutgoing: boolean;
}

const initialState: PlaylistInvitationState = {
    incoming: [],
    outgoing: [],
    incomingCount: 0,
    loadingIncoming: false,
    loadingOutgoing: false,
};

const playlistInvitationSlice = createSlice({
    name: "playlistinvitation",
    initialState,
    reducers: {
        setIncoming: (state, action: PayloadAction<PlaylistInvitation[]>) => {
            state.incoming = action.payload;
            // Re-sync count whenever a full fetch lands. Cheap and
            // avoids drift when an out-of-order count poll resolves
            // after a fetch.
            state.incomingCount = action.payload.length;
            state.loadingIncoming = false;
        },
        setOutgoing: (state, action: PayloadAction<PlaylistInvitation[]>) => {
            state.outgoing = action.payload;
            state.loadingOutgoing = false;
        },
        setIncomingCount: (state, action: PayloadAction<number>) => {
            state.incomingCount = action.payload;
        },
        setLoadingIncoming: (state, action: PayloadAction<boolean>) => {
            state.loadingIncoming = action.payload;
        },
        setLoadingOutgoing: (state, action: PayloadAction<boolean>) => {
            state.loadingOutgoing = action.payload;
        },
        // Used after a successful Accept / Decline. The badge count
        // tracks state.incoming.length, so the optimistic update
        // keeps it consistent.
        removeIncoming: (state, action: PayloadAction<string>) => {
            state.incoming = state.incoming.filter((r) => r.id !== action.payload);
            state.incomingCount = state.incoming.length;
        },
        // Used after a successful Cancel.
        removeOutgoing: (state, action: PayloadAction<string>) => {
            state.outgoing = state.outgoing.filter((r) => r.id !== action.payload);
        },
        // Used after a successful Invite: if the outbox list is already
        // loaded, surface the new row immediately rather than waiting
        // for a refetch. Idempotent — drops duplicates by id.
        addOutgoing: (state, action: PayloadAction<PlaylistInvitation>) => {
            if (!state.outgoing.find((i) => i.id === action.payload.id)) {
                state.outgoing.unshift(action.payload);
            }
        },
    },
});

export const {
    setIncoming,
    setOutgoing,
    setIncomingCount,
    setLoadingIncoming,
    setLoadingOutgoing,
    removeIncoming,
    removeOutgoing,
    addOutgoing,
} = playlistInvitationSlice.actions;

export default playlistInvitationSlice.reducer;

// Selectors. Same naming convention as friendshipslice for muscle-memory
// portability across the two social features.
export const selectIncomingInvitations = (s: RootState) =>
    s.playlistinvitation.incoming;
export const selectOutgoingInvitations = (s: RootState) =>
    s.playlistinvitation.outgoing;
export const selectIncomingInvitationsCount = (s: RootState) =>
    s.playlistinvitation.incomingCount;
export const selectInvitationsLoading = (s: RootState) => ({
    incoming: s.playlistinvitation.loadingIncoming,
    outgoing: s.playlistinvitation.loadingOutgoing,
});