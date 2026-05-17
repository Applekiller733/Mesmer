import { useEffect, useRef, useState } from "react";
import {
    IconButton,
    Badge,
    Popper,
    Paper,
    ClickAwayListener,
    List,
    ListItem,
    ListItemText,
    Button,
    ButtonGroup,
    Typography,
    Box,
    Divider,
} from "@mui/material";
import NotificationsIcon from "@mui/icons-material/Notifications";
import { useSelector } from "react-redux";
import { useNavigate } from "react-router";

import { useAppDispatch } from "../../hooks/hooks";
import {
    selectIncoming,
    selectIncomingCount,
    removeIncoming,
    addFriend,
} from "../../stores/slices/friendshipslice";
import {
    fetchIncoming,
    fetchIncomingCount,
} from "../../stores/thunks/friendshipthunk";
import {
    apiacceptrequest,
    apideclinerequest,
} from "../../stores/api/friendshipapi";
import { formatFriendCode } from "../../utils/helpers/friendshiphelpers";

import {
    selectIncomingInvitations,
    selectIncomingInvitationsCount,
    removeIncoming as removeIncomingInvitation,
} from "../../stores/slices/playlistinvitationslice";
import {
    fetchIncomingInvitations,
    fetchIncomingInvitationsCount,
} from "../../stores/thunks/playlistinvitationthunks";
import {
    apiacceptinvitation,
    apideclineinvitation,
} from "../../stores/api/playlistinvitationapi";
import { fetchPlaylistsSavedByAccountId } from "../../stores/thunks/playlistthunks";
import { selectCurrentUser } from "../../stores/slices/userdataslice";
import VisibilityBadge from "../library/visibility/visibilitybadge";

/**
 * Combined notifications popper. Replaces the previous
 * friendrequestsbadge.tsx — one bell icon, one popper, two sections
 * inside: Friend Requests + Playlist Shares.
 *
 * The badge count on the bell is the SUM of pending friend requests
 * and pending playlist invitations, so a single number reflects "stuff
 * waiting for you". Each section inside polls its own count so the two
 * pieces of state stay independent.
 *
 * Polling: 60s for both, kicked off by useNotificationsPolling() which
 * is mounted from navbar.tsx exactly once per logged-in session. Same
 * cadence the friend polling used previously.
 */

const POLL_INTERVAL_MS = 60_000;
const PREVIEW_LIMIT = 4;

export function useNotificationsPolling() {
    const dispatch = useAppDispatch();
    const tickerRef = useRef<ReturnType<typeof setInterval> | null>(null);

    useEffect(() => {
        // Fire both immediately so the badge reflects state on mount
        // rather than waiting 60s. Then a single interval drives both.
        dispatch(fetchIncomingCount());
        dispatch(fetchIncomingInvitationsCount());
        tickerRef.current = setInterval(() => {
            dispatch(fetchIncomingCount());
            dispatch(fetchIncomingInvitationsCount());
        }, POLL_INTERVAL_MS);
        return () => {
            if (tickerRef.current) clearInterval(tickerRef.current);
        };
    }, [dispatch]);
}

// Re-export under the old name for any caller that might still
// import useFriendshipPolling. Deprecated; remove after migration.
export const useFriendshipPolling = useNotificationsPolling;

export default function NotificationsBadge() {
    const dispatch = useAppDispatch();
    const navigate = useNavigate();
    const currentUser = useSelector(selectCurrentUser);

    const friendCount = useSelector(selectIncomingCount);
    const incomingFriends = useSelector(selectIncoming);

    const invitationCount = useSelector(selectIncomingInvitationsCount);
    const incomingInvitations = useSelector(selectIncomingInvitations);

    // Combined count for the bell badge.
    const totalCount = friendCount + invitationCount;

    const [anchorEl, setAnchorEl] = useState<HTMLButtonElement | null>(null);
    const open = Boolean(anchorEl);

    function handleOpen(e: React.MouseEvent<HTMLButtonElement>) {
        setAnchorEl(anchorEl ? null : e.currentTarget);
        // Refresh both sections when the popper opens so the user sees
        // current data even if a poll cycle hasn't fired recently.
        dispatch(fetchIncoming());
        dispatch(fetchIncomingInvitations());
    }

    function handleClose() {
        setAnchorEl(null);
    }

    // ---------------- Friend handlers ----------------

    async function handleAcceptFriend(row: any) {
        try {
            const updated = await apiacceptrequest(row.id);
            dispatch(removeIncoming(row.id));
            dispatch(addFriend(updated));
        } catch (e: any) {
            alert(e?.message ?? "Accept failed.");
        }
    }

    async function handleDeclineFriend(row: any) {
        try {
            await apideclinerequest(row.id);
            dispatch(removeIncoming(row.id));
        } catch (e: any) {
            alert(e?.message ?? "Decline failed.");
        }
    }

    // ---------------- Invitation handlers ----------------

    async function handleAcceptInvitation(row: any) {
        try {
            await apiacceptinvitation(row.id);
            // Three things happen on a successful accept:
            //  1. The invitation row is gone — drop from local state.
            //  2. The playlist is now in the user's library — refresh
            //     the saved list so it shows up in the sidelist /
            //     library page without a manual nav.
            //  3. The badge count needs no extra action; removeIncoming
            //     keeps it in sync via the slice's setIncomingCount.
            dispatch(removeIncomingInvitation(row.id));
            if (currentUser.id) {
                dispatch(fetchPlaylistsSavedByAccountId(currentUser.id));
            }
        } catch (e: any) {
            alert(e?.message ?? "Accept failed.");
        }
    }

    async function handleDeclineInvitation(row: any) {
        try {
            await apideclineinvitation(row.id);
            dispatch(removeIncomingInvitation(row.id));
        } catch (e: any) {
            alert(e?.message ?? "Decline failed.");
        }
    }

    return (
        <>
            <IconButton onClick={handleOpen} color="inherit" size="large">
                <Badge
                    color="error"
                    badgeContent={totalCount}
                    invisible={totalCount === 0}
                >
                    <NotificationsIcon />
                </Badge>
            </IconButton>

            <Popper
                open={open}
                anchorEl={anchorEl}
                placement="bottom-end"
                sx={{ zIndex: 1300 }}
            >
                <ClickAwayListener onClickAway={handleClose}>
                    <Paper sx={{ width: 360, maxHeight: 540, overflowY: "auto" }}>
                        {/* ----------- Friend Requests section ----------- */}
                        <Box sx={{ p: 1.5 }}>
                            <Typography variant="subtitle2">
                                Friend Requests
                            </Typography>
                        </Box>
                        <Divider />

                        {incomingFriends.length === 0 ? (
                            <Box sx={{ p: 2, opacity: 0.6 }}>
                                <Typography variant="body2">
                                    No pending friend requests.
                                </Typography>
                            </Box>
                        ) : (
                            <List dense>
                                {incomingFriends.slice(0, PREVIEW_LIMIT).map((r) => (
                                    <ListItem key={r.id} divider>
                                        <ListItemText
                                            primary={
                                                <Box
                                                    onClick={() => {
                                                        handleClose();
                                                        navigate(`/profile/${r.senderId}`);
                                                    }}
                                                    sx={{
                                                        cursor: "pointer",
                                                        "&:hover .username": {
                                                            textDecoration: "underline",
                                                        },
                                                    }}
                                                >
                                                    <Typography variant="body2" className="username">
                                                        {r.senderUserName || "(unknown)"}
                                                    </Typography>
                                                    <Typography
                                                        variant="caption"
                                                        sx={{ opacity: 0.6, fontFamily: "monospace" }}
                                                    >
                                                        {formatFriendCode(r.senderFriendCode)}
                                                    </Typography>
                                                </Box>
                                            }
                                            secondary="wants to be friends"
                                        />
                                        <ButtonGroup size="small">
                                            <Button color="success" onClick={() => handleAcceptFriend(r)}>
                                                Accept
                                            </Button>
                                            <Button color="error" onClick={() => handleDeclineFriend(r)}>
                                                Decline
                                            </Button>
                                        </ButtonGroup>
                                    </ListItem>
                                ))}
                            </List>
                        )}

                        {/* ----------- Playlist Shares section ----------- */}
                        <Box sx={{ p: 1.5, mt: 1 }}>
                            <Typography variant="subtitle2">
                                Playlist Shares
                            </Typography>
                        </Box>
                        <Divider />

                        {incomingInvitations.length === 0 ? (
                            <Box sx={{ p: 2, opacity: 0.6 }}>
                                <Typography variant="body2">
                                    No pending playlist shares.
                                </Typography>
                            </Box>
                        ) : (
                            <List dense>
                                {incomingInvitations.slice(0, PREVIEW_LIMIT).map((r) => (
                                    <ListItem key={r.id} divider>
                                        <ListItemText
                                            primary={
                                                <Box
                                                    onClick={() => {
                                                        // Clicking the row body navigates
                                                        // to the sender's profile — mirrors
                                                        // the friend-request behaviour.
                                                        handleClose();
                                                        navigate(`/profile/${r.senderId}`);
                                                    }}
                                                    sx={{
                                                        cursor: "pointer",
                                                        "&:hover .playlist-name": {
                                                            textDecoration: "underline",
                                                        },
                                                    }}
                                                >
                                                    <Box
                                                        sx={{
                                                            display: "flex",
                                                            alignItems: "center",
                                                            gap: 0.75,
                                                        }}
                                                    >
                                                        <Typography
                                                            variant="body2"
                                                            className="playlist-name"
                                                        >
                                                            {r.playlistName}
                                                        </Typography>
                                                        <VisibilityBadge
                                                            visibility={r.playlistVisibility}
                                                        />
                                                    </Box>
                                                    <Typography
                                                        variant="caption"
                                                        sx={{ opacity: 0.6 }}
                                                    >
                                                        from {r.senderUserName || "(unknown)"}
                                                    </Typography>
                                                </Box>
                                            }
                                            secondary="invited you to add this playlist"
                                        />
                                        <ButtonGroup size="small">
                                            <Button
                                                color="success"
                                                onClick={() => handleAcceptInvitation(r)}
                                            >
                                                Accept
                                            </Button>
                                            <Button
                                                color="error"
                                                onClick={() => handleDeclineInvitation(r)}
                                            >
                                                Decline
                                            </Button>
                                        </ButtonGroup>
                                    </ListItem>
                                ))}
                            </List>
                        )}

                        {/* ----------- Footer ----------- */}
                        <Divider />
                        <Box sx={{ p: 1, textAlign: "center" }}>
                            <Button
                                size="small"
                                onClick={() => {
                                    handleClose();
                                    navigate("/socials");
                                }}
                            >
                                See all
                            </Button>
                        </Box>
                    </Paper>
                </ClickAwayListener>
            </Popper>
        </>
    );
}
