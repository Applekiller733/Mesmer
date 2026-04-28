import {
    Button,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    List,
    ListItem,
    Typography,
    Box,
} from "@mui/material";
import { useSelector } from "react-redux";
import { selectSavedPlaylists } from "../../stores/slices/playlistdataslice";
import { useAppDispatch } from "../../hooks/hooks";
import { useEffect } from "react";
import {
    fetchPlaylistsSavedByAccountId,
    updatePlaylist,
} from "../../stores/thunks/playlistthunks";
import { selectCurrentUser } from "../../stores/slices/userdataslice";
import type { Song } from "../../models/song";
import CheckIcon from "@mui/icons-material/Check";
import LibraryAddOutlined from "@mui/icons-material/LibraryAddOutlined";
import { useNavigate } from "react-router";

export default function AddToPlaylistDialog({
    open,
    song,
    handleDialogClose,
}: {
    open: boolean;
    song: Song;
    handleDialogClose: () => void;
}) {
    const user = useSelector(selectCurrentUser);
    const playlists = useSelector(selectSavedPlaylists);
    const dispatch = useAppDispatch();
    const navigate = useNavigate();

    // Fetch on open, not on mount. Mounting once at the parent (where the
    // dialog lives inside SongComponent) and never refetching means stale
    // data after the user creates a playlist elsewhere. Re-fetching when
    // the dialog opens keeps the list current and respects user.id timing.
    useEffect(() => {
        if (open && user.id) {
            dispatch(fetchPlaylistsSavedByAccountId(user.id));
        }
    }, [open, user.id, dispatch]);

    async function handleAddToPlaylist(playlistid: string) {
        const playlist = playlists.find((p) => p.id === playlistid);
        if (playlist === undefined) return;

        const updatedSongs = [...playlist.songs, song];
        const songIds = updatedSongs.map((p) => p.id);

        const response = await dispatch(
            updatePlaylist({
                id: playlist.id,
                name: playlist.name,
                songIds: songIds,
            })
        );

        if (response.meta.requestStatus === "fulfilled" && user.id) {
            await dispatch(fetchPlaylistsSavedByAccountId(user.id));
        }
    }

    function handleCreatePlaylist() {
        // Close the dialog first so it doesn't ride along into the navigation
        // and end up reopening on the new page.
        handleDialogClose();
        // The library page reads ?action=create on mount and lands the user
        // directly on the create-playlist subpage.
        navigate("/library?action=create");
    }

    const hasNoPlaylists = playlists.length === 0;

    return (
        <Dialog open={open} onClose={handleDialogClose} fullWidth maxWidth="xs">
            <DialogTitle>
                {hasNoPlaylists ? "No playlists yet" : "Save to playlist"}
            </DialogTitle>

            <DialogContent>
                {hasNoPlaylists ? (
                    // Empty-state: icon + explanation + primary action.
                    // Sized so the dialog has visible content even with zero
                    // playlists — fixes the "invisible dialog with only a
                    // backdrop" bug.
                    <Box
                        sx={{
                            display: "flex",
                            flexDirection: "column",
                            alignItems: "center",
                            gap: 2,
                            py: 3,
                        }}
                    >
                        <LibraryAddOutlined sx={{ fontSize: 64, opacity: 0.6 }} />
                        <Typography variant="body1" align="center">
                            You need to create a playlist before saving songs.
                        </Typography>
                        <Typography variant="body2" align="center" color="text.secondary">
                            Head over to your library to make one — it only takes a moment.
                        </Typography>
                    </Box>
                ) : (
                    <List>
                        {playlists.map((p) => {
                            const isInPlaylist =
                                p.songs.find((s) => s.id === song.id) !== undefined;

                            return (
                                <ListItem
                                    key={p.id}
                                    secondaryAction={
                                        isInPlaylist ? (
                                            <CheckIcon color="success" />
                                        ) : (
                                            <Button
                                                color="success"
                                                onClick={() => handleAddToPlaylist(p.id)}
                                            >
                                                Add
                                            </Button>
                                        )
                                    }
                                >
                                    <Typography>{p.name}</Typography>
                                </ListItem>
                            );
                        })}
                    </List>
                )}
            </DialogContent>

            <DialogActions>
                {hasNoPlaylists ? (
                    <>
                        <Button onClick={handleDialogClose}>Not now</Button>
                        <Button
                            variant="contained"
                            color="success"
                            onClick={handleCreatePlaylist}
                        >
                            Create playlist
                        </Button>
                    </>
                ) : (
                    <Button onClick={handleDialogClose}>Close</Button>
                )}
            </DialogActions>
        </Dialog>
    );
}