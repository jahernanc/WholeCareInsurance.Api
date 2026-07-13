import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";

function ForgotPassword() {
    const { t } = useTranslation("auth");
    const [email, setEmail] = useState("");
    const [submitting, setSubmitting] = useState(false);
    const [submitted, setSubmitted] = useState(false);

    const handleSubmit = async (e) => {
        e.preventDefault();

        try {
            setSubmitting(true);
            // Sin token todavía (flujo no autenticado) — mismo patrón que Login.jsx.
            await fetch(`${import.meta.env.VITE_API_URL}/auth/forgot-password`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ email }),
            });
        } finally {
            // Se muestra el mismo mensaje exista o no la cuenta, y aunque la
            // request falle de red — no hay nada distinto que mostrarle al usuario.
            setSubmitting(false);
            setSubmitted(true);
        }
    };

    return (
        <div style={{ display: "grid", placeItems: "center", height: "100vh" }}>
            <div style={{ width: 340 }}>
                <h2>{t("forgotPassword.title")}</h2>

                {submitted ? (
                    <p>{t("forgotPassword.successMessage")}</p>
                ) : (
                    <form onSubmit={handleSubmit}>
                        <p style={{ color: "#555", fontSize: 14 }}>{t("forgotPassword.subtitle")}</p>

                        <input
                            type="email"
                            placeholder={t("forgotPassword.emailLabel")}
                            value={email}
                            onChange={(e) => setEmail(e.target.value)}
                            required
                            style={{ width: "100%", marginBottom: 10, padding: 8 }}
                        />

                        <button type="submit" disabled={submitting} style={{ width: "100%", padding: 10 }}>
                            {submitting ? t("forgotPassword.submitting") : t("forgotPassword.submit")}
                        </button>
                    </form>
                )}

                <p style={{ textAlign: "center", marginTop: 10 }}>
                    <Link to="/login">{t("forgotPassword.backToLogin")}</Link>
                </p>
            </div>
        </div>
    );
}

export default ForgotPassword;
