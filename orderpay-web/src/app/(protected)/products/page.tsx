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
import { productsService } from "@/services/products";

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

const Description = styled.span`
  display:     block;
  max-width:   280px;
  overflow:    hidden;
  white-space: nowrap;
  text-overflow: ellipsis;
`;

export default function ProductsPage() {
  const dispatch = useAppDispatch();
  const { data: products, isLoading } = useQuery({
    queryKey: ["products"],
    queryFn:  productsService.getAll,
  });

  return (
    <Card>
      <CardHeader>
        <CardTitle>Products</CardTitle>
        <AdminOnly>
          <Button size="sm" onClick={() => dispatch(openModal({ modal: "createProduct" }))}>
            + New Product
          </Button>
        </AdminOnly>
      </CardHeader>

      {isLoading && (
        <Center><Spinner /></Center>
      )}

      {!isLoading && (!products || products.length === 0) && (
        <Empty>No products found.</Empty>
      )}

      {!isLoading && products && products.length > 0 && (
        <TableWrapper>
          <Table>
            <Thead>
              <Tr>
                <Th>Name</Th>
                <Th>SKU</Th>
                <Th>Description</Th>
                <AdminOnly><Th>Actions</Th></AdminOnly>
              </Tr>
            </Thead>
            <Tbody>
              {products.map((p) => (
                <Tr key={p.id}>
                  <Td>{p.name}</Td>
                  <Td>{p.sku}</Td>
                  <Td><Description title={p.description}>{p.description}</Description></Td>
                  <AdminOnly>
                    <ActionsCell>
                      <Button
                        size="sm"
                        variant="ghost"
                        onClick={() => dispatch(openModal({ modal: "editProduct", payload: p.id }))}
                      >
                        Edit
                      </Button>
                      <Button
                        size="sm"
                        variant="danger"
                        onClick={() => dispatch(openModal({ modal: "confirmDelete", payload: p.id }))}
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
