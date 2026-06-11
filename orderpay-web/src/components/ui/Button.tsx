"use client";

import styled, { css } from "styled-components";
import Spinner from "./Spinner";

type Variant = "primary" | "secondary" | "danger" | "ghost";
type Size    = "sm" | "md" | "lg";

export interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?:  Variant;
  size?:     Size;
  loading?:  boolean;
  fullWidth?: boolean;
}

const sizeMap = {
  sm: css`padding: 5px 12px;  font-size: 12px;`,
  md: css`padding: 9px 16px;  font-size: 14px;`,
  lg: css`padding: 11px 20px; font-size: 16px;`,
};

const variantMap = {
  primary: css`
    background: ${({ theme }) => theme.colors.primary};
    color: #fff;
    &:hover:not(:disabled) { background: ${({ theme }) => theme.colors.primaryHover}; }
  `,
  secondary: css`
    background: transparent;
    color: ${({ theme }) => theme.colors.text};
    border: 1px solid ${({ theme }) => theme.colors.border};
    &:hover:not(:disabled) { background: ${({ theme }) => theme.colors.background}; }
  `,
  danger: css`
    background: ${({ theme }) => theme.colors.error};
    color: #fff;
    &:hover:not(:disabled) { background: #B91C1C; }
  `,
  ghost: css`
    background: transparent;
    color: ${({ theme }) => theme.colors.primary};
    &:hover:not(:disabled) { background: rgba(37, 99, 235, 0.06); }
  `,
};

const StyledButton = styled.button<{
  $variant:   Variant;
  $size:      Size;
  $fullWidth: boolean;
}>`
  display:     inline-flex;
  align-items: center;
  justify-content: center;
  gap:         6px;
  border:      none;
  border-radius: ${({ theme }) => theme.borderRadius.md};
  font-weight:   ${({ theme }) => theme.typography.fontWeight.medium};
  font-family:   ${({ theme }) => theme.typography.fontFamily};
  cursor:        pointer;
  transition:    background-color 0.15s, border-color 0.15s, opacity 0.15s;
  width:         ${({ $fullWidth }) => ($fullWidth ? "100%" : "auto")};

  ${({ $size }) => sizeMap[$size]}
  ${({ $variant }) => variantMap[$variant]}

  &:disabled { opacity: 0.5; cursor: not-allowed; }
`;

export default function Button({
  variant   = "primary",
  size      = "md",
  loading   = false,
  fullWidth = false,
  children,
  disabled,
  ...props
}: ButtonProps) {
  return (
    <StyledButton
      $variant={variant}
      $size={size}
      $fullWidth={fullWidth}
      disabled={disabled || loading}
      {...props}
    >
      {loading && <Spinner size="sm" />}
      {children}
    </StyledButton>
  );
}
