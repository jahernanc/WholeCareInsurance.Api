import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { apiFetch, isAdmin } from "../api";
import { translateEnum } from "../i18n/translateEnum";
import { US_STATES } from "../data/usStates";
import US_COUNTIES from "../data/usCounties.json";

const API = "/api/customers";
const MIGRATION_STATUSES = [
    "Permiso de trabajo",
    "Residente permanente",
    "Ciudadano",
    "Otro",
    "Asilo",
];
const RELACIONES_PRINCIPAL = [
    "Cónyuge",
    "Hijo/a",
    "Madre",
    "Padre",
    "Sobrino/a",
    "Nieto/a",
    "Hijastro/a",
    "Hermano/a",
    "Otro",
];
const MARITAL_STATUSES = ["Soltero/a", "Casado/a", "Divorciado/a", "Viudo/a", "Unión libre"];
const GENDERS = ["Masculino", "Femenino"];
const CONTACT_LANGUAGES = ["Inglés", "Español"];

const emptyForm = {
    socialSecurityNumber: "",
    firstName: "",
    lastName: "",
    dateOfBirth: "",
    email: "",
    address1: "",
    phone: "",
    migrationStatus: "",
    relacionConPrincipal: "",
    zipCode: "",
    state: "",
    city: "",
    county: "",
    maritalStatus: "",
    occupation: "",
    agentId: "",
    assistantAgentId: "",
    recordAgentId: "",
    middleName: "",
    gender: "",
    greenCard: "",
    workPermit: "",
    address2: "",
    employerName: "",
    companyPhone: "",
    annualIncome: "",
    tags: "",
    contactLanguage: "",
};

function Customers() {
    const { t } = useTranslation(["customers", "common"]);
    const [customers, setCustomers] = useState([]);
    const [agents, setAgents] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);
    const [form, setForm] = useState(emptyForm);
    const [editingId, setEditingId] = useState(null);
    const [formError, setFormError] = useState("");
    const [submitting, setSubmitting] = useState(false);

    const userIsAdmin = isAdmin();
    const encargados = agents.filter((a) => a.isEncargado);

    const loadCustomers = async () => {
        try {
            setLoading(true);
            const res = await apiFetch(API);
            if (!res.ok) throw new Error();
            setCustomers(await res.json());
        } catch {
            console.error("No se pudieron cargar los clientes");
        } finally {
            setLoading(false);
        }
    };

    const loadAgents = async () => {
        try {
            const res = await apiFetch("/users?role=Agente");
            if (!res.ok) throw new Error();
            setAgents(await res.json());
        } catch {
            console.error("No se pudieron cargar los agentes");
        }
    };

    useEffect(() => {
        loadCustomers();
        if (userIsAdmin) loadAgents();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const handleField = (e) => {
        const { name, value } = e.target;
        if (name === "state") {
            // el condado depende del estado: si cambia el estado, se resetea
            setForm((f) => ({ ...f, state: value, county: "" }));
            return;
        }
        setForm((f) => ({ ...f, [name]: value }));
    };

    const openCreate = () => {
        setEditingId(null);
        setForm(emptyForm);
        setFormError("");
        setShowForm(true);
    };

    const handleEdit = (c) => {
        setEditingId(c.id);
        setForm({
            socialSecurityNumber: c.socialSecurityNumber,
            firstName: c.firstName,
            lastName: c.lastName,
            dateOfBirth: c.dateOfBirth?.substring(0, 10) ?? "",
            email: c.email,
            address1: c.address1,
            phone: c.phone,
            migrationStatus: c.migrationStatus,
            relacionConPrincipal: c.relacionConPrincipal,
            zipCode: c.zipCode ?? "",
            state: c.state ?? "",
            city: c.city ?? "",
            county: c.county ?? "",
            maritalStatus: c.maritalStatus ?? "",
            occupation: c.occupation ?? "",
            agentId: c.agentId ?? "",
            assistantAgentId: c.assistantAgentId ?? "",
            recordAgentId: c.recordAgentId ?? "",
            middleName: c.middleName ?? "",
            gender: c.gender ?? "",
            greenCard: c.greenCard ?? "",
            workPermit: c.workPermit ?? "",
            address2: c.address2 ?? "",
            employerName: c.employerName ?? "",
            companyPhone: c.companyPhone ?? "",
            annualIncome: c.annualIncome ?? "",
            tags: c.tags ?? "",
            contactLanguage: c.contactLanguage ?? "",
        });
        setFormError("");
        setShowForm(true);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setFormError("");

        const url = editingId ? `${API}/${editingId}` : API;
        const method = editingId ? "PUT" : "POST";

        const body = {
            ...form,
            agentId: form.agentId ? Number(form.agentId) : null,
            assistantAgentId: form.assistantAgentId ? Number(form.assistantAgentId) : null,
            recordAgentId: form.recordAgentId ? Number(form.recordAgentId) : null,
            annualIncome: form.annualIncome === "" ? 0 : Number(form.annualIncome),
        };

        try {
            setSubmitting(true);
            const res = await apiFetch(url, {
                method,
                body: JSON.stringify(body),
            });

            if (!res.ok) {
                const err = await res.json().catch(() => null);
                setFormError(err?.title ?? err ?? t("form.saveError"));
                return;
            }

            setShowForm(false);
            setForm(emptyForm);
            setEditingId(null);
            await loadCustomers();
        } catch {
            setFormError(t("form.saveError"));
        } finally {
            setSubmitting(false);
        }
    };

    const handleDelete = async (id) => {
        if (!confirm(t("deleteConfirm"))) return;
        try {
            const res = await apiFetch(`${API}/${id}`, { method: "DELETE" });
            if (!res.ok) throw new Error();
            await loadCustomers();
        } catch {
            alert(t("deleteError"));
        }
    };

    const inputStyle = { width: "100%", padding: "7px 10px", marginTop: 4, boxSizing: "border-box", borderRadius: 5, border: "1px solid #ccc" };
    const labelStyle = { fontWeight: 500, fontSize: 13 };
    const countiesForState = form.state ? (US_COUNTIES[form.state] ?? []) : [];

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
                <div style={{ border: "1px solid #ddd", borderRadius: 10, padding: 24, marginBottom: 30, background: "#fafafa", maxWidth: 560 }}>
                    <h3 style={{ marginTop: 0 }}>{editingId ? t("form.titleEdit") : t("form.titleCreate")}</h3>

                    <form onSubmit={handleSubmit}>
                        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>

                            <div>
                                <label style={labelStyle}>{t("form.fields.ssn")}</label>
                                <input name="socialSecurityNumber" value={form.socialSecurityNumber} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.dateOfBirth")}</label>
                                <input type="date" name="dateOfBirth" value={form.dateOfBirth} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.firstName")}</label>
                                <input name="firstName" value={form.firstName} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.lastName")}</label>
                                <input name="lastName" value={form.lastName} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.middleName")}</label>
                                <input name="middleName" value={form.middleName} onChange={handleField} style={inputStyle} />
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

                            <div style={{ gridColumn: "1 / -1" }}>
                                <label style={labelStyle}>{t("form.fields.email")}</label>
                                <input type="email" name="email" value={form.email} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div style={{ gridColumn: "1 / -1" }}>
                                <label style={labelStyle}>{t("form.fields.address1")}</label>
                                <input name="address1" value={form.address1} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div style={{ gridColumn: "1 / -1" }}>
                                <label style={labelStyle}>{t("form.fields.address2")}</label>
                                <input name="address2" value={form.address2} onChange={handleField} style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.phone")}</label>
                                <input name="phone" value={form.phone} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.migrationStatus")}</label>
                                <select name="migrationStatus" value={form.migrationStatus} onChange={handleField} required style={inputStyle}>
                                    <option value="">{t("form.selectPlaceholder")}</option>
                                    {MIGRATION_STATUSES.map((s) => (
                                        <option key={s} value={s}>{translateEnum("migrationStatus", s)}</option>
                                    ))}
                                </select>
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.relacionConPrincipal")}</label>
                                <select name="relacionConPrincipal" value={form.relacionConPrincipal} onChange={handleField} required style={inputStyle}>
                                    <option value="">{t("form.selectPlaceholder")}</option>
                                    {RELACIONES_PRINCIPAL.map((r) => (
                                        <option key={r} value={r}>{translateEnum("relacionConPrincipal", r)}</option>
                                    ))}
                                </select>
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.zipCode")}</label>
                                <input name="zipCode" value={form.zipCode} onChange={handleField} style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.state")}</label>
                                <select name="state" value={form.state} onChange={handleField} style={inputStyle}>
                                    <option value="">{t("form.selectPlaceholder")}</option>
                                    {US_STATES.map((s) => (
                                        <option key={s.code} value={s.code}>{s.name}</option>
                                    ))}
                                </select>
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.city")}</label>
                                <input name="city" value={form.city} onChange={handleField} style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.county")}</label>
                                <select name="county" value={form.county} onChange={handleField} disabled={!form.state} style={inputStyle}>
                                    <option value="">{form.state ? t("form.selectPlaceholder") : t("form.selectStateFirst")}</option>
                                    {countiesForState.map((c) => (
                                        <option key={c} value={c}>{c}</option>
                                    ))}
                                </select>
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.maritalStatus")}</label>
                                <select name="maritalStatus" value={form.maritalStatus} onChange={handleField} style={inputStyle}>
                                    <option value="">{t("form.selectPlaceholder")}</option>
                                    {MARITAL_STATUSES.map((m) => (
                                        <option key={m} value={m}>{translateEnum("maritalStatus", m)}</option>
                                    ))}
                                </select>
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.occupation")}</label>
                                <input name="occupation" value={form.occupation} onChange={handleField} style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.greenCard")}</label>
                                <input name="greenCard" value={form.greenCard} onChange={handleField} style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.workPermit")}</label>
                                <input name="workPermit" value={form.workPermit} onChange={handleField} style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.employerName")}</label>
                                <input name="employerName" value={form.employerName} onChange={handleField} style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.companyPhone")}</label>
                                <input name="companyPhone" value={form.companyPhone} onChange={handleField} style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.annualIncome")}</label>
                                <input type="number" min="0" step="0.01" name="annualIncome" value={form.annualIncome} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.contactLanguage")}</label>
                                <select name="contactLanguage" value={form.contactLanguage} onChange={handleField} style={inputStyle}>
                                    <option value="">{t("form.selectPlaceholder")}</option>
                                    {CONTACT_LANGUAGES.map((l) => (
                                        <option key={l} value={l}>{translateEnum("contactLanguage", l)}</option>
                                    ))}
                                </select>
                            </div>

                            <div style={{ gridColumn: "1 / -1" }}>
                                <label style={labelStyle}>{t("form.fields.tags")}</label>
                                <input name="tags" value={form.tags} onChange={handleField} style={inputStyle} />
                            </div>

                            {userIsAdmin && (
                                <>
                                    <div>
                                        <label style={labelStyle}>{t("form.fields.agent")}</label>
                                        <select name="agentId" value={form.agentId} onChange={handleField} style={inputStyle}>
                                            <option value="">{t("form.unassigned")}</option>
                                            {agents.map((a) => (
                                                <option key={a.id} value={a.id}>{a.nombre}</option>
                                            ))}
                                        </select>
                                    </div>

                                    <div>
                                        <label style={labelStyle}>{t("form.fields.assistantAgent")}</label>
                                        <select name="assistantAgentId" value={form.assistantAgentId} onChange={handleField} style={inputStyle}>
                                            <option value="">{t("form.unassigned")}</option>
                                            {agents.map((a) => (
                                                <option key={a.id} value={a.id}>{a.nombre}</option>
                                            ))}
                                        </select>
                                    </div>

                                    <div>
                                        <label style={labelStyle}>{t("form.fields.recordAgent")}</label>
                                        <select name="recordAgentId" value={form.recordAgentId} onChange={handleField} style={inputStyle}>
                                            <option value="">{t("form.unassigned")}</option>
                                            {encargados.map((a) => (
                                                <option key={a.id} value={a.id}>{a.nombre}</option>
                                            ))}
                                        </select>
                                    </div>
                                </>
                            )}

                        </div>

                        {formError && <p style={{ color: "red", marginTop: 12 }}>{formError}</p>}

                        <button
                            type="submit"
                            disabled={submitting}
                            style={{ marginTop: 16, background: "#2563eb", color: "white", padding: "9px 20px", border: "none", borderRadius: 6, cursor: "pointer" }}
                        >
                            {editingId
                                ? (submitting ? t("common:actions.saving") : t("form.submitEdit"))
                                : (submitting ? t("common:actions.creating") : t("form.submitCreate"))}
                        </button>
                    </form>
                </div>
            )}

            {loading ? (
                <p>{t("loading")}</p>
            ) : customers.length === 0 ? (
                <p>{t("empty")}</p>
            ) : (
                <div style={{ display: "grid", gap: 12 }}>
                    {customers.map((c) => (
                        <div key={c.id} style={{ border: "1px solid #ddd", borderRadius: 10, padding: 16, background: "white" }}>
                            <div style={{ fontWeight: "bold", fontSize: 16, marginBottom: 6 }}>
                                {c.firstName} {c.lastName}
                            </div>
                            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "3px 20px", fontSize: 14, color: "#444" }}>
                                <span>{t("card.ssn")}: {c.socialSecurityNumber}</span>
                                <span>{t("card.birth")}: {c.dateOfBirth?.substring(0, 10)}</span>
                                <span>{t("card.email")}: {c.email}</span>
                                <span>{t("card.phone")}: {c.phone}</span>
                                <span style={{ gridColumn: "1 / -1" }}>{t("card.address")}: {[c.address1, c.address2].filter(Boolean).join(", ")}</span>
                                <span>{t("card.status")}: {translateEnum("migrationStatus", c.migrationStatus)}</span>
                                <span>{t("card.relacionConPrincipal")}: {translateEnum("relacionConPrincipal", c.relacionConPrincipal)}</span>
                                <span>{t("card.zipCode")}: {c.zipCode || "-"}</span>
                                <span>{t("card.locationLabel")}: {[c.city, c.county, c.state].filter(Boolean).join(", ") || "-"}</span>
                                <span>{t("card.maritalStatus")}: {translateEnum("maritalStatus", c.maritalStatus) || "-"}</span>
                                <span>{t("card.occupation")}: {c.occupation || "-"}</span>
                                <span>{t("card.middleName")}: {c.middleName || "-"}</span>
                                <span>{t("card.gender")}: {translateEnum("gender", c.gender) || "-"}</span>
                                <span>{t("card.greenCard")}: {c.greenCard || "-"}</span>
                                <span>{t("card.workPermit")}: {c.workPermit || "-"}</span>
                                <span>{t("card.employerName")}: {c.employerName || "-"}</span>
                                <span>{t("card.companyPhone")}: {c.companyPhone || "-"}</span>
                                <span>{t("card.annualIncome")}: {c.annualIncome}</span>
                                <span>{t("card.tags")}: {c.tags || "-"}</span>
                                <span>{t("card.contactLanguage")}: {translateEnum("contactLanguage", c.contactLanguage) || "-"}</span>
                                <span>{t("card.agent")}: {c.agentName || "-"}</span>
                                {userIsAdmin && <span>{t("card.assistantAgent")}: {c.assistantAgentName || "-"}</span>}
                                {userIsAdmin && <span>{t("card.recordAgent")}: {c.recordAgentName || "-"}</span>}
                                <span>{t("card.policiesCount")}: {c.policiesCount}</span>
                            </div>
                            <div style={{ marginTop: 10, display: "flex", gap: 8 }}>
                                <button onClick={() => handleEdit(c)} style={{ background: "#007bff", color: "white", border: "none", padding: "5px 12px", borderRadius: 5, cursor: "pointer" }}>
                                    {t("common:actions.edit")}
                                </button>
                                <button onClick={() => handleDelete(c.id)} style={{ background: "#dc2626", color: "white", border: "none", padding: "5px 12px", borderRadius: 5, cursor: "pointer" }}>
                                    {t("common:actions.delete")}
                                </button>
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}

export default Customers;
