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


    const token = localStorage.getItem("accessToken");

    const getCustomerName = (id) => {
        const customer = customers.find((c) => c.id === Number(id));
        return customer ? customer.name : "Unknown";
    };

    const loadData = async () => {
        try {
            setLoading(true);

            const [policiesRes, customersRes] = await Promise.all([
                fetch("http://localhost:5279/api/policies", {
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
    const handleEdit = (policy) => {
        setEditingId(policy.id);

        setPolicyNumber(policy.policyNumber);
        setType(policy.type);
        setStartDate(policy.startDate.slice(0, 10));
        setEndDate(policy.endDate.slice(0, 10));
        setPremium(policy.premium);
        setStatus(policy.status);
        setCustomerId(policy.customerId);

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

            await loadData();
        } catch (error) {

            console.error(error);
            setFormError("Error saving policy");

        } finally {
            setSubmitting(false);
        }
    };

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
                                            {c.name} ({c.documentNumber})
                                        </option>
                                    ))}
                                </select>
                            </div>



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

                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )
            }
        </div >
    );

}

export default Policies;
