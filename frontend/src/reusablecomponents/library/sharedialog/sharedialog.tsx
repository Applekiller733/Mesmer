import {
    Button,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    List,
    ListItem,
    ListItemText,
    Typography,
    Box,
    CircularProgress,
} from "@mui/material";
import { useEffect, useMemo, useState } from "react";
import { useSelector } from "react-redux";
import {
    apigetfriends,
    type Friendship,
} from "../../../stores/api/friendshipapi";
import {
    apiinviteplaylist,
    apigetoutgoinginvitations,
} from "../../../stores/api/playlistinvitationapi";
import { selectCurrentUser } from "../../../stores/slices/userdataslice";
import { addOutgoing } from "../../../stores/slices/playlistinvitationslice";
import { useAppDispatch } from "../../../hooks/hooks";
import {
    otherUserId,
    otherUserName,
    otherFriendCode,
    formatFriendCode,
} from "../../../utils/helpers/friendshiphelpers";

/**
 * Share-a-playlist dialog. Lists the current user's friends; clicking
 * Invite next to a friend sends an invitation to share this playlist
 * with them.
 *
 * Initial state on open:
 *   - Friends fetched fresh (covers the case where a friendship was
 *     accepted in another tab since the dialog was last open).
 *   - Existing outgoing invitations for this playlist fetched and
 *     intersected with the friends list so already-invited friends
 *     show "Invited ✓" disabled rather than an active Invite button.
 *
 * After an invite:
 *   - The button flips locally to "Invited ✓" disabled.
 *   - The new row is added to the outgoing slice via addOutgoing, so
 *     other consumers (Socials > Playlists > Outgoing tab) reflect
 *     the change immediately.
 *
 * Caveats:
 *   - The backend rejects invitations to users who already have the
 *     playlist saved. The frontend doesn't currently know who has
 *     saved a given playlist (no such field on PlaylistResponse), so
 *     those rejections surface as toasts only after clicking. Adding
 *     a savedByUserIds field would let us hide / disable the button
 *     proactively — separate refactor.
 *   - Friend search/filter not implemented. With a small friends list
 *     this is fine; if it grows, drop a TextField above the list.
 */
export default function ShareDialog({
    open,
    playlistId,
    playlistName,
    onClose,
}: {
    open: boolean;
    playlistId: string;
    playlistName: string;
    onClose: () => void;
}) {
    const dispatch = useAppDispatch();
    const currentUser = useSelector(selectCurrentUser);

    const [friends, setFriends] = useState<Friendship[]>([]);
    const [loading, setLoading] = useState(false);

    // Set of friend user-ids who already have a pending invitation for
    // this specific playlist. Tracked locally so the button state can
    // flip from "Invite" to "Invited ✓" the instant a request resolves
    // without waiting for a refetch. Initialised from the outgoing-
    // invitations API on dialog open.
    const [invitedIds, setInvitedIds] = useState<Set<string>>(new Set());

    // Per-friend in-flight flag. Prevents double-clicks racing each
    // other into duplicate invitations (the backend dedupes via the
    // unique index, but better UX to disable while a click is pending).
    const [pendingIds, setPendingIds] = useState<Set<string>>(new Set());

    // Fetch on open rather than on mount: a parent that lazily mounts
    // this dialog (e.g., behind a Button onClick) wouldn't trigger
    // useEffect at the moment of opening if we keyed on mount.
    useEffect(() => {
        if (!open) return;

        let cancelled = false;
        (async () => {
            setLoading(true);
            try {
                const [friendsList, outgoing] = await Promise.all([
                    apigetfriends(),
                    apigetoutgoinginvitations(),
                ]);
                if (cancelled) return;

                setFriends(friendsList);

                // Build the "already invited" set by filtering outgoing
                // to this playlist and extracting receiver ids.
                const alreadyInvited = new Set(
                    outgoing
                        .filter((i) => i.playlistId === playlistId)
                        .map((i) => i.receiverId)
                );
                setInvitedIds(alreadyInvited);
            } catch (e: any) {
                if (!cancelled) {
                    alert(e?.message ?? "Failed to load friends.");
                }
            } finally {
                if (!cancelled) setLoading(false);
            }
        })();

        return () => {
            cancelled = true;
        };
    }, [open, playlistId]);

    async function handleInvite(friendUserId: string) {
        // Local in-flight guard.
        setPendingIds((prev) => {
            const next = new Set(prev);
            next.add(friendUserId);
            return next;
        });
        try {
            const created = await apiinviteplaylist(playlistId, friendUserId);
            // Optimistic UI flip — the friend now shows as Invited.
            setInvitedIds((prev) => {
                const next = new Set(prev);
                next.add(friendUserId);
                return next;
            });
            // Push the new row into the slice's outgoing list so the
            // Outgoing tab in Socials reflects it without a refetch.
            dispatch(addOutgoing(created));
        } catch (e: any) {
            alert(e?.message ?? "Failed to send invitation.");
        } finally {
            setPendingIds((prev) => {
                const next = new Set(prev);
                next.delete(friendUserId);
                return next;
            });
        }
    }

    // Compute the per-friend display rows. Memoised because mapping
    // through friends every render would be wasteful if the list is
    // long, and the otherUserX helpers are pure.
    const rows = useMemo(() => {
        const cuid = currentUser.id ?? "";
        return friends.map((f) => {
            const oid = otherUserId(f, cuid);
            return {
                id: oid,
                name: otherUserName(f, cuid),
                code: otherFriendCode(f, cuid),
            };
        });
    }, [friends, currentUser.id]);

    const hasNoFriends = rows.length === 0;

    return (
        <Dialog open={open} onClose={onClose} fullWidth maxWidth="xs">
            <DialogTitle>
                <Box>
                    <Typography variant="subtitle1">Share playlist</Typography>
                    <Typography
                        variant="caption"
                        sx={{ opacity: 0.7 }}
                    >
                        {playlistName}
                    </Typography>
                </Box>
            </DialogTitle>

            <DialogContent dividers>
                {loading && (
                    <Box sx={{ display: "flex", justifyContent: "center", py: 3 }}>
                        <CircularProgress size={24} />
                    </Box>
                )}

                {!loading && hasNoFriends && (
                    <Typography sx={{ opacity: 0.6, textAlign: "center", py: 2 }}>
                        You don't have any friends to share with yet.
                    </Typography>
                )}

                {!loading && !hasNoFriends && (
                    <List dense>
                        {rows.map((r) => {
                            const invited = invitedIds.has(r.id);
                            const pending = pendingIds.has(r.id);
                            return (
                                <ListItem key={r.id} divider>
                                    <ListItemText
                                        primary={r.name || "(unknown)"}
                                        secondary={
                                            <Typography
                                                variant="caption"
                                                sx={{
                                                    opacity: 0.6,
                                                    fontFamily: "monospace",
                                                }}
                                            >
                                                {formatFriendCode(r.code)}
                                            </Typography>
                                        }
                                    />
                                    <Button
                                        size="small"
                                        variant={invited ? "outlined" : "contained"}
                                        disabled={invited || pending}
                                        onClick={() => handleInvite(r.id)}
                                    >
                                        {/*
                                          Three visual states:
                                            - Invite (default, ready)
                                            - … (pending request)
                                            - Invited ✓ (already done)
                                        */}
                                        {invited ? "Invited ✓" : pending ? "…" : "Invite"}
                                    </Button>
                                </ListItem>
                            );
                        })}
                    </List>
                )}
            </DialogContent>

            <DialogActions>
                <Button onClick={onClose}>Close</Button>
            </DialogActions>
        </Dialog>
    );
}