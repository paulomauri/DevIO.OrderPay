"use client";

import { SessionProvider } from "next-auth/react";
import { ThemeProvider } from "styled-components";
import theme from "@/styles/theme";
import GlobalStyle from "@/styles/GlobalStyle";
import StyledComponentsRegistry from "./registry";

export default function Providers({ children }: { children: React.ReactNode }) {
  return (
    <StyledComponentsRegistry>
      <SessionProvider>
        <ThemeProvider theme={theme}>
          <GlobalStyle />
          {children}
        </ThemeProvider>
      </SessionProvider>
    </StyledComponentsRegistry>
  );
}
