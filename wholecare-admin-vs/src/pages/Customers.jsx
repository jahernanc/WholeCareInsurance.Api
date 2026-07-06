import { useEffect, useState } from "react";

function Customers() {
    const [customers, setCustomers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [showForm, setShowForm] = useState(false);

    const [name, setName] = useState("");
    const [email, setEmail] = useState("");
    const [documentNumber, setDocumentNumber] = useState("");

    const [formError, setFormError] = useState("");
    const [submitting, setSubmitting] = useState(false);

    const token = localStorage.getItem("accessToken");
    const [editingId, setEditingId] = useState(null);

    

    const loadCustomers = async () => {
        try {
            setLoading(true);

            const response = await fetch("http://localhost:5279/api/customers", {
                headers: {
                    Authorization: "Bearer " + token,
                },
            });

            if (!response.ok) {
                throw new Error("Could not load customers");
            }

            const data = await response.json();
            setCustomers(data);
        } catch (error) {
            console.error(error);
        } finally {
            setLoading(false);
        }
    };
    const handleEdit = (customer) => {
        setEditingId(customer.id);

        setName(customer.name);
        setEmail(customer.email);
        setDocumentNumber(customer.documentNumber);

        setShowForm(true);
    };

    useEffect(() => {
        if (!token) return;

        const fetchCustomers = async () => {
            try {
                setLoading(true);

                const response = await fetch("http://localhost:5279/api/customers", {
                    headers: {
                        Authorization: "Bearer " + token,
                    },
                });

                if (!response.ok) {
                    throw new Error("Could not load customers");
                }

                const data = await response.json();
                setCustomers(data);
            } catch (error) {
                console.error(error);
            } finally {
                setLoading(false);
            }
        };

        fetchCustomers();
    }, [token]);
    


    const handleSubmit = async (e) => {
        e.preventDefault();
        setFormError("");

        if (!name.trim() || !email.trim() || !documentNumber.trim()) {
            setFormError("All fields are required");
            return;
        }

        try {
            setSubmitting(true);

            const url = editingId
                ? `http://localhost:5279/api/customers/${editingId}`
                : "http://localhost:5279/api/customers";

            const method = editingId ? "PUT" : "POST";

            const response = await fetch(url, {
                method: method,
                headers: {
                    "Content-Type": "application/json",
                    Authorization: "Bearer " + token,
                },
                body: JSON.stringify({
                    name,
                    email,
                    documentNumber,
                }),
            });

            if (!response.ok) {
                throw new Error("Error saving customer");
            }

            // reset form
            setName("");
            setEmail("");
            setDocumentNumber("");
            setEditingId(null);

            await loadCustomers();
            setShowForm(false);


        } catch (error) {
            console.error(error);
            setFormError("Error saving customer");
        } finally {
            setSubmitting(false);
        }
    };


    const handleDelete = async (id) => {
        if (!confirm("Are you sure you want to delete this customer?")) return;

        try {
            const response = await fetch(
                `http://localhost:5279/api/customers/${id}`,
                {
                    method: "DELETE",
                    headers: {
                        Authorization: "Bearer " + token,
                    },
                }
            );

            if (!response.ok) {
                throw new Error("Could not delete customer");
            }

            await loadCustomers(); // 🔥 refresca lista

        } catch (error) {
            console.error(error);
            alert("Error deleting customer");
        }
    };

    return (
        <div>
            <h2 style={{ marginBottom: 20 }}>Customers</h2>

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
                {showForm ? "Close Form" : "+ Create Customer"}
            </button>

            {/* Formulario */}
            {
                showForm && (
                    <div
                        style={{
                            border: "1px solid #ddd",
                            borderRadius: 10,
                            padding: 20,
                            marginBottom: 30,
                            background: "#fafafa",
                            maxWidth: 500,
                        }}
                    >
                        <h3 style={{ marginTop: 0 }}>Create Customer</h3>

                        <form onSubmit={handleSubmit}>
                            <div style={{ marginBottom: 12 }}>
                                <label>Name</label>
                                <input
                                    type="text"
                                    value={name}
                                    onChange={(e) => setName(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>Email</label>
                                <input
                                    type="email"
                                    value={email}
                                    onChange={(e) => setEmail(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            <div style={{ marginBottom: 12 }}>
                                <label>Document Number</label>
                                <input
                                    type="text"
                                    value={documentNumber}
                                    onChange={(e) => setDocumentNumber(e.target.value)}
                                    style={{ width: "100%", padding: 8, marginTop: 4 }}
                                />
                            </div>

                            {formError && (
                                <p style={{ color: "red", marginBottom: 12 }}>{formError}</p>
                            )}

                            <button type="submit" disabled={submitting}>
                                {editingId
                                    ? (submitting ? "Updating..." : "Update Customer")
                                    : (submitting ? "Creating..." : "Create Customer")}
                            </button>

                        </form>
                    </div>
                )
            }

            {/* Lista */}
            {loading ? (
                <p>Loading customers...</p>
            ) : customers.length === 0 ? (
                <p>No customers yet</p>
            ) : (
                <div style={{ display: "grid", gap: 12 }}>
                    {customers.map((c) => (
                        <div
                            key={c.id}
                            style={{
                                border: "1px solid #ddd",
                                borderRadius: 10,
                                padding: 15,
                                background: "white",
                            }}
                        >
                            <div style={{ fontWeight: "bold", marginBottom: 4 }}>
                                {c.name}
                            </div>
                            <div style={{ marginBottom: 4 }}>{c.email}</div>
                            <div style={{ marginBottom: 4 }}>
                                Document: {c.documentNumber}
                            </div>
                            <div>Policies: {c.policiesCount}</div>
                            <button
                                onClick={() => handleEdit(c)}
                                style={{
                                    marginTop: 10,
                                    marginRight: 10,
                                    background: "#007bff",
                                    color: "white",
                                    border: "none",
                                    padding: "5px 10px",
                                    cursor: "pointer"
                                }}
                            >
                                Edit
                            </button>

                            <button
                                onClick={() => handleDelete(c.id)}
                                style={{
                                    marginTop: 10,
                                    background: "red",
                                    color: "white",
                                    border: "none",
                                    padding: "5px 10px",
                                    cursor: "pointer"
                                }}
                            >
                                Delete
                            </button>
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}

export default Customers;