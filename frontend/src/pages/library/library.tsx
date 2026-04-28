import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../themes/themes";
import { Backdrop, Box, CircularProgress, Paper } from "@mui/material";
import SideList from "../../reusablecomponents/library/sidelist/sidelist";
import { useAppDispatch } from "../../hooks/hooks";
import LibraryMainPage from "./main-page/main-page";
import React, { useEffect, useState } from "react";
import Navbar from "../../reusablecomponents/navbar";
import CreatePlaylist from "./create-playlist/create-playlist";
import "./library.css";
import ViewPlaylist from "./view-playlist/view-playlist";
import { LibraryPages } from "../../utils/enums";
import {
    deletePlaylist,
    fetchLoadedPlaylist,
    fetchPlaylistsSavedByAccountId,
} from "../../stores/thunks/playlistthunks";
import { useSelector } from "react-redux";
import { selectCurrentUser } from "../../stores/slices/userdataslice";
import {
    selectLoadedPlaylist,
    selectSavedPlaylists,
} from "../../stores/slices/playlistdataslice";
import ListenPlaylist from "./listen-playlist/listen-playlist";
import { useSearchParams } from "react-router";

export default function Library() {
    const user = useSelector(selectCurrentUser);
    const dispatch = useAppDispatch();
    const playlists = useSelector(selectSavedPlaylists);
    const loadedPlaylist = useSelector(selectLoadedPlaylist);

    // Read URL search params so external links can deep-link into a subpage.
    // Currently supported: ?action=create → opens create-playlist subpage.
    const [searchParams, setSearchParams] = useSearchParams();

    // Initial subpage chosen from the URL on first render. Subsequent
    // navigation between subpages is still controlled by local state, so
    // we don't keep rewriting the URL for every internal click.
    const initialPage =
        searchParams.get("action") === "create"
            ? LibraryPages.createplaylist
            : LibraryPages.main;

    const [loadedPage, setLoadedPage] = useState(initialPage);
    const [status, setStatus] = useState("init");

    useEffect(() => {
        if (user.id) dispatch(fetchPlaylistsSavedByAccountId(user.id));
    }, [user, dispatch]);

    // After consuming the ?action= param once, clear it from the URL so
    // refreshing the page or sharing the URL doesn't keep re-opening the
    // create page after the user has navigated elsewhere.
    useEffect(() => {
        if (searchParams.get("action")) {
            const next = new URLSearchParams(searchParams);
            next.delete("action");
            setSearchParams(next, { replace: true });
        }
        // Run once on mount; later setSearchParams calls won't retrigger.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    function handleMainPage() {
        setLoadedPage(LibraryPages.main);
    }

    function handleCreatePlaylist() {
        setLoadedPage(LibraryPages.createplaylist);
    }

    async function loadPlaylist(id: string) {
        await dispatch(fetchLoadedPlaylist(id));
    }

    async function handlePlaylistClick(_: React.MouseEvent, id: string) {
        setStatus("loading");
        setLoadedPage(LibraryPages.viewplaylist);
        await loadPlaylist(id);
        setStatus("finished");
    }

    async function handleDeletePlaylist(id: string) {
        setStatus("loading");
        const response = await dispatch(deletePlaylist({ id }));
        if (response.meta.requestStatus === "fulfilled" && user.id) {
            dispatch(fetchPlaylistsSavedByAccountId(user.id));
            setLoadedPage(LibraryPages.main);
        }
        setStatus("finished");
    }

    async function handlePlay() {
        setLoadedPage(LibraryPages.listenplaylist);
    }

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="library">
                <Navbar />
                <Paper className="library-paper">
                    <div className="sidelist">
                        <SideList
                            handleCreatePlaylist={handleCreatePlaylist}
                            handlePlaylistClick={handlePlaylistClick}
                            playlists={playlists}
                        />
                    </div>
                    <div className="page">
                        {loadedPage === LibraryPages.main && <LibraryMainPage />}
                        {loadedPage === LibraryPages.createplaylist && (
                            <CreatePlaylist handleMainPage={handleMainPage} />
                        )}
                        {loadedPage === LibraryPages.viewplaylist && (
                            <ViewPlaylist
                                handlePlay={handlePlay}
                                handleDeletePlaylist={() =>
                                    handleDeletePlaylist(loadedPlaylist.id)
                                }
                                id={loadedPlaylist.id}
                            />
                        )}
                        {loadedPage === LibraryPages.listenplaylist && (
                            <ListenPlaylist id={loadedPlaylist.id} />
                        )}
                    </div>
                    <Backdrop
                        sx={(theme) => ({
                            color: "#fff",
                            zIndex: theme.zIndex.drawer + 1,
                        })}
                        open={status === "loading"}
                    >
                        <CircularProgress color="inherit" />
                    </Backdrop>
                </Paper>
            </Box>
        </ThemeProvider>
    );
}