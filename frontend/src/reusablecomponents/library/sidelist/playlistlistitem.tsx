import { Box, Card, Typography } from "@mui/material";
import "./playlistlistitem.css";
import type { Playlist } from "../../../models/playlist";

export default function PlaylistListItem(playlist:Playlist){


    return (
        <Box className="listitem-box">
            <Card className="listitem-background">
                <Typography>{playlist.name} - {playlist.createdAt}</Typography>
            </Card>
        </Box>
    );
}