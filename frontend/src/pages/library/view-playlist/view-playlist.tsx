import { Box, Button, IconButton, Typography } from "@mui/material";
import SelectedSongsGrid from "../../../reusablecomponents/library/create-playlist-datagrids/selectedsongsdatagrid";
import type { Playlist } from "../../../models/playlist";
import { useAppDispatch } from "../../../hooks/hooks";
import { useEffect, useState } from "react";
import { deletePlaylist, fetchLoadedPlaylist, updatePlaylist } from "../../../stores/thunks/playlistthunks";
import PauseCircleFilled from "@mui/icons-material/PauseCircleFilled";
import PlayCircleFilled from "@mui/icons-material/PlayCircleFilled";
import { useSelector } from "react-redux";
import { Navigate } from "react-router";
import { selectLoadedPlaylist } from "../../../stores/slices/playlistdataslice";

export default function ViewPlaylist({ id, handleDeletePlaylist, handlePlay }:
    { id: string, handleDeletePlaylist: any, handlePlay:any }) {
    const dispatch = useAppDispatch();
    const [isEditing, setIsEditing] = useState<boolean>(false);
    // const [isDeleted, setIsDeleted] = useState<boolean>(false);
    const playlist = useSelector(selectLoadedPlaylist);
    const numid = (id as unknown) as number;

    // useEffect(() => {
    //     dispatch(fetchLoadedPlaylist(numid));
    // }, [])
    // useEffect(() => {
    //     if (isDeleted){
    //         handleMainPage();
    //     }
    // }, [isDeleted])

    // function handlePlay() {
    //     //should navigate to a separate page with a vertical carousel containing the songs in the playlist, autoplayed
    //     return "NOT IMPLEMENTED";
    // }

    async function handleRowSelect() {
        //should open a side component with details about the song/kind of like a small song profile
        return "NOT IMPLEMENTED";
    }

    async function handleDeleteRow(songid: any) {
        if (playlist.songs.find(s => s.id === songid) != undefined) {
            const filteredsongs = playlist.songs.filter(s => s.id !== songid);
            const songIds = filteredsongs.map(s => s.id);
            const numplaylistid = (id as unknown) as number;

            const response = await dispatch(updatePlaylist({
                id: numplaylistid,
                name: playlist.name,
                songIds: songIds
            }))
            if (response.meta.requestStatus === 'fulfilled') {
                // console.log("Update Successful");
                dispatch(fetchLoadedPlaylist(numid));
            }
        }
    }

    // async function handleDeletePlaylist() {
    //     const response = await dispatch(deletePlaylist({ id: numid }))
    //     if (response) {
    //         // setIsDeleted(true);
    //     }
    // }
    // async function handleDeletePlaylist(id: number) {
    //     const response = await dispatch(deletePlaylist({ id: id }))
    //     if (response.meta.requestStatus === 'fulfilled') {
    //         setIsDeleted(true);
    //         // dispatch(fetchPlaylistsSavedByAccountId(user.id));
    //     }
    // }
    // async function handleDeleteClick(id: number) {
    //     handleDeletePlaylist(id);
    // }

    function handleFlipEditMode() {
        setIsEditing(!isEditing);
    }

    return (
        <Box>
            <Typography>{playlist.name}</Typography>
            <Typography>Created At: {playlist.createdAt}</Typography>
            {!isEditing && <IconButton color="primary" size="medium" onClick={handlePlay}>
                <PlayCircleFilled style={{ fontSize: "50px" }}></PlayCircleFilled>
            </IconButton>}
            {!isEditing && <Button onClick={handleFlipEditMode}>Edit</Button>}
            {isEditing && <Button onClick={handleFlipEditMode}>Stop Editing</Button>}
            <SelectedSongsGrid
                handleRowSelect={handleRowSelect}
                handleDeleteRow={handleDeleteRow}
                rows={playlist.songs}
                isEditing={isEditing}
            ></SelectedSongsGrid>
            {isEditing && <Button onClick={() => { handleDeletePlaylist(numid) }}>Delete Playlist</Button>}
        </Box>
    );
}