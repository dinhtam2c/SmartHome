import React from 'react';
import '../styles/components/button.css';

interface Props extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'danger';
  size?: 'sm' | 'md' | 'lg';
}

export default function Button({
  variant = 'primary',
  size = 'md',
  className = '',
  children,
  ...props
}: Props) {
  let classes = 'btn';

  if (variant !== 'primary') {
    classes += ` btn--${variant}`;
  }

  if (size !== 'md') {
    classes += ` btn--${size}`;
  }

  classes += ` ${className}`;

  return (
    <button className={classes} {...props}>
      {children}
    </button>
  );
};
