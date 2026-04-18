export function timestampToDateTime(timestamp: number) {
  const date = new Date(timestamp < 1e12 ? timestamp * 1000 : timestamp);
  return date.toLocaleString("en-GB", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function formatDuration(totalSeconds: number | null | undefined) {
  if (
    typeof totalSeconds !== "number" ||
    !Number.isFinite(totalSeconds) ||
    totalSeconds < 0
  ) {
    return "0s";
  }

  const roundedSeconds = Math.floor(totalSeconds);
  const days = Math.floor(roundedSeconds / 86_400);
  const hours = Math.floor((roundedSeconds % 86_400) / 3_600);
  const minutes = Math.floor((roundedSeconds % 3_600) / 60);
  const seconds = roundedSeconds % 60;

  const parts: string[] = [];

  if (days > 0) {
    parts.push(`${days}d`);
  }

  if (hours > 0 || days > 0) {
    parts.push(`${hours}h`);
  }

  if (minutes > 0 || hours > 0 || days > 0) {
    parts.push(`${minutes}m`);
  }

  parts.push(`${seconds}s`);

  return parts.join(" ");
}
