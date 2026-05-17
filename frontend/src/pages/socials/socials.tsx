import { useState } from "react";
import { Box, Paper, Tabs, Tab, Badge, Typography } from "@mui/material";
import { ThemeProvider } from "@emotion/react";
import { useSelector } from "react-redux";

import { darkTheme } from "../../themes/themes";
import Navbar from "../../reusablecomponents/navbar";
import {
    selectIncomingCount as selectIncomingFriendCount,
} from "../../stores/slices/friendshipslice";
import {
    selectIncomingInvitationsCount,
} from "../../stores/slices/playlistinvitationslice";
import SocialsFriends from "./friends/socialsfriends";
import SocialsPlaylists from "./playlists/socialsplaylists";
import "./socials.css";

type SocialsTab = "friends" | "playlists";

/**
 * Top-level Socials page. Two main tabs:
 *   - Friends — relationships (existing 4-sub-tab UI from old
 *     /friends page, lifted into SocialsFriends).
 *   - Playlists — playlist invitations (new in this step, in
 *     SocialsPlaylists). Two sub-tabs: Incoming, Outgoing.
 *
 * Replaces the old /friends route. The previous Friends page is
 * now SocialsFriends, mounted under the Friends top tab here.
 *
 * Each top tab carries an aggregate badge for its section so a user
 * can tell at a glance which area has pending items waiting.
 */
export default function SocialsPage() {
    const [tab, setTab] = useState<SocialsTab>("friends");

    const incomingFriendCount = useSelector(selectIncomingFriendCount);
    const incomingInvitationCount = useSelector(selectIncomingInvitationsCount);

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="socials-background">
                <Navbar />
                <Paper className="socials-paper">
                    <Typography variant="h4" sx={{ mb: 2 }}>
                        Socials
                    </Typography>

                    <Tabs
                        value={tab}
                        onChange={(_, v) => setTab(v)}
                        textColor="inherit"
                    >
                        <Tab
                            value="friends"
                            label={
                                <Badge
                                    color="error"
                                    badgeContent={incomingFriendCount}
                                    invisible={incomingFriendCount === 0}
                                >
                                    <span style={{ paddingRight: 8 }}>Friends</span>
                                </Badge>
                            }
                        />
                        <Tab
                            value="playlists"
                            label={
                                <Badge
                                    color="error"
                                    badgeContent={incomingInvitationCount}
                                    invisible={incomingInvitationCount === 0}
                                >
                                    <span style={{ paddingRight: 8 }}>Playlists</span>
                                </Badge>
                            }
                        />
                    </Tabs>

                    <Box sx={{ mt: 2 }}>
                        {tab === "friends" && <SocialsFriends />}
                        {tab === "playlists" && <SocialsPlaylists />}
                    </Box>
                </Paper>
            </Box>
        </ThemeProvider>
    );
}