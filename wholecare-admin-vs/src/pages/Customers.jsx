import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { apiFetch, isAdmin } from "../api";
import { translateEnum } from "../i18n/translateEnum";
import Modal from "../components/Modal";
import ActionsMenu from "../components/ActionsMenu";
import CustomerFormFields from "../components/CustomerFormFields";
import MaskedText from "../components/MaskedText";
import { emptyCustomerForm } from "../data/customerFormOptions";
import { detailSectionHeaderStyle, detailRowStyle } from "../utils/detailModalStyles";
import { tableHeaderRowStyle, tableCellStyle } from "../utils/tableStyles";
import { buildWhatsAppUrl } from "../utils/whatsapp";

const API = "/api/customers";

function Customers() {
    const { t } = useTranslation(["customers", "common"]);
    const [customers, setCustomers] = useState([]);
    const [agents, setAgents] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);
    const [form, setForm] = useState(emptyCustomerForm);
    const [editingId, setEditingId] = useState(null);
    const [formError, setFormError] = useState("");
    const [submitting, setSubmitting] = useState(false);
    const [viewingCustomer, setViewingCustomer] = useState(null);
    const [page, setPage] = useState(1);
    const [totalCount, setTotalCount] = useState(0);
    const [totalPages, setTotalPages] = useState(1);

    const userIsAdmin = isAdmin();

    const loadCustomers = async (pageOverride) => {
        const effectivePage = pageOverride ?? page;
        try {
            setLoading(true);
            const res = await apiFetch(`${API}?page=${effectivePage}`);
            if (!res.ok) throw new Error();
            const data = await res.json();
            setCustomers(data.items);
            setTotalCount(data.totalCount);
            setTotalPages(data.totalPages);
            setPage(data.page);
        } catch {
            console.error("No se pudieron cargar los clientes");
        } finally {
            setLoading(false);
        }
    };

    const handlePageChange = (newPage) => {
        if (newPage < 1 || newPage > totalPages) return;
        loadCustomers(newPage);
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
        // queueMicrotask (§20): loadCustomers() setea loading síncronamente antes de su
        // primer await — ver mismo comentario en Dashboard.jsx.
        queueMicrotask(() => {
            loadCustomers();
            if (userIsAdmin) loadAgents();
        });
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
        setForm(emptyCustomerForm);
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
                setFormError(res.errorMessage ?? t("form.saveError"));
                return;
            }

            setShowForm(false);
            setForm(emptyCustomerForm);
            setEditingId(null);
            await loadCustomers();
        } catch {
            setFormError(t("form.saveError"));
        } finally {
            setSubmitting(false);
        }
    };

    const openDetail = (c) => setViewingCustomer(c);
    const closeDetail = () => setViewingCustomer(null);

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

            <Modal open={showForm} onClose={() => setShowForm(false)} maxWidth={560}>
                <h3 style={{ marginTop: 0 }}>{editingId ? t("form.titleEdit") : t("form.titleCreate")}</h3>

                <form onSubmit={handleSubmit}>
                    <CustomerFormFields form={form} onFieldChange={handleField} agents={agents} userIsAdmin={userIsAdmin} />

                    {formError && <p style={{ color: "red", marginTop: 12 }}>{formError}</p>}

                    <button
                        type="submit"
                        disabled={submitting}
                        style={{ marginTop: 16, background: "#16a34a", color: "white", padding: "9px 20px", border: "none", borderRadius: 6, cursor: "pointer" }}
                    >
                        {editingId
                            ? (submitting ? t("common:actions.saving") : t("form.submitEdit"))
                            : (submitting ? t("common:actions.creating") : t("form.submitCreate"))}
                    </button>
                </form>
            </Modal>

            {loading ? (
                <p>{t("loading")}</p>
            ) : customers.length === 0 ? (
                <p>{t("empty")}</p>
            ) : (
                <div style={{ overflowX: "auto" }}>
                    <table style={{ width: "100%", borderCollapse: "collapse" }}>
                        <thead>
                            <tr style={tableHeaderRowStyle}>
                                <th style={tableCellStyle}>{t("table.fullName")}</th>
                                <th style={tableCellStyle}>{t("table.migrationStatus")}</th>
                                <th style={tableCellStyle}>{t("table.phone")}</th>
                                <th style={tableCellStyle}>{t("table.email")}</th>
                                <th style={tableCellStyle}>{t("table.actions")}</th>
                            </tr>
                        </thead>
                        <tbody>
                            {customers.map((c) => (
                                <tr key={c.id}>
                                    <td style={tableCellStyle}>{c.firstName} {c.lastName}</td>
                                    <td style={tableCellStyle}>{translateEnum("migrationStatus", c.migrationStatus)}</td>
                                    <td style={tableCellStyle}>{c.phone}</td>
                                    <td style={tableCellStyle}>{c.email}</td>
                                    <td style={tableCellStyle}>
                                        <ActionsMenu
                                            items={[
                                                { icon: "✏️", label: t("actionTitles.edit"), onClick: () => handleEdit(c) },
                                                { icon: "🔍", label: t("actionTitles.viewDetails"), onClick: () => openDetail(c) },
                                                ...(c.phone
                                                    ? [{ icon: "💬", label: t("actionTitles.chatWhatsapp"), href: buildWhatsAppUrl(c.phone, t("whatsappMessage")), external: true }]
                                                    : []),
                                                { icon: "🗑", label: t("actionTitles.delete"), onClick: () => handleDelete(c.id) },
                                            ]}
                                        />
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

            {viewingCustomer && (
                <Modal open={true} onClose={closeDetail} maxWidth={500}>
                    <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16, paddingBottom: 12, borderBottom: "1px solid #e5e7eb" }}>
                        <h3 style={{ margin: 0 }}>{viewingCustomer.firstName} {viewingCustomer.lastName}</h3>
                        <button
                            onClick={closeDetail}
                            style={{ background: "transparent", border: "none", cursor: "pointer", fontSize: 18, color: "#6b7280" }}
                        >
                            ✕
                        </button>
                    </div>

                    <h4 style={detailSectionHeaderStyle}>{t("detail.personalSection")}</h4>
                    <p style={detailRowStyle}>{t("card.ssn")}: <MaskedText value={viewingCustomer.socialSecurityNumber} /></p>
                    <p style={detailRowStyle}>{t("card.birth")}: {viewingCustomer.dateOfBirth?.substring(0, 10)}</p>
                    <p style={detailRowStyle}>{t("card.middleName")}: {viewingCustomer.middleName || "-"}</p>
                    <p style={detailRowStyle}>{t("card.gender")}: {translateEnum("gender", viewingCustomer.gender) || "-"}</p>
                    <p style={detailRowStyle}>{t("card.status")}: {translateEnum("migrationStatus", viewingCustomer.migrationStatus)}</p>
                    <p style={detailRowStyle}>{t("card.greenCard")}: {viewingCustomer.greenCard || "-"}</p>
                    <p style={detailRowStyle}>{t("card.workPermit")}: {viewingCustomer.workPermit || "-"}</p>
                    <p style={detailRowStyle}>{t("card.maritalStatus")}: {translateEnum("maritalStatus", viewingCustomer.maritalStatus) || "-"}</p>
                    <p style={detailRowStyle}>{t("card.occupation")}: {viewingCustomer.occupation || "-"}</p>

                    <h4 style={detailSectionHeaderStyle}>{t("detail.contactSection")}</h4>
                    <p style={detailRowStyle}>{t("card.email")}: {viewingCustomer.email}</p>
                    <p style={detailRowStyle}>{t("card.phone")}: {viewingCustomer.phone}</p>
                    <p style={detailRowStyle}>{t("card.address")}: {[viewingCustomer.address1, viewingCustomer.address2].filter(Boolean).join(", ")}</p>
                    <p style={detailRowStyle}>{t("card.zipCode")}: {viewingCustomer.zipCode || "-"}</p>
                    <p style={detailRowStyle}>{t("card.locationLabel")}: {[viewingCustomer.city, viewingCustomer.county, viewingCustomer.state].filter(Boolean).join(", ") || "-"}</p>

                    <h4 style={detailSectionHeaderStyle}>{t("detail.employmentSection")}</h4>
                    <p style={detailRowStyle}>{t("card.employerName")}: {viewingCustomer.employerName || "-"}</p>
                    <p style={detailRowStyle}>{t("card.companyPhone")}: {viewingCustomer.companyPhone || "-"}</p>
                    <p style={detailRowStyle}>{t("card.annualIncome")}: {viewingCustomer.annualIncome}</p>

                    <h4 style={detailSectionHeaderStyle}>{t("detail.otherSection")}</h4>
                    <p style={detailRowStyle}>{t("card.relacionConPrincipal")}: {translateEnum("relacionConPrincipal", viewingCustomer.relacionConPrincipal)}</p>
                    <p style={detailRowStyle}>{t("card.tags")}: {viewingCustomer.tags || "-"}</p>
                    <p style={detailRowStyle}>{t("card.contactLanguage")}: {translateEnum("contactLanguage", viewingCustomer.contactLanguage) || "-"}</p>

                    <h4 style={detailSectionHeaderStyle}>{t("detail.agentsSection")}</h4>
                    <p style={detailRowStyle}>{t("card.agent")}: {viewingCustomer.agentName || "-"}</p>
                    {userIsAdmin && <p style={detailRowStyle}>{t("card.assistantAgent")}: {viewingCustomer.assistantAgentName || "-"}</p>}
                    {userIsAdmin && <p style={detailRowStyle}>{t("card.recordAgent")}: {viewingCustomer.recordAgentName || "-"}</p>}

                    <h4 style={detailSectionHeaderStyle}>{t("detail.policiesSection")}</h4>
                    <p style={detailRowStyle}>{t("card.policiesCount")}: {viewingCustomer.policiesCount}</p>
                </Modal>
            )}
        </div>
    );
}

export default Customers;
