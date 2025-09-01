
export async function ForbiddenView(container) {
  const card = document.createElement('div');
  card.className = 'card';
  card.innerHTML = `<h2>403 — Forbidden</h2><p>You don't have access to this section.</p>`;
  container.appendChild(card);
}
