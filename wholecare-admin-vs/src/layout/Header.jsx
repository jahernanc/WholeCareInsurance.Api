import { useState } from "react";

function Header() {
    const [open, setOpen] = useState(false);

    // ✅ Función dentro del componente
    function getInitials() {
        const token = localStorage.getItem("accessToken");

        if (!token) return "??";

        try {
            const payload = JSON.parse(atob(token.split(".")[1]));

            const name =
                payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/givenname"];

            if (!name) return "??";

            return name
                .split(" ")
                .map((n) => n[0])
                .join("")
                .toUpperCase();
        } catch {
            return "??";
        }
    }

    const handleLogout = () => {
        localStorage.removeItem("accessToken");
        localStorage.removeItem("refreshToken");
        window.location.href = "/login";
    };

    return (
        <div
            style={{
                height: 60,
                borderBottom: "1px solid #ddd",
                display: "flex",
                alignItems: "center",
                padding: "0 20px",
                justifyContent: "space-between",
            }}
        >
            {/* Logo */}
            <div style={{ fontWeight: "bold" }}>WholeCare</div>

            {/* Usuario */}
            <div style={{ position: "relative" }}>
                <button onClick={() => setOpen(!open)}>
                    {getInitials()} ▼
                </button>

                {open && (
                    <div
                        style={{
                            position: "absolute",
                            right: 0,
                            top: 40,
                            background: "white",
                            border: "1px solid #ccc",
                            borderRadius: 8,
                            padding: 10,
                        }}
                    >
                        <div style={{ marginBottom: 10 }}>Profile</div>
                        <div style={{ marginBottom: 10 }}>Help</div>
                        <div
                            onClick={handleLogout}
                            style={{ cursor: "pointer", color: "red" }}
                        >
                            Logout
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}

export default Header;
