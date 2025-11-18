import { ThemeProvider } from "@emotion/react";
import { darkTheme } from "../../../themes/themes";
import { Box, Paper, Typography } from "@mui/material";
import "./signup.css";

export function SignupSuccess() {

    return (
        <ThemeProvider theme={darkTheme}>
            <Box className="signup-page">
                <Paper className="signup-card">
                    <Typography>You have successfully signed up. Please check your email and verify your account.</Typography>
                </Paper>
            </Box>
        </ThemeProvider>
    )
}