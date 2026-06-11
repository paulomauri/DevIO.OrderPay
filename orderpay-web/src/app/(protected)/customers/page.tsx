"use client";

import { useQuery } from "@tanstack/react-query";
import styled from "styled-components";
import { useAppDispatch } from "@/store";
import { openModal } from "@/store/uiSlice";
import { Card, CardHeader, CardTitle } from "@/components/ui/Card";
import { TableWrapper, Table, Thead, Tbody, Tr, Th, Td } from "@/components/ui/Table";
import Button from "@/components/ui/Button";
import AdminOnly from "@/components/ui/AdminOnly";
import Spinner from "@/components/ui/Spinner";
import { customersService } from "@/services/customers";

const Center = styled.div`
  display:         flex;
  justify-content: center;
  padding:         ${({ theme }) => theme.spacing.xxl};
`;

const Empty = styled.p`
  text-align: center;
  color:      ${({ theme }) => theme.colors.textMuted};
  padding:    ${({ theme }) => theme.spacing.xxl};
`;

const ActionsCell = styled(Td)`
  display:     flex;
  gap:         ${({ theme }) => theme.spacing.xs};
  align-items: center;
`;

export default function CustomersPage() {
  const dispatch = useAppDispatch();
  const { data: customers, isLoading } = useQuery({
    queryKey: ["customers"],
    queryFn:  customersService.getAll,
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>Customers</CardTitle>
        <AdminOnly>
          <Button size="sm" onClick={() => dispatch(openModal({ modal: "createCustomer" }))}>
            + New Customer
          </Button>
        </AdminOnly>
      </CardHeader>

      {isLoading && (
        <Center><Spinner /></Center>
      )}

      {!isLoading && (!customers || customers.length === 0) && (
        <Empty>No customers found.</Empty>
      )}

      {!isLoading && customers && customers.length > 0 && (
        <TableWrapper>
          <Table>
            <Thead>
              <Tr>
                <Th>Name</Th>
                <Th>Email</Th>
                <Th>Mobile</Th>
                <AdminOnly><Th>Actions</Th></AdminOnly>
              </Tr>
            </Thead>
            <Tbody>
              {customers.map((c) => (
                <Tr key={c.id}>
                  <Td>{c.name}</Td>
                  <Td>{c.email}</Td>
                  <Td>{c.mobile}</Td>
                  <AdminOnly>
                    <ActionsCell>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => dispatch(openModal({ modal: "editCustomer", payload: c.id }))}
                      >
                        Edit
                      </Button>
                      <Button
                        size="sm"
                        variant="danger"
                        onClick={() => dispatch(openModal({ modal: "confirmDelete", payload: c.id }))}
                      >
                        Delete
                      </Button>
                    </ActionsCell>
                  </AdminOnly>
                </Tr>
              ))}
            </Tbody>
          </Table>
        </TableWrapper>
      )}
    </Card>
  );
}
