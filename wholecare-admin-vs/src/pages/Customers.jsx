import { useEffect, useState } from "react";
import { apiFetch } from "../api";

const API = "/api/customers";
const MIGRATION_STATUSES = [
    "Permiso de trabajo",
    "Residente permanente",
    "Ciudadano",
    "Otro",
];

const emptyForm = {
    socialSecurityNumber: "",
    firstName: "",
    lastName: "",
    dateOfBirth: "",
    email: "",
    address: "",
    phone: "",
    migrationStatus: "",
};

function Customers() {
    const [customers, setCustomers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);
    const [form, setForm] = useState(emptyForm);
    const [editingId, setEditingId] = useState(null);
    const [formError, setFormError] = useState("");
    const [submitting, setSubmitting] = useState(false);

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

    useEffect(() => { loadCustomers(); }, []);

    const handleField = (e) => setForm({ ...form, [e.target.name]: e.target.value });

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
            address: c.address,
            phone: c.phone,
            migrationStatus: c.migrationStatus,
        });
        setFormError("");
        setShowForm(true);
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setFormError("");

        const url = editingId ? `${API}/${editingId}` : API;
        const method = editingId ? "PUT" : "POST";

        try {
            setSubmitting(true);
            const res = await apiFetch(url, {
                method,
                body: JSON.stringify(form),
            });

            if (!res.ok) {
                const err = await res.json().catch(() => null);
                setFormError(err?.title ?? "Error al guardar el cliente");
                return;
            }

            setShowForm(false);
            setForm(emptyForm);
            setEditingId(null);
            await loadCustomers();
        } catch {
            setFormError("Error al guardar el cliente");
        } finally {
            setSubmitting(false);
        }
    };

    const handleDelete = async (id) => {
        if (!confirm("¿Eliminar este cliente?")) return;
        try {
            const res = await apiFetch(`${API}/${id}`, { method: "DELETE" });
            if (!res.ok) throw new Error();
            await loadCustomers();
        } catch {
            alert("Error al eliminar el cliente");
        }
    };

    const inputStyle = { width: "100%", padding: "7px 10px", marginTop: 4, boxSizing: "border-box", borderRadius: 5, border: "1px solid #ccc" };
    const labelStyle = { fontWeight: 500, fontSize: 13 };

    return (
        <div>
            <h2 style={{ marginBottom: 20 }}>Clientes</h2>

            <button
                onClick={showForm ? () => setShowForm(false) : openCreate}
                type="button"
                style={{ marginBottom: 20, background: "#2563eb", color: "white", padding: "8px 14px", border: "none", borderRadius: 6, cursor: "pointer" }}
            >
                {showForm ? "Cerrar formulario" : "+ Nuevo cliente"}
            </button>

            {showForm && (
                <div style={{ border: "1px solid #ddd", borderRadius: 10, padding: 24, marginBottom: 30, background: "#fafafa", maxWidth: 560 }}>
                    <h3 style={{ marginTop: 0 }}>{editingId ? "Editar cliente" : "Nuevo cliente"}</h3>

                    <form onSubmit={handleSubmit}>
                        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>

                            <div>
                                <label style={labelStyle}>Número de seguro social</label>
                                <input name="socialSecurityNumber" value={form.socialSecurityNumber} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>Fecha de nacimiento</label>
                                <input type="date" name="dateOfBirth" value={form.dateOfBirth} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>Nombre</label>
                                <input name="firstName" value={form.firstName} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>Apellido</label>
                                <input name="lastName" value={form.lastName} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div style={{ gridColumn: "1 / -1" }}>
                                <label style={labelStyle}>Correo electrónico</label>
                                <input type="email" name="email" value={form.email} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div style={{ gridColumn: "1 / -1" }}>
                                <label style={labelStyle}>Dirección</label>
                                <input name="address" value={form.address} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>Teléfono</label>
                                <input name="phone" value={form.phone} onChange={handleField} required style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>Estatus migratorio</label>
                                <select name="migrationStatus" value={form.migrationStatus} onChange={handleField} required style={inputStyle}>
                                    <option value="">-- Seleccionar --</option>
                                    {MIGRATION_STATUSES.map((s) => (
                                        <option key={s} value={s}>{s}</option>
                                    ))}
                                </select>
                            </div>

                        </div>

                        {formError && <p style={{ color: "red", marginTop: 12 }}>{formError}</p>}

                        <button
                            type="submit"
                            disabled={submitting}
                            style={{ marginTop: 16, background: "#2563eb", color: "white", padding: "9px 20px", border: "none", borderRadius: 6, cursor: "pointer" }}
                        >
                            {editingId
                                ? (submitting ? "Guardando..." : "Guardar cambios")
                                : (submitting ? "Creando..." : "Crear cliente")}
                        </button>
                    </form>
                </div>
            )}

            {loading ? (
                <p>Cargando clientes...</p>
            ) : customers.length === 0 ? (
                <p>No hay clientes registrados.</p>
            ) : (
                <div style={{ display: "grid", gap: 12 }}>
                    {customers.map((c) => (
                        <div key={c.id} style={{ border: "1px solid #ddd", borderRadius: 10, padding: 16, background: "white" }}>
                            <div style={{ fontWeight: "bold", fontSize: 16, marginBottom: 6 }}>
                                {c.firstName} {c.lastName}
                            </div>
                            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "3px 20px", fontSize: 14, color: "#444" }}>
                                <span>SSN: {c.socialSecurityNumber}</span>
                                <span>Nacimiento: {c.dateOfBirth?.substring(0, 10)}</span>
                                <span>Email: {c.email}</span>
                                <span>Teléfono: {c.phone}</span>
                                <span style={{ gridColumn: "1 / -1" }}>Dirección: {c.address}</span>
                                <span>Estatus: {c.migrationStatus}</span>
                                <span>Pólizas: {c.policiesCount}</span>
                            </div>
                            <div style={{ marginTop: 10, display: "flex", gap: 8 }}>
                                <button onClick={() => handleEdit(c)} style={{ background: "#007bff", color: "white", border: "none", padding: "5px 12px", borderRadius: 5, cursor: "pointer" }}>
                                    Editar
                                </button>
                                <button onClick={() => handleDelete(c.id)} style={{ background: "#dc2626", color: "white", border: "none", padding: "5px 12px", borderRadius: 5, cursor: "pointer" }}>
                                    Eliminar
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
