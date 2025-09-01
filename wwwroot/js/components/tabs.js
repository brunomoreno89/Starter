
export function createTabs(container, tabs, initialId) {
  const wrap = document.createElement('div');
  wrap.className = 'tabs';

  const list = document.createElement('div');
  list.className = 'tab-list';

  const panel = document.createElement('div');
  panel.className = 'tab-panel';

  wrap.appendChild(list);
  wrap.appendChild(panel);
  container.appendChild(wrap);

  let active = initialId || (tabs[0] && tabs[0].id);

  function renderList() {
    list.innerHTML = '';
    for (const t of tabs) {
      const btn = document.createElement('button');
      btn.className = 'tab' + (t.id === active ? ' active' : '');
      btn.textContent = t.label;
      btn.onclick = () => { active = t.id; renderList(); renderPanel(); };
      list.appendChild(btn);
    }
  }

  async function renderPanel() {
    panel.innerHTML = '';
    const t = tabs.find(x => x.id === active);
    if (!t) return;
    await t.render(panel);
  }

  renderList();
  renderPanel();
  return { setActive(id){ active=id; renderList(); renderPanel(); } };
}
