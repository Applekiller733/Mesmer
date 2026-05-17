import {
    Box,
    ToggleButton,
    ToggleButtonGroup,
    Typography,
} from "@mui/material";
import { PlaylistVisibility } from "../../../models/playlist";
import { visibilityDisplay } from "./visibilitybadge";

export default function VisibilitySelector({
    value,
    onChange,
    disabled = false,
}: {
    value: PlaylistVisibility;
    onChange: (next: PlaylistVisibility) => void;
    disabled?: boolean;
}) {
    function handleChange(
        _: React.MouseEvent<HTMLElement>,
        next: PlaylistVisibility | null,
    ) {
        if (next === null) return;
        onChange(next);
    }

    const helper = visibilityDisplay(value).tooltip;

    return (
        <Box>
            <ToggleButtonGroup
                value={value}
                exclusive
                onChange={handleChange}
                disabled={disabled}
                aria-label="Playlist visibility"
                size="small"
            >
                <ToggleButton
                    value={PlaylistVisibility.Private}
                    aria-label="Private"
                >
                    {visibilityDisplay(PlaylistVisibility.Private).icon}
                    <Box component="span" sx={{ ml: 0.75 }}>Private</Box>
                </ToggleButton>
                <ToggleButton
                    value={PlaylistVisibility.Unlisted}
                    aria-label="Unlisted"
                >
                    {visibilityDisplay(PlaylistVisibility.Unlisted).icon}
                    <Box component="span" sx={{ ml: 0.75 }}>Unlisted</Box>
                </ToggleButton>
                <ToggleButton
                    value={PlaylistVisibility.Public}
                    aria-label="Public"
                >
                    {visibilityDisplay(PlaylistVisibility.Public).icon}
                    <Box component="span" sx={{ ml: 0.75 }}>Public</Box>
                </ToggleButton>
            </ToggleButtonGroup>

            <Typography
                variant="caption"
                sx={{ display: "block", mt: 0.5, opacity: 0.7 }}
            >
                {helper}
            </Typography>
        </Box>
    );
}