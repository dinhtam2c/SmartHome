import { useCallback, useState } from "react";
import type { DrawingState, EditorMode, Point } from "../types/floorTypes";

const EMPTY_DRAWING_STATE: DrawingState = {
  points: [],
  mousePos: null,
};

export function useFloorEditor() {
  const [mode, setMode] = useState<EditorMode>("view");
  const [drawing, setDrawing] = useState<DrawingState>(EMPTY_DRAWING_STATE);
  const [selectedRoomId, setSelectedRoomId] = useState<string | null>(null);
  const [selectedPlacementId, setSelectedPlacementId] = useState<string | null>(null);
  const [pendingPlacementDeviceId, setPendingPlacementDeviceId] = useState<string | null>(null);

  const clearSelection = useCallback(() => {
    setSelectedRoomId(null);
    setSelectedPlacementId(null);
  }, []);

  const enterViewMode = useCallback(() => {
    setMode("view");
    setDrawing(EMPTY_DRAWING_STATE);
    setPendingPlacementDeviceId(null);
    clearSelection();
  }, [clearSelection]);

  const enterPlaceDeviceMode = useCallback(() => {
    setMode("place-device");
    setDrawing(EMPTY_DRAWING_STATE);
  }, []);

  const startDrawingRoom = useCallback(() => {
    setMode("draw-room");
    setDrawing(EMPTY_DRAWING_STATE);
    setPendingPlacementDeviceId(null);
    clearSelection();
  }, [clearSelection]);

  const cancelDrawing = useCallback((nextMode: EditorMode = "place-device") => {
    setMode(nextMode);
    setDrawing(EMPTY_DRAWING_STATE);
    setPendingPlacementDeviceId(null);
  }, []);

  const addDrawingPoint = useCallback((point: Point) => {
    setDrawing((current) => ({
      ...current,
      points: [...current.points, point],
    }));
  }, []);

  const removeLastDrawingPoint = useCallback(() => {
    setDrawing((current) => ({
      ...current,
      points: current.points.slice(0, -1),
    }));
  }, []);

  const updateMousePos = useCallback((point: Point | null) => {
    setDrawing((current) => ({
      ...current,
      mousePos: point,
    }));
  }, []);

  const finishDrawing = useCallback((nextMode: EditorMode = "draw-room") => {
    if (drawing.points.length < 3) {
      setMode(nextMode);
      setDrawing(EMPTY_DRAWING_STATE);
      return null;
    }

    const polygon = [...drawing.points];
    setMode(nextMode);
    setDrawing(EMPTY_DRAWING_STATE);
    return polygon;
  }, [drawing.points]);

  const selectRoom = useCallback((roomId: string | null) => {
    setSelectedRoomId(roomId);
    setSelectedPlacementId(null);
  }, []);

  const selectPlacement = useCallback((placementId: string | null) => {
    setSelectedPlacementId(placementId);
    setSelectedRoomId(null);
  }, []);

  return {
    mode,
    drawing,
    selectedRoomId,
    selectedPlacementId,
    pendingPlacementDeviceId,
    isEditMode: mode !== "view",
    addDrawingPoint,
    cancelDrawing,
    clearSelection,
    enterPlaceDeviceMode,
    enterViewMode,
    finishDrawing,
    removeLastDrawingPoint,
    selectPlacement,
    setPendingPlacementDeviceId,
    selectRoom,
    startDrawingRoom,
    updateMousePos,
  };
}
