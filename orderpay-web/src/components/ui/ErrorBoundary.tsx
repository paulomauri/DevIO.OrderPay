"use client";

import React from "react";
import styled from "styled-components";
import Button from "./Button";

const Wrapper = styled.div`
  display:         flex;
  flex-direction:  column;
  align-items:     center;
  justify-content: center;
  min-height:      60vh;
  gap:             ${({ theme }) => theme.spacing.md};
  text-align:      center;
  padding:         ${({ theme }) => theme.spacing.xxl};
`;

const Title = styled.h2`
  font-size:   ${({ theme }) => theme.typography.fontSize.xl};
  font-weight: ${({ theme }) => theme.typography.fontWeight.semibold};
  color:       ${({ theme }) => theme.colors.text};
  margin:      0;
`;

const Message = styled.p`
  font-size: ${({ theme }) => theme.typography.fontSize.sm};
  color:     ${({ theme }) => theme.colors.textMuted};
  margin:    0;
`;

interface State {
  hasError: boolean;
  message:  string;
}

export default class ErrorBoundary extends React.Component<
  { children: React.ReactNode },
  State
> {
  constructor(props: { children: React.ReactNode }) {
    super(props);
    this.state = { hasError: false, message: "" };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, message: error.message };
  }

  componentDidCatch(error: Error, info: React.ErrorInfo) {
    console.error("[ErrorBoundary]", error, info.componentStack);
  }

  reset = () => this.setState({ hasError: false, message: "" });

  render() {
    if (this.state.hasError) {
      return (
        <Wrapper>
          <Title>Something went wrong</Title>
          <Message>{this.state.message || "An unexpected error occurred."}</Message>
          <Button onClick={this.reset}>Try again</Button>
        </Wrapper>
      );
    }
    return this.props.children;
  }
}
