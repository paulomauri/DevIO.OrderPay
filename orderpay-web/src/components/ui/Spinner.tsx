"use client";

import styled, { keyframes } from "styled-components";

const spin = keyframes`
  from { transform: rotate(0deg); }
  to   { transform: rotate(360deg); }
`;

const sizeMap = { sm: "14px", md: "22px", lg: "36px" };

const Ring = styled.span<{ $size: "sm" | "md" | "lg" }>`
  display: inline-block;
  width:  ${({ $size }) => sizeMap[$size]};
  height: ${({ $size }) => sizeMap[$size]};
  border: 2px solid currentColor;
  border-top-color: transparent;
  border-radius: 50%;
  animation: ${spin} 0.6s linear infinite;
  flex-shrink: 0;
`;

interface SpinnerProps {
  size?: "sm" | "md" | "lg";
}

export default function Spinner({ size = "md" }: SpinnerProps) {
  return <Ring $size={size} aria-label="Loading" />;
}
