import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../../themes/themes";
import { Box, Button, List, ListItem, Paper, Typography } from "@mui/material";
import PlaylistListItem from "./playlistlistitem";
import type { Playlist } from "../../../models/playlist";

export default function SideList({ handlePlaylistClick, handleCreatePlaylist, playlists }: {
    handlePlaylistClick: (event: React.MouseEvent, id: string) => void;
    handleCreatePlaylist: () => void;
    playlists: Playlist[];
}) {

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
                                        <Button onClick={(event) => {handlePlaylistClick(event, p.id)}}>
                                            <PlaylistListItem id={p.id}
                                                name={p.name} createdAt={p.createdAt}
                                                updatedAt={p.updatedAt} songs={p.songs} />
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