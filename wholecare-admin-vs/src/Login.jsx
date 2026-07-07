import { useState } from "react";

function Login() {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState("");

    const handleSubmit = async (e) => {
        e.preventDefault();
        setError("");

        try {
            const response = await fetch("http://localhost:5279/auth/login", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    email,
                    password
                })
            });

            if (!response.ok) {
                throw new Error("Login failed");
            }

            const data = await response.json();

            console.log("LOGIN OK:", data);

            // ✅ guardar tokens
            localStorage.setItem("accessToken", data.token.accessToken);
            localStorage.setItem("refreshToken", data.token.refreshToken);

            window.location.replace("/");

        } catch (err) {
            console.error(err);
            setError("Invalid credentials");
        }
    };

    return (
        <div style={{ display: "grid", placeItems: "center", height: "100vh" }}>
            <form onSubmit={handleSubmit} style={{ width: 300 }}>
                <h2>Login</h2>

                <input
                    type="email"
                    placeholder="Email"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                    style={{ width: "100%", marginBottom: 10, padding: 8 }}
                />

                <input
                    type="password"
                    placeholder="Password"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    style={{ width: "100%", marginBottom: 10, padding: 8 }}
                />

                <button type="submit" style={{ width: "100%", padding: 10 }}>
                    Login
                </button>

                {error && <p style={{ color: "red" }}>{error}</p>}
            </form>
        </div>
    );
}

export default Login;