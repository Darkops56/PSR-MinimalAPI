import { AddressManager } from './addresses.js';

export function initAddressSidebar() {
  const sidebarEl = document.getElementById('address-sidebar');
  if (!sidebarEl) return;

  const container = document.getElementById('address-list-container');
  const btnToggleForm = document.getElementById('btn-toggle-add-address-form');
  const formEl = document.getElementById('add-address-form');
  const btnCancelForm = document.getElementById('btn-cancel-add-address');

  async function renderAddresses() {
    if (!container) return;
    container.innerHTML = '<div class="text-center text-muted py-3"><span class="spinner-border spinner-border-sm me-2"></span>Cargando...</div>';

    const addresses = await AddressManager.getAddresses();
    if (!addresses || addresses.length === 0) {
      container.innerHTML = '<div class="text-muted small text-center py-2">No tenés direcciones guardadas. ¡Agregá una nueva abajo!</div>';
      updateActiveDisplay(null);
      return;
    }

    const activeAddress = AddressManager.getActiveAddress();

    container.innerHTML = addresses.map(addr => {
      const isSelected = activeAddress && activeAddress.id === addr.id;
      return `
        <div class="card card-dark p-3 border ${isSelected ? 'border-warning' : 'border-secondary border-opacity-25'} rounded-3 address-item-card" data-address-id="${addr.id}">
          <div class="d-flex align-items-start justify-content-between gap-2">
            <div class="form-check flex-grow-1">
              <input class="form-check-input select-address-radio" type="radio" name="deliveryAddressRadio" id="addr-radio-${addr.id}" value="${addr.id}" ${isSelected ? 'checked' : ''} />
              <label class="form-check-label w-100 cursor-pointer" for="addr-radio-${addr.id}">
                <div class="d-flex align-items-center gap-2">
                  <span class="fw-bold text-light">${escapeHtml(addr.nombre)}</span>
                  ${addr.esPrincipal ? '<span class="badge bg-warning text-dark small py-0 px-2 fw-semibold">Principal</span>' : ''}
                </div>
                <div class="text-muted small mt-1">${escapeHtml(addr.direccionCompleta)}</div>
                ${addr.notas ? `<div class="text-warning small fst-italic mt-1"><i class="bi bi-info-circle me-1"></i>${escapeHtml(addr.notas)}</div>` : ''}
              </label>
            </div>
            <div class="d-flex align-items-center gap-1">
              ${!addr.esPrincipal ? `<button type="button" class="btn btn-sm text-warning p-1 btn-set-principal-addr" data-id="${addr.id}" title="Marcar como principal"><i class="bi bi-star"></i></button>` : ''}
              <button type="button" class="btn btn-sm text-danger p-1 btn-delete-addr" data-id="${addr.id}" title="Eliminar dirección"><i class="bi bi-trash"></i></button>
            </div>
          </div>
        </div>
      `;
    }).join('');

    // Listeners para radios y botones
    container.querySelectorAll('.select-address-radio').forEach(radio => {
      radio.addEventListener('change', () => {
        const id = parseInt(radio.value, 10);
        AddressManager.setActiveAddress(id);
        renderAddresses();
        updateActiveDisplay(AddressManager.getActiveAddress());
      });
    });

    container.querySelectorAll('.btn-set-principal-addr').forEach(btn => {
      btn.addEventListener('click', async (e) => {
        e.stopPropagation();
        const id = parseInt(btn.dataset.id, 10);
        await AddressManager.setPrincipal(id);
        await renderAddresses();
        updateActiveDisplay(AddressManager.getActiveAddress());
        window.showToast?.('Dirección principal actualizada', 'success');
      });
    });

    container.querySelectorAll('.btn-delete-addr').forEach(btn => {
      btn.addEventListener('click', async (e) => {
        e.stopPropagation();
        if (confirm('¿Eliminar esta dirección de entrega?')) {
          const id = parseInt(btn.dataset.id, 10);
          await AddressManager.deleteAddress(id);
          await renderAddresses();
          updateActiveDisplay(AddressManager.getActiveAddress());
          window.showToast?.('Dirección eliminada', 'info');
        }
      });
    });

    updateActiveDisplay(activeAddress);
  }

  function updateActiveDisplay(address) {
    const displayLabelEl = document.getElementById('cart-active-address-label');
    const displayFullEl = document.getElementById('cart-active-address-full');
    const hiddenInput = document.getElementById('selected-address-id-input');

    if (displayLabelEl && displayFullEl) {
      if (address) {
        displayLabelEl.textContent = address.nombre;
        displayFullEl.textContent = address.direccionCompleta;
        if (hiddenInput) hiddenInput.value = address.id;
      } else {
        displayLabelEl.textContent = 'Sin dirección seleccionada';
        displayFullEl.textContent = 'Hacé clic en Cambiar para agregar una dirección.';
        if (hiddenInput) hiddenInput.value = '';
      }
    }
  }

  function escapeHtml(text) {
    if (!text) return '';
    return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#039;");
  }

  // Event listener cuando se abre el sidebar
  sidebarEl.addEventListener('show.bs.offcanvas', () => {
    renderAddresses();
  });

  // Toggle formulario
  btnToggleForm?.addEventListener('click', () => {
    formEl?.classList.toggle('d-none');
  });

  btnCancelForm?.addEventListener('click', () => {
    formEl?.classList.add('d-none');
    formEl?.reset();
  });

  // Envío del formulario de agregar dirección
  formEl?.addEventListener('submit', async (e) => {
    e.preventDefault();
    const label = document.getElementById('address-label-input')?.value.trim();
    const full = document.getElementById('address-full-input')?.value.trim();
    const notes = document.getElementById('address-notes-input')?.value.trim();
    const isPrincipal = document.getElementById('address-principal-check')?.checked || false;

    if (!label || !full) {
      window.showToast?.('Por favor completá el nombre y la dirección', 'error');
      return;
    }

    try {
      const created = await AddressManager.addAddress(label, full, notes, isPrincipal);
      if (created) {
        window.showToast?.(`Dirección "${label}" guardada exitosamente`, 'success');
        formEl.reset();
        formEl.classList.add('d-none');
        await renderAddresses();
      }
    } catch (err) {
      window.showToast?.(err.message || 'Error al guardar la dirección', 'error');
    }
  });

  // Carga inicial de dirección activa
  AddressManager.getAddresses().then(addrs => {
    updateActiveDisplay(AddressManager.getActiveAddress());
  });
}
