import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../themes/themes";
import { Box, Button, Paper } from "@mui/material";
import Navbar from "../../reusablecomponents/navbar";
import AdminUserGrid from "./admingrid";
import type { User } from "../../models/user";
import { useSelector } from "react-redux";
import { selectCurrentUser } from "../../stores/slices/userdataslice";
import { useAppDispatch } from "../../hooks/hooks";
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
                        <AdminUserGrid></AdminUserGrid>
                        <Button href="/song-upload">Upload Song</Button>
                    </Paper>
                </Box>
                :
                <Navigate to={'/'}></Navigate>
            }
        </ThemeProvider>
    );
}
