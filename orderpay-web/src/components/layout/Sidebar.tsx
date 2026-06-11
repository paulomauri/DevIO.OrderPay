"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import styled from "styled-components";
import { useAppSelector } from "@/store";
import { selectSidebarOpen } from "@/store/uiSlice";

// ── Inline SVG icons ──────────────────────────────────────────────────────────

const DashboardIcon = () => (
  <svg width="18" height="18" viewBox="0 0 18 18" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round">
    <rect x="1" y="1" width="7" height="7" rx="1" />
    <rect x="10" y="1" width="7" height="7" rx="1" />
    <rect x="1" y="10" width="7" height="7" rx="1" />
    <rect x="10" y="10" width="7" height="7" rx="1" />
  </svg>
);

const CustomersIcon = () => (
  <svg width="18" height="18" viewBox="0 0 18 18" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round">
    <circle cx="7" cy="6" r="3" />
    <path d="M1 17c0-3.3 2.7-6 6-6s6 2.7 6 6" />
    <path d="M13 3c1.7 0 3 1.3 3 3s-1.3 3-3 3" />
    <path d="M17 17c0-2.8-1.7-5.1-4-5.8" />
  </svg>
);

const ProductsIcon = () => (
  <svg width="18" height="18" viewBox="0 0 18 18" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
    <path d="M9 1L1 5v8l8 4 8-4V5L9 1z" />
    <path d="M1 5l8 4 8-4" />
    <path d="M9 9v8" />
  </svg>
);

const OrdersIcon = () => (
  <svg width="18" height="18" viewBox="0 0 18 18" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round">
    <path d="M3 4h12M3 9h12M3 14h7" />
  </svg>
);

// ── Nav items ─────────────────────────────────────────────────────────────────

const navItems = [
  { href: "/dashboard", label: "Dashboard", Icon: DashboardIcon },
  { href: "/customers", label: "Customers",  Icon: CustomersIcon },
  { href: "/products",  label: "Products",   Icon: ProductsIcon  },
  { href: "/orders",    label: "Orders",     Icon: OrdersIcon    },
];

// ── Styled components ─────────────────────────────────────────────────────────

const Nav = styled.aside<{ $open: boolean }>`
  width:       ${({ $open }) => ($open ? "240px" : "0")};
  min-width:   ${({ $open }) => ($open ? "240px" : "0")};
  overflow:    hidden;
  height:      100vh;
  position:    sticky;
  top:         0;
  background:  ${({ theme }) => theme.colors.surface};
  border-right: 1px solid ${({ theme }) => theme.colors.border};
  display:     flex;
  flex-direction: column;
  transition:  width 0.2s ease, min-width 0.2s ease;
  flex-shrink: 0;
`;

const Brand = styled.div`
  padding:     ${({ theme }) => `${theme.spacing.lg} ${theme.spacing.lg}`};
  font-size:   ${({ theme }) => theme.typography.fontSize.lg};
  font-weight: ${({ theme }) => theme.typography.fontWeight.bold};
  color:       ${({ theme }) => theme.colors.primary};
  border-bottom: 1px solid ${({ theme }) => theme.colors.border};
  white-space: nowrap;
`;

const NavList = styled.ul`
  list-style: none;
  padding: ${({ theme }) => theme.spacing.sm} 0;
  margin:  0;
  flex:    1;
`;

const NavItem = styled.li``;

const NavLink = styled(Link)<{ $active: boolean }>`
  display:     flex;
  align-items: center;
  gap:         ${({ theme }) => theme.spacing.sm};
  padding:     ${({ theme }) => `10px ${theme.spacing.lg}`};
  color:       ${({ theme, $active }) => ($active ? theme.colors.primary : theme.colors.textMuted)};
  background:  ${({ theme, $active }) => ($active ? "rgba(37,99,235,0.08)" : "transparent")};
  font-size:   ${({ theme }) => theme.typography.fontSize.sm};
  font-weight: ${({ theme, $active }) =>
    $active ? theme.typography.fontWeight.semibold : theme.typography.fontWeight.regular};
  text-decoration: none;
  white-space: nowrap;
  border-right: 3px solid
    ${({ theme, $active }) => ($active ? theme.colors.primary : "transparent")};
  transition:  all 0.15s;

  &:hover {
    color:      ${({ theme }) => theme.colors.primary};
    background: rgba(37, 99, 235, 0.05);
  }
`;

// ── Component ─────────────────────────────────────────────────────────────────

export default function Sidebar() {
  const open     = useAppSelector(selectSidebarOpen);
  const pathname = usePathname();

  return (
    <Nav $open={open} aria-label="Main navigation">
      <Brand>OrderPay</Brand>
      <NavList>
        {navItems.map(({ href, label, Icon }) => (
          <NavItem key={href}>
            <NavLink href={href} $active={pathname.startsWith(href)}>
              <Icon />
              {label}
            </NavLink>
          </NavItem>
        ))}
      </NavList>
    </Nav>
  );
}
