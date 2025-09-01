// wwwroot/js/views/home.js
export async function HomeView(container){
  const card = document.createElement('div');
  card.className = 'card';
  card.innerHTML = `
    <div class="view-header">
      <h2 class="view-title">Home</h2>
    </div>
    <div class="alert">Welcome! Choose a section from the menu to get started.</div>
  `;
  container.appendChild(card);
}
