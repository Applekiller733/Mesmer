import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../themes/themes";
import { Box, Button, Paper } from "@mui/material";
import Navbar from "../../reusablecomponents/navbar";
import "./profile.css";
import SmallProfile from "../../reusablecomponents/profile/smallprofile";
import FriendCodeChip from "../../reusablecomponents/profile/friendcodechip";
import ProfilePlaylists from "../../reusablecomponents/profile/profileplaylists";
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
                    {/*
                      Existing profile header — name + picture + friend
                      code + relationship buttons. Sits in the upper
                      portion of the paper. Each piece is wrapped in
                      `.profile-header-block` (see profile.css) so the
                      new playlists section below has a clean break.
                    */}
                    <Box className="profile-header-block">
                        <SmallProfile {...profile} id={paramid} />
                        {profile.friendCode && (
                            <Box sx={{ mt: 2 }}>
                                <FriendCodeChip
                                    code={profile.friendCode}
                                    label={isOwner ? "Your friend code" : "Friend code"}
                                />
                            </Box>
                        )}

                        <Box
                            sx={{
                                mt: 2,
                                display: "flex",
                                gap: 1,
                                alignItems: "center",
                                flexWrap: "wrap",
                            }}
                        >
                            {isOwner || isAdmin && (
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
                    </Box>

                    {/*
                      New: public playlists section. Belongs at the
                      bottom of the paper, taking full width regardless
                      of the existing grid layout above. The component
                      handles its own loading / empty / error state so
                      the parent doesn't have to.

                      Keyed by paramid so React fully unmounts and
                      remounts the section when the user navigates
                      between profiles in-place — avoids any chance
                      of stale state bleeding across users.
                    */}
                    {paramid && (
                        <Box className="profile-playlists-block">
                            <ProfilePlaylists
                                key={paramid}
                                targetUserId={paramid}
                            />
                        </Box>
                    )}
                </Paper>
            </Box>
        </ThemeProvider>
    );
}