"use client";

import styled, { keyframes } from "styled-components";

const shimmer = keyframes`
  0%   { background-position: -400px 0; }
  100% { background-position:  400px 0; }
`;

const SkeletonCell = styled.td`
  padding: 14px 16px;
`;

const Bar = styled.div<{ $width?: string }>`
  height:           14px;
  width:            ${({ $width }) => $width ?? "80%"};
  border-radius:    4px;
  background:       linear-gradient(
    90deg,
    ${({ theme }) => theme.colors.background} 25%,
    ${({ theme }) => theme.colors.border}     50%,
    ${({ theme }) => theme.colors.background} 75%
  );
  background-size:  800px 100%;
  animation:        ${shimmer} 1.4s infinite linear;
`;

const widths = ["60%", "85%", "50%", "70%", "45%"];

interface Props {
  cols: number;
  rows?: number;
}

export default function TableSkeleton({ cols, rows = 5 }: Props) {
  return (
    <>
      {Array.from({ length: rows }).map((_, r) => (
        <tr key={r}>
          {Array.from({ length: cols }).map((_, c) => (
            <SkeletonCell key={c}>
              <Bar $width={widths[(r + c) % widths.length]} />
            </SkeletonCell>
          ))}
        </tr>
      ))}
    </>
  );
}
