import { useState } from "react";
import { useTranslation } from "react-i18next";
import { apiFetch } from "./api";

function ChangePasswordForced() {
    const { t } = useTranslation("auth");
    const [currentPassword, setCurrentPassword] = useState("");
    const [newPassword, setNewPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [error, setError] = useState("");
    const [submitting, setSubmitting] = useState(false);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError("");

        if (newPassword !== confirmPassword) {
            setError(t("changePassword.mismatchError"));
            return;
        }

        try {
            setSubmitting(true);
            const res = await apiFetch("/auth/change-password", {
                method: "POST",
                body: JSON.stringify({ currentPassword, newPassword }),
            });

            if (!res.ok) {
                const err = await res.text().catch(() => null);
                setError(err || t("changePassword.genericError"));
                return;
            }

            localStorage.setItem("mustChangePassword", "false");
            window.location.replace("/");
        } catch {
            setError(t("changePassword.genericError"));
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div style={{ display: "grid", placeItems: "center", height: "100vh" }}>
            <form onSubmit={handleSubmit} style={{ width: 340 }}>
                <h2>{t("changePassword.forcedTitle")}</h2>
                <p style={{ color: "#555", fontSize: 14 }}>{t("changePassword.forcedSubtitle")}</p>

                <input
                    type="password"
                    placeholder={t("changePassword.currentPasswordLabel")}
                    value={currentPassword}
                    onChange={(e) => setCurrentPassword(e.target.value)}
                    required
                    style={{ width: "100%", marginBottom: 10, padding: 8 }}
                />

                <input
                    type="password"
                    placeholder={t("changePassword.newPasswordLabel")}
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    required
                    minLength={8}
                    style={{ width: "100%", marginBottom: 10, padding: 8 }}
                />

                <input
                    type="password"
                    placeholder={t("changePassword.confirmPasswordLabel")}
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    required
                    minLength={8}
                    style={{ width: "100%", marginBottom: 10, padding: 8 }}
                />

                <button type="submit" disabled={submitting} style={{ width: "100%", padding: 10 }}>
                    {submitting ? t("changePassword.submitting") : t("changePassword.submit")}
                </button>

                {error && <p style={{ color: "red" }}>{error}</p>}
            </form>
        </div>
    );
}

export default ChangePasswordForced;
