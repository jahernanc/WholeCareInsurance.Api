import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { apiFetch } from "../api";
import { translateEnum } from "../i18n/translateEnum";
import Modal from "../components/Modal";
import { US_STATES } from "../data/usStates";
import US_COUNTIES from "../data/usCounties.json";
import { GENDERS } from "../data/customerFormOptions";
import { CONTRACT_INTERESTS, AGENCIES, emptyAgentForm } from "../data/agentFormOptions";
import { detailSectionHeaderStyle, detailRowStyle } from "../utils/detailModalStyles";
import { tableHeaderRowStyle, tableCellStyle, actionsCellStyle, actionButtonStyle } from "../utils/tableStyles";

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
    const [search, setSearch] = useState("");
    const [viewingUser, setViewingUser] = useState(null);
    const [togglingId, setTogglingId] = useState(null);
    const [page, setPage] = useState(1);
    const [totalCount, setTotalCount] = useState(0);
    const [totalPages, setTotalPages] = useState(1);

    const countiesForState = form.state ? (US_COUNTIES[form.state] ?? []) : [];

    const loadUsers = async (searchOverride, pageOverride) => {
        try {
            setLoading(true);
            const effectiveSearch = searchOverride !== undefined ? searchOverride : search;
            const effectivePage = pageOverride ?? page;
            const params = new URLSearchParams();
            if (effectiveSearch) params.set("search", effectiveSearch);
            params.set("page", String(effectivePage));
            const res = await apiFetch(`/users?${params.toString()}`);
            if (!res.ok) throw new Error();
            const data = await res.json();
            setUsers(data.items);
            setTotalCount(data.totalCount);
            setTotalPages(data.totalPages);
            setPage(data.page);
        } catch {
            console.error("No se pudieron cargar los agentes");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { loadUsers(); }, []);

    const handleSearch = () => loadUsers(undefined, 1);
    const handleClearSearch = () => {
        setSearch("");
        loadUsers("", 1);
    };
    const handlePageChange = (newPage) => {
        if (newPage < 1 || newPage > totalPages) return;
        loadUsers(undefined, newPage);
    };

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
            isActive: u.isActive ?? true,
            middleName: u.middleName ?? "",
            gender: u.gender ?? "",
            agency: u.agency ?? "",
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
                isActive: form.isActive,
                middleName: form.middleName,
                gender: form.gender,
                agency: form.agency,
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

    const openDetail = (u) => setViewingUser(u);
    const closeDetail = () => setViewingUser(null);

    // Baja lógica (§17) — reenvía el objeto completo con isActive invertido,
    // igual criterio que handleSubmit (UserUpdateDto no admite parcial).
    const handleToggleActive = async (u) => {
        const confirmMessage = u.isActive ? t("deactivateConfirm") : t("activateConfirm");
        if (!confirm(confirmMessage)) return;

        try {
            setTogglingId(u.id);
            const res = await apiFetch(`/users/${u.id}`, {
                method: "PUT",
                body: JSON.stringify({
                    nombre: u.nombre,
                    email: u.email,
                    rol: u.rol,
                    isEncargado: u.isEncargado,
                    isActive: !u.isActive,
                    middleName: u.middleName,
                    gender: u.gender,
                    agency: u.agency,
                    address1: u.address1,
                    address2: u.address2,
                    city: u.city,
                    zipCode: u.zipCode,
                    state: u.state,
                    county: u.county,
                    licensed: u.licensed,
                    licenseNumber: u.licenseNumber,
                    npnNumber: u.npnNumber,
                    npnOverride: u.npnOverride,
                    hasCompanyContract: u.hasCompanyContract,
                    contractNumber: u.contractNumber,
                    companyName: u.companyName,
                    contractsWanted: u.contractsWanted,
                    additionalInformation: u.additionalInformation,
                    termsAccepted: u.termsAccepted,
                }),
            });
            if (!res.ok) throw new Error();
            await loadUsers();
        } catch {
            alert(t("toggleActiveError"));
        } finally {
            setTogglingId(null);
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

            <div style={{ display: "flex", gap: 8, alignItems: "flex-end", marginBottom: 20 }}>
                <div>
                    <label style={{ display: "block", fontSize: 12 }}>{t("search.label")}</label>
                    <input
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                        onKeyDown={(e) => { if (e.key === "Enter") handleSearch(); }}
                        placeholder={t("search.placeholder")}
                        style={{ padding: "7px 10px", borderRadius: 5, border: "1px solid #ccc", minWidth: 240 }}
                    />
                </div>
                <button type="button" onClick={handleSearch} style={{ background: "#2563eb", color: "white", padding: "8px 14px", border: "none", borderRadius: 6, cursor: "pointer" }}>
                    {t("search.searchButton")}
                </button>
                <button type="button" onClick={handleClearSearch} style={{ background: "#e5e7eb", color: "#333", padding: "8px 14px", border: "none", borderRadius: 6, cursor: "pointer" }}>
                    {t("search.clearButton")}
                </button>
            </div>

            <Modal open={showForm} onClose={() => setShowForm(false)} maxWidth={720}>
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

                            <div>
                                <label style={labelStyle}>{t("form.fields.agency")}</label>
                                <select name="agency" value={form.agency} onChange={handleField} style={inputStyle}>
                                    <option value="">{t("form.selectPlaceholder")}</option>
                                    {AGENCIES.map((a) => (
                                        <option key={a} value={a}>{translateEnum("agency", a)}</option>
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
                            style={{ marginTop: 16, background: "#16a34a", color: "white", padding: "9px 20px", border: "none", borderRadius: 6, cursor: "pointer" }}
                        >
                            {editingId
                                ? (submitting ? t("common:actions.saving") : t("common:actions.saveChanges"))
                                : (submitting ? t("common:actions.creating") : t("form.submitCreate"))}
                        </button>
                    </form>
            </Modal>

            {loading ? (
                <p>{t("loading")}</p>
            ) : users.length === 0 ? (
                <p>{t("empty")}</p>
            ) : (
                <div style={{ overflowX: "auto" }}>
                    <table style={{ width: "100%", borderCollapse: "collapse" }}>
                        <thead>
                            <tr style={tableHeaderRowStyle}>
                                <th style={tableCellStyle}>{t("table.fullName")}</th>
                                <th style={tableCellStyle}>{t("table.email")}</th>
                                <th style={tableCellStyle}>{t("table.agency")}</th>
                                <th style={tableCellStyle}>{t("table.companyName")}</th>
                                <th style={tableCellStyle}>{t("table.licenseNumber")}</th>
                                <th style={tableCellStyle}>{t("table.actions")}</th>
                            </tr>
                        </thead>
                        <tbody>
                            {users.map((u) => (
                                <tr key={u.id}>
                                    <td style={tableCellStyle}>
                                        {u.nombre}{" "}
                                        <span
                                            style={{
                                                marginLeft: 6,
                                                fontSize: 11,
                                                padding: "2px 8px",
                                                borderRadius: 999,
                                                background: u.isActive ? "#dcfce7" : "#f3f4f6",
                                                color: u.isActive ? "#166534" : "#6b7280",
                                            }}
                                        >
                                            {u.isActive ? t("card.active") : t("card.inactive")}
                                        </span>
                                    </td>
                                    <td style={tableCellStyle}>{u.email}</td>
                                    <td style={tableCellStyle}>{u.agency ? translateEnum("agency", u.agency) : "-"}</td>
                                    <td style={tableCellStyle}>{u.companyName || "-"}</td>
                                    <td style={tableCellStyle}>{u.licenseNumber || "-"}</td>
                                    <td style={tableCellStyle}>
                                        <div style={actionsCellStyle}>
                                            <button onClick={() => handleEdit(u)} title={t("actionTitles.edit")} style={actionButtonStyle}>
                                                ✏️
                                            </button>
                                            <button onClick={() => openDetail(u)} title={t("actionTitles.viewDetails")} style={actionButtonStyle}>
                                                🔍
                                            </button>
                                            <button
                                                onClick={() => handleToggleActive(u)}
                                                disabled={togglingId === u.id}
                                                title={u.isActive ? t("actionTitles.deactivate") : t("actionTitles.activate")}
                                                style={actionButtonStyle}
                                            >
                                                {u.isActive ? "🗑" : "♻️"}
                                            </button>
                                        </div>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>

                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginTop: 12, fontSize: 14 }}>
                        <span>
                            {t("pagination.showing", {
                                from: (page - 1) * 10 + 1,
                                to: Math.min(page * 10, totalCount),
                                total: totalCount,
                            })}
                        </span>
                        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                            <button
                                type="button"
                                onClick={() => handlePageChange(page - 1)}
                                disabled={page <= 1}
                                style={{ padding: "6px 10px", cursor: page <= 1 ? "default" : "pointer" }}
                            >
                                {t("pagination.previous")}
                            </button>
                            <span>{t("pagination.pageInfo", { page, totalPages })}</span>
                            <button
                                type="button"
                                onClick={() => handlePageChange(page + 1)}
                                disabled={page >= totalPages}
                                style={{ padding: "6px 10px", cursor: page >= totalPages ? "default" : "pointer" }}
                            >
                                {t("pagination.next")}
                            </button>
                        </div>
                    </div>
                </div>
            )}

            {viewingUser && (
                <Modal open={true} onClose={closeDetail} maxWidth={500}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16, paddingBottom: 12, borderBottom: "1px solid #e5e7eb" }}>
                        <h3 style={{ margin: 0 }}>{viewingUser.nombre}</h3>
                        <button
                            onClick={closeDetail}
                            style={{ background: "transparent", border: "none", cursor: "pointer", fontSize: 18, color: "#6b7280" }}
                        >
                            ✕
                        </button>
                    </div>

                    <h4 style={detailSectionHeaderStyle}>{t("detail.contactSection")}</h4>
                    <p style={detailRowStyle}>{t("card.email")}: {viewingUser.email}</p>
                    <p style={detailRowStyle}>{t("card.role")}: {translateEnum("userRol", viewingUser.rol)}</p>
                    <p style={detailRowStyle}>{t("card.isEncargado")}: {viewingUser.isEncargado ? t("card.yes") : t("card.no")}</p>
                    <p style={detailRowStyle}>{t("card.isActive")}: {viewingUser.isActive ? t("card.active") : t("card.inactive")}</p>

                    <h4 style={detailSectionHeaderStyle}>{t("detail.addressSection")}</h4>
                    <p style={detailRowStyle}>{t("card.address1")}: {viewingUser.address1 || "-"}</p>
                    <p style={detailRowStyle}>{t("card.address2")}: {viewingUser.address2 || "-"}</p>
                    <p style={detailRowStyle}>{t("card.location")}: {[viewingUser.city, viewingUser.county, viewingUser.state].filter(Boolean).join(", ") || "-"}</p>
                    <p style={detailRowStyle}>{t("card.zipCode")}: {viewingUser.zipCode || "-"}</p>

                    <h4 style={detailSectionHeaderStyle}>{t("detail.licenseSection")}</h4>
                    <p style={detailRowStyle}>{t("card.licensed")}: {viewingUser.licensed ? t("card.yes") : t("card.no")}</p>
                    <p style={detailRowStyle}>{t("card.licenseNumber")}: {viewingUser.licenseNumber || "-"}</p>
                    <p style={detailRowStyle}>{t("card.npnNumber")}: {viewingUser.npnNumber || "-"}</p>
                    <p style={detailRowStyle}>{t("card.npnOverride")}: {viewingUser.npnOverride ? t("card.yes") : t("card.no")}</p>

                    <h4 style={detailSectionHeaderStyle}>{t("detail.contractSection")}</h4>
                    <p style={detailRowStyle}>{t("card.agency")}: {viewingUser.agency ? translateEnum("agency", viewingUser.agency) : "-"}</p>
                    <p style={detailRowStyle}>{t("card.hasCompanyContract")}: {viewingUser.hasCompanyContract ? t("card.yes") : t("card.no")}</p>
                    <p style={detailRowStyle}>{t("card.contractNumber")}: {viewingUser.contractNumber || "-"}</p>
                    <p style={detailRowStyle}>{t("card.companyName")}: {viewingUser.companyName || "-"}</p>
                    <p style={detailRowStyle}>
                        {t("card.contractsWanted")}: {viewingUser.contractsWanted
                            ? viewingUser.contractsWanted.split(",").filter(Boolean).map((i) => translateEnum("contractInterest", i)).join(", ")
                            : "-"}
                    </p>

                    <h4 style={detailSectionHeaderStyle}>{t("detail.otherSection")}</h4>
                    <p style={detailRowStyle}>{t("card.additionalInformation")}: {viewingUser.additionalInformation || "-"}</p>
                    <p style={detailRowStyle}>{t("card.termsAccepted")}: {viewingUser.termsAccepted ? t("card.yes") : t("card.no")}</p>
                </Modal>
            )}
        </div>
    );
}

export default Agentes;
