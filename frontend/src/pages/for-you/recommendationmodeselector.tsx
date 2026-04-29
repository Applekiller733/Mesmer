import { useEffect } from "react";
import {
    FormControl,
    InputLabel,
    MenuItem,
    Select,
    type SelectChangeEvent,
    Box,
    CircularProgress,
    Typography,
} from "@mui/material";
import { useSelector } from "react-redux";

import { useAppDispatch } from "../../hooks/hooks";
import { selectCurrentUser } from "../../stores/slices/userdataslice";
import { selectSavedPlaylists } from "../../stores/slices/playlistdataslice";
import { fetchPlaylistsSavedByAccountId } from "../../stores/thunks/playlistthunks";
import {
    selectRecommendationMode,
    selectRecommendationStatus,
    setModeAll,
} from "../../stores/slices/recommendationmodeslice";
import { setModePlaylist } from "../../stores/thunks/recommendationthunks";

const ALL_VALUE = "__all__";

export default function RecommendationModeSelector() {
    const dispatch = useAppDispatch();
    const user = useSelector(selectCurrentUser);
    const playlists = useSelector(selectSavedPlaylists);
    const mode = useSelector(selectRecommendationMode);
    const status = useSelector(selectRecommendationStatus);

    useEffect(() => {
        if (user.id) dispatch(fetchPlaylistsSavedByAccountId(user.id));
    }, [user.id, dispatch]);

    const selectValue =
        mode.kind === "all" ? ALL_VALUE : mode.playlistId;

        function handleChange(e: SelectChangeEvent<string>) {
            const value = e.target.value;
            console.log("[selector] handleChange fired, value:", value);
            if (value === ALL_VALUE) {
                console.log("[selector] dispatching setModeAll");
                dispatch(setModeAll());
            } else {
                console.log("[selector] dispatching setModePlaylist with", value);
                dispatch(setModePlaylist(value));
            }
        }

    return (
        <Box sx={{ mb: 2, display: "flex", alignItems: "center", gap: 2 }}>
            <FormControl size="small" sx={{ minWidth: 240 }}>
                <InputLabel id="rec-mode-label">Recommend from</InputLabel>
                <Select
                    labelId="rec-mode-label"
                    label="Recommend from"
                    value={selectValue}
                    onChange={handleChange}
                >
                    <MenuItem value={ALL_VALUE}>None (all songs)</MenuItem>
                    {playlists.map((p) => (
                        <MenuItem key={p.id} value={p.id}>
                            {p.name}
                        </MenuItem>
                    ))}
                </Select>
            </FormControl>

            {status === "loading" && (
                <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                    <CircularProgress size={18} />
                    <Typography variant="body2" sx={{ opacity: 0.7 }}>
                        Loading recommendations…
                    </Typography>
                </Box>
            )}
            {status === "error" && (
                <Typography variant="body2" color="error">
                    Couldn't load recommendations.
                </Typography>
            )}
        </Box>
    );
}