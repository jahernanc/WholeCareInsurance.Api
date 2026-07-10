import i18n from "./index";

// Traduce un valor guardado en español en la DB (ej. Customer.MigrationStatus,
// Policy.Type) al idioma activo, sin tocar cómo se guarda/envía ese valor.
// No usa t() a propósito: varios valores contienen "/" o espacios (ej.
// "Hijo/a", "Unión libre") que romperían el key-parsing por defecto de
// i18next si se pasaran como clave de traducción.
export function translateEnum(group, value) {
    if (!value) return value;
    const dict = i18n.getResourceBundle(i18n.language, "enums")?.[group];
    return dict?.[value] ?? value;
}
