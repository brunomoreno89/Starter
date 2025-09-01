// wwwroot/js/utils/date.js

/** 2025-08-01T11:45:03 -> "01/08/2025" */
export function formatDateToBR(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  if (isNaN(d)) return '';
  const dd = String(d.getDate()).padStart(2, '0');
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const yyyy = d.getFullYear();
  return `${dd}/${mm}/${yyyy}`;
}

/** 2025-08-01T11:45:03 -> "01/08/2025 11:45" (opcional) */
export function formatDateTimeToBR(iso) {
  if (!iso) return '';
  const d = new Date(iso);
  if (isNaN(d)) return '';
  const hh = String(d.getHours()).padStart(2, '0');
  const mi = String(d.getMinutes()).padStart(2, '0');
  return `${formatDateToBR(iso)} ${hh}:${mi}`;
}
