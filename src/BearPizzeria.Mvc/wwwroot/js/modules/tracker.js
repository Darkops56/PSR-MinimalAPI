import { apiFetch } from './api.js';

export class OrderTracker {
  constructor() {
    this.ordersMap = new Map(); // orderId -> estadoActual
    this.pollingInterval = null;

    // Inicializar estados iniciales desde los atributos HTML data-order-id
    const cards = document.querySelectorAll('[data-tracker-order]');
    if (cards.length === 0) return;

    cards.forEach(card => {
      const orderId = card.dataset.orderId;
      const initialStatus = card.dataset.initialStatus || 'EnPreparacion';
      if (orderId) {
        this.ordersMap.set(String(orderId), initialStatus);
        this.updateCardUI(orderId, initialStatus);
      }
    });

    this.startPolling();
  }

  startPolling() {
    this.stopPolling();
    this.checkStatus();
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
      // Consulta TODOS los pedidos activos del usuario autenticado
      const pedidos = await apiFetch('/api/pedidos/mis-pedidos-activos');
      if (!Array.isArray(pedidos)) return;

      let pedidosEnCurso = 0;

      pedidos.forEach(pedido => {
        const idStr = String(pedido.id ?? pedido.Id);
        const nuevoEstado = pedido.estado ?? pedido.Estado;

        if (nuevoEstado && (nuevoEstado === 'EnPreparacion' || nuevoEstado === 'EnViaje')) {
          pedidosEnCurso++;
        }

        const estadoAnterior = this.ordersMap.get(idStr);

        if (nuevoEstado) {
          if (estadoAnterior !== nuevoEstado) {
            this.ordersMap.set(idStr, nuevoEstado);
            window.showToast?.(`¡El pedido #${idStr} ahora está: ${this.formatStatus(nuevoEstado)}!`, 'info');
          }
          this.updateCardUI(idStr, nuevoEstado);
        }
      });

      // Si ya no quedan pedidos en preparación o viaje, detener polling
      if (pedidosEnCurso === 0 && pedidos.length > 0) {
        this.stopPolling();
      }
    } catch (err) {
      console.warn('Error al verificar estado de pedidos:', err);
    }
  }

  updateCardUI(orderIdStr, estado) {
    // Buscar la tarjeta específica de este pedido por data-order-id
    const card = document.querySelector(`[data-tracker-order][data-order-id="${orderIdStr}"]`);
    if (!card) return;

    const stepPrep = card.querySelector('.step-prep');
    const stepTravel = card.querySelector('.step-travel');
    const stepDone = card.querySelector('.step-done');
    const progressBar = card.querySelector('.tracker-progress-bar');
    const statusText = card.querySelector('.active-order-status-text');

    if (statusText) statusText.textContent = this.formatStatus(estado);

    // Resetear clases dentro de ESTA tarjeta
    [stepPrep, stepTravel, stepDone].forEach(s => s?.classList.remove('active', 'completed'));

    if (estado === 'EnPreparacion') {
      stepPrep?.classList.add('active');
      if (progressBar) progressBar.style.width = '0%';
    } else if (estado === 'EnViaje') {
      stepPrep?.classList.add('completed');
      stepTravel?.classList.add('active');
      if (progressBar) progressBar.style.width = '50%';
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
