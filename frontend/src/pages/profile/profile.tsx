import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../themes/themes";
import { Box, Button, Paper } from "@mui/material";
import Navbar from "../../reusablecomponents/navbar";
import "./profile.css";
import SmallProfile from "../../reusablecomponents/profile/smallprofile";
import { useAppDispatch } from "../../hooks/hooks";
import { useEffect } from "react";
import { selectCurrentUser, selectLoadedProfile } from "../../stores/slices/userdataslice";
import { useParams } from "react-router";
import { useSelector } from "react-redux";
import { fetchUserProfile } from "../../stores/thunks/userthunks";

export default function Profile(){
    const params = useParams();
    const dispatch = useAppDispatch();
    const currentuser = useSelector(selectCurrentUser);
    const profile = useSelector(selectLoadedProfile);
    const paramid = (params.id as unknown) as number;
    const isOwner = paramid == currentuser.id || currentuser.role.match('Admin');
 
    useEffect(() => {
        dispatch(fetchUserProfile(paramid));
    }, [])

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="profile-background">
                <Navbar></Navbar>
                <Paper className="profile-paper">
                    <SmallProfile {...profile} id={paramid}></SmallProfile>
                    {/* <FileUploader></FileUploader> */}
                    {
                        isOwner && <Button href={`/profile/${paramid}/edit`} color="inherit">Edit Profile</Button>
                    }
                </Paper>
            </Box>
        </ThemeProvider>
    );
}