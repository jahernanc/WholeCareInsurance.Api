import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { apiFetch } from "../api";
import { translateEnum } from "../i18n/translateEnum";
import { GENDERS } from "../data/customerFormOptions";
import { US_STATES } from "../data/usStates";
import US_COUNTIES from "../data/usCounties.json";

const inputStyle = { width: "100%", padding: "7px 10px", marginTop: 4, boxSizing: "border-box", borderRadius: 5, border: "1px solid #ccc" };
const labelStyle = { fontWeight: 500, fontSize: 13 };

const emptyProfileForm = {
    nombre: "",
    middleName: "",
    gender: "",
    email: "",
    phone: "",
    agency: "",
    address1: "",
    address2: "",
    city: "",
    state: "",
    zipCode: "",
    county: "",
    currentPassword: "",
};

// Sección "Datos del perfil" (§26.5). Componente propio dentro de este mismo archivo
// en vez de extraerse a un componente compartido tipo CustomerFormFields.jsx: esos
// campos de dirección solo se usan acá (una sola pantalla), así que separarlos no
// evita duplicación real — solo agregaría una capa de indirección sin otro consumidor.
function ProfileDataSection() {
    const { t } = useTranslation(["auth", "common"]);
    const [form, setForm] = useState(emptyProfileForm);
    const [originalEmail, setOriginalEmail] = useState("");
    const [loading, setLoading] = useState(true);
    const [submitting, setSubmitting] = useState(false);
    const [error, setError] = useState("");
    const [success, setSuccess] = useState(false);

    const countiesForState = form.state ? (US_COUNTIES[form.state] ?? []) : [];
    const emailChanged = form.email !== originalEmail;

    useEffect(() => {
        (async () => {
            try {
                const res = await apiFetch("/users/me");
                if (!res.ok) {
                    setError(t("profileData.genericError"));
                    return;
                }

                const me = await res.json();
                setForm({
                    nombre: me.nombre ?? "",
                    middleName: me.middleName ?? "",
                    gender: me.gender ?? "",
                    email: me.email ?? "",
                    phone: me.phone ?? "",
                    agency: me.agency ?? "",
                    address1: me.address1 ?? "",
                    address2: me.address2 ?? "",
                    city: me.city ?? "",
                    state: me.state ?? "",
                    zipCode: me.zipCode ?? "",
                    county: me.county ?? "",
                    currentPassword: "",
                });
                setOriginalEmail(me.email ?? "");
            } catch {
                setError(t("profileData.genericError"));
            } finally {
                setLoading(false);
            }
        })();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const handleField = (e) => {
        const { name, value } = e.target;
        if (name === "state") {
            // el condado depende del estado: si cambia el estado, se resetea (mismo
            // criterio que Agentes.jsx/CustomerFormFields.jsx)
            setForm((f) => ({ ...f, state: value, county: "" }));
            return;
        }
        setForm((f) => ({ ...f, [name]: value }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError("");
        setSuccess(false);

        try {
            setSubmitting(true);
            const res = await apiFetch("/users/me", {
                method: "PUT",
                body: JSON.stringify({
                    nombre: form.nombre,
                    email: form.email,
                    middleName: form.middleName,
                    gender: form.gender,
                    phone: form.phone,
                    address1: form.address1,
                    address2: form.address2,
                    city: form.city,
                    zipCode: form.zipCode,
                    state: form.state,
                    county: form.county,
                    // Solo se manda si efectivamente se está cambiando el email — el
                    // backend la exige nada más que en ese caso (§26.2).
                    ...(emailChanged ? { currentPassword: form.currentPassword } : {}),
                }),
            });

            if (!res.ok) {
                setError(res.errorMessage ?? t("profileData.genericError"));
                return;
            }

            const updated = await res.json();
            setOriginalEmail(updated.email ?? form.email);
            setForm((f) => ({ ...f, currentPassword: "" }));
            setSuccess(true);
        } catch {
            setError(t("profileData.genericError"));
        } finally {
            setSubmitting(false);
        }
    };

    if (loading) return <p>{t("profileData.loading")}</p>;

    return (
        <div style={{ border: "1px solid #ddd", borderRadius: 10, padding: 24, background: "#fafafa", maxWidth: 640 }}>
            <h3 style={{ marginTop: 0 }}>{t("profileData.cardTitle")}</h3>

            <form onSubmit={handleSubmit}>
                <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
                    <div>
                        <label style={labelStyle}>{t("profileData.fields.nombre")}</label>
                        <input name="nombre" value={form.nombre} onChange={handleField} required style={inputStyle} />
                    </div>

                    <div>
                        <label style={labelStyle}>{t("profileData.fields.middleName")}</label>
                        <input name="middleName" value={form.middleName} onChange={handleField} style={inputStyle} />
                    </div>

                    <div>
                        <label style={labelStyle}>{t("profileData.fields.gender")}</label>
                        <select name="gender" value={form.gender} onChange={handleField} style={inputStyle}>
                            <option value="">{t("profileData.selectPlaceholder")}</option>
                            {GENDERS.map((g) => (
                                <option key={g} value={g}>{translateEnum("gender", g)}</option>
                            ))}
                        </select>
                    </div>

                    <div>
                        <label style={labelStyle}>{t("profileData.fields.phone")}</label>
                        <input name="phone" value={form.phone} onChange={handleField} style={inputStyle} />
                    </div>

                    <div style={{ gridColumn: "1 / -1" }}>
                        <label style={labelStyle}>{t("profileData.fields.email")}</label>
                        <input type="email" name="email" value={form.email} onChange={handleField} required style={inputStyle} />
                    </div>

                    {emailChanged && (
                        <div style={{ gridColumn: "1 / -1" }}>
                            <label style={labelStyle}>{t("profileData.fields.confirmCurrentPassword")}</label>
                            <input
                                type="password"
                                name="currentPassword"
                                value={form.currentPassword}
                                onChange={handleField}
                                required
                                style={inputStyle}
                            />
                            <p style={{ fontSize: 12, color: "#666", marginTop: 4 }}>{t("profileData.currentPasswordHint")}</p>
                        </div>
                    )}

                    <div style={{ gridColumn: "1 / -1" }}>
                        <label style={labelStyle}>{t("profileData.fields.agency")}</label>
                        <input
                            value={form.agency ? translateEnum("agency", form.agency) : "-"}
                            disabled
                            style={{ ...inputStyle, background: "#eee", color: "#666" }}
                        />
                    </div>

                    <div style={{ gridColumn: "1 / -1" }}>
                        <label style={labelStyle}>{t("profileData.fields.address1")}</label>
                        <input name="address1" value={form.address1} onChange={handleField} required style={inputStyle} />
                    </div>

                    <div style={{ gridColumn: "1 / -1" }}>
                        <label style={labelStyle}>{t("profileData.fields.address2")}</label>
                        <input name="address2" value={form.address2} onChange={handleField} style={inputStyle} />
                    </div>

                    <div>
                        <label style={labelStyle}>{t("profileData.fields.state")}</label>
                        <select name="state" value={form.state} onChange={handleField} required style={inputStyle}>
                            <option value="">{t("profileData.selectPlaceholder")}</option>
                            {US_STATES.map((s) => (
                                <option key={s.code} value={s.code}>{s.name}</option>
                            ))}
                        </select>
                    </div>

                    <div>
                        <label style={labelStyle}>{t("profileData.fields.county")}</label>
                        <select name="county" value={form.county} onChange={handleField} disabled={!form.state} required style={inputStyle}>
                            <option value="">{form.state ? t("profileData.selectPlaceholder") : t("profileData.selectStateFirst")}</option>
                            {countiesForState.map((c) => (
                                <option key={c} value={c}>{c}</option>
                            ))}
                        </select>
                    </div>

                    <div>
                        <label style={labelStyle}>{t("profileData.fields.city")}</label>
                        <input name="city" value={form.city} onChange={handleField} required style={inputStyle} />
                    </div>

                    <div>
                        <label style={labelStyle}>{t("profileData.fields.zipCode")}</label>
                        <input name="zipCode" value={form.zipCode} onChange={handleField} required style={inputStyle} />
                    </div>
                </div>

                <button
                    type="submit"
                    disabled={submitting}
                    style={{ background: "#16a34a", color: "white", padding: "9px 20px", border: "none", borderRadius: 6, cursor: "pointer", marginTop: 16 }}
                >
                    {submitting ? t("profileData.submitting") : t("profileData.submit")}
                </button>

                {error && <p style={{ color: "red", marginTop: 12 }}>{error}</p>}
                {success && <p style={{ color: "green", marginTop: 12 }}>{t("profileData.success")}</p>}
            </form>
        </div>
    );
}

function ChangePasswordSection() {
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
                    style={{ background: "#16a34a", color: "white", padding: "9px 20px", border: "none", borderRadius: 6, cursor: "pointer" }}
                >
                    {submitting ? t("changePassword.submitting") : t("changePassword.submit")}
                </button>

                {error && <p style={{ color: "red", marginTop: 12 }}>{error}</p>}
                {success && <p style={{ color: "green", marginTop: 12 }}>{t("changePassword.success")}</p>}
            </form>
        </div>
    );
}

function Profile() {
    const { t } = useTranslation(["auth", "common"]);
    // Mismo patrón visual que el botón showForm de Customers.jsx/Policies.jsx (§26.4),
    // pero acá son 2 botones fijos (no un solo botón que alterna texto) porque son 2
    // secciones a elegir, no un formulario para abrir/cerrar.
    const [activeSection, setActiveSection] = useState("data");

    const toggleButtonStyle = (active) => ({
        background: active ? "#2563eb" : "white",
        color: active ? "white" : "#2563eb",
        border: "1px solid #2563eb",
        padding: "8px 14px",
        borderRadius: 6,
        cursor: "pointer",
    });

    return (
        <div>
            <h2 style={{ marginBottom: 20 }}>{t("profile.title")}</h2>

            <div style={{ display: "flex", gap: 10, marginBottom: 20 }}>
                <button type="button" onClick={() => setActiveSection("data")} style={toggleButtonStyle(activeSection === "data")}>
                    {t("profile.toggleData")}
                </button>
                <button type="button" onClick={() => setActiveSection("password")} style={toggleButtonStyle(activeSection === "password")}>
                    {t("profile.togglePassword")}
                </button>
            </div>

            {activeSection === "data" ? <ProfileDataSection /> : <ChangePasswordSection />}
        </div>
    );
}

export default Profile;
