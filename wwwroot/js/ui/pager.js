// wwwroot/js/ui/pager.js
export function getPageSize(){
  const raw = localStorage.getItem('pageSize');
  const n = parseInt(raw, 10);
  if (Number.isFinite(n) && n >= 5 && n <= 500) return n;
  return 10; // default
}

export function paginate(arr, page, pageSize = getPageSize()){
  const total = Array.isArray(arr) ? arr.length : 0;
  const pages = Math.max(1, Math.ceil(total / pageSize));
  const p = Math.min(Math.max(1, page || 1), pages);
  const start = (p - 1) * pageSize;
  const items = (arr || []).slice(start, start + pageSize);
  return { items, total, page: p, pages, pageSize };
}

export function renderPager(host, { total, page, pages }, onChange){
  if (!host) return;
  host.innerHTML = `
    <div class="pager" style="display:flex; align-items:center; justify-content:flex-end; gap:8px; margin-top:8px;">
      <span class="muted small">${total} item(s)</span>
      <button class="secondary" data-act="prev" ${page<=1?'disabled':''}>‹</button>
      <span class="muted small">Page ${page} / ${pages}</span>
      <button class="secondary" data-act="next" ${page>=pages?'disabled':''}>›</button>
    </div>
  `;
  host.querySelector('[data-act="prev"]')?.addEventListener('click', ()=> onChange?.(Math.max(1, page-1)));
  host.querySelector('[data-act="next"]')?.addEventListener('click', ()=> onChange?.(Math.min(pages, page+1)));
}
