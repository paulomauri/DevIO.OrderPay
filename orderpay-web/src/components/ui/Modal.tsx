"use client";

import { useEffect, useState } from "react";
import { createPortal } from "react-dom";
import styled, { keyframes } from "styled-components";

const fadeIn = keyframes`
  from { opacity: 0; }
  to   { opacity: 1; }
`;

const slideIn = keyframes`
  from { transform: translateY(-12px); opacity: 0; }
  to   { transform: translateY(0);     opacity: 1; }
`;

const Overlay = styled.div`
  position:        fixed;
  inset:           0;
  background:      rgba(0, 0, 0, 0.45);
  display:         flex;
  align-items:     center;
  justify-content: center;
  z-index:         1000;
  animation:       ${fadeIn} 0.15s ease;
  padding:         ${({ theme }) => theme.spacing.md};
`;

const Panel = styled.div<{ $maxWidth: string }>`
  background:    ${({ theme }) => theme.colors.surface};
  border-radius: ${({ theme }) => theme.borderRadius.lg};
  box-shadow:    ${({ theme }) => theme.shadows.lg};
  width:         100%;
  max-width:     ${({ $maxWidth }) => $maxWidth};
  max-height:    90vh;
  overflow-y:    auto;
  animation:     ${slideIn} 0.15s ease;
`;

const ModalHeader = styled.div`
  display:         flex;
  align-items:     center;
  justify-content: space-between;
  padding:         ${({ theme }) => `${theme.spacing.lg} ${theme.spacing.xl}`};
  border-bottom:   1px solid ${({ theme }) => theme.colors.border};
`;

const ModalTitle = styled.h2`
  font-size:   ${({ theme }) => theme.typography.fontSize.lg};
  font-weight: ${({ theme }) => theme.typography.fontWeight.semibold};
  color:       ${({ theme }) => theme.colors.text};
  margin:      0;
`;

const CloseButton = styled.button`
  background:    none;
  border:        none;
  cursor:        pointer;
  color:         ${({ theme }) => theme.colors.textMuted};
  font-size:     20px;
  line-height:   1;
  padding:       4px 6px;
  border-radius: ${({ theme }) => theme.borderRadius.sm};
  transition:    color 0.15s, background-color 0.15s;

  &:hover {
    color:      ${({ theme }) => theme.colors.text};
    background: ${({ theme }) => theme.colors.background};
  }
`;

export const ModalBody = styled.div`
  padding: ${({ theme }) => theme.spacing.xl};
  display: flex;
  flex-direction: column;
  gap: ${({ theme }) => theme.spacing.md};
`;

export const ModalFooter = styled.div`
  display:         flex;
  justify-content: flex-end;
  gap:             ${({ theme }) => theme.spacing.sm};
  padding:         ${({ theme }) => `${theme.spacing.md} ${theme.spacing.xl}`};
  border-top:      1px solid ${({ theme }) => theme.colors.border};
`;

interface ModalProps {
  isOpen:    boolean;
  onClose:   () => void;
  title:     string;
  children:  React.ReactNode;
  maxWidth?: string;
}

export default function Modal({
  isOpen,
  onClose,
  title,
  children,
  maxWidth = "520px",
}: ModalProps) {
  const [mounted, setMounted] = useState(false);

  useEffect(() => { setMounted(true); }, []);

  // Close on Escape
  useEffect(() => {
    if (!isOpen) return;
    const handler = (e: KeyboardEvent) => { if (e.key === "Escape") onClose(); };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [isOpen, onClose]);

  if (!mounted || !isOpen) return null;

  return createPortal(
    <Overlay onClick={(e) => { if (e.target === e.currentTarget) onClose(); }}>
      <Panel $maxWidth={maxWidth} role="dialog" aria-modal aria-labelledby="modal-title">
        <ModalHeader>
          <ModalTitle id="modal-title">{title}</ModalTitle>
          <CloseButton onClick={onClose} aria-label="Close modal">✕</CloseButton>
        </ModalHeader>
        {children}
      </Panel>
    </Overlay>,
    document.body
  );
}
