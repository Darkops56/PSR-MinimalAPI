import { apiFetch } from './api.js';

export class OrderTracker {
  constructor() {
    this.trackerContainer = document.getElementById('live-status-tracker');
    if (!this.trackerContainer) return;

    this.orderId = parseInt(this.trackerContainer.dataset.orderId || '0', 10);
    this.pollingInterval = null;
    this.currentStatus = this.trackerContainer.dataset.initialStatus || 'EnPreparacion';

    if (this.currentStatus !== 'Entregado') {
      this.startPolling();
    }
  }

  startPolling() {
    this.stopPolling();
    this.pollingInterval = setInterval(() => this.checkStatus(), 3000);
  }

  stopPolling() {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
      this.pollingInterval = null;
    }
  }

  async checkStatus() {
    try {
      // Consulta el pedido activo mediante el token JWT sin exponer IDs en la URL
      const pedido = await apiFetch('/api/pedidos/mi-pedido-activo');
      if (!pedido) return;

      if (pedido.estado !== this.currentStatus) {
        this.currentStatus = pedido.estado;
        this.updateUI(pedido.estado);
        window.showToast?.(`¡Tu pedido ahora está: ${this.formatStatus(pedido.estado)}!`, 'info');

        if (pedido.estado === 'Entregado') {
          this.stopPolling();
        }
      }
    } catch (err) {
      console.warn('Error al verificar estado del pedido:', err);
    }
  }

  updateUI(estado) {
    const stepPrep = document.getElementById('step-prep');
    const stepTravel = document.getElementById('step-travel');
    const stepDone = document.getElementById('step-done');
    const progressBar = document.getElementById('tracker-progress-bar');
    const statusText = document.getElementById('active-order-status-text');

    if (statusText) statusText.textContent = this.formatStatus(estado);

    // Resetear clases
    [stepPrep, stepTravel, stepDone].forEach(s => s?.classList.remove('active', 'completed'));

    if (estado === 'EnPreparacion') {
      stepPrep?.classList.add('active');
      if (progressBar) progressBar.style.width = '20%';
    } else if (estado === 'EnViaje') {
      stepPrep?.classList.add('completed');
      stepTravel?.classList.add('active');
      if (progressBar) progressBar.style.width = '60%';
    } else if (estado === 'Entregado') {
      stepPrep?.classList.add('completed');
      stepTravel?.classList.add('completed');
      stepDone?.classList.add('completed', 'active');
      if (progressBar) progressBar.style.width = '100%';
    }
  }

  formatStatus(estado) {
    switch (estado) {
      case 'EnPreparacion': return '🔥 En Preparación';
      case 'EnViaje': return '🛵 En Camino';
      case 'Entregado': return '✅ Entregado';
      default: return estado;
    }
  }
}
