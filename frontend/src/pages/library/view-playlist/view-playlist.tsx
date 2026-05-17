import { Box, Button, IconButton, Stack, Typography } from "@mui/material";
import ShareIcon from "@mui/icons-material/Share";
import SelectedSongsGrid from "../../../reusablecomponents/library/create-playlist-datagrids/selectedsongsdatagrid";
import { useAppDispatch } from "../../../hooks/hooks";
import { useState } from "react";
import {
    fetchLoadedPlaylist,
    updatePlaylist,
    updatePlaylistVisibility,
} from "../../../stores/thunks/playlistthunks";
import PlayCircleFilled from "@mui/icons-material/PlayCircleFilled";
import { useSelector } from "react-redux";
import { selectLoadedPlaylist } from "../../../stores/slices/playlistdataslice";
import { selectCurrentUser } from "../../../stores/slices/userdataslice";
import { PlaylistVisibility } from "../../../models/playlist";
import VisibilityBadge from "../../../reusablecomponents/library/visibility/visibilitybadge";
import VisibilitySelector from "../../../reusablecomponents/library/visibility/visibilityselector";
import ShareDialog from "../../../reusablecomponents/library/sharedialog/sharedialog";

export default function ViewPlaylist({ id, handleDeletePlaylist, handlePlay }:
    { id: string, handleDeletePlaylist: any, handlePlay: any }) {
    const dispatch = useAppDispatch();
    const [isEditing, setIsEditing] = useState<boolean>(false);
    const [visibilityWorking, setVisibilityWorking] = useState<boolean>(false);
    const [shareOpen, setShareOpen] = useState<boolean>(false);
    const playlist = useSelector(selectLoadedPlaylist);
    const user = useSelector(selectCurrentUser);

    const isOwner =
        !!playlist.createdBy?.id &&
        !!user.id &&
        playlist.createdBy.id === user.id;

    // Share button visibility. Mirrors the backend authority rules
    // (PlaylistInvitationService.EnsureCanInvite):
    //   - Private: nobody can share — button hidden.
    //   - Unlisted: owner only — non-owners viewing an unlisted
    //     playlist they were invited to should NOT be able to fan
    //     it out further; that's the whole point of Unlisted.
    //   - Public: anyone can share — button visible to everyone.
    //
    // Computed at render time rather than memoised because the
    // expression is cheap and the playlist object is stable across
    // re-renders (it comes from a selector).
    const canShare =
        playlist.visibility === PlaylistVisibility.Public ||
        (playlist.visibility === PlaylistVisibility.Unlisted && isOwner);

    async function handleRowSelect() {
        return "NOT IMPLEMENTED";
    }

    async function handleDeleteRow(songid: any) {
        if (playlist.songs.find(s => s.id === songid) != undefined) {
            const filteredsongs = playlist.songs.filter(s => s.id !== songid);
            const songIds = filteredsongs.map(s => s.id);

            const response = await dispatch(updatePlaylist({
                id: id,
                name: playlist.name,
                songIds: songIds
            }))
            if (response.meta.requestStatus === 'fulfilled') {
                dispatch(fetchLoadedPlaylist(id));
            }
        }
    }

    function handleFlipEditMode() {
        setIsEditing(!isEditing);
    }

    async function handleVisibilityChange(next: PlaylistVisibility) {
        if (next === playlist.visibility) return;

        setVisibilityWorking(true);
        try {
            const response = await dispatch(updatePlaylistVisibility({
                playlistId: id,
                visibility: next,
            }));
            if (response.meta.requestStatus === "fulfilled") {
                dispatch(fetchLoadedPlaylist(id));
            }
        } finally {
            setVisibilityWorking(false);
        }
    }

    return (
        <Box>
            <Stack direction="row" spacing={1.5} alignItems="center" sx={{ mb: 0.5 }}>
                <Typography variant="h5">{playlist.name}</Typography>
                <VisibilityBadge visibility={playlist.visibility} />
            </Stack>

            <Typography>Created At: {playlist.createdAt}</Typography>

            {!isEditing && (
                <IconButton color="primary" size="medium" onClick={handlePlay}>
                    <PlayCircleFilled style={{ fontSize: "50px" }} />
                </IconButton>
            )}

            {/*
              Share button. Lives next to Edit/Play because it's the
              same band of header actions on the playlist. Disabled
              while editing — the user is in a different mental mode
              and shouldn't be opening dialogs over their edits.
            */}
            {!isEditing && canShare && (
                <Button
                    startIcon={<ShareIcon />}
                    onClick={() => setShareOpen(true)}
                >
                    Share
                </Button>
            )}

            {!isEditing && isOwner && (
                <Button onClick={handleFlipEditMode}>Edit</Button>
            )}
            {isEditing && (
                <Button onClick={handleFlipEditMode}>Stop Editing</Button>
            )}

            {isEditing && isOwner && (
                <Box sx={{ my: 2 }}>
                    <Typography variant="body2" sx={{ mb: 0.5 }}>
                        Visibility
                    </Typography>
                    <VisibilitySelector
                        value={playlist.visibility}
                        onChange={handleVisibilityChange}
                        disabled={visibilityWorking}
                    />
                </Box>
            )}

            <SelectedSongsGrid
                handleRowSelect={handleRowSelect}
                handleDeleteRow={handleDeleteRow}
                rows={playlist.songs}
                isEditing={isEditing && isOwner}
            />

            {isEditing && isOwner && (
                <Button onClick={() => { handleDeletePlaylist(id) }}>
                    Delete Playlist
                </Button>
            )}

            {/*
              Dialog rendered unconditionally (controlled by `open`
              prop) so its internal state survives close/reopen. The
              dialog handles its own fetch on open via useEffect on
              the `open` prop.
            */}
            <ShareDialog
                open={shareOpen}
                playlistId={id}
                playlistName={playlist.name}
                onClose={() => setShareOpen(false)}
            />
        </Box>
    );
}