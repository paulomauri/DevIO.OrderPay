"use client";

import styled from "styled-components";
import { forwardRef } from "react";

export interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  label?: string;
  error?: string;
}

const Wrapper = styled.div`
  display:        flex;
  flex-direction: column;
  gap:            4px;
`;

const Label = styled.label`
  font-size:   ${({ theme }) => theme.typography.fontSize.sm};
  font-weight: ${({ theme }) => theme.typography.fontWeight.medium};
  color:       ${({ theme }) => theme.colors.text};
`;

const StyledSelect = styled.select<{ $error: boolean }>`
  padding:       10px 12px;
  border:        1px solid ${({ theme, $error }) => ($error ? theme.colors.error : theme.colors.border)};
  border-radius: ${({ theme }) => theme.borderRadius.md};
  font-size:     ${({ theme }) => theme.typography.fontSize.sm};
  font-family:   ${({ theme }) => theme.typography.fontFamily};
  color:         ${({ theme }) => theme.colors.text};
  background:    ${({ theme }) => theme.colors.surface};
  outline:       none;
  width:         100%;
  cursor:        pointer;
  transition:    border-color 0.15s, box-shadow 0.15s;
  appearance:    none;
  background-image: url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='12' height='8' viewBox='0 0 12 8'%3E%3Cpath fill='%236b7280' d='M1 1l5 5 5-5'/%3E%3C/svg%3E");
  background-repeat:   no-repeat;
  background-position: right 12px center;
  padding-right:  36px;

  &:focus {
    border-color: ${({ theme, $error }) => ($error ? theme.colors.error : theme.colors.primary)};
    box-shadow:   0 0 0 3px
      ${({ $error }) => ($error ? "rgba(220,38,38,0.12)" : "rgba(37,99,235,0.12)")};
  }

  &:disabled {
    opacity:    0.6;
    cursor:     not-allowed;
    background-color: ${({ theme }) => theme.colors.background};
  }
`;

const ErrorText = styled.span`
  font-size: ${({ theme }) => theme.typography.fontSize.xs};
  color:     ${({ theme }) => theme.colors.error};
`;

const Select = forwardRef<HTMLSelectElement, SelectProps>(
  ({ label, error, id, children, ...props }, ref) => {
    const selectId = id ?? label?.toLowerCase().replace(/\s+/g, "-");
    return (
      <Wrapper>
        {label && <Label htmlFor={selectId}>{label}</Label>}
        <StyledSelect ref={ref} id={selectId} $error={!!error} {...props}>
          {children}
        </StyledSelect>
        {error && <ErrorText role="alert">{error}</ErrorText>}
      </Wrapper>
    );
  }
);
Select.displayName = "Select";

export default Select;
