import { useEffect, useRef, useState } from "react";
import {
    Box,
    InputBase,
    Paper,
    List,
    ListItemButton,
    Typography,
    CircularProgress,
    ClickAwayListener,
} from "@mui/material";
import SearchIcon from "@mui/icons-material/Search";
import { useNavigate } from "react-router";
import { apisearchusersbyusername } from "../../stores/api/userapi";
import { formatFriendCode } from "../../utils/helpers/friendshiphelpers";

const DEBOUNCE_MS = 300;

interface UserResult {
    id: string;
    userName: string;
    friendCode: string;
}

export default function UserSearch() {
    const [query, setQuery] = useState("");
    const [results, setResults] = useState<UserResult[]>([]);
    const [loading, setLoading] = useState(false);
    const [open, setOpen] = useState(false);

    const navigate = useNavigate();

    const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
    const requestSeq = useRef(0);

    useEffect(() => {
        if (debounceRef.current) {
            clearTimeout(debounceRef.current);
            debounceRef.current = null;
        }

        if (query.trim().length === 0) {
            setResults([]);
            setLoading(false);
            return;
        }

        setLoading(true);
        const mySeq = ++requestSeq.current;

        debounceRef.current = setTimeout(async () => {
            try {
                const data = await apisearchusersbyusername(query);
                if (mySeq === requestSeq.current) {
                    setResults(data ?? []);
                    setLoading(false);
                }
            } catch {
                if (mySeq === requestSeq.current) {
                    setResults([]);
                    setLoading(false);
                }
            }
        }, DEBOUNCE_MS);

        return () => {
            if (debounceRef.current) clearTimeout(debounceRef.current);
        };
    }, [query]);

    function handlePick(userId: string) {
        setOpen(false);
        setQuery("");
        setResults([]);
        navigate(`/profile/${userId}`);
    }

    return (
        <ClickAwayListener onClickAway={() => setOpen(false)}>
            <Box sx={{ position: "relative", width: 280 }}>
                <Paper
                    elevation={0}
                    sx={{
                        display: "flex",
                        alignItems: "center",
                        px: 1,
                        py: 0.25,
                        backgroundColor: "rgba(255,255,255,0.08)",
                        borderRadius: 2,
                    }}
                >
                    <SearchIcon sx={{ opacity: 0.7, mr: 1 }} />
                    <InputBase
                        // Placeholder hints both search modes — name and code.
                        placeholder="Search by name or code…"
                        value={query}
                        onFocus={() => setOpen(true)}
                        onChange={(e) => {
                            setQuery(e.target.value);
                            setOpen(true);
                        }}
                        sx={{ flex: 1, color: "inherit" }}
                    />
                    {loading && <CircularProgress size={16} sx={{ ml: 1 }} />}
                </Paper>

                {open && query.trim().length > 0 && (
                    <Paper
                        sx={{
                            position: "absolute",
                            top: "calc(100% + 4px)",
                            left: 0,
                            right: 0,
                            zIndex: 10,
                            maxHeight: 360,
                            overflowY: "auto",
                        }}
                    >
                        {results.length === 0 && !loading && (
                            <Box sx={{ p: 2, opacity: 0.7 }}>
                                <Typography variant="body2">
                                    No users match "{query}".
                                </Typography>
                            </Box>
                        )}
                        <List dense disablePadding>
                            {results.map((u) => (
                                <ListItemButton
                                    key={u.id}
                                    onClick={() => handlePick(u.id)}
                                >
                                    <Box>
                                        <Typography variant="body1">
                                            {u.userName}
                                        </Typography>
                                        <Typography
                                            variant="caption"
                                            sx={{
                                                opacity: 0.6,
                                                fontFamily: "monospace",
                                            }}
                                        >
                                            {formatFriendCode(u.friendCode)}
                                        </Typography>
                                    </Box>
                                </ListItemButton>
                            ))}
                        </List>
                    </Paper>
                )}
            </Box>
        </ClickAwayListener>
    );
}