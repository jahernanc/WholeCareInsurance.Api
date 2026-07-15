import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { apiFetch } from "../api";

const emptyForm = {
    name: "",
    isActive: true,
};

function InsuranceCompanies() {
    const { t } = useTranslation(["insuranceCompanies", "common"]);
    const [companies, setCompanies] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);
    const [form, setForm] = useState(emptyForm);
    const [editingId, setEditingId] = useState(null);
    const [formError, setFormError] = useState("");
    const [submitting, setSubmitting] = useState(false);

    const loadCompanies = async () => {
        try {
            setLoading(true);
            const res = await apiFetch("/api/insurance-companies");
            if (!res.ok) throw new Error();
            setCompanies(await res.json());
        } catch {
            console.error("No se pudieron cargar las aseguradoras");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { loadCompanies(); }, []);

    const handleField = (e) => {
        const { name, value, type, checked } = e.target;
        setForm((f) => ({ ...f, [name]: type === "checkbox" ? checked : value }));
    };

    const openCreate = () => {
        setEditingId(null);
        setForm(emptyForm);
        setFormError("");
        setShowForm(true);
    };

    const handleEdit = (c) => {
        setEditingId(c.id);
        setForm({ name: c.name, isActive: c.isActive });
        setFormError("");
        setShowForm(true);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setFormError("");

        try {
            setSubmitting(true);

            const res = editingId
                ? await apiFetch(`/api/insurance-companies/${editingId}`, {
                    method: "PUT",
                    body: JSON.stringify(form),
                })
                : await apiFetch("/api/insurance-companies", {
                    method: "POST",
                    body: JSON.stringify(form),
                });

            if (!res.ok) {
                setFormError(res.errorMessage ?? t("form.saveError"));
                return;
            }

            setShowForm(false);
            setForm(emptyForm);
            setEditingId(null);
            await loadCompanies();
        } catch {
            setFormError(t("form.saveError"));
        } finally {
            setSubmitting(false);
        }
    };

    const inputStyle = { width: "100%", padding: "7px 10px", marginTop: 4, boxSizing: "border-box", borderRadius: 5, border: "1px solid #ccc" };
    const labelStyle = { fontWeight: 500, fontSize: 13 };

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
                <div style={{ border: "1px solid #ddd", borderRadius: 10, padding: 24, marginBottom: 30, background: "#fafafa", maxWidth: 420 }}>
                    <h3 style={{ marginTop: 0 }}>{editingId ? t("form.titleEdit") : t("form.titleCreate")}</h3>

                    <form onSubmit={handleSubmit}>
                        <div style={{ display: "grid", gap: 12 }}>

                            <div>
                                <label style={labelStyle}>{t("form.fields.name")}</label>
                                <input name="name" value={form.name} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={{ ...labelStyle, display: "flex", alignItems: "center", gap: 6 }}>
                                    <input type="checkbox" name="isActive" checked={form.isActive} onChange={handleField} />
                                    {t("form.fields.isActive")}
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
            ) : companies.length === 0 ? (
                <p>{t("empty")}</p>
            ) : (
                <div style={{ display: "grid", gap: 12 }}>
                    {companies.map((c) => (
                        <div key={c.id} style={{ border: "1px solid #ddd", borderRadius: 10, padding: 16, background: "white" }}>
                            <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
                                <div style={{ fontWeight: "bold", fontSize: 16 }}>
                                    {c.name}
                                </div>
                                <span style={{
                                    fontSize: 12,
                                    fontWeight: 600,
                                    padding: "3px 8px",
                                    borderRadius: 12,
                                    background: c.isActive ? "#dcfce7" : "#f3f4f6",
                                    color: c.isActive ? "#166534" : "#6b7280",
                                }}>
                                    {c.isActive ? t("card.active") : t("card.inactive")}
                                </span>
                            </div>
                            <div style={{ marginTop: 10, display: "flex", gap: 8 }}>
                                <button onClick={() => handleEdit(c)} style={{ background: "#007bff", color: "white", border: "none", padding: "5px 12px", borderRadius: 5, cursor: "pointer" }}>
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

export default InsuranceCompanies;
