"use client";

import { usePathname } from "next/navigation";
import { signOut, useSession } from "next-auth/react";
import styled from "styled-components";
import { useAppDispatch } from "@/store";
import { toggleSidebar } from "@/store/uiSlice";
import Button from "@/components/ui/Button";

// ── Page title map ────────────────────────────────────────────────────────────

const titleMap: Record<string, string> = {
  "/dashboard": "Dashboard",
  "/customers": "Customers",
  "/products":  "Products",
  "/orders":    "Orders",
};

function getTitle(pathname: string): string {
  for (const [path, title] of Object.entries(titleMap)) {
    if (pathname.startsWith(path)) return title;
  }
  return "OrderPay";
}

// ── Styled components ─────────────────────────────────────────────────────────

const Bar = styled.header`
  height:          60px;
  display:         flex;
  align-items:     center;
  justify-content: space-between;
  padding:         0 ${({ theme }) => theme.spacing.xl};
  background:      ${({ theme }) => theme.colors.surface};
  border-bottom:   1px solid ${({ theme }) => theme.colors.border};
  position:        sticky;
  top:             0;
  z-index:         10;
  flex-shrink:     0;
`;

const Left = styled.div`
  display:     flex;
  align-items: center;
  gap:         ${({ theme }) => theme.spacing.md};
`;

const MenuButton = styled.button`
  background:    none;
  border:        none;
  cursor:        pointer;
  color:         ${({ theme }) => theme.colors.textMuted};
  padding:       6px;
  border-radius: ${({ theme }) => theme.borderRadius.sm};
  display:       flex;
  align-items:   center;
  transition:    color 0.15s, background-color 0.15s;

  &:hover {
    color:      ${({ theme }) => theme.colors.text};
    background: ${({ theme }) => theme.colors.background};
  }
`;

const PageTitle = styled.h1`
  font-size:   ${({ theme }) => theme.typography.fontSize.lg};
  font-weight: ${({ theme }) => theme.typography.fontWeight.semibold};
  color:       ${({ theme }) => theme.colors.text};
  margin:      0;
`;

const Right = styled.div`
  display:     flex;
  align-items: center;
  gap:         ${({ theme }) => theme.spacing.md};
`;

const UserEmail = styled.span`
  font-size: ${({ theme }) => theme.typography.fontSize.sm};
  color:     ${({ theme }) => theme.colors.textMuted};
`;

// ── Component ─────────────────────────────────────────────────────────────────

export default function Header() {
  const dispatch    = useAppDispatch();
  const pathname    = usePathname();
  const { data: session } = useSession();

  return (
    <Bar>
      <Left>
        <MenuButton onClick={() => dispatch(toggleSidebar())} aria-label="Toggle sidebar">
          <svg width="20" height="20" viewBox="0 0 20 20" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round">
            <path d="M3 5h14M3 10h14M3 15h14" />
          </svg>
        </MenuButton>
        <PageTitle>{getTitle(pathname)}</PageTitle>
      </Left>

      <Right>
        {session?.user?.email && (
          <UserEmail>{session.user.email}</UserEmail>
        )}
        <Button variant="secondary" size="sm" onClick={() => signOut({ callbackUrl: "/login" })}>
          Sign out
        </Button>
      </Right>
    </Bar>
  );
}
