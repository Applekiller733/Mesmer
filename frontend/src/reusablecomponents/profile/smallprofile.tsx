import { CircularProgress, TablePagination, Typography } from "@mui/material";
import "./smallprofile.css";
import type { UserProfile } from "../../models/user";
import { useAppDispatch } from "../../hooks/hooks";
import { useEffect, useState } from "react";
import { getProfilePicture } from "../../stores/thunks/userthunks";


export interface SmallProfileProps extends UserProfile {
    id: number,
}

export default function SmallProfile({ username, role, createdAt, updatedAt, id }: SmallProfileProps) {
    const dispatch = useAppDispatch();
    const [imgStatus, setImgStatus] = useState('init');
    const [imgUrl, setImgUrl] = useState('');


    useEffect(() => {
        setImgStatus('loading');
        const fetchImage = async () => {
            const response = await dispatch(getProfilePicture(id));

            // console.log(response.payload);
            const url = URL.createObjectURL(response.payload);
            // console.log(url);
            setImgUrl(url);
            setImgStatus('successful');

            return () => URL.revokeObjectURL(url);
        };
        fetchImage();
    }, [])

    return (
        <div className="smallprofile">
            {imgUrl === '' ?
                imgStatus === 'loading' ?
                    <img src="/default-profile.jpg" className="profile-picture"></img>
                    :
                    <CircularProgress></CircularProgress>
                :
                <img src={`${imgUrl}`} className="profile-picture"></img>
            }
            <Typography variant="h5">{username}</Typography>
            <Typography variant="h6">{role}</Typography>
            <Typography>Created at: {createdAt}</Typography>
            {
                updatedAt &&
                <Typography>Last Updated at: {updatedAt}</Typography>
            }
        </div>
    )
}