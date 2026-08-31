import { apiFetch } from './api.js';

export class OrderTracker {
  constructor() {
    this.trackerElements = document.querySelectorAll('[data-tracker-order]');
    if (!this.trackerElements.length) return;

    this.ordersMap = new Map();
    this.pollingInterval = null;
    this.animationInterval = null;

    this.initTrackers();
    this.startPolling();
    this.startAnimationLoop();
  }

  initTrackers() {
    this.trackerElements.forEach(el => {
      const orderId = parseInt(el.dataset.orderId || '0', 10);
      const status = el.dataset.initialStatus || 'EnPreparacion';
      const progressBar = el.querySelector('.tracker-progress-bar');
      const statusText = el.querySelector('.active-order-status-text');

      const initialProgress = status === 'EnPreparacion' ? 10 : status === 'EnViaje' ? 50 : 100;

      this.ordersMap.set(orderId, {
        element: el,
        status: status,
        progress: initialProgress,
        targetCap: status === 'EnPreparacion' ? 45 : status === 'EnViaje' ? 90 : 100,
        progressBar: progressBar,
        statusText: statusText,
        isCompleted: status === 'Entregado'
      });

      this.updateUI(orderId, status, initialProgress);
    });
  }

  startPolling() {
    this.stopPolling();
    this.pollingInterval = setInterval(() => this.checkStatus(), 2000);
  }

  stopPolling() {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
      this.pollingInterval = null;
    }
  }

  startAnimationLoop() {
    if (this.animationInterval) clearInterval(this.animationInterval);
    
    // Bucle suave a 50 FPS (cada 20ms)
    this.animationInterval = setInterval(() => {
      let activeCount = 0;

      this.ordersMap.forEach((item, orderId) => {
        if (item.isCompleted) return;
        activeCount++;

        // Velocidad: 45% en 5s = +0.18% por tick (20ms). 40% en 3s = +0.26% por tick.
        const speed = item.status === 'EnPreparacion' ? 0.18 : 0.26;

        if (item.progress < item.targetCap) {
          item.progress = Math.min(item.targetCap, item.progress + speed);
          if (item.progressBar) {
            item.progressBar.style.width = `${item.progress.toFixed(1)}%`;
          }
        }
      });

      if (activeCount === 0 && this.animationInterval) {
        clearInterval(this.animationInterval);
      }
    }, 20);
  }

  async checkStatus() {
    try {
      // Consulta todos los pedidos activos del cliente autenticado
      const activos = await apiFetch('/api/pedidos/mis-pedidos-activos');
      if (!activos || !Array.isArray(activos)) return;

      activos.forEach(pedido => {
        const item = this.ordersMap.get(pedido.id);
        if (item && item.status !== pedido.estado) {
          item.status = pedido.estado;
          item.targetCap = pedido.estado === 'EnPreparacion' ? 45 : pedido.estado === 'EnViaje' ? 90 : 100;

          if (pedido.estado === 'EnViaje' && item.progress < 50) {
            item.progress = 50; // Salta al inicio de la etapa de viaje
          } else if (pedido.estado === 'Entregado') {
            item.progress = 100;
            item.isCompleted = true;
          }

          this.updateUI(pedido.id, pedido.estado, item.progress);
          window.showToast?.(`¡Tu Pedido #${pedido.id} ahora está: ${this.formatStatus(pedido.estado)}!`, 'info');
        }
      });
    } catch (err) {
      console.warn('Error al verificar pedidos activos por polling:', err);
    }
  }

  updateUI(orderId, estado, progressPercent) {
    const item = this.ordersMap.get(orderId);
    if (!item) return;

    const el = item.element;
    const stepPrep = el.querySelector('.step-prep');
    const stepTravel = el.querySelector('.step-travel');
    const stepDone = el.querySelector('.step-done');

    if (item.statusText) item.statusText.textContent = this.formatStatus(estado);
    if (item.progressBar) item.progressBar.style.width = `${progressPercent.toFixed(1)}%`;

    [stepPrep, stepTravel, stepDone].forEach(s => s?.classList.remove('active', 'completed'));

    if (estado === 'EnPreparacion') {
      stepPrep?.classList.add('active');
    } else if (estado === 'EnViaje') {
      stepPrep?.classList.add('completed');
      stepTravel?.classList.add('active');
    } else if (estado === 'Entregado') {
      stepPrep?.classList.add('completed');
      stepTravel?.classList.add('completed');
      stepDone?.classList.add('completed', 'active');
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
