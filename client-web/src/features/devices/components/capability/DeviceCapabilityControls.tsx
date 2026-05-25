import { getCapabilityDeviceControlDefinition } from "@/features/capabilities";
import { FallbackCapabilityControls } from "./FallbackCapabilityControls";
import { SpecificCapabilityControl } from "./SpecificCapabilityControl";
import type { DeviceCapabilityControlsProps } from "./deviceCapabilityControlTypes";
import styles from "./DeviceCapability.module.css";

export function DeviceCapabilityControls(props: DeviceCapabilityControlsProps) {
  const definition = getCapabilityDeviceControlDefinition(
    props.capability.capabilityId,
    props.capability.capabilityVersion
  );

  return (
    <div className={styles.capabilityActions}>
      {definition ? (
        <div className={styles.inlineControlArea}>
          <SpecificCapabilityControl definition={definition} props={props} />
        </div>
      ) : (
        <FallbackCapabilityControls {...props} />
      )}
    </div>
  );
}
