import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import Login from "./Login";
import AppLayout from "./layout/AppLayout";
import Customers from "./pages/Customers";
import Policies from "./pages/Policies";
import Agentes from "./pages/Agentes";
import { isAdmin } from "./api";

function Dashboard() {
    return <h1>Dashboard ✅</h1>;
}

function App() {
    return (
        <BrowserRouter>
            <Routes>

                {/* login */}
                <Route path="/login" element={<Login />} />

                {/* redirect si no hay token */}
                <Route
                    path="*"
                    element={
                        localStorage.getItem("accessToken")
                            ? <AppLayout />
                            : <Navigate to="/login" replace />
                    }
                >
                    {/* rutas internas */}
                    <Route index element={<Dashboard />} />
                    <Route path="customers" element={<Customers />} />
                    <Route path="policies" element={<Policies />} />
                    <Route
                        path="agentes"
                        element={isAdmin() ? <Agentes /> : <Navigate to="/" replace />}
                    />
                </Route>

            </Routes>
        </BrowserRouter>
    );
}

export default App;