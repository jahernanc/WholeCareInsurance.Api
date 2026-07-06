import Sidebar from "./Sidebar";
import Header from "./Header";
import { Outlet } from "react-router-dom";

function AppLayout() {
    return (
        <div style={{ display: "flex", height: "100vh" }}>
            <Sidebar />

            <div style={{ flex: 1, display: "flex", flexDirection: "column" }}>
                <Header />

                <main style={{ padding: 20 }}>
                    <Outlet /> ✅
                </main>
            </div>
        </div>
    );
}

export default AppLayout;
