// Estilos de tabla compartidos entre Policies/Customers/Agentes (§17), para que
// las 3 pantallas tengan el mismo header/celdas/acciones.
export const tableHeaderRowStyle = { background: "#f3f4f6", textAlign: "left" };
export const tableCellStyle = { padding: 10 };

// Header clickeable para columnas ordenables (§17.2) — mismo padding que tableCellStyle.
export const sortableHeaderStyle = { ...tableCellStyle, cursor: "pointer", userSelect: "none" };

// Contenedor de los íconos de acciones de cada fila — flex + nowrap para que
// nunca se envuelvan en 2 líneas sin importar el ancho de la columna.
export const actionsCellStyle = { display: "flex", alignItems: "center", gap: 10, flexWrap: "nowrap" };

export const actionButtonStyle = { background: "transparent", border: "none", cursor: "pointer", fontSize: 16 };
export const actionLinkStyle = { fontSize: 16, textDecoration: "none" };
