import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { useOutletContext } from "react-router-dom";
import { apiFetch } from "../api";
import { translateEnum } from "../i18n/translateEnum";

const POLICY_TYPES = ["Obama Care", "Salud", "Auto", "Otro"];
const INSURANCE_COMPANIES = ["WholeCareInsurance", "Otro"];
const POLICY_STATUSES = ["Draft", "Pendiente", "Cancelado", "Por procesar", "En proceso", "En corrección", "Procesado", "Cambio de agente"];
const ALLOWED_DOCUMENT_EXTENSIONS = [".pdf", ".docx", ".jpg", ".jpeg"];
const MAX_DOCUMENT_SIZE_BYTES = 5 * 1024 * 1024;

const formatFileSize = (bytes) => `${(bytes / 1024).toFixed(2)} KB`;

const formatDocumentDate = (iso) => {
    const d = new Date(iso);
    const pad = (n) => String(n).padStart(2, "0");
    const month = pad(d.getMonth() + 1);
    const day = pad(d.getDate());
    const year = d.getFullYear();
    let hours = d.getHours();
    const ampm = hours >= 12 ? "PM" : "AM";
    hours = hours % 12 || 12;
    const minutes = pad(d.getMinutes());
    return `${month}/${day}/${year} ${pad(hours)}:${minutes} ${ampm}`;
};

function Policies() {
    const { t } = useTranslation(["policies", "common"]);
    const { period } = useOutletContext();
    const [policies, setPolicies] = useState([]);
    const [customers, setCustomers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);

    const [policyNumber, setPolicyNumber] = useState("");
    const [type, setType] = useState("");
    const [insuranceCompany, setInsuranceCompany] = useState("");
    const [startDate, setStartDate] = useState("");
    const [endDate, setEndDate] = useState("");
    const [premium, setPremium] = useState("");
    const [status, setStatus] = useState("Draft");
    const [customerId, setCustomerId] = useState("");
    // Período no es editable en el formulario: se toma del selector del header al
    // crear, y se conserva el valor ya guardado de la póliza al editar.
    const [formPeriod, setFormPeriod] = useState(period);
    const [numberOfApplicants, setNumberOfApplicants] = useState("");

    const [formError, setFormError] = useState("");
    const [submitting, setSubmitting] = useState(false);
    const [editingId, setEditingId] = useState(null);

    const [dependents, setDependents] = useState([]);
    const [dependentQuery, setDependentQuery] = useState("");
    const [showDependentPicker, setShowDependentPicker] = useState(false);

    const [filterPolicyNumber, setFilterPolicyNumber] = useState("");
    const [filterFirstName, setFilterFirstName] = useState("");
    const [filterLastName, setFilterLastName] = useState("");
    const [filterStatus, setFilterStatus] = useState("");
    const [filterType, setFilterType] = useState("");
    const [filterInsuranceCompany, setFilterInsuranceCompany] = useState("");

    const [viewingPolicy, setViewingPolicy] = useState(null);
    const [detailDependents, setDetailDependents] = useState([]);
    const [detailDocuments, setDetailDocuments] = useState([]);
    const [documentError, setDocumentError] = useState("");
    const [uploadingDocument, setUploadingDocument] = useState(false);
    const [openDocMenuId, setOpenDocMenuId] = useState(null);

    const getCustomer = (id) => customers.find((c) => c.id === Number(id));

    const getCustomerName = (id) => {
        const customer = customers.find((c) => c.id === Number(id));
        return customer ? `${customer.firstName} ${customer.lastName}` : "Unknown";
    };

    const getCustomerPhone = (id) => {
        const customer = customers.find((c) => c.id === Number(id));
        return customer ? customer.phone : null;
    };

    const buildWhatsAppUrl = (phone) => {
        const digits = phone.replace(/\D/g, "");
        const message = encodeURIComponent(t("whatsappMessage"));
        return `https://wa.me/${digits}?text=${message}`;
    };

    const loadData = async (filterOverrides = {}) => {
        const filters = {
            policyNumber: filterPolicyNumber,
            firstName: filterFirstName,
            lastName: filterLastName,
            status: filterStatus,
            type: filterType,
            insuranceCompany: filterInsuranceCompany,
            period,
            ...filterOverrides,
        };

        const params = new URLSearchParams();
        Object.entries(filters).forEach(([key, value]) => {
            if (value) params.set(key, value);
        });
        const query = params.toString();

        try {
            setLoading(true);

            const [policiesRes, customersRes] = await Promise.all([
                apiFetch(`/api/policies${query ? `?${query}` : ""}`),
                apiFetch("/api/customers"),
            ]);

            if (!policiesRes.ok) {
                throw new Error("Could not load policies");
            }

            if (!customersRes.ok) {
                throw new Error("Could not load customers");
            }

            const policiesData = await policiesRes.json();
            const customersData = await customersRes.json();

            setPolicies(policiesData);
            setCustomers(customersData);
        } catch (error) {
            console.error("ERROR loading policies data:", error);
        } finally {
            setLoading(false);
        }
    };

    const handleSearch = () => loadData();

    const handleClearFilters = () => {
        setFilterPolicyNumber("");
        setFilterFirstName("");
        setFilterLastName("");
        setFilterStatus("");
        setFilterType("");
        setFilterInsuranceCompany("");
        loadData({ policyNumber: "", firstName: "", lastName: "", status: "", type: "", insuranceCompany: "" });
    };
    const loadDependents = async (policyId) => {
        try {
            const res = await apiFetch(`/api/policies/${policyId}/dependents`);
            if (!res.ok) throw new Error();
            setDependents(await res.json());
        } catch (error) {
            console.error("Error loading dependents:", error);
        }
    };

    const openDetail = async (policy) => {
        setViewingPolicy(policy);
        setDetailDependents([]);
        setDetailDocuments([]);
        setDocumentError("");
        setOpenDocMenuId(null);
        try {
            const res = await apiFetch(`/api/policies/${policy.id}/dependents`);
            if (!res.ok) throw new Error();
            setDetailDependents(await res.json());
        } catch (error) {
            console.error("Error loading dependents for detail view:", error);
        }
        await loadDocuments(policy.id);
    };

    const closeDetail = () => {
        setViewingPolicy(null);
        setDetailDependents([]);
        setDetailDocuments([]);
        setDocumentError("");
        setOpenDocMenuId(null);
    };

    const loadDocuments = async (policyId) => {
        try {
            const res = await apiFetch(`/api/policies/${policyId}/documents`);
            if (!res.ok) throw new Error();
            setDetailDocuments(await res.json());
        } catch (error) {
            console.error("Error loading documents:", error);
        }
    };

    const handleUploadDocument = async (e) => {
        const file = e.target.files[0];
        e.target.value = "";
        if (!file) return;

        setDocumentError("");

        const extension = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();
        if (!ALLOWED_DOCUMENT_EXTENSIONS.includes(extension)) {
            setDocumentError(t("documents.invalidExtension"));
            return;
        }

        if (file.size > MAX_DOCUMENT_SIZE_BYTES) {
            setDocumentError(t("documents.tooLarge"));
            return;
        }

        const formData = new FormData();
        formData.append("file", file);

        try {
            setUploadingDocument(true);
            const res = await apiFetch(`/api/policies/${viewingPolicy.id}/documents`, {
                method: "POST",
                body: formData,
            });

            if (!res.ok) {
                const err = await res.json().catch(() => null);
                setDocumentError(typeof err === "string" ? err : t("documents.uploadError"));
                return;
            }

            await loadDocuments(viewingPolicy.id);
        } catch (error) {
            console.error(error);
            setDocumentError(t("documents.uploadError"));
        } finally {
            setUploadingDocument(false);
        }
    };

    const handleDownloadDocument = async (doc) => {
        try {
            const res = await apiFetch(`/api/policies/${viewingPolicy.id}/documents/${doc.id}`);
            if (!res.ok) throw new Error();

            const blob = await res.blob();
            const url = URL.createObjectURL(blob);
            const link = document.createElement("a");
            link.href = url;
            link.download = doc.originalFileName;
            document.body.appendChild(link);
            link.click();
            link.remove();
            URL.revokeObjectURL(url);
        } catch (error) {
            console.error(error);
            alert(t("documents.downloadError"));
        }
    };

    const handleDeleteDocument = async (doc) => {
        if (!confirm(t("documents.deleteConfirm", { name: doc.originalFileName }))) return;

        try {
            const res = await apiFetch(`/api/policies/${viewingPolicy.id}/documents/${doc.id}`, {
                method: "DELETE",
            });
            if (!res.ok) throw new Error();
            await loadDocuments(viewingPolicy.id);
        } catch (error) {
            console.error(error);
            alert(t("documents.deleteError"));
        }
    };

    const handleAddDependent = async (depCustomerId) => {
        try {
            const response = await apiFetch(`/api/policies/${editingId}/dependents`, {
                method: "POST",
                body: JSON.stringify({ customerId: depCustomerId }),
            });
            if (!response.ok) throw new Error("Error adding dependent");
            setDependentQuery("");
            await loadDependents(editingId);
        } catch (error) {
            console.error(error);
            alert(t("dependents.addError"));
        }
    };

    const handleToggleAplicante = async (depCustomerId, isAplicante) => {
        try {
            const response = await apiFetch(`/api/policies/${editingId}/dependents/${depCustomerId}`, {
                method: "PUT",
                body: JSON.stringify({ isAplicante }),
            });
            if (!response.ok) throw new Error("Error updating dependent");
            await loadDependents(editingId);
        } catch (error) {
            console.error(error);
            alert(t("dependents.updateError"));
        }
    };

    const handleRemoveDependent = async (depCustomerId) => {
        try {
            const response = await apiFetch(`/api/policies/${editingId}/dependents/${depCustomerId}`, {
                method: "DELETE",
            });
            if (!response.ok) throw new Error("Error removing dependent");
            await loadDependents(editingId);
        } catch (error) {
            console.error(error);
            alert(t("dependents.removeError"));
        }
    };

    const handleEdit = (policy) => {
        setEditingId(policy.id);

        setPolicyNumber(policy.policyNumber);
        setType(policy.type);
        setInsuranceCompany(policy.insuranceCompany);
        setStartDate(policy.startDate.slice(0, 10));
        setEndDate(policy.endDate.slice(0, 10));
        setPremium(policy.premium);
        setStatus(policy.status);
        setCustomerId(policy.customerId);
        setFormPeriod(policy.period);
        setNumberOfApplicants(policy.numberOfApplicants ?? "");

        setDependentQuery("");
        setShowDependentPicker(false);
        loadDependents(policy.id);

        setShowForm(true);
    };

    const handleDelete = async (id) => {
        if (!confirm(t("deleteConfirm"))) return;

        try {
            const response = await apiFetch(`/api/policies/${id}`, { method: "DELETE" });

            if (!response.ok) {
                throw new Error("Error deleting policy");
            }

            await loadData(); // refresca tabla

        } catch (error) {
            console.error(error);
            alert(t("deleteError"));
        }
    };


    useEffect(() => {
        if (!localStorage.getItem("accessToken")) return;
        // Re-carga cuando cambia el Período activo del header (filtra la lista).
        loadData();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [period]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setFormError("");

        if (
            !policyNumber.trim() ||
            !type.trim() ||
            !insuranceCompany.trim() ||
            !startDate ||
            !endDate ||
            !premium ||
            !status.trim() ||
            !customerId
        ) {
            setFormError(t("form.requiredError"));
            return;
        }

        try {
            setSubmitting(true);

            const url = editingId
                ? `/api/policies/${editingId}`
                : "/api/policies";

            const method = editingId ? "PUT" : "POST";

            const response = await apiFetch(url, {
                method: method,
                body: JSON.stringify({
                    policyNumber,
                    type,
                    insuranceCompany,
                    startDate,
                    endDate,
                    premium: Number(premium),
                    status,
                    customerId: Number(customerId),
                    period: formPeriod,
                    numberOfApplicants: numberOfApplicants === "" ? null : Number(numberOfApplicants),
                }),
            });


            if (!response.ok) {
                throw new Error("Error saving policy");
            }


            // ✅ limpiar
            setEditingId(null);
            setShowForm(false);


            setPolicyNumber("");
            setType("");
            setInsuranceCompany("");
            setStartDate("");
            setEndDate("");
            setPremium("");
            setStatus("Draft");
            setCustomerId("");
            setNumberOfApplicants("");

            setDependents([]);
            setDependentQuery("");
            setShowDependentPicker(false);

            await loadData();
        } catch (error) {

            console.error(error);
            setFormError(t("form.saveError"));

        } finally {
            setSubmitting(false);
        }
    };

    const dependentCandidates = customers.filter(
        (c) =>
            c.id !== Number(customerId) &&
            !dependents.some((d) => d.customerId === c.id) &&
            `${c.firstName} ${c.lastName}`.toLowerCase().includes(dependentQuery.toLowerCase())
    );

    if (loading) {
        return <p>{t("loading")}</p>;
    }

    return (
        <div>
            <h2 style={{ marginBottom: 20 }}>{t("title")}</h2>

            {/* ✅ BOTÓN */}
            <button
                onClick={() => {
                    // Al abrir el formulario para crear (no editar), el Período
                    // se toma del selector activo del header en ese momento.
                    if (!showForm && !editingId) setFormPeriod(period);
                    setShowForm(!showForm);
                }}
                type="button"
                style={{
                    marginBottom: 20,
                    background: "#2563eb",
                    color: "white",
                    padding: "8px 12px",
                    border: "none",
                    borderRadius: 6,
                    cursor: "pointer"
                }}
            >
                {showForm ? t("closeFormButton") : t("newButton")}
            </button>

            {/* ✅ FORMULARIO CONDICIONAL */}
            {
                showForm && (
                    <div
                        style={{
                            border: "1px solid #ddd",
                            borderRadius: 10,
                            padding: 20,
                            marginBottom: 30,
                            background: "#fafafa",
                            maxWidth: 600,
                        }}
                    >
                        <h3 style={{ marginTop: 0 }}>{t("form.title")}</h3>

                        <form onSubmit={handleSubmit}>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.policyNumber")}</label>
                                <input
                                    type="text"
                                    value={policyNumber}
                                    onChange={(e) => setPolicyNumber(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.type")}</label>
                                <select
                                    value={type}
                                    onChange={(e) => setType(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                >
                                    <option value="">{t("form.selectType")}</option>
                                    {POLICY_TYPES.map((t2) => (
                                        <option key={t2} value={t2}>{translateEnum("policyType", t2)}</option>
                                    ))}
                                </select>
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.insuranceCompany")}</label>
                                <select
                                    value={insuranceCompany}
                                    onChange={(e) => setInsuranceCompany(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                >
                                    <option value="">{t("form.selectInsuranceCompany")}</option>
                                    {INSURANCE_COMPANIES.map((ic) => (
                                        <option key={ic} value={ic}>{translateEnum("insuranceCompany", ic)}</option>
                                    ))}
                                </select>
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.startDate")}</label>
                                <input
                                    type="date"
                                    value={startDate}
                                    onChange={(e) => setStartDate(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.endDate")}</label>
                                <input
                                    type="date"
                                    value={endDate}
                                    onChange={(e) => setEndDate(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.premium")}</label>
                                <input
                                    type="number"
                                    value={premium}
                                    onChange={(e) => setPremium(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.status")}</label>
                                <select
                                    value={status}
                                    onChange={(e) => setStatus(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                >
                                    {POLICY_STATUSES.map((s) => (
                                        <option key={s} value={s}>{translateEnum("policyStatus", s)}</option>
                                    ))}
                                </select>
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>{t("form.fields.customer")}</label>
                                <select
                                    value={customerId}
                                    onChange={(e) => setCustomerId(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                >
                                    <option value="">{t("form.selectCustomer")}</option>
                                    {customers.map((c) => (
                                        <option key={c.id} value={c.id}>
                                            {c.firstName} {c.lastName} ({c.socialSecurityNumber})
                                        </option>
                                    ))}
                                </select>
                            </div>

                            {editingId && (
                                <div style={{ marginBottom: 12, borderTop: "1px solid #ddd", paddingTop: 12 }}>
                                    <label style={{ fontWeight: "bold" }}>{t("dependents.title")}</label>

                                    <div style={{ margin: "10px 0" }}>
                                        <label>{t("dependents.numberOfApplicants")}</label>
                                        <input
                                            type="number"
                                            min="0"
                                            value={numberOfApplicants}
                                            onChange={(e) => setNumberOfApplicants(e.target.value)}
                                            style={{ width: "100%", padding: 8, marginTop: 4 }}
                                        />
                                    </div>

                                    {dependents.length === 0 ? (
                                        <p style={{ color: "#666", margin: "8px 0" }}>{t("dependents.empty")}</p>
                                    ) : (
                                        <ul style={{ listStyle: "none", padding: 0, margin: "8px 0" }}>
                                            {dependents.map((d) => (
                                                <li
                                                    key={d.customerId}
                                                    style={{
                                                        display: "flex",
                                                        justifyContent: "space-between",
                                                        alignItems: "center",
                                                        padding: "6px 0",
                                                    }}
                                                >
                                                    <span>{d.firstName} {d.lastName}</span>
                                                    <label style={{ display: "flex", alignItems: "center", gap: 4, fontSize: 13 }}>
                                                        <input
                                                            type="checkbox"
                                                            checked={d.isAplicante}
                                                            onChange={(e) => handleToggleAplicante(d.customerId, e.target.checked)}
                                                        />
                                                        {t("dependents.isAplicante")}
                                                    </label>
                                                    <button
                                                        type="button"
                                                        onClick={() => handleRemoveDependent(d.customerId)}
                                                        title={t("dependents.removeTitle")}
                                                        style={{ background: "transparent", border: "none", cursor: "pointer", fontSize: 16 }}
                                                    >
                                                        🗑
                                                    </button>
                                                </li>
                                            ))}
                                        </ul>
                                    )}

                                    <button
                                        type="button"
                                        onClick={() => setShowDependentPicker(!showDependentPicker)}
                                        style={{
                                            background: "#2563eb",
                                            color: "white",
                                            padding: "6px 10px",
                                            border: "none",
                                            borderRadius: 6,
                                            cursor: "pointer",
                                            marginBottom: 8,
                                        }}
                                    >
                                        {showDependentPicker ? t("dependents.cancelButton") : t("dependents.addButton")}
                                    </button>

                                    {showDependentPicker && (
                                        <div>
                                            <input
                                                type="text"
                                                placeholder={t("dependents.searchPlaceholder")}
                                                value={dependentQuery}
                                                onChange={(e) => setDependentQuery(e.target.value)}
                                                style={{ width: "100%", padding: 8, marginBottom: 8 }}
                                            />
                                            <ul style={{ listStyle: "none", padding: 0, maxHeight: 160, overflowY: "auto" }}>
                                                {dependentCandidates.map((c) => (
                                                    <li
                                                        key={c.id}
                                                        style={{
                                                            display: "flex",
                                                            justifyContent: "space-between",
                                                            alignItems: "center",
                                                            padding: "6px 0",
                                                            borderBottom: "1px solid #eee",
                                                        }}
                                                    >
                                                        <span>{c.firstName} {c.lastName}</span>
                                                        <button
                                                            type="button"
                                                            onClick={() => handleAddDependent(c.id)}
                                                            style={{
                                                                background: "#16a34a",
                                                                color: "white",
                                                                border: "none",
                                                                borderRadius: 4,
                                                                padding: "4px 8px",
                                                                cursor: "pointer",
                                                            }}
                                                        >
                                                            {t("dependents.addAction")}
                                                        </button>
                                                    </li>
                                                ))}
                                                {dependentCandidates.length === 0 && (
                                                    <li style={{ color: "#666", padding: "6px 0" }}>{t("dependents.noMatches")}</li>
                                                )}
                                            </ul>
                                        </div>
                                    )}
                                </div>
                            )}

                            {formError && (
                                <p style={{ color: "red", marginBottom: 12 }}>
                                    {formError}
                                </p>
                            )}

                            <button type="submit" disabled={submitting}>
                                {submitting ? t("form.submitCreating") : t("form.submitCreate")}
                            </button>

                        </form>
                    </div>
                )
            }

            <div
                style={{
                    border: "1px solid #ddd",
                    borderRadius: 10,
                    padding: 16,
                    marginBottom: 20,
                    background: "#fafafa",
                    display: "flex",
                    flexWrap: "wrap",
                    gap: 10,
                    alignItems: "flex-end",
                }}
            >
                <div>
                    <label style={{ display: "block", fontSize: 12 }}>{t("filters.policyNumber")}</label>
                    <input
                        type="text"
                        value={filterPolicyNumber}
                        onChange={(e) => setFilterPolicyNumber(e.target.value)}
                        style={{ padding: 6 }}
                    />
                </div>

                <div>
                    <label style={{ display: "block", fontSize: 12 }}>{t("filters.firstName")}</label>
                    <input
                        type="text"
                        value={filterFirstName}
                        onChange={(e) => setFilterFirstName(e.target.value)}
                        style={{ padding: 6 }}
                    />
                </div>

                <div>
                    <label style={{ display: "block", fontSize: 12 }}>{t("filters.lastName")}</label>
                    <input
                        type="text"
                        value={filterLastName}
                        onChange={(e) => setFilterLastName(e.target.value)}
                        style={{ padding: 6 }}
                    />
                </div>

                <div>
                    <label style={{ display: "block", fontSize: 12 }}>{t("filters.status")}</label>
                    <select
                        value={filterStatus}
                        onChange={(e) => setFilterStatus(e.target.value)}
                        style={{ padding: 6 }}
                    >
                        <option value="">{t("filters.all")}</option>
                        {POLICY_STATUSES.map((s) => (
                            <option key={s} value={s}>{translateEnum("policyStatus", s)}</option>
                        ))}
                    </select>
                </div>

                <div>
                    <label style={{ display: "block", fontSize: 12 }}>{t("filters.type")}</label>
                    <select
                        value={filterType}
                        onChange={(e) => setFilterType(e.target.value)}
                        style={{ padding: 6 }}
                    >
                        <option value="">{t("filters.all")}</option>
                        {POLICY_TYPES.map((t2) => (
                            <option key={t2} value={t2}>{translateEnum("policyType", t2)}</option>
                        ))}
                    </select>
                </div>

                <div>
                    <label style={{ display: "block", fontSize: 12 }}>{t("filters.insuranceCompany")}</label>
                    <select
                        value={filterInsuranceCompany}
                        onChange={(e) => setFilterInsuranceCompany(e.target.value)}
                        style={{ padding: 6 }}
                    >
                        <option value="">{t("filters.all")}</option>
                        {INSURANCE_COMPANIES.map((ic) => (
                            <option key={ic} value={ic}>{translateEnum("insuranceCompany", ic)}</option>
                        ))}
                    </select>
                </div>

                <button
                    type="button"
                    onClick={handleSearch}
                    style={{
                        background: "#2563eb",
                        color: "white",
                        padding: "8px 12px",
                        border: "none",
                        borderRadius: 6,
                        cursor: "pointer",
                    }}
                >
                    {t("common:actions.search")}
                </button>

                <button
                    type="button"
                    onClick={handleClearFilters}
                    style={{
                        background: "transparent",
                        border: "1px solid #ccc",
                        padding: "8px 12px",
                        borderRadius: 6,
                        cursor: "pointer",
                    }}
                >
                    {t("common:actions.clear")}
                </button>
            </div>

            {/* ✅ TABLA SIEMPRE VISIBLE */}
            {
                policies.length === 0 ? (
                    <p>{t("empty")}</p>
                ) : (

                    <div style={{ overflowX: "auto" }}>
                        <table style={{ width: "100%", borderCollapse: "collapse" }}>

                            <thead>
                                <tr style={{ background: "#f3f4f6", textAlign: "left" }}>
                                    <th style={{ padding: 10 }}>{t("table.policy")}</th>
                                    <th style={{ padding: 10 }}>{t("table.type")}</th>
                                    <th style={{ padding: 10 }}>{t("table.insuranceCompany")}</th>
                                    <th style={{ padding: 10 }}>{t("table.status")}</th>
                                    <th style={{ padding: 10 }}>{t("table.period")}</th>
                                    <th style={{ padding: 10 }}>{t("table.premium")}</th>
                                    <th style={{ padding: 10 }}>{t("table.customer")}</th>
                                    <th style={{ padding: 10 }}>{t("table.actions")}</th>
                                </tr>
                            </thead>

                            <tbody>
                                {policies.map((p) => (
                                    <tr key={p.id}>
                                        <td style={{ padding: 10 }}>{p.policyNumber}</td>
                                        <td style={{ padding: 10 }}>{translateEnum("policyType", p.type)}</td>
                                        <td style={{ padding: 10 }}>{translateEnum("insuranceCompany", p.insuranceCompany)}</td>
                                        <td style={{ padding: 10 }}>{translateEnum("policyStatus", p.status)}</td>
                                        <td style={{ padding: 10 }}>{p.period}</td>
                                        <td style={{ padding: 10 }}>{p.premium}</td>
                                        <td style={{ padding: 10 }}>
                                            {getCustomerName(p.customerId)}
                                        </td>
                                        <td style={{ padding: 10 }}>

                                            <button
                                                onClick={() => openDetail(p)}
                                                title={t("actionTitles.viewDetails")}
                                                style={{
                                                    marginRight: 8,
                                                    background: "transparent",
                                                    border: "none",
                                                    cursor: "pointer",
                                                    fontSize: 16
                                                }}
                                            >
                                                🔍
                                            </button>

                                            <button
                                                onClick={() => handleEdit(p)}
                                                title={t("actionTitles.editPolicy")}
                                                style={{
                                                    marginRight: 8,
                                                    background: "transparent",
                                                    border: "none",
                                                    cursor: "pointer",
                                                    fontSize: 16
                                                }}
                                            >
                                                ✏️
                                            </button>



                                            <button
                                                onClick={() => handleDelete(p.id)}
                                                title={t("actionTitles.deletePolicy")}
                                                style={{
                                                    background: "transparent",
                                                    border: "none",
                                                    cursor: "pointer",
                                                    fontSize: 16
                                                }}
                                            >
                                                🗑
                                            </button>

                                            {getCustomerPhone(p.customerId) && (
                                                <a
                                                    href={buildWhatsAppUrl(getCustomerPhone(p.customerId))}
                                                    target="_blank"
                                                    rel="noopener noreferrer"
                                                    title={t("actionTitles.chatWhatsapp")}
                                                    style={{ marginLeft: 8, fontSize: 16, textDecoration: "none" }}
                                                >
                                                    💬
                                                </a>
                                            )}

                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )
            }

            {viewingPolicy && (
                <div
                    style={{
                        position: "fixed",
                        top: 0,
                        left: 0,
                        right: 0,
                        bottom: 0,
                        background: "rgba(0,0,0,0.5)",
                        display: "flex",
                        alignItems: "center",
                        justifyContent: "center",
                        zIndex: 1000,
                    }}
                    onClick={closeDetail}
                >
                    <div
                        onClick={(e) => { e.stopPropagation(); setOpenDocMenuId(null); }}
                        style={{
                            background: "white",
                            borderRadius: 10,
                            padding: 24,
                            width: "90%",
                            maxWidth: 500,
                            maxHeight: "85vh",
                            overflowY: "auto",
                        }}
                    >
                        <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 16 }}>
                            <h3 style={{ margin: 0 }}>{t("detail.policyTitle", { number: viewingPolicy.policyNumber })}</h3>
                            <button
                                onClick={closeDetail}
                                style={{ background: "transparent", border: "none", cursor: "pointer", fontSize: 18 }}
                            >
                                ✕
                            </button>
                        </div>

                        <h4 style={{ marginBottom: 6 }}>{t("detail.policySection")}</h4>
                        <p style={{ margin: "2px 0" }}>{t("detail.type")}: {translateEnum("policyType", viewingPolicy.type)}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.insuranceCompany")}: {translateEnum("insuranceCompany", viewingPolicy.insuranceCompany)}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.status")}: {translateEnum("policyStatus", viewingPolicy.status)}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.period")}: {viewingPolicy.period}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.startDate")}: {viewingPolicy.startDate?.slice(0, 10)}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.endDate")}: {viewingPolicy.endDate?.slice(0, 10)}</p>
                        <p style={{ margin: "2px 0" }}>{t("detail.premium")}: {viewingPolicy.premium}</p>

                        <h4 style={{ marginTop: 16, marginBottom: 6 }}>{t("detail.titularSection")}</h4>
                        {(() => {
                            const titular = getCustomer(viewingPolicy.customerId);
                            if (!titular) return <p>{t("detail.unknown")}</p>;
                            return (
                                <>
                                    <p style={{ margin: "2px 0" }}>{titular.firstName} {titular.lastName}</p>
                                    <p style={{ margin: "2px 0" }}>{t("detail.ssn")}: {titular.socialSecurityNumber}</p>
                                    <p style={{ margin: "2px 0" }}>{t("detail.email")}: {titular.email}</p>
                                    <p style={{ margin: "2px 0" }}>{t("detail.phone")}: {titular.phone}</p>
                                    <p style={{ margin: "2px 0" }}>{t("detail.address1")}: {titular.address1}</p>
                                    <p style={{ margin: "2px 0" }}>{t("detail.migrationStatus")}: {translateEnum("migrationStatus", titular.migrationStatus)}</p>
                                </>
                            );
                        })()}

                        <h4 style={{ marginTop: 16, marginBottom: 6 }}>{t("detail.dependentsSection")}</h4>
                        <p style={{ margin: "2px 0" }}>{t("detail.numberOfApplicants")}: {viewingPolicy.numberOfApplicants ?? "-"}</p>
                        {detailDependents.length === 0 ? (
                            <p style={{ color: "#666" }}>{t("detail.noDependents")}</p>
                        ) : (
                            <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
                                {detailDependents.map((d) => (
                                    <li key={d.customerId} style={{ padding: "4px 0" }}>
                                        {d.firstName} {d.lastName} ({d.socialSecurityNumber})
                                    </li>
                                ))}
                            </ul>
                        )}

                        <div
                            style={{
                                border: "1px solid #ddd",
                                borderRadius: 10,
                                padding: 16,
                                marginTop: 16,
                            }}
                        >
                            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", marginBottom: 12 }}>
                                <h4 style={{ margin: 0 }}>{t("documents.title")}</h4>
                                <label
                                    style={{
                                        display: "inline-flex",
                                        alignItems: "center",
                                        gap: 6,
                                        background: "#2563eb",
                                        color: "white",
                                        padding: "6px 12px",
                                        borderRadius: 6,
                                        cursor: uploadingDocument ? "default" : "pointer",
                                        opacity: uploadingDocument ? 0.6 : 1,
                                        fontSize: 14,
                                    }}
                                >
                                    📎 {uploadingDocument ? t("documents.uploading") : t("documents.newButton")}
                                    <input
                                        type="file"
                                        accept={ALLOWED_DOCUMENT_EXTENSIONS.join(",")}
                                        onChange={handleUploadDocument}
                                        disabled={uploadingDocument}
                                        style={{ display: "none" }}
                                    />
                                </label>
                            </div>

                            {documentError && (
                                <p style={{ color: "red", margin: "0 0 8px" }}>{documentError}</p>
                            )}

                            {detailDocuments.length === 0 ? (
                                <p style={{ color: "#666" }}>{t("documents.empty")}</p>
                            ) : (
                                <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
                                    {detailDocuments.map((doc) => (
                                        <li
                                            key={doc.id}
                                            style={{
                                                display: "flex",
                                                justifyContent: "space-between",
                                                alignItems: "flex-start",
                                                padding: "10px 0",
                                                borderBottom: "1px solid #eee",
                                                position: "relative",
                                            }}
                                        >
                                            <div>
                                                <span style={{ color: "#2563eb", fontWeight: 500 }}>
                                                    {doc.originalFileName}
                                                </span>
                                                <div style={{ color: "#666", fontSize: 12, marginTop: 2 }}>
                                                    {formatFileSize(doc.sizeBytes)} - {formatDocumentDate(doc.uploadedAt)}
                                                </div>
                                            </div>

                                            <div style={{ position: "relative" }}>
                                                <button
                                                    type="button"
                                                    onClick={(e) => {
                                                        e.stopPropagation();
                                                        setOpenDocMenuId(openDocMenuId === doc.id ? null : doc.id);
                                                    }}
                                                    title={t("documents.optionsTitle")}
                                                    style={{ background: "transparent", border: "none", cursor: "pointer", fontSize: 18, padding: "0 6px", lineHeight: 1 }}
                                                >
                                                    ⋮
                                                </button>

                                                {openDocMenuId === doc.id && (
                                                    <div
                                                        style={{
                                                            position: "absolute",
                                                            right: 0,
                                                            top: "100%",
                                                            background: "white",
                                                            border: "1px solid #ddd",
                                                            borderRadius: 6,
                                                            boxShadow: "0 2px 8px rgba(0,0,0,0.15)",
                                                            zIndex: 10,
                                                            minWidth: 130,
                                                            overflow: "hidden",
                                                        }}
                                                    >
                                                        <button
                                                            type="button"
                                                            onClick={() => {
                                                                setOpenDocMenuId(null);
                                                                handleDownloadDocument(doc);
                                                            }}
                                                            style={{
                                                                display: "block",
                                                                width: "100%",
                                                                textAlign: "left",
                                                                background: "transparent",
                                                                border: "none",
                                                                padding: "8px 12px",
                                                                cursor: "pointer",
                                                                fontSize: 14,
                                                            }}
                                                        >
                                                            {t("documents.download")}
                                                        </button>
                                                        <button
                                                            type="button"
                                                            onClick={() => {
                                                                setOpenDocMenuId(null);
                                                                handleDeleteDocument(doc);
                                                            }}
                                                            style={{
                                                                display: "block",
                                                                width: "100%",
                                                                textAlign: "left",
                                                                background: "transparent",
                                                                border: "none",
                                                                padding: "8px 12px",
                                                                cursor: "pointer",
                                                                fontSize: 14,
                                                                color: "#dc2626",
                                                            }}
                                                        >
                                                            {t("documents.delete")}
                                                        </button>
                                                    </div>
                                                )}
                                            </div>
                                        </li>
                                    ))}
                                </ul>
                            )}
                        </div>
                    </div>
                </div>
            )}
        </div >
    );

}

export default Policies;
