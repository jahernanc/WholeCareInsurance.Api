import { useEffect, useState } from "react";
import { apiFetch, isAdmin } from "../api";
import { US_STATES } from "../data/usStates";
import US_COUNTIES from "../data/usCounties.json";

const API = "/api/customers";
const MIGRATION_STATUSES = [
    "Permiso de trabajo",
    "Residente permanente",
    "Ciudadano",
    "Otro",
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

const emptyForm = {
    socialSecurityNumber: "",
    firstName: "",
    lastName: "",
    dateOfBirth: "",
    email: "",
    address: "",
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
};

function Customers() {
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
            address: c.address,
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
        };

        try {
            setSubmitting(true);
            const res = await apiFetch(url, {
                method,
                body: JSON.stringify(body),
            });

            if (!res.ok) {
                const err = await res.json().catch(() => null);
                setFormError(err?.title ?? err ?? "Error al guardar el cliente");
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
    const countiesForState = form.state ? (US_COUNTIES[form.state] ?? []) : [];

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

                            <div>
                                <label style={labelStyle}>Relación con el principal</label>
                                <select name="relacionConPrincipal" value={form.relacionConPrincipal} onChange={handleField} required style={inputStyle}>
                                    <option value="">-- Seleccionar --</option>
                                    {RELACIONES_PRINCIPAL.map((r) => (
                                        <option key={r} value={r}>{r}</option>
                                    ))}
                                </select>
                            </div>

                            <div>
                                <label style={labelStyle}>Código postal</label>
                                <input name="zipCode" value={form.zipCode} onChange={handleField} style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>Estado</label>
                                <select name="state" value={form.state} onChange={handleField} style={inputStyle}>
                                    <option value="">-- Seleccionar --</option>
                                    {US_STATES.map((s) => (
                                        <option key={s.code} value={s.code}>{s.name}</option>
                                    ))}
                                </select>
                            </div>

                            <div>
                                <label style={labelStyle}>Ciudad</label>
                                <input name="city" value={form.city} onChange={handleField} style={inputStyle} />
                            </div>

                            <div>
                                <label style={labelStyle}>Condado</label>
                                <select name="county" value={form.county} onChange={handleField} disabled={!form.state} style={inputStyle}>
                                    <option value="">{form.state ? "-- Seleccionar --" : "Elegí un estado primero"}</option>
                                    {countiesForState.map((c) => (
                                        <option key={c} value={c}>{c}</option>
                                    ))}
                                </select>
                            </div>

                            <div>
                                <label style={labelStyle}>Estado civil</label>
                                <select name="maritalStatus" value={form.maritalStatus} onChange={handleField} style={inputStyle}>
                                    <option value="">-- Seleccionar --</option>
                                    {MARITAL_STATUSES.map((m) => (
                                        <option key={m} value={m}>{m}</option>
                                    ))}
                                </select>
                            </div>

                            <div>
                                <label style={labelStyle}>Ocupación</label>
                                <input name="occupation" value={form.occupation} onChange={handleField} style={inputStyle} />
                            </div>

                            {userIsAdmin && (
                                <>
                                    <div>
                                        <label style={labelStyle}>Agente</label>
                                        <select name="agentId" value={form.agentId} onChange={handleField} style={inputStyle}>
                                            <option value="">-- Sin asignar --</option>
                                            {agents.map((a) => (
                                                <option key={a.id} value={a.id}>{a.nombre}</option>
                                            ))}
                                        </select>
                                    </div>

                                    <div>
                                        <label style={labelStyle}>Agente Asistente</label>
                                        <select name="assistantAgentId" value={form.assistantAgentId} onChange={handleField} style={inputStyle}>
                                            <option value="">-- Sin asignar --</option>
                                            {agents.map((a) => (
                                                <option key={a.id} value={a.id}>{a.nombre}</option>
                                            ))}
                                        </select>
                                    </div>

                                    <div>
                                        <label style={labelStyle}>Agente Record</label>
                                        <select name="recordAgentId" value={form.recordAgentId} onChange={handleField} style={inputStyle}>
                                            <option value="">-- Sin asignar --</option>
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
                                <span>Relación con el principal: {c.relacionConPrincipal}</span>
                                <span>Código postal: {c.zipCode || "-"}</span>
                                <span>Estado/Ciudad/Condado: {[c.city, c.county, c.state].filter(Boolean).join(", ") || "-"}</span>
                                <span>Estado civil: {c.maritalStatus || "-"}</span>
                                <span>Ocupación: {c.occupation || "-"}</span>
                                <span>Agente: {c.agentName || "-"}</span>
                                {userIsAdmin && <span>Agente Asistente: {c.assistantAgentName || "-"}</span>}
                                {userIsAdmin && <span>Agente Record: {c.recordAgentName || "-"}</span>}
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
