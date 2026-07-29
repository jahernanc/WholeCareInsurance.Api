import { useState } from "react";
import { useTranslation } from "react-i18next";
import { apiFetch } from "../api";
import LifeInsuranceFields from "./LifeInsuranceFields";

function buildFormFromCustomer(c) {
    return {
        age: c?.age ?? "",
        countryOfBirth: c?.countryOfBirth ?? "",
        height: c?.height ?? "",
        weight: c?.weight ?? "",
        backDateToSaveAge: c?.backDateToSaveAge ?? false,
        spentMoreThan4MonthsAbroad: c?.spentMoreThan4MonthsAbroad ?? false,
        militaryOrganizationMember: c?.militaryOrganizationMember ?? false,
        currentlyEmployed: c?.currentlyEmployed === null || c?.currentlyEmployed === undefined ? "" : String(c.currentlyEmployed),
        hasDriverLicense: c?.hasDriverLicense ?? false,
        driverLicenseNumber: c?.driverLicenseNumber ?? "",
        netWorth: c?.netWorth ?? "",
        householdIncome: c?.householdIncome ?? "",
        householdNetWorth: c?.householdNetWorth ?? "",
    };
}

// Sección "Datos Life Insurance del titular" de Policies.jsx (§20.3) — extraída a
// subcomponente propio para eliminar el useEffect que sincronizaba manualmente
// titularLifeForm con el Customer seleccionado (violaba react-hooks/set-state-in-effect;
// era además el patrón "you might not need an effect" de la doc de React: estado
// derivado de una prop/identidad, no algo que sincronizar a mano). El padre monta este
// componente con key={customerId} — al cambiar el titular, React lo desmonta y remonta
// entero, así el useState arranca fresco con los datos del nuevo Customer sin efecto.
function TitularLifeSection({ customer, customerId, onSaved }) {
    const { t } = useTranslation("policies");
    const [form, setForm] = useState(() => buildFormFromCustomer(customer));
    const [error, setError] = useState("");
    const [saving, setSaving] = useState(false);

    const handleField = (e) => {
        const { name, value, type: inputType, checked } = e.target;
        setForm((f) => ({ ...f, [name]: inputType === "checkbox" ? checked : value }));
    };

    const handleSave = async () => {
        if (!customer) return;

        setError("");
        setSaving(true);

        try {
            const body = {
                socialSecurityNumber: customer.socialSecurityNumber,
                firstName: customer.firstName,
                lastName: customer.lastName,
                dateOfBirth: customer.dateOfBirth?.slice(0, 10),
                email: customer.email,
                address1: customer.address1,
                phone: customer.phone,
                migrationStatus: customer.migrationStatus,
                relacionConPrincipal: customer.relacionConPrincipal,
                zipCode: customer.zipCode ?? "",
                state: customer.state ?? "",
                city: customer.city ?? "",
                county: customer.county ?? "",
                maritalStatus: customer.maritalStatus ?? "",
                occupation: customer.occupation ?? "",
                agentId: customer.agentId ?? null,
                assistantAgentId: customer.assistantAgentId ?? null,
                recordAgentId: customer.recordAgentId ?? null,
                middleName: customer.middleName ?? "",
                gender: customer.gender ?? "",
                greenCard: customer.greenCard ?? "",
                workPermit: customer.workPermit ?? "",
                address2: customer.address2 ?? "",
                employerName: customer.employerName ?? "",
                companyPhone: customer.companyPhone ?? "",
                annualIncome: customer.annualIncome ?? 0,
                tags: customer.tags ?? "",
                contactLanguage: customer.contactLanguage ?? "",
                age: form.age === "" ? null : Number(form.age),
                countryOfBirth: form.countryOfBirth,
                height: form.height,
                weight: form.weight,
                backDateToSaveAge: form.backDateToSaveAge,
                spentMoreThan4MonthsAbroad: form.spentMoreThan4MonthsAbroad,
                militaryOrganizationMember: form.militaryOrganizationMember,
                currentlyEmployed: form.currentlyEmployed === "" ? null : form.currentlyEmployed === "true",
                hasDriverLicense: form.hasDriverLicense,
                driverLicenseNumber: form.hasDriverLicense ? form.driverLicenseNumber : "",
                netWorth: form.netWorth === "" ? null : Number(form.netWorth),
                householdIncome: form.householdIncome === "" ? null : Number(form.householdIncome),
                householdNetWorth: form.householdNetWorth === "" ? null : Number(form.householdNetWorth),
            };

            const res = await apiFetch(`/api/customers/${customerId}`, {
                method: "PUT",
                body: JSON.stringify(body),
            });

            if (!res.ok) {
                setError(t("form.titularLifeSaveError"));
                return;
            }

            await onSaved();
        } catch (err) {
            console.error(err);
            setError(t("form.titularLifeSaveError"));
        } finally {
            setSaving(false);
        }
    };

    return (
        <div style={{ border: "1px solid #ddd", borderRadius: 8, padding: 12, marginBottom: 12, background: "white" }}>
            <label style={{ fontWeight: "bold" }}>{t("form.titularLifeSection")}</label>

            <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12, marginTop: 8 }}>
                <LifeInsuranceFields form={form} onFieldChange={handleField} />
            </div>

            {error && <p style={{ color: "red", marginTop: 8 }}>{error}</p>}

            <button
                type="button"
                onClick={handleSave}
                disabled={saving}
                style={{ marginTop: 8, background: "#16a34a", color: "white", padding: "8px 16px", border: "none", borderRadius: 6, cursor: "pointer" }}
            >
                {saving ? t("form.savingTitularLife") : t("form.saveTitularLife")}
            </button>
        </div>
    );
}

export default TitularLifeSection;
