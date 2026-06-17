"use client";

import { useQuery } from "@tanstack/react-query";
import styled from "styled-components";
import { useAppDispatch } from "@/store";
import { openModal } from "@/store/uiSlice";
import { Card, CardHeader, CardTitle } from "@/components/ui/Card";
import { TableWrapper, Table, Thead, Tbody, Tr, Th, Td } from "@/components/ui/Table";
import Button from "@/components/ui/Button";
import AdminOnly from "@/components/ui/AdminOnly";
import TableSkeleton from "@/components/ui/TableSkeleton";
import EmptyState from "@/components/ui/EmptyState";
import { productsService } from "@/services/products";

const ActionsCell = styled(Td)`
  display:     flex;
  gap:         ${({ theme }) => theme.spacing.xs};
  align-items: center;
`;

const Description = styled.span`
  display:       block;
  max-width:     280px;
  overflow:      hidden;
  white-space:   nowrap;
  text-overflow: ellipsis;
`;

export default function ProductsPage() {
  const dispatch = useAppDispatch();
  const { data: products, isLoading, isError } = useQuery({
    queryKey: ["products"],
    queryFn:  productsService.getAll,
  });

  const isEmpty = !isLoading && !isError && (!products || products.length === 0);

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

      {isError  && <EmptyState message="Failed to load products." icon="⚠️" />}
      {isEmpty  && <EmptyState message="No products found." icon="📦" />}

      {!isEmpty && (
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
              {isLoading ? (
                <TableSkeleton cols={4} />
              ) : (
                products!.map((p) => (
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
                          onClick={() => dispatch(openModal({ modal: "confirmDelete", payload: "product:" + p.id }))}
                        >
                          Delete
                        </Button>
                      </ActionsCell>
                    </AdminOnly>
                  </Tr>
                ))
              )}
            </Tbody>
          </Table>
        </TableWrapper>
      )}
    </Card>
  );
}
