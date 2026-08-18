import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../../themes/themes";
import { Box, Button, List, ListItem, Paper, Typography } from "@mui/material";
import PlaylistListItem from "./playlistlistitem";
import type { Playlist } from "../../../models/playlist";
import React from "react";
import { useSelector } from "react-redux";
import { selectCurrentUser } from "../../../stores/slices/userdataslice";
import "./sidelist.css";

export default function SideList({ handlePlaylistClick, handleCreatePlaylist, playlists }: {
    handlePlaylistClick: (event: React.MouseEvent, id: string) => void;
    handleCreatePlaylist: () => void;
    playlists: Playlist[];
}) {
    const currentUser = useSelector(selectCurrentUser);
    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="sidelist-root">
                <Paper>
                    <div className="upper">
                        <Typography>Library</Typography>
                    </div>
                    <div>
                        <List className="sidelist-list">
                            {playlists.map((p) => (
                                <ListItem key={p.id} disablePadding className="sidelist-item">
                                    <Button
                                        className="sidelist-item-button"
                                        onClick={(event) => { handlePlaylistClick(event, p.id); }}
                                    >
                                        <PlaylistListItem
                                            playlist={p}
                                            currentUserId={currentUser.id ?? ""}
                                        />
                                    </Button>
                                </ListItem>
                            ))}
                            <Button className="sidelist-create-button" onClick={handleCreatePlaylist}>
                                Create Playlist
                            </Button>
                        </List>
                    </div>
                </Paper>
            </Box>
        </ThemeProvider>
    );
}