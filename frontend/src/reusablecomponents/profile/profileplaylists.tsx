import {
    Box,
    CircularProgress,
    Divider,
    Typography,
} from "@mui/material";
import { useEffect, useMemo, useState } from "react";
import { useSelector } from "react-redux";

import type { Playlist } from "../../models/playlist";
import { PlaylistVisibility } from "../../models/playlist";
import { apifetchplaylistscreatedbyaccount } from "../../stores/api/playlistapi";
import { apisaveplaylist } from "../../stores/api/playlistapi";
import { useAppDispatch } from "../../hooks/hooks";
import { selectCurrentUser } from "../../stores/slices/userdataslice";
import {
    selectSavedPlaylists,
} from "../../stores/slices/playlistdataslice";
import { fetchPlaylistsSavedByAccountId } from "../../stores/thunks/playlistthunks";
import ProfilePlaylistItem from "./profileplaylistitem";
import ShareDialog from "../library/sharedialog/sharedialog";

/**
 * The playlists section on a profile page.
 *
 * What it shows:
 *   - Other user's profile: Public playlists only. Unlisted are
 *     deliberately excluded from profile views (per the backend's
 *     GetCreatedByAccount filtering for non-self viewers; this is
 *     belt-and-braces on the client too in case the backend
 *     contract drifts).
 *   - Own profile: same — only Public, because the profile IS the
 *     public face. Private/Unlisted live in the library. This is a
 *     deliberate UX choice rather than a backend constraint: the
 *     backend would return all visibilities for a self-view here.
 *
 * State:
 *   - `playlists` (local): the fetched list. Local rather than slice
 *     state because this is a per-profile read; the slice already
 *     covers "playlists I created" via fetchPlaylistsCreatedByAccountId
 *     for the current user, but a stranger's playlists don't belong
 *     in that slice and would conflict if cross-loaded.
 *   - `savedIds` (derived from slice): the current user's saved
 *     library, narrowed to a set of ids for O(1) "is this saved?"
 *     checks per card.
 *   - `savingIds` (local): per-card in-flight flags for the save
 *     button. Prevents double-clicks; allows independent per-card
 *     loading states.
 *   - `shareTarget` (local): the playlist currently shown in the
 *     share dialog (or null if closed).
 */
export default function ProfilePlaylists({
    targetUserId,
}: {
    targetUserId: string;
}) {
    const dispatch = useAppDispatch();
    const currentUser = useSelector(selectCurrentUser);
    const savedPlaylists = useSelector(selectSavedPlaylists);

    const isOwner = !!currentUser.id && currentUser.id === targetUserId;

    const [playlists, setPlaylists] = useState<Playlist[] | null>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const [savingIds, setSavingIds] = useState<Set<string>>(new Set());
    const [shareTarget, setShareTarget] = useState<Playlist | null>(null);

    // Fast lookup for the Save button. Re-derived only when the saved
    // list changes — typical case is "doesn't change at all while the
    // profile is open", but if the user accepts an invitation in
    // another tab, this would pick it up on next slice refresh.
    const savedIds = useMemo(
        () => new Set(savedPlaylists.map((p) => p.id)),
        [savedPlaylists],
    );

    // Fetch on mount and whenever the target user changes (e.g., the
    // user navigates between profiles in-app without unmounting).
    useEffect(() => {
        if (!targetUserId) return;

        let cancelled = false;
        (async () => {
            setLoading(true);
            setError(null);
            try {
                const list = await apifetchplaylistscreatedbyaccount(targetUserId);
                if (cancelled) return;
                // Belt-and-braces filter: the backend already excludes
                // non-Public playlists for non-self viewers, but for
                // self-view it returns everything. We deliberately
                // narrow to Public on the profile for both cases —
                // see component-level doc for why.
                const publicOnly = (list as Playlist[]).filter(
                    (p) => p.visibility === PlaylistVisibility.Public,
                );
                setPlaylists(publicOnly);
            } catch (e: any) {
                if (!cancelled) {
                    setError(e?.message ?? "Failed to load playlists.");
                    setPlaylists([]);
                }
            } finally {
                if (!cancelled) setLoading(false);
            }
        })();

        return () => {
            cancelled = true;
        };
    }, [targetUserId]);

    async function handleSave(playlistId: string) {
        if (!currentUser.id) return;
        setSavingIds((prev) => {
            const next = new Set(prev);
            next.add(playlistId);
            return next;
        });
        try {
            await apisaveplaylist(playlistId);
            // Refresh the saved-playlists slice so this card flips to
            // "Saved" and any other view of the user's library
            // updates too. Cheaper than dispatching addToSaved
            // manually — covers concurrent updates we might miss.
            dispatch(fetchPlaylistsSavedByAccountId(currentUser.id));
        } catch (e: any) {
            alert(e?.message ?? "Save failed.");
        } finally {
            setSavingIds((prev) => {
                const next = new Set(prev);
                next.delete(playlistId);
                return next;
            });
        }
    }

    if (loading) {
        return (
            <Box sx={{ display: "flex", justifyContent: "center", py: 3 }}>
                <CircularProgress size={24} />
            </Box>
        );
    }

    if (error) {
        return (
            <Typography sx={{ opacity: 0.6, textAlign: "center", py: 2 }}>
                {error}
            </Typography>
        );
    }

    if (!playlists || playlists.length === 0) {
        return (
            <Typography sx={{ opacity: 0.6, textAlign: "center", py: 2 }}>
                {isOwner
                    ? "You have no public playlists yet."
                    : "This user has no public playlists yet."}
            </Typography>
        );
    }

    return (
        <Box>
            <Typography variant="h6" sx={{ mb: 1 }}>
                Public Playlists
            </Typography>
            <Divider sx={{ mb: 1.5 }} />

            {playlists.map((p) => (
                <ProfilePlaylistItem
                    key={p.id}
                    playlist={p}
                    isOwner={isOwner}
                    isSaved={savedIds.has(p.id)}
                    saving={savingIds.has(p.id)}
                    onSave={handleSave}
                    onShare={(pl) => setShareTarget(pl)}
                />
            ))}

            {/*
              Single ShareDialog instance, anchored by `shareTarget`.
              Mounting one per card would be wasteful and would also
              break the dialog's "open" timing (each card's open
              state would fight for focus). The dialog handles its
              own fetch on open.
            */}
            <ShareDialog
                open={shareTarget !== null}
                playlistId={shareTarget?.id ?? ""}
                playlistName={shareTarget?.name ?? ""}
                onClose={() => setShareTarget(null)}
            />
        </Box>
    );
}