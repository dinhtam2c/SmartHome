import { useEffect, useMemo, useRef, useState } from "react";

export function useCanvasScale(
  canvasWidth: number,
  canvasHeight: number,
  fitPaddingPx = 0
) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [containerSize, setContainerSize] = useState({ width: canvasWidth, height: canvasHeight });

  useEffect(() => {
    const container = containerRef.current;

    if (!container) {
      return undefined;
    }

    const observer = new ResizeObserver(([entry]) => {
      const { width, height } = entry.contentRect;
      setContainerSize({
        width: Math.max(width, 1),
        height: Math.max(height, 1),
      });
    });

    observer.observe(container);

    return () => {
      observer.disconnect();
    };
  }, []);

  const scale = useMemo(() => {
    if (canvasWidth <= 0 || canvasHeight <= 0) {
      return 1;
    }

    const availableWidth = Math.max(containerSize.width - fitPaddingPx * 2, 1);
    const availableHeight = Math.max(containerSize.height - fitPaddingPx * 2, 1);
    const scaleX = availableWidth / canvasWidth;
    const scaleY = availableHeight / canvasHeight;

    return Math.min(scaleX, scaleY, 1);
  }, [
    canvasHeight,
    canvasWidth,
    containerSize.height,
    containerSize.width,
    fitPaddingPx,
  ]);

  return {
    containerRef,
    scale,
    scaledWidth: canvasWidth * scale,
    scaledHeight: canvasHeight * scale,
  };
}
