import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../themes/themes";
import { Box, Button, Paper, Typography } from "@mui/material";
import Navbar from "../../reusablecomponents/navbar";
import AdminUserGrid from "../../reusablecomponents/admin/admingrid";
import AdminSongGrid from "../../reusablecomponents/admin/adminsonggrid";
import type { User } from "../../models/user";
import { useSelector } from "react-redux";
import { selectCurrentUser } from "../../stores/slices/userdataslice";
import { useEffect, useState } from "react";
import { Navigate } from "react-router";
import './admin.css';


export default function AdminDashboard() {
    const currentuser: User = useSelector(selectCurrentUser);
    const [isAdmin, setIsAdmin] = useState(true);

    useEffect(() => {
        setIsAdmin(currentuser.role === 'Admin' || currentuser.role === 'admin');
    }, [currentuser])

    return (
        <ThemeProvider theme={darkTheme}>
            {
                isAdmin ?
                <Box className="admin">
                    <Navbar></Navbar>

                    <Paper className="user-list-paper">
                        <Typography variant="h5" sx={{ p: 2 }}>Users</Typography>
                        <AdminUserGrid></AdminUserGrid>
                    </Paper>

                    <Paper className="user-list-paper" sx={{ mt: 3 }}>
                        <Typography variant="h5" sx={{ p: 2 }}>Songs</Typography>
                        <AdminSongGrid></AdminSongGrid>
                        <Box sx={{ p: 2 }}>
                            <Button href="/song-upload">Upload Song</Button>
                        </Box>
                    </Paper>
                </Box>
                :
                <Navigate to={'/'}></Navigate>
            }
        </ThemeProvider>
    );
}