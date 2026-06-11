"use client";

import styled from "styled-components";
import { forwardRef } from "react";

export interface InputProps extends React.InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
}

const Wrapper = styled.div`
  display: flex;
  flex-direction: column;
  gap: 4px;
`;

const Label = styled.label`
  font-size:   ${({ theme }) => theme.typography.fontSize.sm};
  font-weight: ${({ theme }) => theme.typography.fontWeight.medium};
  color:       ${({ theme }) => theme.colors.text};
`;

const StyledInput = styled.input<{ $error: boolean }>`
  padding:       10px 12px;
  border:        1px solid ${({ theme, $error }) => ($error ? theme.colors.error : theme.colors.border)};
  border-radius: ${({ theme }) => theme.borderRadius.md};
  font-size:     ${({ theme }) => theme.typography.fontSize.sm};
  font-family:   ${({ theme }) => theme.typography.fontFamily};
  color:         ${({ theme }) => theme.colors.text};
  background:    ${({ theme }) => theme.colors.surface};
  outline:       none;
  width:         100%;
  transition:    border-color 0.15s, box-shadow 0.15s;

  &:focus {
    border-color: ${({ theme, $error }) => ($error ? theme.colors.error : theme.colors.primary)};
    box-shadow:   0 0 0 3px
      ${({ $error }) => ($error ? "rgba(220,38,38,0.12)" : "rgba(37,99,235,0.12)")};
  }

  &::placeholder { color: ${({ theme }) => theme.colors.textMuted}; }

  &:disabled {
    opacity:    0.6;
    cursor:     not-allowed;
    background: ${({ theme }) => theme.colors.background};
  }
`;

const ErrorText = styled.span`
  font-size: ${({ theme }) => theme.typography.fontSize.xs};
  color:     ${({ theme }) => theme.colors.error};
`;

const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, id, ...props }, ref) => {
    const inputId = id ?? label?.toLowerCase().replace(/\s+/g, "-");
    return (
      <Wrapper>
        {label && <Label htmlFor={inputId}>{label}</Label>}
        <StyledInput ref={ref} id={inputId} $error={!!error} {...props} />
        {error && <ErrorText role="alert">{error}</ErrorText>}
      </Wrapper>
    );
  }
);
Input.displayName = "Input";

export default Input;
