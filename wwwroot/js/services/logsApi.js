// wwwroot/js/services/logsApi.js
// Requer que api.js exporte `apiGet` (GET com Authorization) e, opcionalmente, `errorToString`.

import { apiGet /*, errorToString*/ } from '../api.js';

/**
 * Busca uma lista “leve” de usuários para combobox.
 * Tenta /api/users?light=1 e cai para /api/users.
 * Retorna [{ id, name }]
 */
export async function getUsersLight(signal) {
  const tryEndpoints = ['/api/users?light=1', '/api/users'];
  for (const url of tryEndpoints) {
    try {
      const data = await apiGet(url, signal);
      const list = Array.isArray(data) ? data : (data.items || data.Items || []);
      const mapped = list.map(u => ({
        id:   u.id ?? u.Id ?? u.userId ?? u.UserId,
        name: u.username ?? u.Username ?? u.name ?? u.Name ?? `#${u.id ?? u.Id}`
      })).filter(x => x.id != null);
      if (mapped.length) return mapped;
    } catch {
      // tenta o próximo endpoint
    }
  }
  return [];
}

/**
 * Obtém logs paginados com filtros.
 * Parâmetros:
 *  - page, size
 *  - userId (opcional)
 *  - startDate, endDate (YYYY-MM-DD) – opcionais
 *  - sort ('asc'|'desc') para data
 * Retorna: { total, items }
 */
export async function getLogs({ page = 1, size = 10, userId, startDate, endDate, sort = 'desc' } = {}, signal) {
  const p = new URLSearchParams();
  p.set('page', String(page));
  p.set('size', String(size));
  if (userId)   p.set('userId', String(userId));
  if (startDate) p.set('startDate', startDate);
  if (endDate)   p.set('endDate', endDate);
  if (sort)      p.set('sort', sort);

  return apiGet(`/api/logs?${p.toString()}`, signal);
}

/**
 * Exporta TODOS os resultados (todas as páginas) em CSV respeitando os filtros.
 * Retorna um Blob 'text/csv'.
 */
export async function exportLogsCsv({ userId, startDate, endDate, sort = 'desc' } = {}, chunkSize = 1000, signal) {
  // primeira página para descobrir total
  const first = await getLogs({ page: 1, size: chunkSize, userId, startDate, endDate, sort }, signal);
  const rows = [...(first.items || first.Items || [])];
  const total = first.total ?? first.Total ?? rows.length;

  const pages = Math.ceil(total / chunkSize);
  for (let page = 2; page <= pages; page++) {
    const data = await getLogs({ page, size: chunkSize, userId, startDate, endDate, sort }, signal);
    rows.push(...(data.items || data.Items || []));
  }

  const header = ['ExecDate', 'UserId', 'Description'];
  const escape = (v) => {
    if (v == null) return '';
    const s = String(v).replace(/"/g, '""');
    return `"${s}"`;
  };
  const toISO = (iso) => {
    try { return new Date(iso).toISOString(); } catch { return iso || ''; }
  };

  const lines = [header.join(',')];
  for (const r of rows) {
    const exec = toISO(r.execDate ?? r.ExecDate);
    const uid  = r.userId ?? r.UserId ?? '';
    const desc = r.description ?? r.Description ?? '';
    lines.push([exec, uid, desc].map(escape).join(','));
  }

  return new Blob([lines.join('\n')], { type: 'text/csv;charset=utf-8;' });
}
