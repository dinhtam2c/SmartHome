import { useTranslation } from "react-i18next";
import {
  CAPABILITY_LABEL_KEYS,
  CapabilityBooleanControl,
  getCapabilityBooleanLabels,
  type SchemaField,
} from "@/features/capabilities";
import { FormGroup } from "@/shared/ui/FormGroup";
import type { AutomationConditionDraft } from "../../services/automationFormService";
import styles from "../AutomationEditor.module.css";

type Props = {
  condition: AutomationConditionDraft;
  field: SchemaField | undefined;
  disabled: boolean;
  onChange: (patch: Partial<AutomationConditionDraft>) => void;
};

export function ConditionValueEditor({ condition, field, disabled, onChange }: Props) {
  const { t } = useTranslation("automations");
  const { t: tScenes } = useTranslation("scenes");

  if (condition.operator === "Between") {
    return (
      <>
        <FormGroup
          label={t("automations.minValue")}
          htmlFor={`automation-condition-min-${condition.key}`}
        >
          <input
            id={`automation-condition-min-${condition.key}`}
            className={styles.textInput}
            type="number"
            value={condition.betweenMinText}
            min={field?.min ?? undefined}
            max={field?.max ?? undefined}
            step={field?.step ?? (field?.type === "integer" ? 1 : undefined)}
            disabled={disabled}
            onChange={(event) => onChange({ betweenMinText: event.target.value })}
            required
          />
        </FormGroup>
        <FormGroup
          label={t("automations.maxValue")}
          htmlFor={`automation-condition-max-${condition.key}`}
        >
          <input
            id={`automation-condition-max-${condition.key}`}
            className={styles.textInput}
            type="number"
            value={condition.betweenMaxText}
            min={field?.min ?? undefined}
            max={field?.max ?? undefined}
            step={field?.step ?? (field?.type === "integer" ? 1 : undefined)}
            disabled={disabled}
            onChange={(event) => onChange({ betweenMaxText: event.target.value })}
            required
          />
        </FormGroup>
      </>
    );
  }

  if (field?.type === "boolean") {
    return (
      <div className={styles.fullRow}>
        <div className={styles.toggleRow}>
          <CapabilityBooleanControl
            capabilityId={condition.capabilityId}
            id={`automation-condition-value-${condition.key}`}
            checked={condition.compareValueText === "true"}
            disabled={disabled}
            labels={getCapabilityBooleanLabels(tScenes, CAPABILITY_LABEL_KEYS.scene)}
            onChange={(checked) =>
              onChange({ compareValueText: checked ? "true" : "false" })
            }
          />
        </div>
      </div>
    );
  }

  if (field?.type === "enum") {
    return (
      <FormGroup
        label={t("automations.compareValue")}
        htmlFor={`automation-condition-value-${condition.key}`}
      >
        <select
          id={`automation-condition-value-${condition.key}`}
          className={styles.select}
          value={condition.compareValueText}
          disabled={disabled}
          onChange={(event) => onChange({ compareValueText: event.target.value })}
          required
        >
          {field.enumValues.map((value) => (
            <option key={`${condition.key}:enum:${value}`} value={value}>
              {value}
            </option>
          ))}
        </select>
      </FormGroup>
    );
  }

  return (
    <FormGroup
      label={t("automations.compareValue")}
      htmlFor={`automation-condition-value-${condition.key}`}
    >
      <input
        id={`automation-condition-value-${condition.key}`}
        className={styles.textInput}
        type={field?.type === "number" || field?.type === "integer" ? "number" : "text"}
        value={condition.compareValueText}
        min={field?.min ?? undefined}
        max={field?.max ?? undefined}
        step={field?.step ?? (field?.type === "integer" ? 1 : undefined)}
        disabled={disabled}
        onChange={(event) => onChange({ compareValueText: event.target.value })}
        required
      />
    </FormGroup>
  );
}
