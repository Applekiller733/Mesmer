import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../../themes/themes";
import { Box, Paper, Typography } from "@mui/material";
import { Link, useParams } from "react-router";
import "../signup/signup.css";
import { useEffect } from "react";
import { useAppDispatch } from "../../../hooks/hooks";
import { verify } from "../../../stores/thunks/userthunks";

export function VerifyEmail() {
    const { token } = useParams();
    const dispatch = useAppDispatch();

    useEffect(() => {
        // dispatch to verify mail thunk?
        if (token ){
            dispatch(verify({
                token: token
            }));
        }
    }, [])

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="signup-page">
                <Paper className="signup-card">
                    <Typography>Your email has been successfully verified. Go back to <Link to={'../'}>Home</Link></Typography>
                </Paper>
            </Box>
        </ThemeProvider>
    )
}