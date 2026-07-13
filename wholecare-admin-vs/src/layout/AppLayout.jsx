import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import Sidebar from "./Sidebar";
import Header from "./Header";
import { Outlet } from "react-router-dom";
import { apiFetch } from "../api";

const currentYear = new Date().getFullYear();

function AppLayout() {
    const { i18n } = useTranslation();

    // Período de trabajo global (año), controlado desde el selector del Header.
    // Persistido en localStorage (mismo patrón que preferredLanguage) para que
    // sobreviva a un reload; no hay librería de estado global en este proyecto.
    const [period, setPeriod] = useState(() => {
        const stored = parseInt(localStorage.getItem("selectedPeriod"), 10);
        return !isNaN(stored) ? stored : currentYear;
    });

    const handlePeriodChange = (newPeriod) => {
        setPeriod(newPeriod);
        localStorage.setItem("selectedPeriod", String(newPeriod));
    };

    useEffect(() => {
        // Reconciliación en segundo plano: localStorage es solo un cache de
        // arranque rápido, el backend (User.PreferredLanguage) manda. Si el
        // usuario cambió el idioma desde otra computadora, esto lo corrige
        // sin bloquear el primer render.
        (async () => {
            try {
                const res = await apiFetch("/users/me");
                if (!res.ok) return;

                const me = await res.json();
                const lang = me.preferredLanguage === "es" ? "es" : "en";

                if (lang !== i18n.language) {
                    i18n.changeLanguage(lang);
                }
                localStorage.setItem("preferredLanguage", lang);
            } catch {
                // best-effort: si falla, se queda con el idioma cacheado
            }
        })();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return (
        <div style={{ display: "flex", height: "100vh" }}>
            <Sidebar />

            <div style={{ flex: 1, display: "flex", flexDirection: "column" }}>
                <Header period={period} onPeriodChange={handlePeriodChange} />

                <main style={{ padding: 20 }}>
                    <Outlet context={{ period }} /> ✅
                </main>
            </div>
        </div>
    );
}

export default AppLayout;
