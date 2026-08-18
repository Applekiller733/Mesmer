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
           
            if (action.meta.requestStatus === "fulfilled") {
                setStatus("ready");
            } else {
                
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
            
            if (currentUser.id) {
                dispatch(fetchPlaylistsSavedByAccountId(currentUser.id));
            }
            
            navigate("/library");
        }
    }

    function handlePlay() {
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