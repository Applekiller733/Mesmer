
// Small display of a friend code with a copy-to-clipboard button.
// Used on the profile page to show "your code" prominently — users
// will share this with friends who want to add them.

import { useState } from "react";
import { Box, Typography, IconButton, Tooltip } from "@mui/material";
import ContentCopyIcon from "@mui/icons-material/ContentCopy";
import CheckIcon from "@mui/icons-material/Check";
import { formatFriendCode } from "../../utils/helpers/friendshiphelpers";

interface Props {
    code: string;
    label?: string;
}

export default function FriendCodeChip({ code, label = "Friend code" }: Props) {
    const [copied, setCopied] = useState(false);

    async function handleCopy() {
        if (!code) return;
        try {
            await navigator.clipboard.writeText(formatFriendCode(code));
            setCopied(true);
            // Reset back after 1.5s so the user can copy again if needed.
            setTimeout(() => setCopied(false), 1500);
        } catch {
            // Browsers can refuse clipboard writes for many reasons
            // (insecure context, denied permission). Fall back to alert.
            alert(`Friend code: ${formatFriendCode(code)}`);
        }
    }

    if (!code) return null;

    return (
        <Box
            sx={{
                display: "inline-flex",
                alignItems: "center",
                gap: 1,
                px: 1.5,
                py: 0.75,
                borderRadius: 2,
                backgroundColor: "rgba(255,255,255,0.06)",
                border: "1px solid rgba(255,255,255,0.12)",
            }}
        >
            <Box>
                <Typography variant="caption" sx={{ opacity: 0.7, display: "block" }}>
                    {label}
                </Typography>
                <Typography
                    variant="body1"
                    sx={{ fontFamily: "monospace", letterSpacing: 1 }}
                >
                    {formatFriendCode(code)}
                </Typography>
            </Box>
            <Tooltip
                title={copied ? "Copied!" : "Copy"}
                placement="top"
                arrow
            >
                <IconButton size="small" onClick={handleCopy}>
                    {copied ? (
                        <CheckIcon fontSize="small" color="success" />
                    ) : (
                        <ContentCopyIcon fontSize="small" />
                    )}
                </IconButton>
            </Tooltip>
        </Box>
    );
}