import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { apiFetch } from "../api";
import { translateEnum } from "../i18n/translateEnum";

const ROLES = ["Admin", "Agente"];

const emptyForm = {
    nombre: "",
    email: "",
    password: "",
    rol: "Agente",
    isEncargado: false,
};

function Agentes() {
    const { t } = useTranslation(["agentes", "common"]);
    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);
    const [form, setForm] = useState(emptyForm);
    const [editingId, setEditingId] = useState(null);
    const [formError, setFormError] = useState("");
    const [submitting, setSubmitting] = useState(false);

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
        setForm((f) => ({ ...f, [name]: type === "checkbox" ? checked : value }));
    };

    const openCreate = () => {
        setEditingId(null);
        setForm(emptyForm);
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
        });
        setFormError("");
        setShowForm(true);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setFormError("");

        try {
            setSubmitting(true);

            const res = editingId
                ? await apiFetch(`/users/${editingId}`, {
                    method: "PUT",
                    body: JSON.stringify({
                        nombre: form.nombre,
                        email: form.email,
                        rol: form.rol,
                        isEncargado: form.isEncargado,
                    }),
                })
                : await apiFetch("/auth/register", {
                    method: "POST",
                    body: JSON.stringify(form),
                });

            if (!res.ok) {
                const err = await res.json().catch(() => null);
                setFormError(err?.title ?? err ?? t("form.saveError"));
                return;
            }

            setShowForm(false);
            setForm(emptyForm);
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
                <div style={{ border: "1px solid #ddd", borderRadius: 10, padding: 24, marginBottom: 30, background: "#fafafa", maxWidth: 480 }}>
                    <h3 style={{ marginTop: 0 }}>{editingId ? t("form.titleEdit") : t("form.titleCreate")}</h3>

                    <form onSubmit={handleSubmit}>
                        <div style={{ display: "grid", gap: 12 }}>

                            <div>
                                <label style={labelStyle}>{t("form.fields.name")}</label>
                                <input name="nombre" value={form.nombre} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>{t("form.fields.email")}</label>
                                <input type="email" name="email" value={form.email} onChange={handleField} required style={inputStyle} />
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

                            <div>
                                <label style={{ ...labelStyle, display: "flex", alignItems: "center", gap: 6 }}>
                                    <input type="checkbox" name="isEncargado" checked={form.isEncargado} onChange={handleField} />
                                    {t("form.fields.isEncargado")}
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
