"use client";

import styled from "styled-components";

export const TableWrapper = styled.div`
  width:      100%;
  overflow-x: auto;
`;

export const Table = styled.table`
  width:           100%;
  border-collapse: collapse;
  font-size:       ${({ theme }) => theme.typography.fontSize.sm};
`;

export const Thead = styled.thead`
  background: ${({ theme }) => theme.colors.background};
`;

export const Tbody = styled.tbody``;

export const Tr = styled.tr`
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
  transition:    background-color 0.1s;

  &:last-child { border-bottom: none; }
  &:hover      { background: ${({ theme }) => theme.colors.background}; }
`;

export const Th = styled.th`
  padding:     ${({ theme }) => `${theme.spacing.sm} ${theme.spacing.md}`};
  text-align:  left;
  font-weight: ${({ theme }) => theme.typography.fontWeight.semibold};
  color:       ${({ theme }) => theme.colors.textMuted};
  white-space: nowrap;
`;

export const Td = styled.td`
  padding: ${({ theme }) => `${theme.spacing.sm} ${theme.spacing.md}`};
  color:   ${({ theme }) => theme.colors.text};
`;
