import { createSlice, type PayloadAction } from "@reduxjs/toolkit";
import type { RootState } from "../store";
import type { Friendship } from "../api/friendshipapi";

interface FriendshipState {
    friends: Friendship[];
    incoming: Friendship[];
    outgoing: Friendship[];
    blocked: Friendship[];
    incomingCount: number;
    loadingFriends: boolean;
    loadingIncoming: boolean;
    loadingOutgoing: boolean;
    loadingBlocked: boolean;
}

const initialState: FriendshipState = {
    friends: [],
    incoming: [],
    outgoing: [],
    blocked: [],
    incomingCount: 0,
    loadingFriends: false,
    loadingIncoming: false,
    loadingOutgoing: false,
    loadingBlocked: false,
};

const friendshipSlice = createSlice({
    name: "friendship",
    initialState,
    reducers: {
        setFriends: (state, action: PayloadAction<Friendship[]>) => {
            state.friends = action.payload;
            state.loadingFriends = false;
        },
        setIncoming: (state, action: PayloadAction<Friendship[]>) => {
            state.incoming = action.payload;
            state.incomingCount = action.payload.length;
            state.loadingIncoming = false;
        },
        setOutgoing: (state, action: PayloadAction<Friendship[]>) => {
            state.outgoing = action.payload;
            state.loadingOutgoing = false;
        },
        setBlocked: (state, action: PayloadAction<Friendship[]>) => {
            state.blocked = action.payload;
            state.loadingBlocked = false;
        },
        setIncomingCount: (state, action: PayloadAction<number>) => {
            state.incomingCount = action.payload;
        },
        setLoadingFriends: (state, action: PayloadAction<boolean>) => {
            state.loadingFriends = action.payload;
        },
        setLoadingIncoming: (state, action: PayloadAction<boolean>) => {
            state.loadingIncoming = action.payload;
        },
        setLoadingOutgoing: (state, action: PayloadAction<boolean>) => {
            state.loadingOutgoing = action.payload;
        },
        setLoadingBlocked: (state, action: PayloadAction<boolean>) => {
            state.loadingBlocked = action.payload;
        },
        removeIncoming: (state, action: PayloadAction<string>) => {
            state.incoming = state.incoming.filter((r) => r.id !== action.payload);
            state.incomingCount = state.incoming.length;
        },
        removeOutgoing: (state, action: PayloadAction<string>) => {
            state.outgoing = state.outgoing.filter((r) => r.id !== action.payload);
        },
        addFriend: (state, action: PayloadAction<Friendship>) => {
            if (!state.friends.find((f) => f.id === action.payload.id)) {
                state.friends.unshift(action.payload);
            }
        },
        removeFriend: (state, action: PayloadAction<string>) => {
            state.friends = state.friends.filter(
                (f) =>
                    f.senderId !== action.payload &&
                    f.receiverId !== action.payload
            );
        },
        removeBlocked: (state, action: PayloadAction<string>) => {
            state.blocked = state.blocked.filter(
                (b) => b.receiverId !== action.payload
            );
        },
    },
});

export const {
    setFriends,
    setIncoming,
    setOutgoing,
    setBlocked,
    setIncomingCount,
    setLoadingFriends,
    setLoadingIncoming,
    setLoadingOutgoing,
    setLoadingBlocked,
    removeIncoming,
    removeOutgoing,
    addFriend,
    removeFriend,
    removeBlocked,
} = friendshipSlice.actions;

export default friendshipSlice.reducer;


export const selectFriends = (s: RootState) => s.friendship.friends;
export const selectIncoming = (s: RootState) => s.friendship.incoming;
export const selectOutgoing = (s: RootState) => s.friendship.outgoing;
export const selectBlocked = (s: RootState) => s.friendship.blocked;
export const selectIncomingCount = (s: RootState) => s.friendship.incomingCount;
export const selectFriendshipLoading = (s: RootState) => ({
    friends: s.friendship.loadingFriends,
    incoming: s.friendship.loadingIncoming,
    outgoing: s.friendship.loadingOutgoing,
    blocked: s.friendship.loadingBlocked,
});