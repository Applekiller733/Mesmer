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
import { deletePlaylist, fetchLoadedPlaylist, fetchPlaylistById, fetchPlaylistsSavedByAccountId } from "../../stores/thunks/playlistthunks";
import type { Playlist } from "../../models/playlist";
import { useSelector } from "react-redux";
import { selectCurrentUser } from "../../stores/slices/userdataslice";
import { selectLoadedPlaylist, selectSavedPlaylists } from "../../stores/slices/playlistdataslice";
import ListenPlaylist from "./listen-playlist/listen-playlist";

export default function Library() {
    const user = useSelector(selectCurrentUser);
    const dispatch = useAppDispatch();
    const playlists = useSelector(selectSavedPlaylists);
    const loadedPlaylist = useSelector(selectLoadedPlaylist);
    const [loadedPage, setLoadedPage] = useState(LibraryPages.main);
    const [status, setStatus] = useState('init');

    //todo might need to be changed
    useEffect(() => {
        if (user.id)
            dispatch(fetchPlaylistsSavedByAccountId(user.id));
    }, [user])

    function handleMainPage() {
        setLoadedPage(LibraryPages.main);
    }

    function handleCreatePlaylist() {
        setLoadedPage(LibraryPages.createplaylist);
    }

    async function loadPlaylist(id: string) {
        // const response = await dispatch(fetchPlaylistById(id));
        // if (response) {
        //     setLoadedPlaylist(response.payload);
        // }
        const numid = (id as unknown) as number;
        const response = await dispatch(fetchLoadedPlaylist(numid));
    }

    async function handlePlaylistClick(event: React.MouseEvent, id: string) {
        setStatus('loading');
        setLoadedPage(LibraryPages.viewplaylist);
        loadPlaylist(id);
        // let numid = (id as unknown) as number;`
        // await loadPlaylist(numid);`
        setStatus('finished');
    }

    async function handleDeletePlaylist(id: number) {
        setStatus('loading');
        const response = await dispatch(deletePlaylist({ id: id }))
        if (response.meta.requestStatus === 'fulfilled' && user.id) {
            dispatch(fetchPlaylistsSavedByAccountId(user.id));
            setLoadedPage(LibraryPages.main);
        }
        setStatus('finished');
    }

    async function handlePlay() {
        setLoadedPage(LibraryPages.listenplaylist);
    }

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="library">
                <Navbar ></Navbar>
                <Paper className="library-paper">
                    <div className="sidelist">
                        <SideList
                            handleCreatePlaylist={handleCreatePlaylist}
                            handlePlaylistClick={handlePlaylistClick}
                            playlists={playlists}
                        />
                    </div>
                    <div className="page">
                        {loadedPage === LibraryPages.main && <LibraryMainPage></LibraryMainPage>}
                        {loadedPage === LibraryPages.createplaylist
                            && <CreatePlaylist
                                handleMainPage={handleMainPage}
                            ></CreatePlaylist>}
                        {loadedPage === LibraryPages.viewplaylist
                            && <ViewPlaylist
                                handlePlay={handlePlay}
                                handleDeletePlaylist={() => { handleDeletePlaylist(loadedPlaylist.id as unknown as number) }}
                                id={loadedPlaylist.id}
                            ></ViewPlaylist>}
                        {loadedPage === LibraryPages.listenplaylist
                            && <ListenPlaylist 
                            id={loadedPlaylist.id}  
                                                      
                            >

                            </ListenPlaylist>
                        }
                    </div>
                    <Backdrop
                        sx={(theme) => ({ color: '#fff', zIndex: theme.zIndex.drawer + 1 })}
                        open={status === 'loading'}
                    >
                        <CircularProgress color="inherit" />
                    </Backdrop>
                </Paper>
            </Box>
        </ThemeProvider>
    );
}