import { useEffect, useState } from "react";

const POLICY_TYPES = ["Obama Care", "Salud", "Auto", "Otro"];

function Policies() {
    const [policies, setPolicies] = useState([]);
    const [customers, setCustomers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);

    const [policyNumber, setPolicyNumber] = useState("");
    const [type, setType] = useState("");
    const [startDate, setStartDate] = useState("");
    const [endDate, setEndDate] = useState("");
    const [premium, setPremium] = useState("");
    const [status, setStatus] = useState("Active");
    const [customerId, setCustomerId] = useState("");

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

    const [viewingPolicy, setViewingPolicy] = useState(null);
    const [detailDependents, setDetailDependents] = useState([]);

    const token = localStorage.getItem("accessToken");

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
        const message = encodeURIComponent("Hola, te contactamos desde WholeCare Insurance.");
        return `https://wa.me/${digits}?text=${message}`;
    };

    const loadData = async (filterOverrides = {}) => {
        const filters = {
            policyNumber: filterPolicyNumber,
            firstName: filterFirstName,
            lastName: filterLastName,
            status: filterStatus,
            type: filterType,
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
                fetch(`http://localhost:5279/api/policies${query ? `?${query}` : ""}`, {
                    headers: {
                        Authorization: "Bearer " + token,
                    },
                }),
                fetch("http://localhost:5279/api/customers", {
                    headers: {
                        Authorization: "Bearer " + token,
                    },
                }),
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
        loadData({ policyNumber: "", firstName: "", lastName: "", status: "", type: "" });
    };
    const loadDependents = async (policyId) => {
        try {
            const res = await fetch(`http://localhost:5279/api/policies/${policyId}/dependents`, {
                headers: { Authorization: "Bearer " + token },
            });
            if (!res.ok) throw new Error();
            setDependents(await res.json());
        } catch (error) {
            console.error("Error loading dependents:", error);
        }
    };

    const openDetail = async (policy) => {
        setViewingPolicy(policy);
        setDetailDependents([]);
        try {
            const res = await fetch(`http://localhost:5279/api/policies/${policy.id}/dependents`, {
                headers: { Authorization: "Bearer " + token },
            });
            if (!res.ok) throw new Error();
            setDetailDependents(await res.json());
        } catch (error) {
            console.error("Error loading dependents for detail view:", error);
        }
    };

    const closeDetail = () => {
        setViewingPolicy(null);
        setDetailDependents([]);
    };

    const handleAddDependent = async (depCustomerId) => {
        try {
            const response = await fetch(
                `http://localhost:5279/api/policies/${editingId}/dependents`,
                {
                    method: "POST",
                    headers: {
                        "Content-Type": "application/json",
                        Authorization: "Bearer " + token,
                    },
                    body: JSON.stringify({ customerId: depCustomerId }),
                }
            );
            if (!response.ok) throw new Error("Error adding dependent");
            setDependentQuery("");
            await loadDependents(editingId);
        } catch (error) {
            console.error(error);
            alert("Error adding dependent");
        }
    };

    const handleRemoveDependent = async (depCustomerId) => {
        try {
            const response = await fetch(
                `http://localhost:5279/api/policies/${editingId}/dependents/${depCustomerId}`,
                {
                    method: "DELETE",
                    headers: { Authorization: "Bearer " + token },
                }
            );
            if (!response.ok) throw new Error("Error removing dependent");
            await loadDependents(editingId);
        } catch (error) {
            console.error(error);
            alert("Error removing dependent");
        }
    };

    const handleEdit = (policy) => {
        setEditingId(policy.id);

        setPolicyNumber(policy.policyNumber);
        setType(policy.type);
        setStartDate(policy.startDate.slice(0, 10));
        setEndDate(policy.endDate.slice(0, 10));
        setPremium(policy.premium);
        setStatus(policy.status);
        setCustomerId(policy.customerId);

        setDependentQuery("");
        setShowDependentPicker(false);
        loadDependents(policy.id);

        setShowForm(true);
    };

    const handleDelete = async (id) => {
        if (!confirm("Are you sure you want to delete this policy?")) return;

        try {
            const response = await fetch(
                `http://localhost:5279/api/policies/${id}`,
                {
                    method: "DELETE",
                    headers: {
                        Authorization: "Bearer " + token,
                    },
                }
            );

            if (!response.ok) {
                throw new Error("Error deleting policy");
            }

            await loadData(); // refresca tabla

        } catch (error) {
            console.error(error);
            alert("Error deleting policy");
        }
    };


    useEffect(() => {
        if (!token) return;

        const fetchData = async () => {
            try {
                setLoading(true);

                const [policiesRes, customersRes] = await Promise.all([
                    fetch("http://localhost:5279/api/policies", {
                        headers: { Authorization: "Bearer " + token },
                    }),
                    fetch("http://localhost:5279/api/customers", {
                        headers: { Authorization: "Bearer " + token },
                    }),
                ]);

                const policiesData = await policiesRes.json();
                const customersData = await customersRes.json();

                setPolicies(policiesData);
                setCustomers(customersData);

            } catch (error) {
                console.error(error);
            } finally {
                setLoading(false);
            }
        };

        fetchData();
    }, [token]);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setFormError("");

        if (
            !policyNumber.trim() ||
            !type.trim() ||
            !startDate ||
            !endDate ||
            !premium ||
            !status.trim() ||
            !customerId
        ) {
            setFormError("All fields are required");
            return;
        }

        try {
            setSubmitting(true);

            const url = editingId
                ? `http://localhost:5279/api/policies/${editingId}`
                : "http://localhost:5279/api/policies";

            const method = editingId ? "PUT" : "POST";

            const response = await fetch(url, {
                method: method,
                headers: {
                    "Content-Type": "application/json",
                    Authorization: "Bearer " + token,
                },
                body: JSON.stringify({
                    policyNumber,
                    type,
                    startDate,
                    endDate,
                    premium: Number(premium),
                    status,
                    customerId: Number(customerId),
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
            setStartDate("");
            setEndDate("");
            setPremium("");
            setStatus("Active");
            setCustomerId("");

            setDependents([]);
            setDependentQuery("");
            setShowDependentPicker(false);

            await loadData();
        } catch (error) {

            console.error(error);
            setFormError("Error saving policy");

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
        return <p>Loading policies...</p>;
    }

    return (
        <div>
            <h2 style={{ marginBottom: 20 }}>Policies</h2>

            {/* ✅ BOTÓN */}
            <button
                onClick={() => setShowForm(!showForm)}
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
                {showForm ? "Close Form" : "+ Create Policy"}
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
                        <h3 style={{ marginTop: 0 }}>Create Policy</h3>

                        <form onSubmit={handleSubmit}>

                            <div style={{ marginBottom: 12 }}>
                                <label>Policy Number</label>
                                <input
                                    type="text"
                                    value={policyNumber}
                                    onChange={(e) => setPolicyNumber(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>Type</label>
                                <select
                                    value={type}
                                    onChange={(e) => setType(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                >
                                    <option value="">Select type</option>
                                    {POLICY_TYPES.map((t) => (
                                        <option key={t} value={t}>{t}</option>
                                    ))}
                                </select>
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>Start Date</label>
                                <input
                                    type="date"
                                    value={startDate}
                                    onChange={(e) => setStartDate(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>End Date</label>
                                <input
                                    type="date"
                                    value={endDate}
                                    onChange={(e) => setEndDate(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>Premium</label>
                                <input
                                    type="number"
                                    value={premium}
                                    onChange={(e) => setPremium(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>Status</label>
                                <select
                                    value={status}
                                    onChange={(e) => setStatus(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                >
                                    <option value="Active">Active</option>
                                    <option value="Expired">Expired</option>
                                    <option value="Cancelled">Cancelled</option>
                                    <option value="activa">activa</option>
                                </select>
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>Customer</label>
                                <select
                                    value={customerId}
                                    onChange={(e) => setCustomerId(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                >
                                    <option value="">Select customer</option>
                                    {customers.map((c) => (
                                        <option key={c.id} value={c.id}>
                                            {c.firstName} {c.lastName} ({c.socialSecurityNumber})
                                        </option>
                                    ))}
                                </select>
                            </div>

                            {editingId && (
                                <div style={{ marginBottom: 12, borderTop: "1px solid #ddd", paddingTop: 12 }}>
                                    <label style={{ fontWeight: "bold" }}>Dependientes</label>

                                    {dependents.length === 0 ? (
                                        <p style={{ color: "#666", margin: "8px 0" }}>No dependents yet</p>
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
                                                    <button
                                                        type="button"
                                                        onClick={() => handleRemoveDependent(d.customerId)}
                                                        title="Remove dependent"
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
                                        {showDependentPicker ? "Cancelar" : "+ Agregar dependiente"}
                                    </button>

                                    {showDependentPicker && (
                                        <div>
                                            <input
                                                type="text"
                                                placeholder="Buscar cliente por nombre..."
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
                                                            Add
                                                        </button>
                                                    </li>
                                                ))}
                                                {dependentCandidates.length === 0 && (
                                                    <li style={{ color: "#666", padding: "6px 0" }}>No matches</li>
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
                                {submitting ? "Creating..." : "Create Policy"}
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
                    <label style={{ display: "block", fontSize: 12 }}>Policy Number</label>
                    <input
                        type="text"
                        value={filterPolicyNumber}
                        onChange={(e) => setFilterPolicyNumber(e.target.value)}
                        style={{ padding: 6 }}
                    />
                </div>

                <div>
                    <label style={{ display: "block", fontSize: 12 }}>First Name</label>
                    <input
                        type="text"
                        value={filterFirstName}
                        onChange={(e) => setFilterFirstName(e.target.value)}
                        style={{ padding: 6 }}
                    />
                </div>

                <div>
                    <label style={{ display: "block", fontSize: 12 }}>Last Name</label>
                    <input
                        type="text"
                        value={filterLastName}
                        onChange={(e) => setFilterLastName(e.target.value)}
                        style={{ padding: 6 }}
                    />
                </div>

                <div>
                    <label style={{ display: "block", fontSize: 12 }}>Status</label>
                    <select
                        value={filterStatus}
                        onChange={(e) => setFilterStatus(e.target.value)}
                        style={{ padding: 6 }}
                    >
                        <option value="">All</option>
                        <option value="Active">Active</option>
                        <option value="Expired">Expired</option>
                        <option value="Cancelled">Cancelled</option>
                        <option value="activa">activa</option>
                    </select>
                </div>

                <div>
                    <label style={{ display: "block", fontSize: 12 }}>Type</label>
                    <select
                        value={filterType}
                        onChange={(e) => setFilterType(e.target.value)}
                        style={{ padding: 6 }}
                    >
                        <option value="">All</option>
                        {POLICY_TYPES.map((t) => (
                            <option key={t} value={t}>{t}</option>
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
                    Search
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
                    Clear
                </button>
            </div>

            {/* ✅ TABLA SIEMPRE VISIBLE */}
            {
                policies.length === 0 ? (
                    <p>No policies yet</p>
                ) : (

                    <div style={{ overflowX: "auto" }}>
                        <table style={{ width: "100%", borderCollapse: "collapse" }}>

                            <thead>
                                <tr style={{ background: "#f3f4f6", textAlign: "left" }}>
                                    <th style={{ padding: 10 }}>Policy</th>
                                    <th style={{ padding: 10 }}>Type</th>
                                    <th style={{ padding: 10 }}>Status</th>
                                    <th style={{ padding: 10 }}>Premium</th>
                                    <th style={{ padding: 10 }}>Customer</th>
                                    <th style={{ padding: 10 }}>Actions</th>
                                </tr>
                            </thead>

                            <tbody>
                                {policies.map((p) => (
                                    <tr key={p.id}>
                                        <td style={{ padding: 10 }}>{p.policyNumber}</td>
                                        <td style={{ padding: 10 }}>{p.type}</td>
                                        <td style={{ padding: 10 }}>{p.status}</td>
                                        <td style={{ padding: 10 }}>{p.premium}</td>
                                        <td style={{ padding: 10 }}>
                                            {getCustomerName(p.customerId)}
                                        </td>
                                        <td style={{ padding: 10 }}>

                                            <button
                                                onClick={() => openDetail(p)}
                                                title="View details"
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
                                                title="Edit policy"
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
                                                title="Delete policy"
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
                                                    title="Chat by WhatsApp"
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
                        onClick={(e) => e.stopPropagation()}
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
                            <h3 style={{ margin: 0 }}>Policy {viewingPolicy.policyNumber}</h3>
                            <button
                                onClick={closeDetail}
                                style={{ background: "transparent", border: "none", cursor: "pointer", fontSize: 18 }}
                            >
                                ✕
                            </button>
                        </div>

                        <h4 style={{ marginBottom: 6 }}>Policy</h4>
                        <p style={{ margin: "2px 0" }}>Type: {viewingPolicy.type}</p>
                        <p style={{ margin: "2px 0" }}>Status: {viewingPolicy.status}</p>
                        <p style={{ margin: "2px 0" }}>Start Date: {viewingPolicy.startDate?.slice(0, 10)}</p>
                        <p style={{ margin: "2px 0" }}>End Date: {viewingPolicy.endDate?.slice(0, 10)}</p>
                        <p style={{ margin: "2px 0" }}>Premium: {viewingPolicy.premium}</p>

                        <h4 style={{ marginTop: 16, marginBottom: 6 }}>Titular</h4>
                        {(() => {
                            const titular = getCustomer(viewingPolicy.customerId);
                            if (!titular) return <p>Unknown</p>;
                            return (
                                <>
                                    <p style={{ margin: "2px 0" }}>{titular.firstName} {titular.lastName}</p>
                                    <p style={{ margin: "2px 0" }}>SSN: {titular.socialSecurityNumber}</p>
                                    <p style={{ margin: "2px 0" }}>Email: {titular.email}</p>
                                    <p style={{ margin: "2px 0" }}>Phone: {titular.phone}</p>
                                    <p style={{ margin: "2px 0" }}>Address: {titular.address}</p>
                                    <p style={{ margin: "2px 0" }}>Migration Status: {titular.migrationStatus}</p>
                                </>
                            );
                        })()}

                        <h4 style={{ marginTop: 16, marginBottom: 6 }}>Dependents</h4>
                        {detailDependents.length === 0 ? (
                            <p style={{ color: "#666" }}>No dependents</p>
                        ) : (
                            <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
                                {detailDependents.map((d) => (
                                    <li key={d.customerId} style={{ padding: "4px 0" }}>
                                        {d.firstName} {d.lastName} ({d.socialSecurityNumber})
                                    </li>
                                ))}
                            </ul>
                        )}
                    </div>
                </div>
            )}
        </div >
    );

}

export default Policies;
