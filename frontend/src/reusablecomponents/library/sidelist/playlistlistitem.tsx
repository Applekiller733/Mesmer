import { useRef, useEffect, useState } from "react";
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
    const creatorName = playlist.createdBy?.userName ?? "(unknown)";

    const clipRef = useRef<HTMLDivElement>(null);
    const textRef = useRef<HTMLSpanElement>(null);
    const [overflow, setOverflow] = useState(0);

    useEffect(() => {
        const clip = clipRef.current;
        const text = textRef.current;
        if (!clip || !text) return;
        const diff = text.scrollWidth - clip.clientWidth;
        setOverflow(diff > 0 ? diff : 0);
    }, [playlist.name]);

    return (
        <Card className="listitem-background">
            <Stack direction="row" alignItems="center" spacing={1} className="listitem-row">
                <Box className="listitem-title-clip" ref={clipRef}>
                    <Typography
                        ref={textRef}
                        variant="body2"
                        className={`listitem-title${overflow > 0 ? " is-overflowing" : ""}`}
                        style={{ ["--marquee-shift" as any]: `-${overflow}px` }}
                    >
                        {playlist.name}
                    </Typography>
                </Box>
                <VisibilityBadge visibility={playlist.visibility} />
            </Stack>
            <Typography variant="caption" noWrap className="listitem-subtitle">
                {isOwned ? "Owned" : `Saved · by ${creatorName}`}
            </Typography>
        </Card>
    );
}