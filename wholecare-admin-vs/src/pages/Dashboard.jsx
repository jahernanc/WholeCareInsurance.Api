import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Link } from "react-router-dom";
import { apiFetch, isAdmin } from "../api";
import { translateEnum } from "../i18n/translateEnum";
import { POLICY_STATUSES } from "../utils/policyStatusStyle";

// Paleta categórica validada (skill de dataviz) — orden fijo, nunca se reordena
// según el ranking de la vista filtrada (una misma categoría conserva siempre
// el mismo color, sea cual sea el filtro activo).
const POLICY_TYPES = ["Health Insurance (ACA)", "Medicare", "Life Insurance", "Supplemental Plans", "Auto", "Otro"];
const CATEGORICAL_COLORS = ["#2a78d6", "#eb6834", "#1baf7a", "#eda100", "#e87ba4", "#008300", "#4a3aa7", "#e34948"];

const typeColor = (type) => CATEGORICAL_COLORS[POLICY_TYPES.indexOf(type) % CATEGORICAL_COLORS.length] ?? "#898781";
const statusColor = (status) => CATEGORICAL_COLORS[POLICY_STATUSES.indexOf(status) % CATEGORICAL_COLORS.length] ?? "#898781";

const cardStyle = { border: "1px solid #ddd", borderRadius: 10, padding: 16, background: "white" };
const sectionTitleStyle = { margin: "0 0 12px", fontSize: 16 };

function StatTile({ label, value }) {
    return (
        <div style={{ ...cardStyle, flex: 1, minWidth: 140 }}>
            <div style={{ fontSize: 13, color: "#666" }}>{label}</div>
            <div style={{ fontSize: 28, fontWeight: 700, marginTop: 4 }}>{value}</div>
        </div>
    );
}

function DonutChart({ segments, total }) {
    if (total === 0) return null;

    const stops = segments.reduce((acc, s) => {
        const start = acc.cumulativePercent;
        const end = start + (s.count / total) * 100;
        acc.parts.push(`${s.color} ${start}% ${end}%`);
        acc.cumulativePercent = end;
        return acc;
    }, { cumulativePercent: 0, parts: [] }).parts;

    return (
        <div
            role="img"
            aria-label={segments.map((s) => `${s.label}: ${s.count}`).join(", ")}
            style={{
                width: 140,
                height: 140,
                borderRadius: "50%",
                background: `conic-gradient(${stops.join(", ")})`,
                flexShrink: 0,
                position: "relative",
            }}
        >
            <div
                style={{
                    position: "absolute",
                    inset: 28,
                    borderRadius: "50%",
                    background: "white",
                    display: "flex",
                    alignItems: "center",
                    justifyContent: "center",
                    fontSize: 13,
                    color: "#666",
                    textAlign: "center",
                }}
            >
                {total}
            </div>
        </div>
    );
}

function ChartWithLegend({ title, segments }) {
    const total = segments.reduce((sum, s) => sum + s.count, 0);
    return (
        <div style={cardStyle}>
            <h4 style={sectionTitleStyle}>{title}</h4>
            <div style={{ display: "flex", gap: 20, alignItems: "center", flexWrap: "wrap" }}>
                <DonutChart segments={segments} total={total} />
                <ul style={{ listStyle: "none", padding: 0, margin: 0, flex: 1, minWidth: 180 }}>
                    {segments.map((s) => (
                        <li key={s.label} style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 13, padding: "3px 0" }}>
                            <span style={{ width: 10, height: 10, borderRadius: 2, background: s.color, flexShrink: 0 }} />
                            <span style={{ flex: 1 }}>{s.label}</span>
                            <span style={{ color: "#666" }}>
                                {s.count} ({total > 0 ? Math.round((s.count / total) * 100) : 0}%)
                            </span>
                        </li>
                    ))}
                </ul>
            </div>
        </div>
    );
}

function NameCountList({ title, items, unspecifiedLabel, andMoreLabel, limit = 10 }) {
    const visible = items.slice(0, limit);
    const restCount = items.length - visible.length;
    const total = items.reduce((sum, i) => sum + i.policiesCount, 0);

    return (
        <div style={cardStyle}>
            <h5 style={{ margin: "0 0 8px", fontSize: 14 }}>{title}</h5>
            <ul style={{ listStyle: "none", padding: 0, margin: 0 }}>
                {visible.map((i) => (
                    <li key={i.name} style={{ display: "flex", justifyContent: "space-between", fontSize: 13, padding: "3px 0" }}>
                        <span>{i.name === "Sin especificar" || i.name === "Unspecified" ? unspecifiedLabel : i.name}</span>
                        <span style={{ color: "#666" }}>{i.policiesCount}</span>
                    </li>
                ))}
            </ul>
            {restCount > 0 && (
                <div style={{ fontSize: 12, color: "#888", marginTop: 6 }}>{andMoreLabel(restCount)}</div>
            )}
            {items.length === 0 && total === 0 && <div style={{ fontSize: 13, color: "#888" }}>—</div>}
        </div>
    );
}

function Dashboard() {
    const { t } = useTranslation(["dashboard", "common"]);
    const admin = isAdmin();

    const [loading, setLoading] = useState(true);
    const [agents, setAgents] = useState([]);
    const [selectedAgentId, setSelectedAgentId] = useState("");
    const [from, setFrom] = useState("");
    const [to, setTo] = useState("");

    const [summary, setSummary] = useState(null);
    const [byStatus, setByStatus] = useState([]);
    const [byType, setByType] = useState([]);
    const [stats, setStats] = useState(null);
    const [latestPolicies, setLatestPolicies] = useState([]);
    const [upcoming65, setUpcoming65] = useState([]);

    useEffect(() => {
        if (!admin) return;
        (async () => {
            try {
                const res = await apiFetch("/users?role=Agente");
                if (res.ok) setAgents(await res.json());
            } catch {
                // best-effort: sin la lista de agentes, el selector queda vacío pero el resto funciona
            }
        })();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const loadDashboard = async (overrides = {}) => {
        const effective = {
            agentId: selectedAgentId,
            from,
            to,
            ...overrides,
        };

        const commonParams = new URLSearchParams();
        if (admin && effective.agentId) commonParams.set("agentId", effective.agentId);
        const dateParams = new URLSearchParams(commonParams);
        if (effective.from) dateParams.set("from", effective.from);
        if (effective.to) dateParams.set("to", effective.to);

        try {
            setLoading(true);
            const [summaryRes, byStatusRes, byTypeRes, statsRes, latestRes, upcomingRes] = await Promise.all([
                apiFetch(`/api/dashboard/summary?${dateParams}`),
                apiFetch(`/api/dashboard/by-status?${dateParams}`),
                apiFetch(`/api/dashboard/by-type?${dateParams}`),
                apiFetch(`/api/dashboard/stats?${dateParams}`),
                apiFetch(`/api/dashboard/latest-policies?${commonParams}`),
                apiFetch(`/api/dashboard/upcoming-65?${commonParams}`),
            ]);

            if (summaryRes.ok) setSummary(await summaryRes.json());
            if (byStatusRes.ok) setByStatus(await byStatusRes.json());
            if (byTypeRes.ok) setByType(await byTypeRes.json());
            if (statsRes.ok) setStats(await statsRes.json());
            if (latestRes.ok) setLatestPolicies(await latestRes.json());
            if (upcomingRes.ok) setUpcoming65(await upcomingRes.json());
        } catch (error) {
            console.error("Error loading dashboard:", error);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        // queueMicrotask (§20): loadDashboard() setea loading síncronamente antes de su
        // primer await — llamarlo directo acá dispara react-hooks/set-state-in-effect
        // (cascading render). Diferir un microtask rompe esa cadena de reachability
        // síncrona sin cambiar nada perceptible para el usuario.
        queueMicrotask(loadDashboard);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const handleSearch = () => loadDashboard();
    const handleClear = () => {
        setSelectedAgentId("");
        setFrom("");
        setTo("");
        loadDashboard({ agentId: "", from: "", to: "" });
    };

    const typeSegments = byType.map((x) => ({ label: translateEnum("policyType", x.type), count: x.policiesCount, color: typeColor(x.type) }));
    const statusSegments = byStatus.map((x) => ({ label: translateEnum("policyStatus", x.status), count: x.policiesCount, color: statusColor(x.status) }));

    return (
        <div>
            <h2 style={{ marginBottom: 20 }}>{t("dashboard:title")}</h2>

            {/* Filtros: por fecha (EffectiveDate) y por agente (solo Admin puede elegir; un
                Agente ve siempre lo suyo, el selector ni se muestra). */}
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
                    <label style={{ display: "block", fontSize: 12 }}>{t("dashboard:filters.from")}</label>
                    <input type="date" value={from} onChange={(e) => setFrom(e.target.value)} style={{ padding: 6 }} />
                </div>
                <div>
                    <label style={{ display: "block", fontSize: 12 }}>{t("dashboard:filters.to")}</label>
                    <input type="date" value={to} onChange={(e) => setTo(e.target.value)} style={{ padding: 6 }} />
                </div>
                {admin && (
                    <div>
                        <label style={{ display: "block", fontSize: 12 }}>{t("dashboard:filters.agent")}</label>
                        <select value={selectedAgentId} onChange={(e) => setSelectedAgentId(e.target.value)} style={{ padding: 6 }}>
                            <option value="">{t("dashboard:filters.allAgents")}</option>
                            {agents.map((a) => (
                                <option key={a.id} value={a.id}>{a.nombre}</option>
                            ))}
                        </select>
                    </div>
                )}
                <button
                    type="button"
                    onClick={handleSearch}
                    style={{ background: "#2563eb", color: "white", padding: "8px 12px", border: "none", borderRadius: 6, cursor: "pointer" }}
                >
                    {t("common:actions.search")}
                </button>
                <button
                    type="button"
                    onClick={handleClear}
                    style={{ background: "transparent", border: "1px solid #ccc", padding: "8px 12px", borderRadius: 6, cursor: "pointer" }}
                >
                    {t("common:actions.clear")}
                </button>
            </div>

            {loading || !summary ? (
                <p>{t("dashboard:loading")}</p>
            ) : (
                <>
                    {/* KPIs: Agencias/Agentes solo existen en summary cuando la vista es global
                        (null en cualquier vista scopeada a un agente, sea porque el usuario logueado
                        ES un Agente, o porque el Admin eligió "pararse en los zapatos" de uno). */}
                    <div style={{ display: "flex", gap: 12, flexWrap: "wrap", marginBottom: 20 }}>
                        {summary.agenciesCount !== null && <StatTile label={t("dashboard:summary.agencies")} value={summary.agenciesCount} />}
                        {summary.agentsCount !== null && <StatTile label={t("dashboard:summary.agents")} value={summary.agentsCount} />}
                        <StatTile label={t("dashboard:summary.policies")} value={summary.policiesCount} />
                        <StatTile label={t("dashboard:summary.members")} value={summary.membersCount} />
                    </div>

                    <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(300px, 1fr))", gap: 16, marginBottom: 20 }}>
                        <ChartWithLegend title={t("dashboard:byType.title")} segments={typeSegments} />
                        <ChartWithLegend title={t("dashboard:byStatus.title")} segments={statusSegments} />
                    </div>

                    {stats && (
                        <div style={{ marginBottom: 20 }}>
                            <h3 style={sectionTitleStyle}>{t("dashboard:stats.title")}</h3>
                            <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(220px, 1fr))", gap: 12 }}>
                                <NameCountList
                                    title={t("dashboard:stats.byInsuranceCompany")}
                                    items={stats.byInsuranceCompany}
                                    unspecifiedLabel={t("dashboard:stats.unspecified")}
                                    andMoreLabel={(n) => t("dashboard:stats.andMore", { count: n })}
                                />
                                <NameCountList
                                    title={t("dashboard:stats.byCounty")}
                                    items={stats.byCounty}
                                    unspecifiedLabel={t("dashboard:stats.unspecified")}
                                    andMoreLabel={(n) => t("dashboard:stats.andMore", { count: n })}
                                />
                                <NameCountList
                                    title={t("dashboard:stats.byCity")}
                                    items={stats.byCity}
                                    unspecifiedLabel={t("dashboard:stats.unspecified")}
                                    andMoreLabel={(n) => t("dashboard:stats.andMore", { count: n })}
                                />
                            </div>
                        </div>
                    )}

                    <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fit, minmax(340px, 1fr))", gap: 16 }}>
                        <div style={cardStyle}>
                            <h4 style={sectionTitleStyle}>{t("dashboard:latestPolicies.title")}</h4>
                            {latestPolicies.length === 0 ? (
                                <p style={{ color: "#666", fontSize: 13 }}>{t("dashboard:latestPolicies.empty")}</p>
                            ) : (
                                <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 13 }}>
                                    <thead>
                                        <tr style={{ textAlign: "left", color: "#666" }}>
                                            <th style={{ padding: "4px 6px" }}>{t("dashboard:latestPolicies.columns.customer")}</th>
                                            <th style={{ padding: "4px 6px" }}>{t("dashboard:latestPolicies.columns.contact")}</th>
                                            <th style={{ padding: "4px 6px" }}>{t("dashboard:latestPolicies.columns.status")}</th>
                                            <th style={{ padding: "4px 6px" }}>{t("dashboard:latestPolicies.columns.updatedAt")}</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {latestPolicies.map((p) => (
                                            <tr key={p.id} style={{ borderTop: "1px solid #eee" }}>
                                                <td style={{ padding: "4px 6px" }}>
                                                    <Link to="/customers">{p.customerName}</Link>
                                                </td>
                                                <td style={{ padding: "4px 6px" }}>{p.phone || p.email}</td>
                                                <td style={{ padding: "4px 6px" }}>{translateEnum("policyStatus", p.status)}</td>
                                                <td style={{ padding: "4px 6px" }}>{new Date(p.updatedAt).toLocaleString()}</td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            )}
                        </div>

                        <div style={cardStyle}>
                            <h4 style={sectionTitleStyle}>{t("dashboard:upcoming65.title")}</h4>
                            {upcoming65.length === 0 ? (
                                <p style={{ color: "#666", fontSize: 13 }}>{t("dashboard:upcoming65.empty")}</p>
                            ) : (
                                <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 13 }}>
                                    <thead>
                                        <tr style={{ textAlign: "left", color: "#666" }}>
                                            <th style={{ padding: "4px 6px" }}>{t("dashboard:upcoming65.columns.name")}</th>
                                            <th style={{ padding: "4px 6px" }}>{t("dashboard:upcoming65.columns.dateOfBirth")}</th>
                                            <th style={{ padding: "4px 6px" }}>{t("dashboard:upcoming65.columns.age")}</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {upcoming65.map((c) => (
                                            <tr key={c.customerId} style={{ borderTop: "1px solid #eee" }}>
                                                <td style={{ padding: "4px 6px" }}>
                                                    <Link to="/customers">{c.customerName}</Link>
                                                </td>
                                                <td style={{ padding: "4px 6px" }}>{c.dateOfBirth.slice(0, 10)}</td>
                                                <td style={{ padding: "4px 6px" }}>{c.age}</td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            )}
                        </div>
                    </div>
                </>
            )}
        </div>
    );
}

export default Dashboard;
