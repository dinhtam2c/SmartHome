import { useTranslation } from "react-i18next";
import { Button } from "@/shared/ui/Button";
import {
  CAPABILITY_LABEL_KEYS,
  getCapabilityBooleanLabels,
  getCapabilityColorLabel,
  localizeCapabilityCommandPath,
  type CapabilityControlCommitPolicy,
  type RgbChannel,
} from "@/features/capabilities";
import { validateCommandValue, type OperationRule } from "../../services/deviceCapabilityService";
import {
  buildOperationPayload,
  hasOperationFieldValidationError,
  hasUnsupportedOperationFields,
  resolveOperationFieldRenderPlan,
  type OperationFieldState,
} from "../../services/deviceOperationFieldService";
import type { DeviceCapabilityDto } from "../../types/deviceTypes";
import { DeviceOperationFieldsEditor } from "./DeviceOperationFieldsEditor";
import pageStyles from "../capability/DeviceCapability.module.css";

type Props = {
  capability: DeviceCapabilityDto;
  operation: string;
  rule: OperationRule | null;
  contextKey: string;
  fields: OperationFieldState[];
  disabled: boolean;
  requireOperation?: boolean;
  rgbCommitPolicy?: CapabilityControlCommitPolicy;
  rgbThrottleMs?: number;
  onChangeField: (path: string, rawValue: string) => void;
  onInlineCommandSend: (
    capabilityId: string,
    capabilityKey: string,
    endpointId: string,
    operation: string,
    rule: OperationRule | null,
    rawValue: string,
    valuePath?: string | null,
    valueOverride?: unknown
  ) => void;
};

export function DeviceOperationCommandForm({
  capability,
  operation,
  rule,
  contextKey,
  fields,
  disabled,
  requireOperation = false,
  rgbCommitPolicy = "immediate",
  rgbThrottleMs,
  onChangeField,
  onInlineCommandSend,
}: Props) {
  const { t } = useTranslation("devices");
  const hasValidationError = hasOperationFieldValidationError(fields);
  const renderPlan = resolveOperationFieldRenderPlan(
    capability.capabilityId,
    fields,
    operation
  );
  const hasUnsupportedField =
    hasUnsupportedOperationFields(fields) || renderPlan.skippedFields.length > 0;
  const isSendDisabled =
    disabled ||
    (requireOperation && !operation) ||
    hasValidationError;
  const isRgbOperation = renderPlan.kind === "rgb";
  const rgbPathToChannel = isRgbOperation
    ? (Object.fromEntries(
      Object.entries(renderPlan.channelPaths).map(([channel, path]) => [path, channel])
    ) as Record<string, RgbChannel>)
    : null;

  if (renderPlan.kind === "unsupported" || hasUnsupportedField) {
    return null;
  }

  return (
    <>
      <DeviceOperationFieldsEditor
        capabilityId={capability.capabilityId}
        operation={operation}
        contextKey={contextKey}
        fields={fields}
        disabled={disabled}
        getFieldLabel={(path) =>
          localizeCapabilityCommandPath(t, path, CAPABILITY_LABEL_KEYS.device)
        }
        onChangeField={onChangeField}
        rgbCommitPolicy={rgbCommitPolicy}
        rgbThrottleMs={rgbThrottleMs}
        onRgbCommit={(nextRgb) => {
          if (!isRgbOperation || disabled || !rgbPathToChannel) {
            return;
          }

          const nextFieldStates = fields.map((field) => {
            const channel = rgbPathToChannel[field.path.trim()];
            if (!channel) {
              return field;
            }

            const rawValue = String(nextRgb[channel]);
            return {
              ...field,
              rawValue,
              validation: validateCommandValue(field.rule, rawValue),
            };
          });

          if (nextFieldStates.some((field) => field.validation.errorKey)) {
            return;
          }

          onInlineCommandSend(
            capability.capabilityId,
            capability.id,
            capability.endpointId,
            operation,
            rule,
            "",
            null,
            buildOperationPayload(nextFieldStates)
          );
        }}
        labels={{
          enterValue: t("enterValue"),
          ...getCapabilityBooleanLabels(t, CAPABILITY_LABEL_KEYS.device),
          rgbColor: getCapabilityColorLabel(t, CAPABILITY_LABEL_KEYS.device),
          validationError: (errorKey, options) =>
            t(errorKey, {
              ...(options ?? {}),
              defaultValue: errorKey,
            }),
        }}
      />

      {isRgbOperation ? null : (
        <Button
          size="sm"
          className={pageStyles.compactActionButton}
          onClick={() => {
            onInlineCommandSend(
              capability.capabilityId,
              capability.id,
              capability.endpointId,
              operation,
              rule,
              "",
              null,
              buildOperationPayload(fields)
            );
          }}
          disabled={isSendDisabled}
        >
          {t("send")}
        </Button>
      )}
    </>
  );
}
