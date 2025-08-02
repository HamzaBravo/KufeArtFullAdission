
class TabletDashboard {
    constructor() {
        this.orders = [];
        this.currentFilter = 'all';
        this.refreshInterval = null;
        this.department = window.tabletSession?.department || '';
        this.currentOrderId = null;
        this.isLoading = false;
        this.apiEndpoints = {
            getOrders: '/api/orders',
            getOrderDetail: '/api/orders',
            markAsReady: '/api/orders'
        };
    }

    static init() {
        window.TabletDashboard = new TabletDashboard();
        window.TabletDashboard.initialize();
        return window.TabletDashboard;
    }

    initialize() {
        this.bindEvents();
        this.loadOrders();
        this.startAutoRefresh();
    }

    bindEvents() {
      
        document.querySelectorAll('.filter-tab').forEach(tab => {
            tab.addEventListener('click', (e) => {
                const status = e.currentTarget.dataset.status;
                this.setFilter(status);
            });
        });

        const modal = document.getElementById('orderDetailModal');
        if (modal) {
            modal.addEventListener('show.bs.modal', (e) => {
                const orderId = e.relatedTarget?.dataset.orderId;
                if (orderId) {
                    this.currentOrderId = orderId;
                    this.loadOrderDetails(orderId);
                }
            });

            modal.addEventListener('hidden.bs.modal', () => {
                this.currentOrderId = null;
            });
        }

        const markReadyBtn = document.getElementById('markAsReadyBtn');
        if (markReadyBtn) {
            markReadyBtn.addEventListener('click', () => {
                if (this.currentOrderId) {
                    this.markOrderAsReadyDirect(this.currentOrderId);
                }
            });
        }
    }

    setFilter(status) {
        this.currentFilter = status;

        document.querySelectorAll('.filter-tab').forEach(tab => {
            tab.classList.toggle('active', tab.dataset.status === status);
        });

        this.renderOrders();
    }

    async loadOrders() {
        if (this.isLoading) {
            return;
        }

        this.isLoading = true;

        const isFirstLoad = this.orders.length === 0;
        if (isFirstLoad) {
            this.showLoading(true);
        }

        try {
            const response = await fetch(this.apiEndpoints.getOrders, {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin'
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                const newOrders = result.data.orders || [];

                if (!isFirstLoad && this.currentOrderId) {
                    const currentOrder = newOrders.find(o => o.orderBatchId === this.currentOrderId);
                    if (!currentOrder) {
                        const modal = bootstrap.Modal.getInstance(document.getElementById('orderDetailModal'));
                        if (modal) modal.hide();
                        this.currentOrderId = null;
                    }
                }

                if (isFirstLoad || this.orders.length !== newOrders.length) {
                    this.orders = newOrders;
                } else {
                    this.updateOrderStatuses(newOrders);
                }

                this.renderOrders();
                this.updateStats();

                if (this.orders.length === 0 && isFirstLoad) {
                    this.showEmptyState();
                }

            } else {
                throw new Error(result.message || 'Siparişler yüklenemedi');
            }

        } catch (error) {
            console.error('Siparişler yüklenemedi:', error);
            if (isFirstLoad) {
                TabletUtils.showToast('Siparişler yüklenemedi: ' + error.message, 'error');
                this.showEmptyState();
            }
        } finally {
            this.isLoading = false;
            if (isFirstLoad) {
                this.showLoading(false);
            }
        }
    }

    updateOrderStatuses(newOrders) {
        if (this.orders.length !== newOrders.length) {
            this.orders = newOrders;
            return;
        }

        this.orders.forEach((existingOrder, index) => {
            const updatedOrder = newOrders.find(o => o.orderBatchId === existingOrder.orderBatchId);
            if (updatedOrder) {
                if (updatedOrder.status !== existingOrder.status) {
                }
                this.orders[index] = updatedOrder; 
            }
        });

        const newOrderIds = newOrders.map(o => o.orderBatchId);
        const existingOrderIds = this.orders.map(o => o.orderBatchId);
        const hasNewOrders = newOrderIds.some(id => !existingOrderIds.includes(id));

        if (hasNewOrders) {
            this.orders = newOrders;
        }
    }

    async loadOrderDetails(orderId) {
        try {
            const order = this.orders.find(o => o.orderBatchId === orderId);
            if (!order) {
                await this.loadOrders();

                const orderAfterRefresh = this.orders.find(o => o.orderBatchId === orderId);
                if (!orderAfterRefresh) {
                    throw new Error('Sipariş 5 dakikadan uzun süre hazır durumda olduğu için listeden kaldırılmış olabilir');
                }

                this.renderOrderDetails(orderAfterRefresh);
                return;
            }
            this.renderOrderDetails(order);

        } catch (error) {
            TabletUtils.showToast('Sipariş detayı yüklenemedi: ' + error.message, 'error');
        }
    }
    renderOrderDetails(orderDetail) {
        const modalContent = document.getElementById('orderDetailContent');
        if (!modalContent) return;

        modalContent.innerHTML = `
        <div class="order-detail-header">
            <h4>${orderDetail.tableName} - Sipariş Detayı</h4>
            <div class="detail-meta">
                <span><i class="fas fa-clock"></i> ${this.formatTime(new Date(orderDetail.orderTime))}</span>
                <span><i class="fas fa-user"></i> ${orderDetail.waiterName}</span>
                <span class="status-badge ${orderDetail.status.toLowerCase()}">${this.getStatusText(orderDetail.status)}</span>
            </div>
            ${orderDetail.note ? `<div class="order-note"><i class="fas fa-sticky-note"></i> ${orderDetail.note}</div>` : ''}
        </div>
        
        <div class="order-detail-items">
            <h5><i class="fas fa-list"></i> Ürünler (${orderDetail.items.length})</h5>
            <div class="detail-products-list">
                ${orderDetail.items.map(item => `
                    <div class="detail-product-item">
                        <div class="product-info">
                            <span class="product-name">${item.productName}</span>
                            <span class="product-category">${item.categoryName || 'Kategori'}</span>
                        </div>
                        <div class="product-quantity">
                            <span class="quantity">x${item.quantity}</span>
                            <span class="price">₺${(item.price * item.quantity).toFixed(2)}</span>
                        </div>
                    </div>
                `).join('')}
            </div>
            
            <div class="detail-total">
                <strong>Toplam: ₺${orderDetail.totalAmount.toFixed(2)}</strong>
            </div>
        </div>
    `;

        const markReadyBtn = document.getElementById('markAsReadyBtn');
        if (markReadyBtn) {
            if (orderDetail.status === 'Ready') {
                markReadyBtn.style.display = 'none';
            } else {
                markReadyBtn.style.display = 'inline-block';
                markReadyBtn.innerHTML = '<i class="fas fa-check"></i> Hazır Olarak İşaretle';
            }
        }
    }

    async markOrderAsReadyDirect(orderId) {
        if (!orderId) {
            TabletUtils.showToast('Sipariş ID bulunamadı!', 'error');
            return;
        }

        const markReadyBtn = document.getElementById('markAsReadyBtn');
        const originalText = markReadyBtn?.innerHTML;

        try {
            console.log('✅ Sipariş hazır işaretleniyor:', orderId);

            if (markReadyBtn) {
                markReadyBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> İşleniyor...';
                markReadyBtn.disabled = true;
            }

            const response = await fetch(`${this.apiEndpoints.markAsReady}/${orderId}/ready`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'same-origin'
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                TabletUtils.showToast('Sipariş hazır olarak işaretlendi!', 'success');

                const modal = bootstrap.Modal.getInstance(document.getElementById('orderDetailModal'));
                if (modal) modal.hide();

                await this.loadOrders();

            } else {
                throw new Error(result.message || 'Sipariş durumu güncellenemedi');
            }

        } catch (error) {
            TabletUtils.showToast('Sipariş güncellenemedi: ' + error.message, 'error');
        } finally {
            if (markReadyBtn) {
                markReadyBtn.innerHTML = originalText;
                markReadyBtn.disabled = false;
            }
        }
    }

    renderOrders() {
        const container = document.getElementById('ordersContainer');
        if (!container) return;

        let filteredOrders = this.orders;

        if (this.currentFilter !== 'all') {
            filteredOrders = this.orders.filter(order => {
                if (this.currentFilter === 'New') return order.status === 'New';
                if (this.currentFilter === 'InProgress') return order.status === 'InProgress';
                if (this.currentFilter === 'Ready') return order.status === 'Ready';
                return true;
            });
        }

        filteredOrders.sort((a, b) => {
            const statusPriority = { 'New': 1, 'InProgress': 2, 'Ready': 3 };
            const aPriority = statusPriority[a.status] || 4;
            const bPriority = statusPriority[b.status] || 4;

            if (aPriority !== bPriority) {
                return aPriority - bPriority;
            }

            return new Date(a.orderTime) - new Date(b.orderTime);
        });

        if (filteredOrders.length === 0) {
            this.showEmptyState();
            return;
        }

        const emptyState = document.getElementById('emptyState');
        if (emptyState) emptyState.style.display = 'none';

        const currentScrollTop = container.scrollTop;
        container.innerHTML = filteredOrders.map(order => this.renderOrderCard(order)).join('');
        container.scrollTop = currentScrollTop;
    }

    renderOrderCard(order) {
        const statusClass = order.status.toLowerCase();
        const timeElapsed = this.getTimeElapsed(new Date(order.orderTime));
        const isNewOrder = order.isNew || false;

        if (!order.orderBatchId) {
            console.error('❌ Sipariş ID eksik:', order);
            return '';
        }

        let readyTimeRemaining = '';
        if (order.status === 'Ready' && order.completedAt) {
            const completedTime = new Date(order.completedAt);
            const fiveMinutesAfter = new Date(completedTime.getTime() + (5 * 60 * 1000));
            const now = new Date();
            const remaining = Math.max(0, Math.ceil((fiveMinutesAfter - now) / 1000 / 60));

            if (remaining > 0) {
                readyTimeRemaining = `<span class="ready-countdown">🕒 ${remaining} dk sonra gizlenecek</span>`;
            } else {
            }
        }

        return `
    <div class="order-row status-${statusClass} ${isNewOrder ? 'new-order-effect' : ''}" 
         data-order-id="${order.orderBatchId}">
        
        <div class="order-info">
            <div class="table-section">
                <h4 class="table-name">
                    <i class="fas fa-utensils"></i> ${order.tableName}
                </h4>
                <span class="waiter-name">
                    <i class="fas fa-user"></i> ${order.waiterName}
                </span>
            </div>
            
            <div class="order-summary">
                <span class="item-count">${order.items.length} ürün</span>
                ${order.note ? `<span class="order-note">${order.note}</span>` : ''}
                ${readyTimeRemaining}
            </div>
        </div>

        <div class="order-products">
            ${order.items.slice(0, 3).map(item => `
                <div class="product-item">
                    <span class="product-name">${item.productName}</span>
                    <span class="product-quantity">x${item.quantity}</span>
                </div>
            `).join('')}
            ${order.items.length > 3 ? `<div class="more-items">+${order.items.length - 3} ürün daha</div>` : ''}
        </div>

        <div class="order-actions">
            <div class="order-status">
                <span class="status-badge ${statusClass}">
                    ${this.getStatusIcon(order.status)} ${this.getStatusText(order.status)}
                </span>
                <span class="order-time">${timeElapsed}</span>
            </div>
            
            <div class="action-buttons">
                ${order.status !== 'Ready' ? `
                    <button class="btn-ready" onclick="window.TabletDashboard.markOrderAsReadyDirect('${order.orderBatchId}')">
                        <i class="fas fa-check"></i>
                        Hazır
                    </button>
                ` : `
                    <span class="completed-badge">
                        <i class="fas fa-check-circle"></i>
                        Tamamlandı
                    </span>
                `}
                
                <button class="btn-details" data-bs-toggle="modal" 
                        data-bs-target="#orderDetailModal" 
                        data-order-id="${order.orderBatchId}">
                    <i class="fas fa-eye"></i>
                    Detay
                </button>
            </div>
        </div>
    </div>
    `;
    }

    updateStats() {
        const stats = {
            pending: this.orders.filter(o => o.status === 'New').length,
            today: this.orders.length,
            completed: this.orders.filter(o => o.status === 'Ready').length
        };

        const pendingElement = document.querySelector('.stat-card.pending h3');
        const todayElement = document.querySelector('.stat-card.today h3');
        const completedElement = document.querySelector('.stat-card.completed h3');

        if (pendingElement) pendingElement.textContent = stats.pending;
        if (todayElement) todayElement.textContent = stats.today;
        if (completedElement) completedElement.textContent = stats.completed;
    }

    showLoading(show) {
        const container = document.getElementById('ordersContainer');
        const emptyState = document.getElementById('emptyState');

        if (show) {
            if (container) {
                container.innerHTML = `
                    <div class="loading-spinner">
                        <i class="fas fa-spinner fa-spin"></i>
                        <p>Siparişler yükleniyor...</p>
                    </div>
                `;
            }
            if (emptyState) emptyState.style.display = 'none';
        }
    }

    showEmptyState() {
        const emptyState = document.getElementById('emptyState');
        const container = document.getElementById('ordersContainer');

        if (container) container.innerHTML = '';
        if (emptyState) emptyState.style.display = 'block';
    }

    startAutoRefresh() {
        this.loadOrders();

        this.refreshInterval = setInterval(() => {
            this.loadOrders(); 
        }, 60000); 
    }

    async refreshOrders() {
        try {
            await this.loadOrders();
        } catch (error) {
            console.error('Auto refresh hatası:', error);
        }
    }

    formatTime(date) {
        return date.toLocaleTimeString('tr-TR', {
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    getStatusText(status) {
        const statusTexts = {
            'New': 'Yeni',
            'InProgress': 'Hazırlanıyor',
            'Ready': 'Hazır'
        };
        return statusTexts[status] || status;
    }

    getStatusIcon(status) {
        const statusIcons = {
            'New': '🆕',
            'InProgress': '⏳',
            'Ready': '✅'
        };
        return statusIcons[status] || '📋';
    }

    getTimeElapsed(orderTime) {
        const elapsed = Math.floor((new Date() - orderTime) / 1000 / 60);
        if (elapsed < 1) return 'Az önce';
        if (elapsed < 60) return `${elapsed} dk önce`;
        return `${Math.floor(elapsed / 60)} saat önce`;
    }

    handleNewOrder(orderData) {
        this.loadOrders();
        TabletUtils.showToast(`Yeni sipariş: ${orderData.TableName}`, 'info', 5000);
    }

    updateOrderStatus(statusData) {
        const order = this.orders.find(o => o.orderBatchId === statusData.orderBatchId);
        if (order) {
            order.status = statusData.status;
            this.renderOrders();
            this.updateStats();
        }
    }

    destroy() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
    }
}

window.TabletDashboard = TabletDashboard;

TabletDashboard.showOrderDetails = function (orderId) {
    if (window.TabletDashboard) {
        window.TabletDashboard.loadOrderDetails(orderId);
    }
};

TabletDashboard.markAsReady = function (orderId) {
    if (window.TabletDashboard) {
        window.TabletDashboard.markOrderAsReadyDirect(orderId);
    }
};