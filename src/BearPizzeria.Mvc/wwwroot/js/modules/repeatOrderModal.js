export function showRepeatOrderStockModal(response) {
  const modalEl = document.getElementById('repeat-order-stock-modal');
  if (!modalEl) return;

  const msgEl = document.getElementById('repeat-stock-modal-message');
  const tbody = document.getElementById('repeat-stock-table-body');

  if (msgEl) {
    msgEl.textContent = response.mensaje || 'Algunas pizzas de tu pedido anterior no tenían suficiente stock.';
  }

  if (tbody && Array.isArray(response.itemsSinStockSuficiente)) {
    tbody.innerHTML = response.itemsSinStockSuficiente.map(item => `
      <tr class="border-bottom border-secondary border-opacity-10">
        <td>
          <div class="fw-bold text-light">${escapeHtml(item.pizzaNombre)}</div>
          <span class="badge bg-secondary bg-opacity-25 text-warning border border-secondary border-opacity-25 px-2 py-0 small">${escapeHtml(item.tamano)}</span>
        </td>
        <td class="text-center fw-bold text-light">${item.cantidadSolicitada}</td>
        <td class="text-center text-warning fw-bold">${item.cantidadDisponible}</td>
        <td class="text-end fw-bold text-success">${item.cantidadAgregada}</td>
      </tr>
    `).join('');
  }

  modalEl.classList.add('active');

  const closeBtns = modalEl.querySelectorAll('[data-close-repeat-modal]');
  closeBtns.forEach(btn => {
    btn.onclick = () => {
      modalEl.classList.remove('active');
    };
  });
}

function escapeHtml(text) {
  if (!text) return '';
  return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#039;");
}
