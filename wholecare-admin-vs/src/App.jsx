import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import Login from "./Login";
import AppLayout from "./layout/AppLayout";
import Customers from "./pages/Customers";
import Policies from "./pages/Policies";

function Dashboard() {
    return <h1>Dashboard ✅</h1>;
}

function App() {
    const token = localStorage.getItem("accessToken");

    return (
        <BrowserRouter>
            <Routes>

                {/* login */}
                <Route path="/login" element={<Login />} />

                {/* redirect si no hay token */}
                <Route
                    path="*"
                    element={
                        token ? <AppLayout /> : <Navigate to="/login" replace />
                    }
                >
                    {/* rutas internas */}
                    <Route index element={<Dashboard />} />
                    <Route path="customers" element={<Customers />} />
                    <Route path="policies" element={<Policies />} />
                </Route>

            </Routes>
        </BrowserRouter>
    );
}

export default App;