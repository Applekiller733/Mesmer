import { useEffect, useState } from "react";
import {
    Box,
    Paper,
    Tabs,
    Tab,
    Badge,
    Typography,
    List,
    ListItem,
    ListItemText,
    Button,
    ButtonGroup,
    CircularProgress,
} from "@mui/material";
import { ThemeProvider } from "@emotion/react";
import { useSelector } from "react-redux";
import { useNavigate } from "react-router";

import { darkTheme } from "../../themes/themes";
import Navbar from "../../reusablecomponents/navbar";
import { useAppDispatch } from "../../hooks/hooks";
import { selectCurrentUser } from "../../stores/slices/userdataslice";
import {
    selectFriends,
    selectIncoming,
    selectOutgoing,
    selectBlocked,
    selectFriendshipLoading,
    selectIncomingCount,
    removeIncoming,
    removeOutgoing,
    removeFriend,
    removeBlocked,
    addFriend,
} from "../../stores/slices/friendshipslice";
import {
    fetchFriends,
    fetchIncoming,
    fetchOutgoing,
    fetchBlocked,
} from "../../stores/thunks/friendshipthunk";
import {
    apiacceptrequest,
    apideclinerequest,
    apiremovefriend,
    apiunblockuser,
    type Friendship,
} from "../../stores/api/friendshipapi";
import {
    otherUserId,
    otherUserName,
    otherFriendCode,
    formatFriendCode,
} from "../../utils/helpers/friendshiphelpers";
import "./friends.css";

type TabKey = "friends" | "incoming" | "outgoing" | "blocked";

export default function FriendsPage() {
    const dispatch = useAppDispatch();
    const navigate = useNavigate();
    const currentUser = useSelector(selectCurrentUser);

    const friends = useSelector(selectFriends);
    const incoming = useSelector(selectIncoming);
    const outgoing = useSelector(selectOutgoing);
    const blocked = useSelector(selectBlocked);
    const incomingCount = useSelector(selectIncomingCount);
    const loading = useSelector(selectFriendshipLoading);

    const [tab, setTab] = useState<TabKey>("friends");

    useEffect(() => {
        if (!currentUser?.id) return;
        if (tab === "friends") dispatch(fetchFriends());
        else if (tab === "incoming") dispatch(fetchIncoming());
        else if (tab === "outgoing") dispatch(fetchOutgoing());
        else if (tab === "blocked") dispatch(fetchBlocked());
    }, [tab, currentUser?.id, dispatch]);

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="friends-background">
                <Navbar />
                <Paper className="friends-paper">
                    <Typography variant="h4" sx={{ mb: 2 }}>
                        Friends
                    </Typography>

                    <Tabs
                        value={tab}
                        onChange={(_, v) => setTab(v)}
                        textColor="inherit"
                    >
                        <Tab value="friends" label="Friends" />
                        <Tab
                            value="incoming"
                            label={
                                <Badge
                                    color="error"
                                    badgeContent={incomingCount}
                                    invisible={incomingCount === 0}
                                >
                                    <span style={{ paddingRight: 8 }}>Incoming</span>
                                </Badge>
                            }
                        />
                        <Tab value="outgoing" label="Outgoing" />
                        <Tab value="blocked" label="Blocked" />
                    </Tabs>

                    <Box sx={{ mt: 2 }}>
                        {tab === "friends" && (
                            <FriendsTab
                                rows={friends}
                                loading={loading.friends}
                                currentUserId={currentUser.id ?? ""}
                                onOpen={(id) => navigate(`/profile/${id}`)}
                                onRemove={async (id) => {
                                    await apiremovefriend(id);
                                    dispatch(removeFriend(id));
                                }}
                            />
                        )}

                        {tab === "incoming" && (
                            <IncomingTab
                                rows={incoming}
                                loading={loading.incoming}
                                currentUserId={currentUser.id ?? ""}
                                onOpen={(id) => navigate(`/profile/${id}`)}
                                onAccept={async (row) => {
                                    const updated = await apiacceptrequest(row.id);
                                    dispatch(removeIncoming(row.id));
                                    dispatch(addFriend(updated));
                                }}
                                onDecline={async (row) => {
                                    await apideclinerequest(row.id);
                                    dispatch(removeIncoming(row.id));
                                }}
                            />
                        )}

                        {tab === "outgoing" && (
                            <OutgoingTab
                                rows={outgoing}
                                loading={loading.outgoing}
                                currentUserId={currentUser.id ?? ""}
                                onOpen={(id) => navigate(`/profile/${id}`)}
                                onCancel={async (row) => {
                                    const other = otherUserId(row, currentUser.id ?? "");
                                    await apiremovefriend(other);
                                    dispatch(removeOutgoing(row.id));
                                }}
                            />
                        )}

                        {tab === "blocked" && (
                            <BlockedTab
                                rows={blocked}
                                loading={loading.blocked}
                                currentUserId={currentUser.id ?? ""}
                                onOpen={(id) => navigate(`/profile/${id}`)}
                                onUnblock={async (row) => {
                                    const other = otherUserId(row, currentUser.id ?? "");
                                    await apiunblockuser(other);
                                    dispatch(removeBlocked(other));
                                }}
                            />
                        )}
                    </Box>
                </Paper>
            </Box>
        </ThemeProvider>
    );
}

// tab content components
//
// Each tab renders the row's username + friend code rather than the
// raw UUID. The friend code is shown smaller and dimmer — informational
// for disambiguation, not the primary read.

interface BaseTabProps {
    loading: boolean;
    currentUserId: string;
    onOpen: (userId: string) => void;
}

/**
 * Reusable name + code stack. Used by every tab so the formatting stays
 * consistent — username on top in normal weight, friend code below in a
 * smaller dim font.
 */
function UserLabel({
    name,
    code,
    onClick,
}: {
    name: string;
    code: string;
    onClick: () => void;
}) {
    return (
        <Box
            onClick={onClick}
            sx={{
                cursor: "pointer",
                "&:hover .username": { textDecoration: "underline" },
            }}
        >
            <Typography variant="body1" className="username">
                {name || "(unknown)"}
            </Typography>
            <Typography
                variant="caption"
                sx={{ opacity: 0.6, fontFamily: "monospace" }}
            >
                {formatFriendCode(code)}
            </Typography>
        </Box>
    );
}

function FriendsTab(
    props: BaseTabProps & {
        rows: Friendship[];
        onRemove: (otherUserId: string) => void | Promise<void>;
    }
) {
    if (props.loading) return <CenteredSpinner />;
    if (props.rows.length === 0) return <EmptyState text="No friends yet." />;
    return (
        <List>
            {props.rows.map((r) => {
                const otherId = otherUserId(r, props.currentUserId);
                const name = otherUserName(r, props.currentUserId);
                const code = otherFriendCode(r, props.currentUserId);
                return (
                    <ListItem key={r.id} divider>
                        <ListItemText
                            primary={
                                <UserLabel
                                    name={name}
                                    code={code}
                                    onClick={() => props.onOpen(otherId)}
                                />
                            }
                        />
                        <Button
                            variant="outlined"
                            color="error"
                            size="small"
                            onClick={async () => {
                                if (!confirm("Remove this friend?")) return;
                                await props.onRemove(otherId);
                            }}
                        >
                            Remove
                        </Button>
                    </ListItem>
                );
            })}
        </List>
    );
}

function IncomingTab(
    props: BaseTabProps & {
        rows: Friendship[];
        onAccept: (row: Friendship) => void | Promise<void>;
        onDecline: (row: Friendship) => void | Promise<void>;
    }
) {
    if (props.loading) return <CenteredSpinner />;
    if (props.rows.length === 0) return <EmptyState text="No incoming requests." />;
    return (
        <List>
            {props.rows.map((r) => (
                <ListItem key={r.id} divider>
                    <ListItemText
                        primary={
                            <UserLabel
                                name={r.senderUserName}
                                code={r.senderFriendCode}
                                onClick={() => props.onOpen(r.senderId)}
                            />
                        }
                        secondary="wants to be your friend"
                    />
                    <ButtonGroup size="small">
                        <Button color="success" variant="contained" onClick={() => props.onAccept(r)}>
                            Accept
                        </Button>
                        <Button color="error" variant="outlined" onClick={() => props.onDecline(r)}>
                            Decline
                        </Button>
                    </ButtonGroup>
                </ListItem>
            ))}
        </List>
    );
}

function OutgoingTab(
    props: BaseTabProps & {
        rows: Friendship[];
        onCancel: (row: Friendship) => void | Promise<void>;
    }
) {
    if (props.loading) return <CenteredSpinner />;
    if (props.rows.length === 0) return <EmptyState text="No outgoing requests." />;
    return (
        <List>
            {props.rows.map((r) => (
                <ListItem key={r.id} divider>
                    <ListItemText
                        primary={
                            <UserLabel
                                name={r.receiverUserName}
                                code={r.receiverFriendCode}
                                onClick={() => props.onOpen(r.receiverId)}
                            />
                        }
                        secondary="Pending — sent by you"
                    />
                    <Button
                        variant="outlined"
                        size="small"
                        onClick={async () => {
                            if (!confirm("Cancel this request?")) return;
                            await props.onCancel(r);
                        }}
                    >
                        Cancel
                    </Button>
                </ListItem>
            ))}
        </List>
    );
}

function BlockedTab(
    props: BaseTabProps & {
        rows: Friendship[];
        onUnblock: (row: Friendship) => void | Promise<void>;
    }
) {
    if (props.loading) return <CenteredSpinner />;
    if (props.rows.length === 0) return <EmptyState text="No blocked users." />;
    return (
        <List>
            {props.rows.map((r) => {
                const otherId = otherUserId(r, props.currentUserId);
                const name = otherUserName(r, props.currentUserId);
                const code = otherFriendCode(r, props.currentUserId);
                return (
                    <ListItem key={r.id} divider>
                        <ListItemText
                            primary={
                                <UserLabel
                                    name={name}
                                    code={code}
                                    onClick={() => props.onOpen(otherId)}
                                />
                            }
                            secondary="Blocked"
                        />
                        <Button variant="outlined" size="small" onClick={() => props.onUnblock(r)}>
                            Unblock
                        </Button>
                    </ListItem>
                );
            })}
        </List>
    );
}

function CenteredSpinner() {
    return (
        <Box sx={{ display: "flex", justifyContent: "center", py: 4 }}>
            <CircularProgress />
        </Box>
    );
}

function EmptyState({ text }: { text: string }) {
    return (
        <Typography sx={{ opacity: 0.6, textAlign: "center", py: 4 }}>
            {text}
        </Typography>
    );
}