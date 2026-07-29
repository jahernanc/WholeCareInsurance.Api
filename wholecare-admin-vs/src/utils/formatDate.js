// dd/mm/aaaa, sin hora (§17.2/§18.5) — independiente del locale del browser.
export const formatDateOnly = (isoString) => {
    const d = new Date(isoString);
    const dd = String(d.getDate()).padStart(2, "0");
    const mm = String(d.getMonth() + 1).padStart(2, "0");
    return `${dd}/${mm}/${d.getFullYear()}`;
};
