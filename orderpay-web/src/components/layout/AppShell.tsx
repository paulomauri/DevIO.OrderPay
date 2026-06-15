"use client";

import styled from "styled-components";
import Sidebar from "./Sidebar";
import Header from "./Header";
import ModalManager from "@/components/ModalManager";

const Shell = styled.div`
  display:    flex;
  min-height: 100vh;
  background: ${({ theme }) => theme.colors.background};
`;

const RightPane = styled.div`
  flex:            1;
  display:         flex;
  flex-direction:  column;
  min-width:       0; /* prevents flex overflow */
  overflow:        hidden;
`;

const Content = styled.main`
  flex:       1;
  padding:    ${({ theme }) => theme.spacing.xl};
  overflow-y: auto;
`;

export default function AppShell({ children }: { children: React.ReactNode }) {
  return (
    <Shell>
      <Sidebar />
      <RightPane>
        <Header />
        <Content>{children}</Content>
      </RightPane>
      <ModalManager />
    </Shell>
  );
}
