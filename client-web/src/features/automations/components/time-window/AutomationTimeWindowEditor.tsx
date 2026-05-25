import { useTranslation } from "react-i18next";
import { BooleanSwitch } from "@/shared/ui/BooleanSwitch";
import { FormGroup } from "@/shared/ui/FormGroup";
import {
  AUTOMATION_DAYS_OF_WEEK,
  type AutomationTimeWindowDraft,
} from "../../services/automationFormService";
import styles from "../AutomationEditor.module.css";

type Props = {
  value: AutomationTimeWindowDraft;
  disabled?: boolean;
  onChange: (value: AutomationTimeWindowDraft) => void;
};

export function AutomationTimeWindowEditor({
  value,
  disabled = false,
  onChange,
}: Props) {
  const { t } = useTranslation("automations");
  const fieldsDisabled = disabled || !value.enabled;

  const update = (patch: Partial<AutomationTimeWindowDraft>) => {
    onChange({
      ...value,
      ...patch,
    });
  };

  const toggleDay = (day: AutomationTimeWindowDraft["daysOfWeek"][number]) => {
    const nextDays = value.daysOfWeek.includes(day)
      ? value.daysOfWeek.filter((item) => item !== day)
      : [...value.daysOfWeek, day];

    update({ daysOfWeek: nextDays });
  };

  return (
    <div className={styles.stack}>
      <div className={styles.sectionHeader}>
        <div className={styles.sectionTitle}>{t("automations.timeWindow.title")}</div>
      </div>

      <div className={styles.fullRow}>
        <BooleanSwitch
          id="automation-time-window-enabled"
          checked={value.enabled}
          disabled={disabled}
          label={
            value.enabled
              ? t("automations.timeWindow.enabled")
              : t("automations.timeWindow.disabled")
          }
          onChange={(enabled) => update({ enabled })}
        />
      </div>

      <fieldset className={styles.fields} disabled={fieldsDisabled}>
        <FormGroup
          label={t("automations.startTime")}
          htmlFor="automation-time-window-start"
        >
          <input
            id="automation-time-window-start"
            className={styles.textInput}
            type="time"
            value={value.startTimeText}
            onChange={(event) => update({ startTimeText: event.target.value })}
            required={value.enabled}
          />
        </FormGroup>

        <FormGroup
          label={t("automations.endTime")}
          htmlFor="automation-time-window-end"
        >
          <input
            id="automation-time-window-end"
            className={styles.textInput}
            type="time"
            value={value.endTimeText}
            onChange={(event) => update({ endTimeText: event.target.value })}
            required={value.enabled}
          />
        </FormGroup>

        <div className={styles.fullRow}>
          <div className={styles.daysGrid}>
            {AUTOMATION_DAYS_OF_WEEK.map((day) => (
              <label key={`automation-time-window-day-${day}`} className={styles.dayToggle}>
                <input
                  type="checkbox"
                  checked={value.daysOfWeek.includes(day)}
                  onChange={() => toggleDay(day)}
                />
                <span>{t(`automations.daysShort.${day}`)}</span>
              </label>
            ))}
          </div>
        </div>
      </fieldset>
    </div>
  );
}
