import { useState } from "react";
import { useTranslation } from "react-i18next";
import { apiFetch } from "../api";

function Profile() {
    const { t } = useTranslation(["auth", "common"]);
    const [currentPassword, setCurrentPassword] = useState("");
    const [newPassword, setNewPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    const [error, setError] = useState("");
    const [success, setSuccess] = useState(false);
    const [submitting, setSubmitting] = useState(false);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError("");
        setSuccess(false);

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

            setCurrentPassword("");
            setNewPassword("");
            setConfirmPassword("");
            setSuccess(true);
        } catch {
            setError(t("changePassword.genericError"));
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <div>
            <h2 style={{ marginBottom: 20 }}>{t("profile.title")}</h2>

            <div style={{ border: "1px solid #ddd", borderRadius: 10, padding: 24, background: "#fafafa", maxWidth: 400 }}>
                <h3 style={{ marginTop: 0 }}>{t("changePassword.profileTitle")}</h3>

                <form onSubmit={handleSubmit}>
                    <div style={{ marginBottom: 12 }}>
                        <label>{t("changePassword.currentPasswordLabel")}</label>
                        <input
                            type="password"
                            value={currentPassword}
                            onChange={(e) => setCurrentPassword(e.target.value)}
                            required
                            style={{ width: "100%", padding: 8, marginTop: 4, boxSizing: "border-box" }}
                        />
                    </div>

                    <div style={{ marginBottom: 12 }}>
                        <label>{t("changePassword.newPasswordLabel")}</label>
                        <input
                            type="password"
                            value={newPassword}
                            onChange={(e) => setNewPassword(e.target.value)}
                            required
                            minLength={8}
                            style={{ width: "100%", padding: 8, marginTop: 4, boxSizing: "border-box" }}
                        />
                    </div>

                    <div style={{ marginBottom: 12 }}>
                        <label>{t("changePassword.confirmPasswordLabel")}</label>
                        <input
                            type="password"
                            value={confirmPassword}
                            onChange={(e) => setConfirmPassword(e.target.value)}
                            required
                            minLength={8}
                            style={{ width: "100%", padding: 8, marginTop: 4, boxSizing: "border-box" }}
                        />
                    </div>

                    <button
                        type="submit"
                        disabled={submitting}
                        style={{ background: "#2563eb", color: "white", padding: "9px 20px", border: "none", borderRadius: 6, cursor: "pointer" }}
                    >
                        {submitting ? t("changePassword.submitting") : t("changePassword.submit")}
                    </button>

                    {error && <p style={{ color: "red", marginTop: 12 }}>{error}</p>}
                    {success && <p style={{ color: "green", marginTop: 12 }}>{t("changePassword.success")}</p>}
                </form>
            </div>
        </div>
    );
}

export default Profile;
