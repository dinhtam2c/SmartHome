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
  const [selectedPlacedFloorDeviceId, setSelectedPlacedFloorDeviceId] = useState<string | null>(null);

  const clearSelection = useCallback(() => {
    setSelectedRoomId(null);
    setSelectedPlacedFloorDeviceId(null);
  }, []);

  const enterViewMode = useCallback(() => {
    setMode("view");
    setDrawing(EMPTY_DRAWING_STATE);
    clearSelection();
  }, [clearSelection]);

  const enterPlaceDeviceMode = useCallback(() => {
    setMode("place-device");
    setDrawing(EMPTY_DRAWING_STATE);
  }, []);

  const startDrawingRoom = useCallback(() => {
    setMode("draw-room");
    setDrawing(EMPTY_DRAWING_STATE);
    clearSelection();
  }, [clearSelection]);

  const cancelDrawing = useCallback((nextMode: EditorMode = "place-device") => {
    setMode(nextMode);
    setDrawing(EMPTY_DRAWING_STATE);
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
    setSelectedPlacedFloorDeviceId(null);
  }, []);

  const selectPlacedFloorDevice = useCallback((placedFloorDeviceId: string | null) => {
    setSelectedPlacedFloorDeviceId(placedFloorDeviceId);
    setSelectedRoomId(null);
  }, []);

  return {
    mode,
    drawing,
    selectedRoomId,
    selectedPlacedFloorDeviceId,
    isEditMode: mode !== "view",
    addDrawingPoint,
    cancelDrawing,
    clearSelection,
    enterPlaceDeviceMode,
    enterViewMode,
    finishDrawing,
    removeLastDrawingPoint,
    selectPlacedFloorDevice,
    selectRoom,
    setMode,
    startDrawingRoom,
    updateMousePos,
  };
}
