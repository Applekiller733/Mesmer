import { useEffect, useState } from "react";
import {
    Box,
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
    Stack,
} from "@mui/material";
import { useSelector } from "react-redux";
import { useNavigate } from "react-router";

import { useAppDispatch } from "../../../hooks/hooks";
import { selectCurrentUser } from "../../../stores/slices/userdataslice";
import {
    selectIncomingInvitations,
    selectOutgoingInvitations,
    selectInvitationsLoading,
    selectIncomingInvitationsCount,
    removeIncoming as removeIncomingInvitation,
    removeOutgoing as removeOutgoingInvitation,
} from "../../../stores/slices/playlistinvitationslice";
import {
    fetchIncomingInvitations,
    fetchOutgoingInvitations,
} from "../../../stores/thunks/playlistinvitationthunks";
import {
    apiacceptinvitation,
    apideclineinvitation,
    apicancelinvitation,
} from "../../../stores/api/playlistinvitationapi";
import { fetchPlaylistsSavedByAccountId } from "../../../stores/thunks/playlistthunks";
import {
    formatFriendCode,
} from "../../../utils/helpers/friendshiphelpers";
import type { PlaylistInvitation } from "../../../models/playlistinvitation";
import VisibilityBadge from "../../../reusablecomponents/library/visibility/visibilitybadge";

type InvTabKey = "incoming" | "outgoing";

/**
 * Playlist-invitation tabs of the Socials page. Two sub-tabs:
 * Incoming (shares received) and Outgoing (shares I've sent).
 *
 * Decline (incoming) and Cancel (outgoing) both reduce to "delete the
 * row" server-side — the distinction is who's authorised. We surface
 * them as separate verbs in the UI because they mean different things
 * to the user.
 */
export default function SocialsPlaylists() {
    const dispatch = useAppDispatch();
    const navigate = useNavigate();
    const currentUser = useSelector(selectCurrentUser);

    const incoming = useSelector(selectIncomingInvitations);
    const outgoing = useSelector(selectOutgoingInvitations);
    const incomingCount = useSelector(selectIncomingInvitationsCount);
    const loading = useSelector(selectInvitationsLoading);

    const [tab, setTab] = useState<InvTabKey>("incoming");

    useEffect(() => {
        if (!currentUser?.id) return;
        if (tab === "incoming") dispatch(fetchIncomingInvitations());
        else if (tab === "outgoing") dispatch(fetchOutgoingInvitations());
    }, [tab, currentUser?.id, dispatch]);

    return (
        <Box>
            <Tabs
                value={tab}
                onChange={(_, v) => setTab(v)}
                textColor="inherit"
            >
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
            </Tabs>

            <Box sx={{ mt: 2 }}>
                {tab === "incoming" && (
                    <IncomingInvitationsTab
                        rows={incoming}
                        loading={loading.incoming}
                        onOpenSender={(id) => navigate(`/profile/${id}`)}
                        onOpenPlaylist={(id) => {
                            // Standalone /playlist/:id route added in
                            // Step 8. Previously this fell back to the
                            // sender's profile because there was no
                            // playlist-view route. Now lands the user
                            // directly on the playlist they're being
                            // offered, where they can play and decide.
                            navigate(`/playlist/${id}`);
                        }}
                        onAccept={async (row) => {
                            await apiacceptinvitation(row.id);
                            dispatch(removeIncomingInvitation(row.id));
                            // Refresh the user's saved playlists so the
                            // newly-accepted playlist shows in the library.
                            if (currentUser.id) {
                                dispatch(fetchPlaylistsSavedByAccountId(currentUser.id));
                            }
                        }}
                        onDecline={async (row) => {
                            await apideclineinvitation(row.id);
                            dispatch(removeIncomingInvitation(row.id));
                        }}
                    />
                )}

                {tab === "outgoing" && (
                    <OutgoingInvitationsTab
                        rows={outgoing}
                        loading={loading.outgoing}
                        onOpenReceiver={(id) => navigate(`/profile/${id}`)}
                        onCancel={async (row) => {
                            await apicancelinvitation(row.id);
                            dispatch(removeOutgoingInvitation(row.id));
                        }}
                    />
                )}
            </Box>
        </Box>
    );
}

// ---------------- Tab components ----------------

function IncomingInvitationsTab(props: {
    rows: PlaylistInvitation[];
    loading: boolean;
    onOpenSender: (id: string) => void;
    onOpenPlaylist: (playlistId: string) => void;
    onAccept: (row: PlaylistInvitation) => void | Promise<void>;
    onDecline: (row: PlaylistInvitation) => void | Promise<void>;
}) {
    if (props.loading) return <CenteredSpinner />;
    if (props.rows.length === 0) {
        return <EmptyState text="No playlist invitations." />;
    }
    return (
        <List>
            {props.rows.map((r) => (
                <ListItem key={r.id} divider>
                    <ListItemText
                        primary={
                            <Stack direction="row" alignItems="center" spacing={1}>
                                {/*
                                  Clicking the playlist name navigates
                                  to the standalone playlist viewer at
                                  /playlist/:id. Visibility badge sits
                                  next to the name so the receiver knows
                                  what they are accepting.
                                */}
                                <Typography
                                    variant="body1"
                                    onClick={() => props.onOpenPlaylist(r.playlistId)}
                                    sx={{
                                        cursor: "pointer",
                                        "&:hover": { textDecoration: "underline" },
                                    }}
                                >
                                    {r.playlistName}
                                </Typography>
                                <VisibilityBadge visibility={r.playlistVisibility} />
                            </Stack>
                        }
                        secondary={
                            <Box
                                sx={{
                                    cursor: "pointer",
                                    "&:hover .sender-name": {
                                        textDecoration: "underline",
                                    },
                                }}
                                onClick={() => props.onOpenSender(r.senderId)}
                            >
                                <Typography variant="caption" component="span">
                                    from{" "}
                                </Typography>
                                <Typography
                                    variant="caption"
                                    component="span"
                                    className="sender-name"
                                >
                                    {r.senderUserName || "(unknown)"}
                                </Typography>
                                <Typography
                                    variant="caption"
                                    component="span"
                                    sx={{
                                        opacity: 0.6,
                                        fontFamily: "monospace",
                                        ml: 0.5,
                                    }}
                                >
                                    {formatFriendCode(r.senderFriendCode)}
                                </Typography>
                            </Box>
                        }
                    />
                    <ButtonGroup size="small">
                        <Button
                            color="success"
                            variant="contained"
                            onClick={() => props.onAccept(r)}
                        >
                            Accept
                        </Button>
                        <Button
                            color="error"
                            variant="outlined"
                            onClick={() => props.onDecline(r)}
                        >
                            Decline
                        </Button>
                    </ButtonGroup>
                </ListItem>
            ))}
        </List>
    );
}

function OutgoingInvitationsTab(props: {
    rows: PlaylistInvitation[];
    loading: boolean;
    onOpenReceiver: (id: string) => void;
    onCancel: (row: PlaylistInvitation) => void | Promise<void>;
}) {
    if (props.loading) return <CenteredSpinner />;
    if (props.rows.length === 0) {
        return <EmptyState text="No outgoing invitations." />;
    }
    return (
        <List>
            {props.rows.map((r) => (
                <ListItem key={r.id} divider>
                    <ListItemText
                        primary={
                            <Stack direction="row" alignItems="center" spacing={1}>
                                <Typography variant="body1">
                                    {r.playlistName}
                                </Typography>
                                <VisibilityBadge visibility={r.playlistVisibility} />
                            </Stack>
                        }
                        secondary={
                            <Box
                                sx={{
                                    cursor: "pointer",
                                    "&:hover .receiver-name": {
                                        textDecoration: "underline",
                                    },
                                }}
                                onClick={() => props.onOpenReceiver(r.receiverId)}
                            >
                                <Typography variant="caption" component="span">
                                    to{" "}
                                </Typography>
                                <Typography
                                    variant="caption"
                                    component="span"
                                    className="receiver-name"
                                >
                                    {r.receiverUserName || "(unknown)"}
                                </Typography>
                                <Typography
                                    variant="caption"
                                    component="span"
                                    sx={{
                                        opacity: 0.6,
                                        fontFamily: "monospace",
                                        ml: 0.5,
                                    }}
                                >
                                    {formatFriendCode(r.receiverFriendCode)}
                                </Typography>
                            </Box>
                        }
                    />
                    <Button
                        variant="outlined"
                        size="small"
                        onClick={async () => {
                            if (!confirm("Cancel this invitation?")) return;
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