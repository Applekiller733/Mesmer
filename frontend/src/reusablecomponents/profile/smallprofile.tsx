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