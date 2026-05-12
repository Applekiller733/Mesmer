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

import { useAppDispatch } from "../hooks/hooks";
import {
    selectIncoming,
    selectIncomingCount,
    removeIncoming,
    addFriend,
} from "../stores/slices/friendshipslice";
import {
    fetchIncoming,
    fetchIncomingCount,
} from "../stores/thunks/friendshipthunk";
import {
    apiacceptrequest,
    apideclinerequest,
} from "../stores/api/friendshipapi";

const POLL_INTERVAL_MS = 60_000;
const PREVIEW_LIMIT = 4;

export function useFriendshipPolling() {
    const dispatch = useAppDispatch();
    const tickerRef = useRef<ReturnType<typeof setInterval> | null>(null);

    useEffect(() => {
        
        dispatch(fetchIncomingCount());

        tickerRef.current = setInterval(() => {
            dispatch(fetchIncomingCount());
        }, POLL_INTERVAL_MS);

        return () => {
            if (tickerRef.current) clearInterval(tickerRef.current);
        };
    }, [dispatch]);
}

export default function FriendRequestsBadge() {
    const dispatch = useAppDispatch();
    const navigate = useNavigate();
    const count = useSelector(selectIncomingCount);
    const incoming = useSelector(selectIncoming);

    const [anchorEl, setAnchorEl] = useState<HTMLButtonElement | null>(null);
    const open = Boolean(anchorEl);

    function handleOpen(e: React.MouseEvent<HTMLButtonElement>) {
        setAnchorEl(anchorEl ? null : e.currentTarget);
        // Fetch the full list when the dropdown opens — we have a count
        // but the dropdown needs the actual rows to show.
        dispatch(fetchIncoming());
    }

    function handleClose() {
        setAnchorEl(null);
    }

    async function handleAccept(row: any) {
        try {
            const updated = await apiacceptrequest(row.id);
            dispatch(removeIncoming(row.id));
            dispatch(addFriend(updated));
        } catch (e: any) {
            alert(e?.message ?? "Accept failed.");
        }
    }

    async function handleDecline(row: any) {
        try {
            await apideclinerequest(row.id);
            dispatch(removeIncoming(row.id));
        } catch (e: any) {
            alert(e?.message ?? "Decline failed.");
        }
    }

    return (
        <>
            <IconButton onClick={handleOpen} color="inherit" size="large">
                <Badge color="error" badgeContent={count} invisible={count === 0}>
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
                    <Paper sx={{ width: 320, maxHeight: 480, overflowY: "auto" }}>
                        <Box sx={{ p: 1.5 }}>
                            <Typography variant="subtitle2">
                                Friend Requests
                            </Typography>
                        </Box>
                        <Divider />

                        {incoming.length === 0 ? (
                            <Box sx={{ p: 2, opacity: 0.6 }}>
                                <Typography variant="body2">
                                    No pending requests.
                                </Typography>
                            </Box>
                        ) : (
                            <List dense>
                                {incoming.slice(0, PREVIEW_LIMIT).map((r) => (
                                    <ListItem key={r.id} divider>
                                        <ListItemText
                                            primary={
                                                <Button
                                                    variant="text"
                                                    sx={{
                                                        textTransform: "none",
                                                        p: 0,
                                                        minWidth: 0,
                                                    }}
                                                    onClick={() => {
                                                        handleClose();
                                                        navigate(`/profile/${r.senderId}`);
                                                    }}
                                                >
                                                    {r.senderId}
                                                </Button>
                                            }
                                            secondary="wants to be friends"
                                        />
                                        <ButtonGroup size="small">
                                            <Button
                                                color="success"
                                                onClick={() => handleAccept(r)}
                                            >
                                                Accept
                                            </Button>
                                            <Button
                                                color="error"
                                                onClick={() => handleDecline(r)}
                                            >
                                                Decline
                                            </Button>
                                        </ButtonGroup>
                                    </ListItem>
                                ))}
                            </List>
                        )}

                        <Divider />
                        <Box sx={{ p: 1, textAlign: "center" }}>
                            <Button
                                size="small"
                                onClick={() => {
                                    handleClose();
                                    navigate("/friends");
                                }}
                            >
                                See all friends
                            </Button>
                        </Box>
                    </Paper>
                </ClickAwayListener>
            </Popper>
        </>
    );
}