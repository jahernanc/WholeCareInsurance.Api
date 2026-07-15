import { Link } from "react-router-dom";
import { useTranslation } from "react-i18next";
import { isAdmin } from "../api";

const menu = [
    { path: "/", labelKey: "nav.dashboard", icon: "📊" },
    { path: "/customers", labelKey: "nav.customers", icon: "👤" },
    { path: "/policies", labelKey: "nav.policies", icon: "📄" },
    { path: "/agentes", labelKey: "nav.agentes", icon: "🧑‍💼", adminOnly: true },
    { path: "/insurance-companies", labelKey: "nav.insuranceCompanies", icon: "🏢", adminOnly: true },
];

function Sidebar() {
    const { t } = useTranslation("common");
    const userIsAdmin = isAdmin();

    return (
        <div style={{
            width: 70,
            background: "#111",
            color: "white",
            display: "flex",
            flexDirection: "column",
            alignItems: "center",
            paddingTop: 10
        }}>
            {menu.filter(item => !item.adminOnly || userIsAdmin).map(item => (
                <Link
                    key={item.path}
                    to={item.path}
                    title={t(item.labelKey)} // ✅ tooltip nativo
                    style={{
                        color: "white",
                        textDecoration: "none",
                        marginBottom: 20,
                        fontSize: 20
                    }}
                >
                    {item.icon}
                </Link>
            ))}
        </div>
    );
}

export default Sidebar;
