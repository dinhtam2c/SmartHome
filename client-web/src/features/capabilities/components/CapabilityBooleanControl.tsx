import { BooleanSwitch } from "@/shared/ui/BooleanSwitch";
import type { ReactElement } from "react";
import {
  getCapabilityBooleanLabel,
  LOCK_STATE_CAPABILITY_ID,
  normalizeCapabilityId,
  type CapabilityBooleanLabels,
} from "@/features/capabilities";
import { LockStateToggle } from "./LockStateToggle";

type BooleanControlRendererProps = {
  id?: string;
  checked: boolean;
  disabled?: boolean;
  label: string;
  onChange: (checked: boolean) => void;
};

type Props = Omit<BooleanControlRendererProps, "label"> & {
  capabilityId?: string | null;
  labels: CapabilityBooleanLabels;
  label?: string;
};

const BOOLEAN_CONTROL_RENDERERS: Record<
  string,
  (props: BooleanControlRendererProps) => ReactElement
> = {
  [LOCK_STATE_CAPABILITY_ID]: (props) => <LockStateToggle {...props} />,
};

export function CapabilityBooleanControl({
  capabilityId,
  checked,
  disabled = false,
  id,
  label,
  labels,
  onChange,
}: Props) {
  const resolvedLabel =
    label ?? getCapabilityBooleanLabel(capabilityId, checked, labels);
  const renderer =
    BOOLEAN_CONTROL_RENDERERS[normalizeCapabilityId(capabilityId)];

  if (renderer) {
    return renderer({
      id,
      checked,
      disabled,
      label: resolvedLabel,
      onChange,
    });
  }

  return (
    <BooleanSwitch
      id={id}
      checked={checked}
      disabled={disabled}
      label={resolvedLabel}
      onChange={onChange}
    />
  );
}
