import { useTranslation } from "react-i18next";

const inputStyle = { width: "100%", padding: "7px 10px", marginTop: 4, boxSizing: "border-box", borderRadius: 5, border: "1px solid #ccc" };
const labelStyle = { fontWeight: 500, fontSize: 13 };

// Campos específicos de Life Insurance (§12.3), extraídos como componente compartido
// para reusarse tanto dentro de CustomerFormFields (sección "crear dependiente nuevo"
// de Policies.jsx) como en la sección "Datos Life Insurance del titular" de Policies.jsx
// (§12.3/§12.6) — mismo criterio de paridad de campos por estructura que CustomerFormFields.
function LifeInsuranceFields({ form, onFieldChange }) {
    const { t } = useTranslation(["customers", "common"]);

    return (
        <>
            <div>
                <label style={labelStyle}>{t("form.fields.age")}</label>
                <input type="number" min="0" max="150" name="age" value={form.age} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.countryOfBirth")}</label>
                <input name="countryOfBirth" value={form.countryOfBirth} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.height")}</label>
                <input name="height" value={form.height} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.weight")}</label>
                <input name="weight" value={form.weight} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.currentlyEmployed")}</label>
                <select name="currentlyEmployed" value={form.currentlyEmployed} onChange={onFieldChange} style={inputStyle}>
                    <option value="">{t("form.selectPlaceholder")}</option>
                    <option value="true">{t("form.yes")}</option>
                    <option value="false">{t("form.no")}</option>
                </select>
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.netWorth")}</label>
                <input type="number" min="0" step="0.01" name="netWorth" value={form.netWorth} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.householdIncome")}</label>
                <input type="number" min="0" step="0.01" name="householdIncome" value={form.householdIncome} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.householdNetWorth")}</label>
                <input type="number" min="0" step="0.01" name="householdNetWorth" value={form.householdNetWorth} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div style={{ gridColumn: "1 / -1", display: "flex", flexDirection: "column", gap: 6 }}>
                <label style={{ display: "flex", alignItems: "center", gap: 6, fontSize: 13 }}>
                    <input type="checkbox" name="backDateToSaveAge" checked={form.backDateToSaveAge} onChange={onFieldChange} />
                    {t("form.fields.backDateToSaveAge")}
                </label>

                <label style={{ display: "flex", alignItems: "center", gap: 6, fontSize: 13 }}>
                    <input type="checkbox" name="spentMoreThan4MonthsAbroad" checked={form.spentMoreThan4MonthsAbroad} onChange={onFieldChange} />
                    {t("form.fields.spentMoreThan4MonthsAbroad")}
                </label>

                <label style={{ display: "flex", alignItems: "center", gap: 6, fontSize: 13 }}>
                    <input type="checkbox" name="militaryOrganizationMember" checked={form.militaryOrganizationMember} onChange={onFieldChange} />
                    {t("form.fields.militaryOrganizationMember")}
                </label>

                <label style={{ display: "flex", alignItems: "center", gap: 6, fontSize: 13 }}>
                    <input type="checkbox" name="hasDriverLicense" checked={form.hasDriverLicense} onChange={onFieldChange} />
                    {t("form.fields.hasDriverLicense")}
                </label>
            </div>

            {form.hasDriverLicense && (
                <div>
                    <label style={labelStyle}>{t("form.fields.driverLicenseNumber")}</label>
                    <input name="driverLicenseNumber" value={form.driverLicenseNumber} onChange={onFieldChange} style={inputStyle} />
                </div>
            )}
        </>
    );
}

export default LifeInsuranceFields;
