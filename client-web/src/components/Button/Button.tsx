import React from "react";
import styles from "./Button.module.css";

interface Props extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "primary" | "secondary" | "danger";
  size?: "sm" | "md" | "lg";
}

export function Button({
  variant = "primary",
  size = "md",
  className,
  children,
  ...props
}: Props) {
  const classes = [
    styles.btn,
    variant !== "primary" && styles[`btn--${variant}`],
    size !== "md" && styles[`btn--${size}`],
    className,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <button className={classes} {...props}>
      {children}
    </button>
  );
}
