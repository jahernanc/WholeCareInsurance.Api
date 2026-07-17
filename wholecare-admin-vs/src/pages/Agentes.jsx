import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { apiFetch } from "../api";
import { translateEnum } from "../i18n/translateEnum";
import { US_STATES } from "../data/usStates";
import US_COUNTIES from "../data/usCounties.json";
import { GENDERS } from "../data/customerFormOptions";
import { CONTRACT_INTERESTS, emptyAgentForm } from "../data/agentFormOptions";

const ROLES = ["Admin", "Agente"];

function Agentes() {
    const { t } = useTranslation(["agentes", "common"]);
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);
    const [form, setForm] = useState(emptyAgentForm);
    const [editingId, setEditingId] = useState(null);
    const [formError, setFormError] = useState("");
    const [submitting, setSubmitting] = useState(false);

    const countiesForState = form.state ? (US_COUNTIES[form.state] ?? []) : [];

    const loadUsers = async () => {
        try {
            setLoading(true);
            const res = await apiFetch("/users");
            if (!res.ok) throw new Error();
            setUsers(await res.json());
        } catch {
            console.error("No se pudieron cargar los agentes");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { loadUsers(); }, []);

    const handleField = (e) => {
        const { name, value, type, checked } = e.target;
        if (name === "state") {
            // el condado depende del estado: si cambia el estado, se resetea
            setForm((f) => ({ ...f, state: value, county: "" }));
            return;
        }
        setForm((f) => ({ ...f, [name]: type === "checkbox" ? checked : value }));
    };

    const handleBoolSelect = (e) => {
        const { name, value } = e.target;
        setForm((f) => ({ ...f, [name]: value === "true" }));
    };

    const handleContractInterestToggle = (interest) => {
        setForm((f) => ({
            ...f,
            contractsWanted: f.contractsWanted.includes(interest)
                ? f.contractsWanted.filter((i) => i !== interest)
                : [...f.contractsWanted, interest],
        }));
    };

    const openCreate = () => {
        setEditingId(null);
        setForm(emptyAgentForm);
        setFormError("");
        setShowForm(true);
    };

    const handleEdit = (u) => {
        setEditingId(u.id);
        setForm({
            nombre: u.nombre,
            email: u.email,
            password: "",
            rol: u.rol,
            isEncargado: u.isEncargado,
            middleName: u.middleName ?? "",
            gender: u.gender ?? "",
            address1: u.address1 ?? "",
            address2: u.address2 ?? "",
            city: u.city ?? "",
            zipCode: u.zipCode ?? "",
            state: u.state ?? "",
            county: u.county ?? "",
            licensed: u.licensed ?? false,
            licenseNumber: u.licenseNumber ?? "",
            npnNumber: u.npnNumber ?? "",
            npnOverride: u.npnOverride ?? false,
            hasCompanyContract: u.hasCompanyContract ?? false,
            contractNumber: u.contractNumber ?? "",
            companyName: u.companyName ?? "",
            contractsWanted: u.contractsWanted ? u.contractsWanted.split(",").filter(Boolean) : [],
            additionalInformation: u.additionalInformation ?? "",
            termsAccepted: u.termsAccepted ?? false,
        });
        setFormError("");
        setShowForm(true);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setFormError("");

        try {
            setSubmitting(true);

            const sharedFields = {
                nombre: form.nombre,
                email: form.email,
                rol: form.rol,
                isEncargado: form.isEncargado,
                middleName: form.middleName,
                gender: form.gender,
                address1: form.address1,
                address2: form.address2,
                city: form.city,
                zipCode: form.zipCode,
                state: form.state,
                county: form.county,
                licensed: form.licensed,
                licenseNumber: form.licensed ? form.licenseNumber : "",
                npnNumber: form.npnNumber,
                npnOverride: form.npnOverride,
                hasCompanyContract: form.hasCompanyContract,
                contractNumber: form.hasCompanyContract ? form.contractNumber : "",
                companyName: form.hasCompanyContract ? form.companyName : "",
                contractsWanted: form.contractsWanted.join(","),
                additionalInformation: form.additionalInformation,
                termsAccepted: form.termsAccepted,
            };

            const res = editingId
                ? await apiFetch(`/users/${editingId}`, {
                    method: "PUT",
                    body: JSON.stringify(sharedFields),
                })
                : await apiFetch("/auth/register", {
                    method: "POST",
                    body: JSON.stringify({ ...sharedFields, password: form.password }),
                });

            if (!res.ok) {
                setFormError(res.errorMessage ?? t("form.saveError"));
                return;
            }

            setShowForm(false);
            setForm(emptyAgentForm);
            setEditingId(null);
            await loadUsers();
        } catch {
            setFormError(t("form.saveError"));
        } finally {
            setSubmitting(false);
        }
    };

    const inputStyle = { width: "100%", padding: "7px 10px", marginTop: 4, boxSizing: "border-box", borderRadius: 5, border: "1px solid #ccc" };
    const labelStyle = { fontWeight: 500, fontSize: 13 };
    const fullRowStyle = { gridColumn: "1 / -1" };

    return (
        <div>
            <h2 style={{ marginBottom: 20 }}>{t("title")}</h2>

            <button
                onClick={showForm ? () => setShowForm(false) : openCreate}
                type="button"
                style={{ marginBottom: 20, background: "#2563eb", color: "white", padding: "8px 14px", border: "none", borderRadius: 6, cursor: "pointer" }}
            >
                {showForm ? t("closeFormButton") : t("newButton")}
            </button>

            {showForm && (
                <div style={{ border: "1px solid #ddd", borderRadius: 10, padding: 24, marginBottom: 30, background: "#fafafa", maxWidth: 720 }}>
                    <h3 style={{ marginTop: 0 }}>{editingId ? t("form.titleEdit") : t("form.titleCreate")}</h3>

                    <form onSubmit={handleSubmit}>
                        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>

                            <div>
                                <label style={labelStyle}>{t("form.fields.name")}</label>
                                <input name="nombre" value={form.nombre} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.middleName")}</label>
                                <input name="middleName" value={form.middleName} onChange={handleField} style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.email")}</label>
                                <input type="email" name="email" value={form.email} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.gender")}</label>
                                <select name="gender" value={form.gender} onChange={handleField} style={inputStyle}>
                                    <option value="">{t("form.selectPlaceholder")}</option>
                                    {GENDERS.map((g) => (
                                        <option key={g} value={g}>{translateEnum("gender", g)}</option>
                                    ))}
                                </select>
                            </div>

                            {!editingId && (
                                <div>
                                    <label style={labelStyle}>{t("form.fields.password")}</label>
                                    <input type="password" name="password" value={form.password} onChange={handleField} required style={inputStyle} />
                                </div>
                            )}

                            <div>
                                <label style={labelStyle}>{t("form.fields.role")}</label>
                                <select name="rol" value={form.rol} onChange={handleField} required style={inputStyle}>
                                    {ROLES.map((r) => (
                                        <option key={r} value={r}>{translateEnum("userRol", r)}</option>
                                    ))}
                                </select>
                            </div>

                            <div style={fullRowStyle}>
                                <label style={labelStyle}>{t("form.fields.address1")}</label>
                                <input name="address1" value={form.address1} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div style={fullRowStyle}>
                                <label style={labelStyle}>{t("form.fields.address2")}</label>
                                <input name="address2" value={form.address2} onChange={handleField} style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.zipCode")}</label>
                                <input name="zipCode" value={form.zipCode} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.city")}</label>
                                <input name="city" value={form.city} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.country")}</label>
                                <input value={t("form.fields.countryValue")} disabled style={{ ...inputStyle, background: "#eee", color: "#666" }} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.state")}</label>
                                <select name="state" value={form.state} onChange={handleField} required style={inputStyle}>
                                    <option value="">{t("form.selectPlaceholder")}</option>
                                    {US_STATES.map((s) => (
                                        <option key={s.code} value={s.code}>{s.name}</option>
                                    ))}
                                </select>
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.county")}</label>
                                <select name="county" value={form.county} onChange={handleField} disabled={!form.state} required style={inputStyle}>
                                    <option value="">{form.state ? t("form.selectPlaceholder") : t("form.selectStateFirst")}</option>
                                    {countiesForState.map((c) => (
                                        <option key={c} value={c}>{c}</option>
                                    ))}
                                </select>
                            </div>

                            <div>
                                <label style={{ ...labelStyle, display: "flex", alignItems: "center", gap: 6 }}>
                                    <input type="checkbox" name="isEncargado" checked={form.isEncargado} onChange={handleField} />
                                    {t("form.fields.isEncargado")}
                                </label>
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.licensed")}</label>
                                <select name="licensed" value={String(form.licensed)} onChange={handleBoolSelect} style={inputStyle}>
                                    <option value="false">{t("card.no")}</option>
                                    <option value="true">{t("card.yes")}</option>
                                </select>
                            </div>

                            {form.licensed && (
                                <div>
                                    <label style={labelStyle}>{t("form.fields.licenseNumber")}</label>
                                    <input name="licenseNumber" value={form.licenseNumber} onChange={handleField} style={inputStyle} />
                                </div>
                            )}

                            <div>
                                <label style={labelStyle}>{t("form.fields.npnNumber")}</label>
                                <input name="npnNumber" value={form.npnNumber} onChange={handleField} style={inputStyle} />
                            </div>

                            <div>
                                <label style={{ ...labelStyle, display: "flex", alignItems: "center", gap: 6 }}>
                                    <input type="checkbox" name="npnOverride" checked={form.npnOverride} onChange={handleField} />
                                    {t("form.fields.npnOverride")}
                                </label>
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.hasCompanyContract")}</label>
                                <select name="hasCompanyContract" value={String(form.hasCompanyContract)} onChange={handleBoolSelect} style={inputStyle}>
                                    <option value="false">{t("card.no")}</option>
                                    <option value="true">{t("card.yes")}</option>
                                </select>
                            </div>

                            {form.hasCompanyContract && (
                                <>
                                    <div>
                                        <label style={labelStyle}>{t("form.fields.contractNumber")}</label>
                                        <input name="contractNumber" value={form.contractNumber} onChange={handleField} style={inputStyle} />
                                    </div>

                                    <div>
                                        <label style={labelStyle}>{t("form.fields.companyName")}</label>
                                        <input name="companyName" value={form.companyName} onChange={handleField} style={inputStyle} />
                                    </div>
                                </>
                            )}

                            <div style={fullRowStyle}>
                                <label style={labelStyle}>{t("form.fields.contractsWanted")}</label>
                                <div style={{ display: "flex", gap: 16, flexWrap: "wrap", marginTop: 6 }}>
                                    {CONTRACT_INTERESTS.map((interest) => (
                                        <label key={interest} style={{ ...labelStyle, display: "flex", alignItems: "center", gap: 6, fontWeight: 400 }}>
                                            <input
                                                type="checkbox"
                                                checked={form.contractsWanted.includes(interest)}
                                                onChange={() => handleContractInterestToggle(interest)}
                                            />
                                            {translateEnum("contractInterest", interest)}
                                        </label>
                                    ))}
                                </div>
                            </div>

                            <div style={fullRowStyle}>
                                <label style={labelStyle}>{t("form.fields.additionalInformation")}</label>
                                <textarea name="additionalInformation" value={form.additionalInformation} onChange={handleField} rows={3} style={{ ...inputStyle, resize: "vertical" }} />
                            </div>

                            <div style={fullRowStyle}>
                                <label style={{ ...labelStyle, display: "flex", alignItems: "center", gap: 6 }}>
                                    <input type="checkbox" name="termsAccepted" checked={form.termsAccepted} onChange={handleField} required />
                                    {t("form.fields.termsAccepted")}
                                </label>
                            </div>

                        </div>

                        {formError && <p style={{ color: "red", marginTop: 12 }}>{formError}</p>}

                        <button
                            type="submit"
                            disabled={submitting}
                            style={{ marginTop: 16, background: "#2563eb", color: "white", padding: "9px 20px", border: "none", borderRadius: 6, cursor: "pointer" }}
                        >
                            {editingId
                                ? (submitting ? t("common:actions.saving") : t("common:actions.saveChanges"))
                                : (submitting ? t("common:actions.creating") : t("form.submitCreate"))}
                        </button>
                    </form>
                </div>
            )}

            {loading ? (
                <p>{t("loading")}</p>
            ) : users.length === 0 ? (
                <p>{t("empty")}</p>
            ) : (
                <div style={{ display: "grid", gap: 12 }}>
                    {users.map((u) => (
                        <div key={u.id} style={{ border: "1px solid #ddd", borderRadius: 10, padding: 16, background: "white" }}>
                            <div style={{ fontWeight: "bold", fontSize: 16, marginBottom: 6 }}>
                                {u.nombre}
                            </div>
                            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "3px 20px", fontSize: 14, color: "#444" }}>
                                <span>{t("card.email")}: {u.email}</span>
                                <span>{t("card.role")}: {translateEnum("userRol", u.rol)}</span>
                                <span>{t("card.isEncargado")}: {u.isEncargado ? t("card.yes") : t("card.no")}</span>
                                <span>{t("card.location")}: {[u.city, u.county, u.state].filter(Boolean).join(", ") || "-"}</span>
                                <span>{t("card.licensed")}: {u.licensed ? `${t("card.yes")} (${u.licenseNumber || "-"})` : t("card.no")}</span>
                                <span>{t("card.npnNumber")}: {u.npnNumber || "-"}</span>
                                <span>{t("card.hasCompanyContract")}: {u.hasCompanyContract ? t("card.yes") : t("card.no")}</span>
                            </div>
                            <div style={{ marginTop: 10, display: "flex", gap: 8 }}>
                                <button onClick={() => handleEdit(u)} style={{ background: "#007bff", color: "white", border: "none", padding: "5px 12px", borderRadius: 5, cursor: "pointer" }}>
                                    {t("common:actions.edit")}
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}

export default Agentes;
