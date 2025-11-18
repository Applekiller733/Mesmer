import { Button, Dialog, List, ListItem, Typography } from "@mui/material";
import { useSelector } from "react-redux";
import { selectSavedPlaylists } from "../../stores/slices/playlistdataslice";
import { useAppDispatch } from "../../hooks/hooks";
import { useEffect, useState } from "react";
import { fetchPlaylistsSavedByAccountId, updatePlaylist } from "../../stores/thunks/playlistthunks";
import { selectCurrentUser } from "../../stores/slices/userdataslice";
import type { Playlist } from "../../models/playlist";
import type { Song } from "../../models/song";
import CheckIcon from '@mui/icons-material/Check';

export default function AddToPlaylistDialog({ open, song, handleDialogClose }
    : { open: boolean, song: Song, handleDialogClose: any }) {
    const user = useSelector(selectCurrentUser);
    const playlists = useSelector(selectSavedPlaylists);
    const dispatch = useAppDispatch();

    useEffect(() => {
        if (user.id)
            dispatch(fetchPlaylistsSavedByAccountId(user.id));
    }, [])

    async function handleAddToPlaylist(playlistid: string) {
        const playlist = playlists.find(p => p.id === playlistid);
        if (playlist === undefined)
            return "Playlist could not be found";
        const updatedPlaylist = {
            ...playlist,
            songs: [...playlist.songs, song],
          };
        const songIds = updatedPlaylist.songs.map(p => p.id);
        const playlistnumid = playlist.id as unknown as number;
        const response = await dispatch(updatePlaylist({
            id: playlistnumid,
            name: playlist.name,
            songIds: songIds,
        }));
        if (response.meta.requestStatus === 'fulfilled' && user.id) {
            await dispatch(fetchPlaylistsSavedByAccountId(user.id));
        }
        else {
            console.log(response.payload);
        }
    }

    return (
        <Dialog open={open} onClose={handleDialogClose}>
            <List>
                {

                    playlists.map(p => {
                        const isInPlaylist: boolean = (p.songs.find(s => s.id == song.id) !== undefined);

                        return (
                            <ListItem key={p.id}>
                                <Typography>{p.name}</Typography>
                                {!isInPlaylist && <Button color="success" onClick={() => { handleAddToPlaylist(p.id) }}>Add</Button>}
                                {isInPlaylist && <CheckIcon />}
                            </ListItem>
                        )
                    })
                }
            </List>
        </Dialog>
    );
}