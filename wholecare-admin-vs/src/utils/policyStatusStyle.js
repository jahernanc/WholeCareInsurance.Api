// Mismo orden categórico fijo que Dashboard.jsx (paleta validada, skill de dataviz —
// nunca se reordena según los datos). Se reexpresa acá como par pastel (fondo claro +
// texto oscuro del mismo tono), mismo criterio que agencyStyle.js, para usarlo como
// badge de tabla (§18.1) en vez del hex saturado que usan las marcas de gráfico.
export const POLICY_STATUSES = ["Draft", "Pendiente", "Cancelado", "Por procesar", "En proceso", "Actualizado", "Procesado", "Cambio de agente"];

const STATUS_STYLES = [
    { bg: "#dbeafe", text: "#1e40af" }, // Draft
    { bg: "#ffedd5", text: "#9a3412" }, // Pendiente
    { bg: "#d1fae5", text: "#065f46" }, // Cancelado
    { bg: "#fef3c7", text: "#92400e" }, // Por procesar
    { bg: "#fce7f3", text: "#9d174d" }, // En proceso
    { bg: "#dcfce7", text: "#14532d" }, // Actualizado
    { bg: "#ede9fe", text: "#5b21b6" }, // Procesado
    { bg: "#fee2e2", text: "#991b1b" }, // Cambio de agente
];
const DEFAULT_STYLE = { bg: "#f3f4f6", text: "#6b7280" };

export function policyStatusStyle(status) {
    const idx = POLICY_STATUSES.indexOf(status);
    return idx === -1 ? DEFAULT_STYLE : STATUS_STYLES[idx % STATUS_STYLES.length];
}
