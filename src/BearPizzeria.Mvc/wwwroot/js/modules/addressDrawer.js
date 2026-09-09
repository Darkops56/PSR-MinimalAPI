import { AddressManager } from './addresses.js';

export function initAddressSidebar() {
  const sidebarEl = document.getElementById('address-sidebar');
  const profileListEl = document.getElementById('profile-address-list');

  // Si no hay sidebar ni lista de perfil, salir
  if (!sidebarEl && !profileListEl) return;

  const container = document.getElementById('address-list-container');
  const btnToggleForm = document.getElementById('btn-toggle-add-address-form');
  const formEl = document.getElementById('add-address-form');
  const btnCancelForm = document.getElementById('btn-cancel-add-address');

  async function renderAddresses() {
    const addresses = await AddressManager.getAddresses();

    // 1. Renderizar en el Sidebar / Drawer (si existe en la página)
    if (container) {
      if (!addresses || addresses.length === 0) {
        container.innerHTML = '<div class="alert alert-warning py-2 px-3 small mb-0"><i class="bi bi-info-circle me-1"></i>No tenés direcciones guardadas. ¡Completá el formulario abajo para agregar una!</div>';
        formEl?.classList.remove('d-none');
        updateActiveDisplay(null);
      } else {
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

        // Listeners para radios y botones en el sidebar
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
    }

    // 2. Renderizar en la sección de Perfil (si existe)
    renderProfileAddresses(addresses);
  }

  function renderProfileAddresses(addresses) {
    const pList = document.getElementById('profile-address-list');
    if (!pList) return;

    if (!addresses || addresses.length === 0) {
      pList.innerHTML = `
        <div class="col-12">
          <div class="p-4 text-center border border-secondary border-opacity-25 rounded-3 bg-dark">
            <div class="fs-1 text-warning mb-2"><i class="bi bi-geo-alt"></i></div>
            <h6 class="text-light font-display mb-1">Aún no registraste ninguna dirección</h6>
            <p class="text-muted small mb-3">Agregá tus direcciones de entrega (Casa, Trabajo, etc.) para que estén listas al hacer un pedido.</p>
            <button type="button" class="btn btn-warning btn-sm fw-bold px-3 py-2" data-bs-toggle="offcanvas" data-bs-target="#address-sidebar">
              <i class="bi bi-plus-circle me-1"></i>Agregar mi Primera Dirección
            </button>
          </div>
        </div>
      `;
      return;
    }

    pList.innerHTML = addresses.map(addr => `
      <div class="col-12 col-md-6">
        <div class="card card-dark p-3 border ${addr.esPrincipal ? 'border-warning' : 'border-secondary border-opacity-25'} rounded-3 h-100 d-flex flex-column justify-content-between">
          <div>
            <div class="d-flex align-items-center justify-content-between gap-2 mb-2">
              <div class="d-flex align-items-center gap-2">
                <span class="badge bg-warning text-dark fw-bold px-2 py-1">${escapeHtml(addr.nombre)}</span>
                ${addr.esPrincipal ? '<span class="badge bg-success bg-opacity-25 text-success border border-success px-2 py-1 small">Principal</span>' : ''}
              </div>
              <div class="d-flex align-items-center gap-1">
                ${!addr.esPrincipal ? `<button type="button" class="btn btn-sm btn-outline-warning p-1 btn-profile-set-principal" data-id="${addr.id}" title="Marcar como principal"><i class="bi bi-star"></i></button>` : ''}
                <button type="button" class="btn btn-sm btn-outline-danger p-1 btn-profile-delete" data-id="${addr.id}" title="Eliminar dirección"><i class="bi bi-trash"></i></button>
              </div>
            </div>
            <div class="text-light small mb-1 fw-semibold">
              <i class="bi bi-geo-alt text-warning me-1"></i>${escapeHtml(addr.direccionCompleta)}
            </div>
            ${addr.notas ? `<div class="text-muted small fst-italic"><i class="bi bi-card-text me-1"></i>${escapeHtml(addr.notas)}</div>` : ''}
          </div>
        </div>
      </div>
    `).join('');

    pList.querySelectorAll('.btn-profile-set-principal').forEach(btn => {
      btn.addEventListener('click', async () => {
        const id = parseInt(btn.dataset.id, 10);
        await AddressManager.setPrincipal(id);
        await renderAddresses();
        updateActiveDisplay(AddressManager.getActiveAddress());
        window.showToast?.('Dirección principal actualizada', 'success');
      });
    });

    pList.querySelectorAll('.btn-profile-delete').forEach(btn => {
      btn.addEventListener('click', async () => {
        if (confirm('¿Eliminar esta dirección de entrega?')) {
          const id = parseInt(btn.dataset.id, 10);
          await AddressManager.deleteAddress(id);
          await renderAddresses();
          updateActiveDisplay(AddressManager.getActiveAddress());
          window.showToast?.('Dirección eliminada', 'info');
        }
      });
    });
  }

  function updateActiveDisplay(address) {
    const displayLabelEl = document.getElementById('cart-active-address-label');
    const displayFullEl = document.getElementById('cart-active-address-full');
    const displayWrapper = document.getElementById('cart-active-address-wrapper');
    const hiddenInput = document.getElementById('selected-address-id-input');
    const noAddressAlert = document.getElementById('cart-no-address-alert');
    const previewHeaderDireccion = document.getElementById('preview-display-direccion');

    if (address) {
      if (displayLabelEl) {
        displayLabelEl.textContent = address.nombre;
        displayLabelEl.classList.remove('d-none');
      }
      if (displayFullEl) {
        displayFullEl.textContent = address.direccionCompleta;
        displayFullEl.classList.remove('d-none');
      }
      if (displayWrapper) displayWrapper.classList.remove('d-none');
      if (noAddressAlert) noAddressAlert.classList.add('d-none');
      if (hiddenInput) hiddenInput.value = address.id;
      if (previewHeaderDireccion) previewHeaderDireccion.textContent = `${address.nombre}: ${address.direccionCompleta}`;
    } else {
      if (displayWrapper) displayWrapper.classList.add('d-none');
      if (noAddressAlert) noAddressAlert.classList.remove('d-none');
      if (hiddenInput) hiddenInput.value = '';
      if (previewHeaderDireccion) previewHeaderDireccion.textContent = 'Sin dirección configurada';
    }
  }

  function escapeHtml(text) {
    if (!text) return '';
    return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#039;");
  }

  // Event listener cuando se abre el sidebar
  sidebarEl?.addEventListener('show.bs.offcanvas', () => {
    renderAddresses();
  });

  // Toggle formulario en el sidebar
  btnToggleForm?.addEventListener('click', () => {
    formEl?.classList.toggle('d-none');
    if (!formEl?.classList.contains('d-none')) {
      document.getElementById('address-label-input')?.focus();
    }
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
        updateActiveDisplay(AddressManager.getActiveAddress());
      }
    } catch (err) {
      window.showToast?.(err.message || 'Error al guardar la dirección', 'error');
    }
  });

  // Carga inicial
  AddressManager.getAddresses().then(addrs => {
    renderAddresses();
    updateActiveDisplay(AddressManager.getActiveAddress());
  });
}
