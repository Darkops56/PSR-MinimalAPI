export function initUI() {
  // Drawer Lateral Móvil
  const drawerBtn = document.getElementById('hamburger-toggle-btn');
  const drawer = document.getElementById('mobile-drawer');
  const drawerOverlay = document.getElementById('drawer-overlay');
  const drawerCloseBtn = document.getElementById('drawer-close-btn');

  function openDrawer() {
    drawer?.classList.add('active');
    drawerOverlay?.classList.add('active');
    document.body.style.overflow = 'hidden';
  }

  function closeDrawer() {
    drawer?.classList.remove('active');
    drawerOverlay?.classList.remove('active');
    document.body.style.overflow = '';
  }

  drawerBtn?.addEventListener('click', openDrawer);
  drawerCloseBtn?.addEventListener('click', closeDrawer);
  drawerOverlay?.addEventListener('click', closeDrawer);

  // Toast Manager
  window.showToast = function(message, type = 'info') {
    const container = document.getElementById('toast-container');
    if (!container) return;

    const toast = document.createElement('div');
    toast.className = `toast-item ${type === 'success' ? 'success' : ''}`;
    const iconClass = type === 'success' ? 'bi-check-circle-fill text-success' : type === 'error' ? 'bi-exclamation-triangle-fill text-danger' : 'bi-info-circle-fill text-warning';

    toast.innerHTML = `
      <i class="bi ${iconClass} fs-5"></i>
      <span class="small fw-semibold text-light">${message}</span>
    `;

    container.appendChild(toast);

    setTimeout(() => {
      toast.style.opacity = '0';
      toast.style.transform = 'translateX(100%)';
      setTimeout(() => toast.remove(), 300);
    }, 4000);
  };

  // Auth Guard Modal Listener
  const authModal = document.getElementById('auth-guard-modal');
  if (authModal) {
    const closeModal = () => {
      authModal.classList.remove('active');
    };

    authModal.querySelectorAll('[data-close-modal]').forEach(btn => {
      btn.addEventListener('click', closeModal);
    });

    window.addEventListener('auth:required', () => {
      authModal.classList.add('active');
    });
  }

  // Filtrado de Pizzas por Búsqueda y Categorías
  const searchInput = document.getElementById('global-search-input');
  const productCards = document.querySelectorAll('.product-card');

  let debounceTimer;
  searchInput?.addEventListener('input', (e) => {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(() => {
      const term = e.target.value.toLowerCase().trim();
      productCards.forEach(card => {
        const title = card.querySelector('.product-title')?.textContent.toLowerCase() || '';
        const desc = card.querySelector('.product-desc')?.textContent.toLowerCase() || '';
        const match = title.includes(term) || desc.includes(term);
        card.style.display = match ? '' : 'none';
      });
    }, 300);
  });

  // Categorías Chips
  const categoryChips = document.querySelectorAll('.category-chip');
  categoryChips.forEach(chip => {
    chip.addEventListener('click', () => {
      categoryChips.forEach(c => c.classList.remove('active'));
      chip.classList.add('active');

      const cat = chip.dataset.category || 'Todas';
      productCards.forEach(card => {
        if (cat === 'Todas') {
          card.style.display = '';
        } else {
          const cardCat = card.dataset.category || '';
          card.style.display = cardCat === cat ? '' : 'none';
        }
      });
    });
  });
}
