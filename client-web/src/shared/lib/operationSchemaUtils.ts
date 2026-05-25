export function resolveOperationKey(
  operations: Record<string, unknown> | null | undefined,
  operation: string
) {
  const normalizedOperation = operation.trim().toLowerCase();

  if (!operations || !normalizedOperation) {
    return null;
  }

  if (Object.prototype.hasOwnProperty.call(operations, operation)) {
    return operation;
  }

  return (
    Object.keys(operations).find(
      (key) => key.trim().toLowerCase() === normalizedOperation
    ) ?? null
  );
}
