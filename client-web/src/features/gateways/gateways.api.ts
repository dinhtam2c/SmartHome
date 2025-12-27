import { api } from "@/services/http";
import type {
  GatewayListElement,
  GatewayDetails,
  GatewayHomeAssignRequest,
} from "./gateways.types";

const basePath = "/gateways";

export function getGateways() {
  return api<GatewayListElement[]>(`${basePath}`);
}

export function getGatewayDetails(gatewayId: string) {
  return api<GatewayDetails>(`${basePath}/${gatewayId}`);
}

export function assignHomeToGateway(
  gatewayId: string,
  request: GatewayHomeAssignRequest
) {
  return api(`${basePath}/${gatewayId}/home`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}
