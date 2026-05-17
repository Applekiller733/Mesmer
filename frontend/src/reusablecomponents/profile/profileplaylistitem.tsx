import {
    Box,
    Button,
    ButtonGroup,
    Card,
    CardContent,
    Stack,
    Typography,
} from "@mui/material";
import PlayCircleOutlineIcon from "@mui/icons-material/PlayCircleOutline";
import BookmarkAddOutlinedIcon from "@mui/icons-material/BookmarkAddOutlined";
import BookmarkAddedIcon from "@mui/icons-material/BookmarkAdded";
import ShareIcon from "@mui/icons-material/Share";
import { useNavigate } from "react-router";

import type { Playlist } from "../../models/playlist";
import VisibilityBadge from "../library/visibility/visibilitybadge";

/**
 * One playlist card on a profile page. Renders the playlist name + a
 * visibility badge, song count, and three actions:
 *
 *   - Play: navigates to the standalone /playlist/:id route. Available
 *     to anyone who can see the playlist.
 *   - Save: adds to the current user's library. Hidden when isOwner
 *     (saving your own playlist is meaningless — it's already there).
 *     Disabled when `isSaved` (already in library) and shows
 *     "Saved" instead of "Save" so the state is obvious.
 *   - Share: opens the share-with-friends dialog. Hidden when isOwner
 *     viewing their own profile only if visibility is Private, but
 *     since we don't show Private playlists outside the owner-view at
 *     all, in practice this button is always visible for Public.
 *
 * Action state (isSaved, pending) is owned by the parent so it can
 * coordinate the "save → flip to Saved" optimistic update across
 * multiple cards without each card re-deriving from selectors.
 */
export default function ProfilePlaylistItem({
    playlist,
    isOwner,
    isSaved,
    saving,
    onSave,
    onShare,
}: {
    playlist: Playlist;
    isOwner: boolean;
    isSaved: boolean;
    saving: boolean;
    onSave: (playlistId: string) => void;
    onShare: (playlist: Playlist) => void;
}) {
    const navigate = useNavigate();

    return (
        <Card variant="outlined" sx={{ mb: 1 }}>
            <CardContent>
                <Stack
                    direction="row"
                    alignItems="center"
                    justifyContent="space-between"
                    spacing={2}
                    flexWrap="wrap"
                >
                    {/*
                      Left side: name + badge stacked, plus a small
                      song count line. Clickable: the name navigates
                      to the playlist view.
                    */}
                    <Box
                        onClick={() => navigate(`/playlist/${playlist.id}`)}
                        sx={{
                            cursor: "pointer",
                            "&:hover .playlist-name": {
                                textDecoration: "underline",
                            },
                            minWidth: 0,
                            flex: 1,
                        }}
                    >
                        <Stack direction="row" alignItems="center" spacing={1}>
                            <Typography
                                variant="body1"
                                className="playlist-name"
                                noWrap
                            >
                                {playlist.name}
                            </Typography>
                            <VisibilityBadge visibility={playlist.visibility} />
                        </Stack>
                        <Typography variant="caption" sx={{ opacity: 0.6 }}>
                            {playlist.songs?.length ?? 0} song
                            {(playlist.songs?.length ?? 0) === 1 ? "" : "s"}
                        </Typography>
                    </Box>

                    {/* Right side: action buttons. */}
                    <ButtonGroup size="small" variant="outlined">
                        <Button
                            startIcon={<PlayCircleOutlineIcon />}
                            onClick={() => navigate(`/playlist/${playlist.id}`)}
                        >
                            Play
                        </Button>

                        {/*
                          Save button: hidden for the owner (their own
                          playlist is by definition already in their
                          library — see backend Save endpoint which
                          would reject the owner anyway).
                        */}
                        {!isOwner && (
                            <Button
                                startIcon={
                                    isSaved ? <BookmarkAddedIcon /> : <BookmarkAddOutlinedIcon />
                                }
                                color={isSaved ? "success" : "primary"}
                                disabled={isSaved || saving}
                                onClick={() => onSave(playlist.id)}
                            >
                                {isSaved ? "Saved" : saving ? "…" : "Save"}
                            </Button>
                        )}

                        <Button
                            startIcon={<ShareIcon />}
                            onClick={() => onShare(playlist)}
                        >
                            Share
                        </Button>
                    </ButtonGroup>
                </Stack>
            </CardContent>
        </Card>
    );
}