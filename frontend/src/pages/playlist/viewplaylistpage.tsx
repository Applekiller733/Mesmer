import { ThemeProvider } from "@emotion/react";
import { Backdrop, Box, CircularProgress, Paper } from "@mui/material";
import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router";

import { darkTheme } from "../../themes/themes";
import Navbar from "../../reusablecomponents/navbar";
import ViewPlaylist from "../library/view-playlist/view-playlist";
import { useAppDispatch } from "../../hooks/hooks";
import {
    deletePlaylist,
    fetchLoadedPlaylist,
    fetchPlaylistsSavedByAccountId,
} from "../../stores/thunks/playlistthunks";
import { useSelector } from "react-redux";
import { selectCurrentUser } from "../../stores/slices/userdataslice";

/**
 * Standalone /playlist/:id page. Wraps the existing ViewPlaylist
 * component (which previously only ran inside the library's three-pane
 * layout) so it can be navigated to directly from anywhere:
 *
 *   - Profile page playlist cards (Step 8)
 *   - Notification popper invitation rows (Step 7 — was navigating to
 *     the sender's profile as a fallback; now lands on the real
 *     playlist view)
 *   - Future: shareable external links, search results, etc.
 *
 * Responsibilities the library page used to own that this page now
 * does itself: fetching the loaded playlist on mount, providing a
 * delete handler that navigates back when the playlist is removed,
 * providing the "play" handler. Because ViewPlaylist also serves as
 * the in-library editor, those handlers need to behave reasonably
 * here without a library context to return to.
 */
export default function ViewPlaylistPage() {
    const params = useParams();
    const navigate = useNavigate();
    const dispatch = useAppDispatch();
    const currentUser = useSelector(selectCurrentUser);

    const playlistId = params.id ?? "";
    const [status, setStatus] = useState<"loading" | "ready" | "missing">("loading");

    useEffect(() => {
        if (!playlistId) return;

        let cancelled = false;
        (async () => {
            setStatus("loading");
            const action = await dispatch(fetchLoadedPlaylist(playlistId));
            if (cancelled) return;
            // fetchLoadedPlaylist's thunk uses rejectWithValue on error,
            // so we check requestStatus rather than the payload shape.
            if (action.meta.requestStatus === "fulfilled") {
                setStatus("ready");
            } else {
                // 404 from the backend means either the playlist doesn't
                // exist, OR the visibility rules don't let the current
                // user see it. Same UI: "missing". Don't disambiguate —
                // matches the backend's deliberate non-leaking behaviour.
                setStatus("missing");
            }
        })();

        return () => {
            cancelled = true;
        };
    }, [playlistId, dispatch]);

    async function handleDeletePlaylist(id: string) {
        const action = await dispatch(deletePlaylist({ id }));
        if (action.meta.requestStatus === "fulfilled") {
            // Refresh the user's saved-playlists slice (the deleted
            // playlist was almost certainly in it — every owner is
            // also in their playlist's SavedByAccounts) so any other
            // open view of the library reflects the deletion.
            if (currentUser.id) {
                dispatch(fetchPlaylistsSavedByAccountId(currentUser.id));
            }
            // No clean "go back" target — the user might have arrived
            // from anywhere. Library is the safest landing pad: it's
            // their own space and the deleted playlist won't be there.
            navigate("/library");
        }
    }

    function handlePlay() {
        // Standalone listen route mirrors this one — keeps the user
        // in the standalone playlist flow rather than punting back to
        // the library (which doesn't have the playlist if they're a
        // non-saver previewing it).
        navigate(`/playlist/${playlistId}/listen`);
    }

    return (
        <ThemeProvider theme={darkTheme}>
            <Box sx={{ minHeight: "100vh", backgroundColor: "#121212" }}>
                <Navbar />
                <Paper
                    sx={{
                        margin: "2rem auto",
                        padding: "2rem",
                        maxWidth: 960,
                        width: "80%",
                    }}
                >
                    {status === "missing" && (
                        <Box sx={{ textAlign: "center", py: 4, opacity: 0.7 }}>
                            This playlist doesn't exist or isn't available to you.
                        </Box>
                    )}

                    {status === "ready" && playlistId && (
                        <ViewPlaylist
                            id={playlistId}
                            handleDeletePlaylist={handleDeletePlaylist}
                            handlePlay={handlePlay}
                        />
                    )}
                </Paper>

                {/*
                  Loading overlay rather than inline spinner so a quick
                  fetch doesn't flash the empty paper between the route
                  mount and the response arriving.
                */}
                <Backdrop
                    sx={(theme) => ({
                        color: "#fff",
                        zIndex: theme.zIndex.drawer + 1,
                    })}
                    open={status === "loading"}
                >
                    <CircularProgress color="inherit" />
                </Backdrop>
            </Box>
        </ThemeProvider>
    );
}