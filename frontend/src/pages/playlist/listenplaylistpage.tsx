import { ThemeProvider } from "@emotion/react";
import { Backdrop, Box, CircularProgress, IconButton, Tooltip } from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router";

import { darkTheme } from "../../themes/themes";
import Navbar from "../../reusablecomponents/navbar";
import ListenPlaylist from "../library/listen-playlist/listen-playlist";
import { useAppDispatch } from "../../hooks/hooks";
import { fetchLoadedPlaylist } from "../../stores/thunks/playlistthunks";

export default function ListenPlaylistPage() {
    const params = useParams();
    const navigate = useNavigate();
    const dispatch = useAppDispatch();
    const playlistId = params.id ?? "";
    const [status, setStatus] = useState<"loading" | "ready" | "missing">("loading");

    useEffect(() => {
        if (!playlistId) return;
        let cancelled = false;
        (async () => {
            setStatus("loading");
            const action = await dispatch(fetchLoadedPlaylist(playlistId));
            if (cancelled) return;
            setStatus(
                action.meta.requestStatus === "fulfilled" ? "ready" : "missing",
            );
        })();
        return () => { cancelled = true; };
    }, [playlistId, dispatch]);

    return (
        <ThemeProvider theme={darkTheme}>
            <Box sx={{ minHeight: "100vh", backgroundColor: "#121212" }}>
                <Navbar />

                {/*
                  Back-to-view affordance. Floats top-left under the
                  navbar so the carousel layout below stays untouched.
                  Uses navigate rather than href so the loaded-playlist
                  slice stays warm — bouncing between view and listen
                  doesn't trigger a full page reload.
                */}
                {status === "ready" && playlistId && (
                    <Box sx={{ px: 2, pt: 1 }}>
                        <Tooltip title="Back to playlist details" arrow>
                            <IconButton
                                onClick={() => navigate(`/playlist/${playlistId}`)}
                                color="white"
                            >
                                <ArrowBackIcon className="back-arrow"/>
                            </IconButton>
                        </Tooltip>
                    </Box>
                )}

                {status === "missing" && (
                    <Box sx={{ textAlign: "center", py: 4, opacity: 0.7 }}>
                        This playlist doesn't exist or isn't available to you.
                    </Box>
                )}
                {status === "ready" && playlistId && (
                    <ListenPlaylist id={playlistId} />
                )}
                <Backdrop
                    sx={(theme) => ({ color: "#fff", zIndex: theme.zIndex.drawer + 1 })}
                    open={status === "loading"}
                >
                    <CircularProgress color="inherit" />
                </Backdrop>
            </Box>
        </ThemeProvider>
    );
}