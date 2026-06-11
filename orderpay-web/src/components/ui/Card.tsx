"use client";

import styled from "styled-components";

export const Card = styled.div`
  background:    ${({ theme }) => theme.colors.surface};
  border:        1px solid ${({ theme }) => theme.colors.border};
  border-radius: ${({ theme }) => theme.borderRadius.lg};
  box-shadow:    ${({ theme }) => theme.shadows.sm};
  padding:       ${({ theme }) => theme.spacing.lg};
`;

export const CardHeader = styled.div`
  display:         flex;
  align-items:     center;
  justify-content: space-between;
  margin-bottom:   ${({ theme }) => theme.spacing.md};
`;

export const CardTitle = styled.h3`
  font-size:   ${({ theme }) => theme.typography.fontSize.md};
  font-weight: ${({ theme }) => theme.typography.fontWeight.semibold};
  color:       ${({ theme }) => theme.colors.text};
  margin:      0;
`;
