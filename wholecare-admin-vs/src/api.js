const BASE_URL = import.meta.env.VITE_API_URL;

let refreshPromise = null;

async function refreshTokens() {
    if (refreshPromise) return refreshPromise;

    refreshPromise = (async () => {
        const refreshToken = localStorage.getItem("refreshToken");
        if (!refreshToken) throw new Error("No refresh token");

        const res = await fetch(`${BASE_URL}/auth/refresh`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ refreshToken }),
        });

        if (!res.ok) throw new Error("Refresh failed");

        const data = await res.json();
        localStorage.setItem("accessToken", data.accessToken);
        localStorage.setItem("refreshToken", data.refreshToken);
        return data.accessToken;
    })();

    try {
        return await refreshPromise;
    } finally {
        refreshPromise = null;
    }
}

let logoutPromise = null;

export async function logout() {
    if (logoutPromise) return logoutPromise;

    logoutPromise = (async () => {
        const accessToken = localStorage.getItem("accessToken");

        if (accessToken) {
            try {
                await fetch(`${BASE_URL}/auth/logout`, {
                    method: "POST",
                    headers: { Authorization: "Bearer " + accessToken },
                });
            } catch {
                // best-effort: still clear local session even if the request fails
            }
        }

        localStorage.removeItem("accessToken");
        localStorage.removeItem("refreshToken");
        window.location.href = "/login";
    })();

    return logoutPromise;
}

export async function apiFetch(path, options = {}) {
    const doFetch = (accessToken) =>
        fetch(`${BASE_URL}${path}`, {
            ...options,
            headers: {
                ...(options.body && !(options.body instanceof FormData) ? { "Content-Type": "application/json" } : {}),
                ...options.headers,
                ...(accessToken ? { Authorization: "Bearer " + accessToken } : {}),
            },
        });

    let res = await doFetch(localStorage.getItem("accessToken"));

    if (res.status === 401 && localStorage.getItem("refreshToken")) {
        try {
            const newToken = await refreshTokens();
            res = await doFetch(newToken);
        } catch {
            await logout();
            throw new Error("Session expired");
        }
    }

    return res;
}
