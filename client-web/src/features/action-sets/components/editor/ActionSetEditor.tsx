import { useTranslation } from "react-i18next";
import type { CapabilityRegistryMap } from "@/features/capabilities";
import type { SelectableDeviceDto } from "@/features/capabilities";
import type { HomeRoomOverviewDto } from "@/features/homes";
import { Button } from "@/shared/ui/Button";
import type { ActionSetSection } from "../../types/actionSetTypes";
import {
  createEmptyInvokeOperationActionDraft,
  createEmptySetStateActionDraft,
  type ActionDraft,
  type ActionSetDraft,
} from "../../services/actionSetFormService";
import { ActionEditor } from "../action/ActionEditor";
import styles from "../action/ActionEditor.module.css";

type Props = {
  value: ActionSetDraft;
  rooms?: HomeRoomOverviewDto[];
  availableDevices?: SelectableDeviceDto[];
  availableDevicesByRoom?: Record<string, SelectableDeviceDto[]>;
  registryMap?: CapabilityRegistryMap;
  title?: string;
  disabled?: boolean;
  labels?: {
    mainActions?: string;
    beforeHooks?: string;
    successHooks?: string;
    failureHooks?: string;
    sequential?: string;
    parallel?: string;
    continueOnError?: string;
  };
  onChange: (value: ActionSetDraft) => void;
};

const HOOK_SECTIONS: Array<{
  section: Exclude<ActionSetSection, "main">;
  labelKey: "beforeHooks" | "successHooks" | "failureHooks";
}> = [
    { section: "before", labelKey: "beforeHooks" },
    { section: "onSuccess", labelKey: "successHooks" },
    { section: "onFailure", labelKey: "failureHooks" },
  ];

function updateAt<T>(items: T[], index: number, value: T) {
  return items.map((item, currentIndex) => (currentIndex === index ? value : item));
}

function removeAt<T>(items: T[], index: number) {
  return items.filter((_, currentIndex) => currentIndex !== index);
}

function moveAt<T>(items: T[], index: number, direction: -1 | 1) {
  const nextIndex = index + direction;
  if (nextIndex < 0 || nextIndex >= items.length) {
    return items;
  }

  const next = [...items];
  const [item] = next.splice(index, 1);
  next.splice(nextIndex, 0, item);
  return next;
}

function createAction(type: ActionDraft["type"]) {
  return type === "setState"
    ? createEmptySetStateActionDraft()
    : createEmptyInvokeOperationActionDraft();
}

export function ActionSetEditor({
  value,
  title,
  rooms = [],
  availableDevices = [],
  availableDevicesByRoom = {},
  registryMap,
  disabled = false,
  labels,
  onChange,
}: Props) {
  const { t } = useTranslation("scenes");
  const isSequential = value.executionPolicy.mode === "sequential";

  const setMainActions = (actions: ActionDraft[]) => {
    onChange({ ...value, actions });
  };

  const setHookActions = (
    section: Exclude<ActionSetSection, "main">,
    actions: ActionDraft[]
  ) => {
    onChange({
      ...value,
      hooks: {
        ...value.hooks,
        [section]: actions,
      },
    });
  };

  return (
    <>
      {title ? (
        <div className={styles.sectionHeader}>
          <div className={styles.sectionTitle}>{title}</div>
        </div>
      ) : null}
      <div className={styles.policyBar}>
        <div className={styles.segmentedControl}>
          <Button
            type="button"
            size="sm"
            variant={value.executionPolicy.mode === "sequential" ? "primary" : "secondary"}
            onClick={() =>
              onChange({
                ...value,
                executionPolicy: {
                  ...value.executionPolicy,
                  mode: "sequential",
                },
              })
            }
            disabled={disabled}
          >
            {labels?.sequential ?? t("scenes.actionSet.sequential")}
          </Button>
          <Button
            type="button"
            size="sm"
            variant={value.executionPolicy.mode === "parallel" ? "primary" : "secondary"}
            onClick={() =>
              onChange({
                ...value,
                executionPolicy: {
                  mode: "parallel",
                  continueOnError: false,
                },
              })
            }
            disabled={disabled}
          >
            {labels?.parallel ?? t("scenes.actionSet.parallel")}
          </Button>
        </div>

        {isSequential ? (
          <label className={styles.checkboxLabel}>
            <input
              type="checkbox"
              checked={value.executionPolicy.continueOnError}
              disabled={disabled}
              onChange={(event) =>
                onChange({
                  ...value,
                  executionPolicy: {
                    ...value.executionPolicy,
                    continueOnError: event.target.checked,
                  },
                })
              }
            />
            <span>{labels?.continueOnError ?? t("scenes.actionSet.continueOnError")}</span>
          </label>
        ) : null}
      </div>

      <ActionEditor
        actions={value.actions}
        title={labels?.mainActions ?? t("scenes.actionSet.mainActions")}
        allowReorder
        rooms={rooms}
        availableDevices={availableDevices}
        availableDevicesByRoom={availableDevicesByRoom}
        registryMap={registryMap}
        disabled={disabled}
        onChangeAction={(index, action) => setMainActions(updateAt(value.actions, index, action))}
        onAddAction={(type) => setMainActions([...value.actions, createAction(type)])}
        onRemoveAction={(index) => setMainActions(removeAt(value.actions, index))}
        onMoveAction={(index, direction) => setMainActions(moveAt(value.actions, index, direction))}
      />

      {HOOK_SECTIONS.map(({ section, labelKey }) => {
        const actions = value.hooks[section];

        return (
          <ActionEditor
            key={section}
            actions={actions}
            title={labels?.[labelKey]}
            allowReorder
            rooms={rooms}
            availableDevices={availableDevices}
            availableDevicesByRoom={availableDevicesByRoom}
            registryMap={registryMap}
            disabled={disabled}
            onChangeAction={(index, action) =>
              setHookActions(section, updateAt(actions, index, action))
            }
            onAddAction={(type) => setHookActions(section, [...actions, createAction(type)])}
            onRemoveAction={(index) => setHookActions(section, removeAt(actions, index))}
            onMoveAction={(index, direction) =>
              setHookActions(section, moveAt(actions, index, direction))
            }
          />
        );
      })}
    </>
  );
}
