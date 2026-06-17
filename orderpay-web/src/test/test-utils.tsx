import React, { PropsWithChildren, ReactElement } from "react";
import { render, RenderOptions } from "@testing-library/react";
import { ThemeProvider } from "styled-components";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { Provider as ReduxProvider } from "react-redux";
import { configureStore } from "@reduxjs/toolkit";
import theme from "@/styles/theme";
import uiReducer from "@/store/uiSlice";
import cartReducer from "@/store/cartSlice";

function makeStore() {
  return configureStore({
    reducer: { ui: uiReducer, cart: cartReducer },
  });
}

function makeQueryClient() {
  // retry off + silent logging → deterministic, fast tests
  return new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
}

export function renderWithProviders(ui: ReactElement, options?: RenderOptions) {
  const store = makeStore();
  const queryClient = makeQueryClient();

  function Wrapper({ children }: PropsWithChildren) {
    return (
      <ReduxProvider store={store}>
        <QueryClientProvider client={queryClient}>
          <ThemeProvider theme={theme}>{children}</ThemeProvider>
        </QueryClientProvider>
      </ReduxProvider>
    );
  }

  return { store, queryClient, ...render(ui, { wrapper: Wrapper, ...options }) };
}

export * from "@testing-library/react";
