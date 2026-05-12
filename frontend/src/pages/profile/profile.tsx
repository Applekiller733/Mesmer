import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../themes/themes";
import { Box, Button, Paper } from "@mui/material";
import Navbar from "../../reusablecomponents/navbar";
import "./profile.css";
import SmallProfile from "../../reusablecomponents/profile/smallprofile";
import { useAppDispatch } from "../../hooks/hooks";
import { useEffect } from "react";
import {
    selectCurrentUser,
    selectLoadedProfile,
} from "../../stores/slices/userdataslice";
import { useParams } from "react-router";
import { useSelector } from "react-redux";
import { fetchUserProfile } from "../../stores/thunks/userthunks";
import FriendshipButton from "../../reusablecomponents/profile/friendshipbutton";

export default function Profile() {
    const params = useParams();
    const dispatch = useAppDispatch();
    const currentuser = useSelector(selectCurrentUser);
    const profile = useSelector(selectLoadedProfile);
    const paramid = params.id ?? "";
    const isOwner = paramid == currentuser.id;
    const isAdmin = currentuser.role.match("Admin");

    useEffect(() => {
        dispatch(fetchUserProfile(paramid));
    }, [paramid, dispatch]);

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="profile-background">
                <Navbar />
                <Paper className="profile-paper">
                    <SmallProfile {...profile} id={paramid} />
                    <Box
                        sx={{
                            mt: 2,
                            display: "flex",
                            gap: 1,
                            alignItems: "center",
                            flexWrap: "wrap",
                        }}
                    >
                        {isOwner && (
                            <Button
                                href={`/profile/${paramid}/edit`}
                                color="inherit"
                                variant="outlined"
                            >
                                Edit Profile
                            </Button>
                        )}
                        {isAdmin && !isOwner && (
                            <Button
                            href={`/profile/${paramid}/edit`}
                            color="inherit"
                            variant="outlined"
                        >
                            Edit Profile
                        </Button>
                        )}
                        {!isOwner && paramid && (
                            <FriendshipButton targetUserId={paramid} />
                        )}
                    </Box>
                </Paper>
            </Box>
        </ThemeProvider>
    );
}