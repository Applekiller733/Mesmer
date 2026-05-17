import { Chip, Tooltip } from "@mui/material";
import LockIcon from "@mui/icons-material/Lock";
import LinkIcon from "@mui/icons-material/Link";
import PublicIcon from "@mui/icons-material/Public";
import { PlaylistVisibility, visibilityLabel } from "../../../models/playlist";

export default function VisibilityBadge({
    visibility,
    size = "small",
}: {
    visibility: PlaylistVisibility;
    size?: "small" | "medium";
}) {
    const { icon, tooltip } = visibilityDisplay(visibility);

    return (
        <Tooltip title={tooltip} arrow>
            <Chip
                icon={icon}
                label={visibilityLabel(visibility)}
                size={size}
                variant="outlined"
            />
        </Tooltip>
    );
}

export function visibilityDisplay(visibility: PlaylistVisibility) {
    switch (visibility) {
        case PlaylistVisibility.Private:
            return {
                icon: <LockIcon fontSize="small" />,
                tooltip: "Only you can see this playlist",
            };
        case PlaylistVisibility.Unlisted:
            return {
                icon: <LinkIcon fontSize="small" />,
                tooltip: "Hidden from your profile. Only people you share it with can access it.",
            };
        case PlaylistVisibility.Public:
            return {
                icon: <PublicIcon fontSize="small" />,
                tooltip: "Listed on your profile. Anyone can save or share it.",
            };
        default:
            return { icon: <LockIcon fontSize="small" />, tooltip: "" };
    }
}