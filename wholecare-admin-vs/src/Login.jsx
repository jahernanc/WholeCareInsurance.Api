import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";

function Login() {
    const { t, i18n } = useTranslation("login");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError("");

        try {
            const response = await fetch(`${import.meta.env.VITE_API_URL}/auth/login`, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    email,
                    password
                })
            });

            if (!response.ok) {
                throw new Error("Login failed");
            }

            const data = await response.json();

            console.log("LOGIN OK:", data);

            // ✅ guardar tokens
            localStorage.setItem("accessToken", data.token.accessToken);
            localStorage.setItem("refreshToken", data.token.refreshToken);
            localStorage.setItem("mustChangePassword", String(!!data.token.mustChangePassword));

            // el idioma es la fuente de verdad del backend; se cachea acá
            // para que el próximo arranque de la app pinte instantáneo
            const language = data.token.preferredLanguage === "es" ? "es" : "en";
            localStorage.setItem("preferredLanguage", language);
            i18n.changeLanguage(language);

            window.location.replace(data.token.mustChangePassword ? "/change-password" : "/");

        } catch (err) {
            console.error(err);
            setError(t("error"));
        }
    };

    return (
        <div style={{ display: "grid", placeItems: "center", height: "100vh" }}>
            <form onSubmit={handleSubmit} style={{ width: 300 }}>
                <h2>{t("title")}</h2>

                <input
                    type="email"
                    placeholder={t("emailPlaceholder")}
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    style={{ width: "100%", marginBottom: 10, padding: 8 }}
                />

                <input
                    type="password"
                    placeholder={t("passwordPlaceholder")}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    style={{ width: "100%", marginBottom: 10, padding: 8 }}
                />

                <button type="submit" style={{ width: "100%", padding: 10 }}>
                    {t("submit")}
                </button>

                {error && <p style={{ color: "red" }}>{error}</p>}

                <p style={{ textAlign: "center", marginTop: 10 }}>
                    <Link to="/forgot-password">{t("forgotPasswordLink")}</Link>
                </p>
            </form>
        </div>
    );
}

export default Login;
