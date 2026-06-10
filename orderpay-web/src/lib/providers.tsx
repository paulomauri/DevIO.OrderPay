"use client";

import { useState } from "react";
import { Provider as ReduxProvider } from "react-redux";
import { SessionProvider } from "next-auth/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "styled-components";
import { store } from "@/store";
import theme from "@/styles/theme";
import GlobalStyle from "@/styles/GlobalStyle";
import StyledComponentsRegistry from "./registry";

export default function Providers({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(() => new QueryClient({
    defaultOptions: {
      queries: {
        staleTime: 30 * 1000,
        retry: 1,
      },
    },
  }));

  return (
    <StyledComponentsRegistry>
      <SessionProvider>
        <ReduxProvider store={store}>
          <QueryClientProvider client={queryClient}>
            <ThemeProvider theme={theme}>
              <GlobalStyle />
              {children}
            </ThemeProvider>
          </QueryClientProvider>
        </ReduxProvider>
      </SessionProvider>
    </StyledComponentsRegistry>
  );
}
