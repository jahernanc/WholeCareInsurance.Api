import { useState } from "react";
import { useTranslation } from "react-i18next";
import { logout, apiFetch } from "../api";

const currentYear = new Date().getFullYear();
// Año actual hasta 5 años atrás (6 opciones en total).
const PERIOD_OPTIONS = Array.from({ length: 6 }, (_, i) => currentYear - i);

function Header({ period, onPeriodChange }) {
    const { t, i18n } = useTranslation("common");
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

    const handleLogout = async () => {
        await logout();
    };

    const handleLanguageChange = async (lang) => {
        i18n.changeLanguage(lang);
        localStorage.setItem("preferredLanguage", lang);

        try {
            await apiFetch("/users/me/language", {
                method: "PUT",
                body: JSON.stringify({ language: lang }),
            });
        } catch {
            // best-effort: la UI ya refleja el cambio localmente aunque falle el guardado
        }
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
            <div style={{ fontWeight: "bold" }}>{t("header.appName")}</div>

            <div style={{ display: "flex", alignItems: "center", gap: 14 }}>
                <select
                    value={period}
                    onChange={(e) => onPeriodChange(Number(e.target.value))}
                    title={t("header.period")}
                    style={{ padding: "4px 6px", borderRadius: 4, border: "1px solid #ccc" }}
                >
                    {PERIOD_OPTIONS.map((year) => (
                        <option key={year} value={year}>{year}</option>
                    ))}
                </select>

                <select
                    value={i18n.language}
                    onChange={(e) => handleLanguageChange(e.target.value)}
                    style={{ padding: "4px 6px", borderRadius: 4, border: "1px solid #ccc" }}
                >
                    <option value="en">English</option>
                    <option value="es">Español</option>
                </select>

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
                            <div style={{ marginBottom: 10 }}>{t("header.profile")}</div>
                            <div style={{ marginBottom: 10 }}>{t("header.help")}</div>
                            <div
                                onClick={handleLogout}
                                style={{ cursor: "pointer", color: "red" }}
                            >
                                {t("header.logout")}
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}

export default Header;
