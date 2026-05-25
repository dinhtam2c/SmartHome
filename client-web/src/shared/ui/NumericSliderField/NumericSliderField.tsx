import { useEffect, useRef } from "react";
import { Input } from "@/shared/ui/Input";
import styles from "./NumericSliderField.module.css";

export type NumericSliderCommitPolicy =
  | "commitOnRelease"
  | "formOnly"
  | "immediate"
  | "liveThrottle";

type Props = {
  id?: string;
  inputValue: string;
  sliderValue: number;
  min: number;
  max: number;
  step?: number;
  disabled?: boolean;
  placeholder?: string;
  commitPolicy?: NumericSliderCommitPolicy;
  throttleMs?: number;
  onInputChange: (value: string) => void;
  onSliderChange: (value: number) => void;
  onSliderCommit?: (value: number) => void;
};

export function NumericSliderField({
  id,
  inputValue,
  sliderValue,
  min,
  max,
  step,
  disabled = false,
  placeholder,
  commitPolicy = "liveThrottle",
  throttleMs = 100,
  onInputChange,
  onSliderChange,
  onSliderCommit,
}: Props) {
  const latestCommitValueRef = useRef<number | null>(null);
  const lastCommittedValueRef = useRef<number | null>(null);
  const lastCommittedAtRef = useRef(0);
  const commitTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const clearCommitTimer = () => {
    if (commitTimerRef.current) {
      clearTimeout(commitTimerRef.current);
      commitTimerRef.current = null;
    }
  };

  const commitSliderValue = (value: number) => {
    if (!onSliderCommit || !Number.isFinite(value)) {
      return;
    }

    if (lastCommittedValueRef.current === value) {
      return;
    }

    lastCommittedValueRef.current = value;
    lastCommittedAtRef.current = Date.now();
    onSliderCommit(value);
  };

  const scheduleSliderCommit = (value: number) => {
    if (!onSliderCommit || commitPolicy === "formOnly") {
      return;
    }

    latestCommitValueRef.current = value;

    if (commitPolicy === "commitOnRelease") {
      return;
    }

    if (commitPolicy === "immediate") {
      clearCommitTimer();
      commitSliderValue(value);
      return;
    }

    const boundedThrottleMs = Math.max(0, throttleMs);
    const elapsedMs = Date.now() - lastCommittedAtRef.current;

    if (elapsedMs >= boundedThrottleMs) {
      clearCommitTimer();
      commitSliderValue(value);
      return;
    }

    if (!commitTimerRef.current) {
      commitTimerRef.current = setTimeout(() => {
        commitTimerRef.current = null;
        const latestValue = latestCommitValueRef.current;

        if (latestValue !== null) {
          commitSliderValue(latestValue);
        }
      }, boundedThrottleMs - elapsedMs);
    }
  };

  const flushSliderCommit = () => {
    if (!onSliderCommit || commitPolicy === "formOnly") {
      return;
    }

    clearCommitTimer();

    if (latestCommitValueRef.current !== null) {
      commitSliderValue(latestCommitValueRef.current);
    }
  };

  useEffect(() => {
    return () => {
      if (commitTimerRef.current) {
        clearTimeout(commitTimerRef.current);
      }
    };
  }, []);

  return (
    <div className={styles.field}>
      <Input
        id={id}
        type="number"
        value={inputValue}
        min={min}
        max={max}
        step={step}
        disabled={disabled}
        placeholder={placeholder}
        onChange={(event) => onInputChange(event.target.value)}
      />

      <input
        className={styles.range}
        type="range"
        min={min}
        max={max}
        step={step}
        value={sliderValue}
        disabled={disabled}
        onChange={(event) => {
          const nextValue = Number(event.target.value);
          if (!Number.isFinite(nextValue)) {
            return;
          }

          onSliderChange(nextValue);
          scheduleSliderCommit(nextValue);
        }}
        onBlur={flushSliderCommit}
        onKeyUp={flushSliderCommit}
        onPointerCancel={flushSliderCommit}
        onPointerUp={flushSliderCommit}
      />
    </div>
  );
}
