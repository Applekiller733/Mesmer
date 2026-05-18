import { Box, Card, Stack, Typography } from "@mui/material";
import "./playlistlistitem.css";
import type { Playlist } from "../../../models/playlist";
import VisibilityBadge from "../visibility/visibilitybadge";

export default function PlaylistListItem({
    playlist,
    currentUserId,
}: {
    playlist: Playlist;
    currentUserId: string;
}) {
    const isOwned =
        !!playlist.createdBy?.id &&
        !!currentUserId &&
        playlist.createdBy.id === currentUserId;

    // Creator label only relevant for saved-not-owned rows. Fall back
    // to "(unknown)" if the response somehow omitted createdBy.userName
    // — shouldn't happen in normal use but the type allows it.
    const creatorName = playlist.createdBy?.userName ?? "(unknown)";

    return (
        <Box className="listitem-box">
            <Card className="listitem-background">
                <Stack direction="row" alignItems="center" spacing={1}>
                    <Typography variant="body2" noWrap sx={{ flex: 1 }}>
                        {playlist.name}
                    </Typography>
                    <VisibilityBadge visibility={playlist.visibility} />
                </Stack>
                <Typography variant="caption" sx={{ opacity: 0.65 }}>
                    {isOwned ? "Owned" : `Saved · by ${creatorName}`}
                </Typography>
            </Card>
        </Box>
    );
}