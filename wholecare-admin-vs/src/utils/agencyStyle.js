import { AGENCIES } from "../data/agentFormOptions";

// Mismo orden categórico fijo que Dashboard.jsx (paleta validada, skill de dataviz:
// slot 1 azul, slot 2 naranja — nunca se reordena según los datos). Acá se expresa como
// par pastel (fondo claro + texto oscuro del mismo tono), mismo criterio que el badge de
// IsActive ya existente en Agentes.jsx, en vez del hex saturado que usan las marcas de
// gráfico — es una identidad de UI (badge), no una marca de chart.
const AGENCY_STYLES = [
    { bg: "#dbeafe", text: "#1e40af" }, // slot 1 (azul) — Whole Care Insurance Group llC
    { bg: "#ffedd5", text: "#9a3412" }, // slot 2 (naranja) — Preventive Health Insurance
];
const DEFAULT_STYLE = { bg: "#f3f4f6", text: "#6b7280" };

export function agencyStyle(agency) {
    const idx = AGENCIES.indexOf(agency);
    return idx === -1 ? DEFAULT_STYLE : AGENCY_STYLES[idx % AGENCY_STYLES.length];
}
