import { Typography } from "@mui/material";
import "./smallprofile.css";
import type { UserProfile } from "../../models/user";
import { useAppDispatch } from "../../hooks/hooks";
import { useEffect, useState } from "react";
import { getProfilePicture } from "../../stores/thunks/userthunks";

export interface SmallProfileProps extends UserProfile {
    id: string;
}

const PLACEHOLDER_SRC = "/default-profile.jpg";

export default function SmallProfile({
    username,
    role,
    createdAt,
    updatedAt,
    id,
}: SmallProfileProps) {
    const dispatch = useAppDispatch();
    const [imgUrl, setImgUrl] = useState<string | null>(null);

    useEffect(() => {
        // Track whether this effect run is still the "current" one. If the
        // user navigates to a different profile mid-fetch, the in-flight
        // request must not write to state — otherwise we'd flash the wrong
        // image. This is the standard async-effect cancellation pattern.
        let cancelled = false;
        let createdUrl: string | null = null;

        async function fetchImage() {
            const action = await dispatch(getProfilePicture(id));
            if (cancelled) return;

            if (action.meta.requestStatus === "fulfilled" && action.payload instanceof Blob) {
                const url = URL.createObjectURL(action.payload);
                createdUrl = url;
                setImgUrl(url);
            } else {
                // Rejected (404, network error, etc.) — keep imgUrl null
                // so the placeholder shows.
                setImgUrl(null);
            }
        }

        fetchImage();

        // Cleanup runs on unmount AND on id change. The captured createdUrl
        // is whatever this effect run created; revoking it releases the
        // blob memory. Future-self note: this is the cleanup that was
        // broken before — being inside the async function instead of the
        // effect itself meant React never received it as a cleanup fn.
        return () => {
            cancelled = true;
            if (createdUrl) URL.revokeObjectURL(createdUrl);
        };
    }, [id, dispatch]);

    return (
        <div className="smallprofile">
            <img
                src={imgUrl ?? PLACEHOLDER_SRC}
                className="profile-picture"
                alt={`${username} profile`}
                // Belt-and-braces: if for any reason the blob URL fails to
                // render (corrupt blob, revoked too early, etc.), fall back
                // to the placeholder rather than showing the broken-image
                // icon. This is the last line of defence against the bug
                // you just reported.
                onError={(e) => {
                    if (e.currentTarget.src !== window.location.origin + PLACEHOLDER_SRC) {
                        e.currentTarget.src = PLACEHOLDER_SRC;
                    }
                }}
            />
            <Typography variant="h5">{username}</Typography>
            <Typography variant="h6">{role}</Typography>
            <Typography>Created at: {createdAt}</Typography>
            {updatedAt && <Typography>Last Updated at: {updatedAt}</Typography>}
        </div>
    );
}