import { useEffect, useState } from "react";
import { Button, ButtonGroup, CircularProgress, Box } from "@mui/material";
import PersonAddIcon from "@mui/icons-material/PersonAdd";
import CheckIcon from "@mui/icons-material/Check";
import CloseIcon from "@mui/icons-material/Close";
import PeopleIcon from "@mui/icons-material/People";
import HourglassEmptyIcon from "@mui/icons-material/HourglassEmpty";
import BlockIcon from "@mui/icons-material/Block";
import {
    apiacceptrequest,
    apideclinerequest,
    apigetrelationship,
    apiremovefriend,
    apisendfriendrequest,
    apiunblockuser,
    FriendshipStatus,
    type RelationshipStatus,
} from "../../stores/api/friendshipapi";

// Removed: previous STATUS_PENDING / STATUS_ACCEPTED / STATUS_BLOCKED
// numeric constants. Compare against FriendshipStatus.* now — the
// wire format moved to strings, and the imported const-object gives
// the same ergonomics as the old local numbers.

interface FriendshipButtonProps {
    targetUserId: string;
}

export default function FriendshipButton({ targetUserId }: FriendshipButtonProps) {
    const [relationship, setRelationship] = useState<RelationshipStatus | null>(null);

    const [working, setWorking] = useState(false);

    useEffect(() => {
        let cancelled = false;
        (async () => {
            try {
                const data = await apigetrelationship(targetUserId);
                if (!cancelled) setRelationship(data);
            } catch {
                if (!cancelled) setRelationship(null);
            }
        })();
        return () => {
            cancelled = true;
        };
    }, [targetUserId]);

    async function handleSendRequest() {
        setWorking(true);
        try {
            await apisendfriendrequest(targetUserId);
            const fresh = await apigetrelationship(targetUserId);
            setRelationship(fresh);
        } catch (e: any) {
            alert(e?.message ?? "Failed to send request.");
        } finally {
            setWorking(false);
        }
    }

    async function handleAccept() {

        setWorking(true);
        try {
            const incoming = await (await import("../../stores/api/friendshipapi"))
                .apigetincoming();
            const row = incoming.find((r) => r.senderId === targetUserId);
            if (!row) {
                alert("Couldn't find that friend request.");
                return;
            }
            await apiacceptrequest(row.id);
            const fresh = await apigetrelationship(targetUserId);
            setRelationship(fresh);
        } catch (e: any) {
            alert(e?.message ?? "Failed to accept request.");
        } finally {
            setWorking(false);
        }
    }

    async function handleDecline() {
        setWorking(true);
        try {
            const incoming = await (await import("../../stores/api/friendshipapi"))
                .apigetincoming();
            const row = incoming.find((r) => r.senderId === targetUserId);
            if (!row) {
                alert("Couldn't find that friend request.");
                return;
            }
            await apideclinerequest(row.id);
            const fresh = await apigetrelationship(targetUserId);
            setRelationship(fresh);
        } catch (e: any) {
            alert(e?.message ?? "Failed to decline request.");
        } finally {
            setWorking(false);
        }
    }

    async function handleRemoveFriend() {
        if (!confirm("Remove this user from your friends?")) return;
        setWorking(true);
        try {
            await apiremovefriend(targetUserId);
            const fresh = await apigetrelationship(targetUserId);
            setRelationship(fresh);
        } catch (e: any) {
            alert(e?.message ?? "Failed to remove friend.");
        } finally {
            setWorking(false);
        }
    }

    async function handleUnblock() {
        setWorking(true);
        try {
            await apiunblockuser(targetUserId);
            const fresh = await apigetrelationship(targetUserId);
            setRelationship(fresh);
        } catch (e: any) {
            alert(e?.message ?? "Failed to unblock.");
        } finally {
            setWorking(false);
        }
    }

    if (relationship === null) {
        return (
            <Box sx={{ display: "inline-flex", alignItems: "center", gap: 1 }}>
                <CircularProgress size={18} />
            </Box>
        );
    }

    if (relationship.isSelf) return null;

    if (relationship.status === null) {
        return (
            <Button
                variant="contained"
                startIcon={<PersonAddIcon />}
                onClick={handleSendRequest}
                disabled={working}
            >
                Add Friend
            </Button>
        );
    }

    if (relationship.status === FriendshipStatus.Pending) {
        if (relationship.isCurrentUserSender) {
            return (
                <Button
                    variant="outlined"
                    startIcon={<HourglassEmptyIcon />}
                    onClick={handleRemoveFriend}
                    disabled={working}
                >
                    Request Sent — Cancel?
                </Button>
            );
        }

        return (
            <ButtonGroup variant="contained" disabled={working}>
                <Button
                    color="success"
                    startIcon={<CheckIcon />}
                    onClick={handleAccept}
                >
                    Accept
                </Button>
                <Button
                    color="error"
                    startIcon={<CloseIcon />}
                    onClick={handleDecline}
                >
                    Decline
                </Button>
            </ButtonGroup>
        );
    }

    if (relationship.status === FriendshipStatus.Accepted) {
        return (
            <Button
                variant="outlined"
                color="success"
                startIcon={<PeopleIcon />}
                onClick={handleRemoveFriend}
                disabled={working}
            >
                Friends
            </Button>
        );
    }

    if (relationship.status === FriendshipStatus.Blocked) {
        return (
            <Button
                variant="outlined"
                color="error"
                startIcon={<BlockIcon />}
                onClick={handleUnblock}
                disabled={working}
            >
                Blocked — Unblock
            </Button>
        );
    }

    return null;
}