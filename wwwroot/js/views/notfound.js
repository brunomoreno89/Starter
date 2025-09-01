
export async function NotFound(container) {
  const card = document.createElement('div');
  card.className = 'card';
  card.innerHTML = `<h2>404</h2><p>Page not found.</p>`;
  container.appendChild(card);
}
