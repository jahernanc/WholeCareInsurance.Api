// Red de seguridad adicional al <datalist> de City (§22.3): normaliza a Title Case
// en el blur del campo, aunque el usuario no haya usado el autocomplete. Capitaliza
// tras espacios, guiones y apóstrofos ("la vergne" -> "La Vergne", "o'brien" -> "O'Brien").
export function toTitleCase(value) {
    if (!value) return value;

    return value
        .toLowerCase()
        .split(" ")
        .map((word) => word.split("-").map(capitalizeAfterApostrophes).join("-"))
        .join(" ");
}

function capitalizeAfterApostrophes(part) {
    return part
        .split("'")
        .map((segment) => (segment ? segment.charAt(0).toUpperCase() + segment.slice(1) : segment))
        .join("'");
}
