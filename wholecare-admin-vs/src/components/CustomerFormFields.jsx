import { useTranslation } from "react-i18next";
import { translateEnum } from "../i18n/translateEnum";
import { US_STATES } from "../data/usStates";
import US_COUNTIES from "../data/usCounties.json";
import {
    MIGRATION_STATUSES,
    RELACIONES_PRINCIPAL,
    MARITAL_STATUSES,
    GENDERS,
    CONTACT_LANGUAGES,
} from "../data/customerFormOptions";

const inputStyle = { width: "100%", padding: "7px 10px", marginTop: 4, boxSizing: "border-box", borderRadius: 5, border: "1px solid #ccc" };
const labelStyle = { fontWeight: 500, fontSize: 13 };

// Campos del formulario de Customer, extraídos como componente compartido para que
// Customers.jsx y la sección "crear dependiente nuevo" de Policies.jsx (§2) tengan
// paridad de campos garantizada por estructura, no por copiar/pegar el mismo JSX dos veces.
function CustomerFormFields({ form, onFieldChange, agents = [], userIsAdmin = false }) {
    const { t } = useTranslation(["customers", "common"]);
    const encargados = agents.filter((a) => a.isEncargado);
    const countiesForState = form.state ? (US_COUNTIES[form.state] ?? []) : [];

    return (
        <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>

            <div>
                <label style={labelStyle}>{t("form.fields.ssn")}</label>
                <input name="socialSecurityNumber" value={form.socialSecurityNumber} onChange={onFieldChange} required style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.dateOfBirth")}</label>
                <input type="date" name="dateOfBirth" value={form.dateOfBirth} onChange={onFieldChange} required style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.firstName")}</label>
                <input name="firstName" value={form.firstName} onChange={onFieldChange} required style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.lastName")}</label>
                <input name="lastName" value={form.lastName} onChange={onFieldChange} required style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.middleName")}</label>
                <input name="middleName" value={form.middleName} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.gender")}</label>
                <select name="gender" value={form.gender} onChange={onFieldChange} style={inputStyle}>
                    <option value="">{t("form.selectPlaceholder")}</option>
                    {GENDERS.map((g) => (
                        <option key={g} value={g}>{translateEnum("gender", g)}</option>
                    ))}
                </select>
            </div>

            <div style={{ gridColumn: "1 / -1" }}>
                <label style={labelStyle}>{t("form.fields.email")}</label>
                <input type="email" name="email" value={form.email} onChange={onFieldChange} required style={inputStyle} />
            </div>

            <div style={{ gridColumn: "1 / -1" }}>
                <label style={labelStyle}>{t("form.fields.address1")}</label>
                <input name="address1" value={form.address1} onChange={onFieldChange} required style={inputStyle} />
            </div>

            <div style={{ gridColumn: "1 / -1" }}>
                <label style={labelStyle}>{t("form.fields.address2")}</label>
                <input name="address2" value={form.address2} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.phone")}</label>
                <input name="phone" value={form.phone} onChange={onFieldChange} required style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.migrationStatus")}</label>
                <select name="migrationStatus" value={form.migrationStatus} onChange={onFieldChange} required style={inputStyle}>
                    <option value="">{t("form.selectPlaceholder")}</option>
                    {MIGRATION_STATUSES.map((s) => (
                        <option key={s} value={s}>{translateEnum("migrationStatus", s)}</option>
                    ))}
                </select>
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.relacionConPrincipal")}</label>
                <select name="relacionConPrincipal" value={form.relacionConPrincipal} onChange={onFieldChange} required style={inputStyle}>
                    <option value="">{t("form.selectPlaceholder")}</option>
                    {RELACIONES_PRINCIPAL.map((r) => (
                        <option key={r} value={r}>{translateEnum("relacionConPrincipal", r)}</option>
                    ))}
                </select>
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.zipCode")}</label>
                <input name="zipCode" value={form.zipCode} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.state")}</label>
                <select name="state" value={form.state} onChange={onFieldChange} style={inputStyle}>
                    <option value="">{t("form.selectPlaceholder")}</option>
                    {US_STATES.map((s) => (
                        <option key={s.code} value={s.code}>{s.name}</option>
                    ))}
                </select>
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.city")}</label>
                <input name="city" value={form.city} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.county")}</label>
                <select name="county" value={form.county} onChange={onFieldChange} disabled={!form.state} style={inputStyle}>
                    <option value="">{form.state ? t("form.selectPlaceholder") : t("form.selectStateFirst")}</option>
                    {countiesForState.map((c) => (
                        <option key={c} value={c}>{c}</option>
                    ))}
                </select>
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.maritalStatus")}</label>
                <select name="maritalStatus" value={form.maritalStatus} onChange={onFieldChange} style={inputStyle}>
                    <option value="">{t("form.selectPlaceholder")}</option>
                    {MARITAL_STATUSES.map((m) => (
                        <option key={m} value={m}>{translateEnum("maritalStatus", m)}</option>
                    ))}
                </select>
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.occupation")}</label>
                <input name="occupation" value={form.occupation} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.greenCard")}</label>
                <input name="greenCard" value={form.greenCard} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.workPermit")}</label>
                <input name="workPermit" value={form.workPermit} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.employerName")}</label>
                <input name="employerName" value={form.employerName} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.companyPhone")}</label>
                <input name="companyPhone" value={form.companyPhone} onChange={onFieldChange} style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.annualIncome")}</label>
                <input type="number" min="0" step="0.01" name="annualIncome" value={form.annualIncome} onChange={onFieldChange} required style={inputStyle} />
            </div>

            <div>
                <label style={labelStyle}>{t("form.fields.contactLanguage")}</label>
                <select name="contactLanguage" value={form.contactLanguage} onChange={onFieldChange} style={inputStyle}>
                    <option value="">{t("form.selectPlaceholder")}</option>
                    {CONTACT_LANGUAGES.map((l) => (
                        <option key={l} value={l}>{translateEnum("contactLanguage", l)}</option>
                    ))}
                </select>
            </div>

            <div style={{ gridColumn: "1 / -1" }}>
                <label style={labelStyle}>{t("form.fields.tags")}</label>
                <input name="tags" value={form.tags} onChange={onFieldChange} style={inputStyle} />
            </div>

            {userIsAdmin && (
                <>
                    <div>
                        <label style={labelStyle}>{t("form.fields.agent")}</label>
                        <select name="agentId" value={form.agentId} onChange={onFieldChange} style={inputStyle}>
                            <option value="">{t("form.unassigned")}</option>
                            {agents.map((a) => (
                                <option key={a.id} value={a.id}>{a.nombre}</option>
                            ))}
                        </select>
                    </div>

                    <div>
                        <label style={labelStyle}>{t("form.fields.assistantAgent")}</label>
                        <select name="assistantAgentId" value={form.assistantAgentId} onChange={onFieldChange} style={inputStyle}>
                            <option value="">{t("form.unassigned")}</option>
                            {agents.map((a) => (
                                <option key={a.id} value={a.id}>{a.nombre}</option>
                            ))}
                        </select>
                    </div>

                    <div>
                        <label style={labelStyle}>{t("form.fields.recordAgent")}</label>
                        <select name="recordAgentId" value={form.recordAgentId} onChange={onFieldChange} style={inputStyle}>
                            <option value="">{t("form.unassigned")}</option>
                            {encargados.map((a) => (
                                <option key={a.id} value={a.id}>{a.nombre}</option>
                            ))}
                        </select>
                    </div>
                </>
            )}

        </div>
    );
}

export default CustomerFormFields;
