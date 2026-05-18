import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../../themes/themes";
import { Box, Button, List, ListItem, Paper, Typography } from "@mui/material";
import PlaylistListItem from "./playlistlistitem";
import type { Playlist } from "../../../models/playlist";
import React from "react";
import { useSelector } from "react-redux";
import { selectCurrentUser } from "../../../stores/slices/userdataslice";

export default function SideList({ handlePlaylistClick, handleCreatePlaylist, playlists }: {
    handlePlaylistClick: (event: React.MouseEvent, id: string) => void;
    handleCreatePlaylist: () => void;
    playlists: Playlist[];
}) {
    // Current user pulled here rather than threaded down as a prop so
    // existing callers of SideList don't have to change. The selector
    // is cheap; PlaylistListItem only needs the id.
    const currentUser = useSelector(selectCurrentUser);

    return (
        <ThemeProvider theme={darkTheme}>
            <Box>
                <Paper>
                    <div className="upper">
                        <Typography>Library</Typography>
                    </div>
                    <div>
                        <List>
                            {
                                playlists.map((p) => (
                                    <ListItem key={p.id}>
                                        <Button onClick={(event) => { handlePlaylistClick(event, p.id) }}>
                                            <PlaylistListItem
                                                playlist={p}
                                                currentUserId={currentUser.id ?? ""}
                                            />
                                        </Button>
                                    </ListItem>
                                ))
                            }
                            <Button onClick={handleCreatePlaylist}>
                                Create Playlist
                            </Button>
                        </List>
                    </div>
                </Paper>
            </Box>
        </ThemeProvider>
    );
}