import axios from "axios";
import authHeader from "./apihelper";

const API_URL = `${import.meta.env.VITE_API_URL}/files`;

// Matches the backend FileCategory enum (serialized as strings).
export type FileCategory = "Image" | "Audio" | "Video";

export interface PresignedUpload {
    key: string;
    url: string;
    expiresAtUtc: string;
}

// Legacy multipart upload straight through the API. Fine for small files, but
// large media (audio/video) must use the presigned flow below to avoid the
// API Gateway payload limit.
export function apiuploadfile(request: FormData) {
    const url = `${API_URL}`;
    return axios.post(url, request);
}

// Step 1: ask the API for a short-lived presigned PUT URL.
export async function apipresignupload(
    category: FileCategory,
    fileName: string,
    keyPrefix?: string
): Promise<PresignedUpload> {
    const url = `${API_URL}/presign-upload`;
    const response = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", ...authHeader(url) },
        body: JSON.stringify({ category, fileName, keyPrefix }),
    });
    const data = await response.json();
    if (!response.ok) {
        throw new Error(data.message || "Failed to get an upload URL");
    }
    return data;
}

// Step 2: upload the bytes straight to S3. No app auth header here — the
// presigned URL carries its own authorization, and forwarding a Bearer token
// to S3 would be rejected.
export async function apiputtos3(uploadUrl: string, file: File): Promise<void> {
    const response = await fetch(uploadUrl, {
        method: "PUT",
        headers: { "Content-Type": file.type || "application/octet-stream" },
        body: file,
    });
    if (!response.ok) {
        throw new Error(`Direct upload to storage failed (${response.status})`);
    }
}

// Step 3: tell the API the upload finished; it verifies + records the file and
// returns the new file id.
export async function apiconfirmupload(
    key: string,
    fileName: string,
    category: FileCategory
): Promise<string> {
    const url = `${API_URL}/confirm-upload`;
    const response = await fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json", ...authHeader(url) },
        body: JSON.stringify({ key, fileName, category }),
    });
    // On success the API returns the new file id as a bare string, which
    // ASP.NET serializes as text/plain (not JSON) — so read it as text.
    // Error responses are JSON ({ message }); parse those for the message.
    const body = await response.text();
    if (!response.ok) {
        let message = "Failed to confirm upload";
        try {
            message = JSON.parse(body).message || message;
        } catch {
            // Non-JSON error body; fall back to the default message.
        }
        throw new Error(message);
    }
    return body.trim(); // the file id (string)
}

// Convenience: run the full presign -> PUT -> confirm dance and return the file
// id, which is then attached to a song / profile instead of the raw bytes.
export async function uploadFileToS3(
    file: File,
    category: FileCategory,
    keyPrefix?: string
): Promise<string> {
    const presigned = await apipresignupload(category, file.name, keyPrefix);
    await apiputtos3(presigned.url, file);
    return await apiconfirmupload(presigned.key, file.name, category);
}
