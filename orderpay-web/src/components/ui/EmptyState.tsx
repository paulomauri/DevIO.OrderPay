"use client";

import styled from "styled-components";

const Wrapper = styled.div`
  display:         flex;
  flex-direction:  column;
  align-items:     center;
  justify-content: center;
  padding:         ${({ theme }) => theme.spacing.xxl};
  gap:             ${({ theme }) => theme.spacing.sm};
`;

const Icon = styled.span`
  font-size: 2rem;
  line-height: 1;
`;

const Text = styled.p`
  font-size:   ${({ theme }) => theme.typography.fontSize.sm};
  color:       ${({ theme }) => theme.colors.textMuted};
  margin:      0;
  text-align:  center;
`;

interface Props {
  message: string;
  icon?:   string;
}

export default function EmptyState({ message, icon = "📭" }: Props) {
  return (
    <Wrapper>
      <Icon>{icon}</Icon>
      <Text>{message}</Text>
    </Wrapper>
  );
}
