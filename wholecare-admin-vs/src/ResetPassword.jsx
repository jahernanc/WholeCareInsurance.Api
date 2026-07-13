import { useState } from "react";
import { useTranslation } from "react-i18next";
import { Link, useSearchParams } from "react-router-dom";

function ResetPassword() {
    const { t } = useTranslation("auth");
    const [searchParams] = useSearchParams();
    const token = searchParams.get("token") ?? "";

    const [newPassword, setNewPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [error, setError] = useState("");
    const [submitting, setSubmitting] = useState(false);
    const [success, setSuccess] = useState(false);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError("");

        if (newPassword !== confirmPassword) {
            setError(t("changePassword.mismatchError"));
            return;
        }

        try {
            setSubmitting(true);
            // Sin token de sesión todavía (flujo no autenticado) — solo el token del link.
            const res = await fetch(`${import.meta.env.VITE_API_URL}/auth/reset-password`, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify({ token, newPassword }),
            });

            if (!res.ok) {
                setError(t("resetPassword.invalidTokenError"));
                return;
            }

            setSuccess(true);
        } catch {
            setError(t("resetPassword.invalidTokenError"));
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div style={{ display: "grid", placeItems: "center", height: "100vh" }}>
            <div style={{ width: 340 }}>
                <h2>{t("resetPassword.title")}</h2>

                {success ? (
                    <>
                        <p>{t("resetPassword.successMessage")}</p>
                        <p style={{ textAlign: "center" }}>
                            <Link to="/login">{t("resetPassword.goToLogin")}</Link>
                        </p>
                    </>
                ) : (
                    <form onSubmit={handleSubmit}>
                        <input
                            type="password"
                            placeholder={t("resetPassword.newPasswordLabel")}
                            value={newPassword}
                            onChange={(e) => setNewPassword(e.target.value)}
                            required
                            minLength={8}
                            style={{ width: "100%", marginBottom: 10, padding: 8 }}
                        />

                        <input
                            type="password"
                            placeholder={t("resetPassword.confirmPasswordLabel")}
                            value={confirmPassword}
                            onChange={(e) => setConfirmPassword(e.target.value)}
                            required
                            minLength={8}
                            style={{ width: "100%", marginBottom: 10, padding: 8 }}
                        />

                        <button type="submit" disabled={submitting} style={{ width: "100%", padding: 10 }}>
                            {submitting ? t("resetPassword.submitting") : t("resetPassword.submit")}
                        </button>

                        {error && (
                            <p style={{ color: "red" }}>
                                {error}{" "}
                                <Link to="/forgot-password">{t("resetPassword.requestNewLink")}</Link>
                            </p>
                        )}
                    </form>
                )}
            </div>
        </div>
    );
}

export default ResetPassword;
