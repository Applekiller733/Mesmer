import { createSlice } from "@reduxjs/toolkit";
import type { RootState } from "../store";

export type RecommendationMode =
    | { kind: "all" }
    | { kind: "playlist"; playlistId: string };

interface RecommendationModeState {
    mode: RecommendationMode;
    recommendedIds: string[];
    status: "idle" | "loading" | "ready" | "error";
    error: string | null;
}

const initialState: RecommendationModeState = {
    mode: { kind: "all" },
    recommendedIds: [],
    status: "idle",
    error: null,
};

const PENDING = "recommendationmode/setModePlaylist/pending";
const FULFILLED = "recommendationmode/setModePlaylist/fulfilled";
const REJECTED = "recommendationmode/setModePlaylist/rejected";

const recommendationModeSlice = createSlice({
    name: "recommendationmode",
    initialState,
    reducers: {
        setModeAll: (state) => {
            state.mode = { kind: "all" };
            state.recommendedIds = [];
            state.status = "idle";
            state.error = null;
        },
    },
    extraReducers: (builder) => {
        builder
            .addMatcher(
                (a): a is { type: string; meta: { arg: string } } =>
                    a.type === "recommendationmode/setModePlaylist/pending",
                (state, action) => {
                    state.mode = { kind: "playlist", playlistId: action.meta.arg };
                    state.status = "loading";
                    state.error = null;
                }
            )
            .addMatcher(
                (a): a is { type: string; payload: { ids: string[] } } =>
                    a.type === "recommendationmode/setModePlaylist/fulfilled",
                (state, action) => {
                    state.recommendedIds = action.payload.ids;
                    state.status = "ready";
                }
            )
            .addMatcher(
                (a): a is { type: string; payload: string } =>
                    a.type === "recommendationmode/setModePlaylist/rejected",
                (state, action) => {
                    state.recommendedIds = [];
                    state.status = "error";
                    state.error = action.payload ?? "Recommendation fetch failed";
                }
            );
    },
});

export const { setModeAll } = recommendationModeSlice.actions;
export default recommendationModeSlice.reducer;


export const selectRecommendationMode = (s: RootState) =>
    s.recommendationmode.mode;
export const selectRecommendedIds = (s: RootState) =>
    s.recommendationmode.recommendedIds;
export const selectRecommendationStatus = (s: RootState) =>
    s.recommendationmode.status;
export const selectRecommendationError = (s: RootState) =>
    s.recommendationmode.error;