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
        localStorage.removeItem("mustChangePassword");
        window.location.href = "/login";
    })();

    return logoutPromise;
}

export function getCurrentUserRole() {
    const token = localStorage.getItem("accessToken");
    if (!token) return null;

    try {
        const payload = JSON.parse(atob(token.split(".")[1]));
        return payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ?? null;
    } catch {
        return null;
    }
}

export function isAdmin() {
    return getCurrentUserRole() === "Admin";
}

// BadRequest(string) en el backend devuelve Content-Type: text/plain, pero
// BadRequest(new ProblemDetails{...}) (y los fallos automáticos de validación
// de DataAnnotations) devuelven application/problem+json con .title — no
// "application/json" a secas, por eso se chequea "json" en general.
async function parseErrorMessage(res) {
    try {
        const contentType = res.headers.get("content-type") || "";
        if (contentType.includes("json")) {
            const body = await res.clone().json();
            return typeof body === "string" ? body : body?.title;
        }
        const text = await res.clone().text();
        return text || undefined;
    } catch {
        return undefined;
    }
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

    if (!res.ok) {
        res.errorMessage = await parseErrorMessage(res);
    }

    return res;
}
