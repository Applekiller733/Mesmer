import type { User } from "../../models/user";
import { apirefreshToken } from "../../stores/api/userapi";
import { updateCurrentUser } from "../../stores/slices/userdataslice";
import { getAppDispatch } from "../../stores/storedispatch";

const STORAGE_KEY = 'currentuser';

// storage helpers

interface StoredUser {
    id: string;
    userName?: string;
    email?: string;
    role?: string;
    jwtToken: string;
    refreshToken?: string;
}

function readStoredUser(): StoredUser | null {
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        if (!raw) return null;
        const parsed = JSON.parse(raw);
        if (!parsed || typeof parsed.jwtToken !== 'string') return null;
        return parsed as StoredUser;
    } catch {
        // corrupt JSON in storage, wipe so we don't keep failing.
        localStorage.removeItem(STORAGE_KEY);
        return null;
    }
}

function writeStoredUser(user: StoredUser): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
}

// Decode the payload of a JWT without verifying. The signature we couldn't
// verify in the browser anyway — we only use `exp` to schedule a refresh.
function decodeJwtPayload(token: string): { exp?: number } | null {
    try {
        const parts = token.split('.');
        if (parts.length !== 3) return null;
        // atob doesn't handle URL-safe base64; convert before decoding.
        const b64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
        return JSON.parse(atob(b64));
    } catch {
        return null;
    }
}

// state helpers

export function updateCurrentUserHelper(user: any) {
    const dispatch = getAppDispatch();

    writeStoredUser(user);

    // sync the slice
    dispatch(updateCurrentUser(user));
}

export function deleteCurrentUserHelper() {
    const dispatch = getAppDispatch();
    localStorage.removeItem(STORAGE_KEY);
    const emptyuser: User = {
        id: undefined,
        username: '',
        email: '',
        role: '',
        token: '',
    };
    dispatch(updateCurrentUser(emptyuser));
}

export function forceLogout() {
    stopRefreshTokenTimer();
    deleteCurrentUserHelper();
}

// refresh timer

let refreshTokenTimeout: ReturnType<typeof setTimeout> | null = null;

/**
 * Schedules a refresh ~1 minute before the current JWT expires. Idempotent:
 * calling twice cancels the previous timer.
 *
 * Behaviors worth knowing:
 *   - If the stored JWT is already expired (or expires within the next
 *     minute), we attempt a refresh immediately rather than scheduling a
 *     no-op timer.
 *   - If no user is stored, this is a no-op. Safe to call from app bootstrap.
 *   - If the refresh fails, we force-logout. No silent retry loops — the
 *     previous version's retry logic was both wrong AND hid the symptom.
 */
export function startRefreshTokenTimer() {
    stopRefreshTokenTimer();

    const user = readStoredUser();
    if (!user) return;

    const payload = decodeJwtPayload(user.jwtToken);
    if (!payload?.exp) {
        // token is structurally invalid — can't schedule
        // try a refresh immediately, the refresh cookie may still be valid
        attemptRefreshNow();
        return;
    }

    const expiresAt = payload.exp * 1000;
    const refreshAt = expiresAt - 60 * 1000; // 1 minute before expiry
    const delay = refreshAt - Date.now();

    if (delay <= 0) {
        // already expired or expiring imminently — refresh now.
        attemptRefreshNow();
        return;
    }

    refreshTokenTimeout = setTimeout(attemptRefreshNow, delay);
}

export function stopRefreshTokenTimer() {
    if (refreshTokenTimeout !== null) {
        clearTimeout(refreshTokenTimeout);
        refreshTokenTimeout = null;
    }
}

async function attemptRefreshNow() {
    const ok = await apirefreshToken();
    if (ok) {
        // apirefreshToken already wrote new state and called updateCurrentUserHelper.
        // Schedule the next refresh based on the new token's exp.
        startRefreshTokenTimer();
    } else {
        // Refresh cookie expired, revoked, or backend unreachable.
        // Don't strand the user — clear local state so they can log in again.
        forceLogout();
    }
}