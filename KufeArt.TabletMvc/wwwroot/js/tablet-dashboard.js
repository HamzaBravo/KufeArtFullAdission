// KufeArt.TabletMvc/wwwroot/js/tablet-dashboard.js

class TabletDashboard {
    constructor() {
        this.orders = [];
        this.currentFilter = 'all';
        this.refreshInterval = null;
        this.department = window.tabletSession?.department || '';
    }

    static init() {
        if (!window.TabletDashboard) {
            window.TabletDashboard = new TabletDashboard();
            window.TabletDashboard.initialize();
        }
        return window.TabletDashboard;
    }

    initialize() {
        console.log('📱 Tablet Dashboard başlatılıyor...', this.department);

        this.bindEvents();
        this.loadOrders();
        this.startAutoRefresh();
        this.updateStats();
    }

    bindEvents() {
        // Filter tabs
        document.querySelectorAll('.filter-tab').forEach(tab => {
            tab.addEventListener('click', (e) => {
                const status = e.currentTarget.dataset.status;
                this.setFilter(status);
            });
        });

        // Modal events
        const modal = document.getElementById('orderDetailModal');
        if (modal) {
            modal.addEventListener('show.bs.modal', (e) => {
                const orderId = e.relatedTarget?.dataset.orderId;
                if (orderId) {
                    this.loadOrderDetails(orderId);
                }
            });
        }

        // Mark as ready button
        const markReadyBtn = document.getElementById('markAsReadyBtn');
        if (markReadyBtn) {
            markReadyBtn.addEventListener('click', () => {
                this.markOrderAsReady();
            });
        }
    }

    async loadOrders() {
        try {
            this.showLoading(true);

            // Şimdilik mock data kullanıyoruz - sonra API'ye bağlayacağız
            const response = await this.getMockOrders();

            this.orders = response.orders || [];
            this.renderOrders();
            this.updateStats();

        } catch (error) {
            console.error('Siparişler yüklenemedi:', error);
            TabletUtils.showToast('Siparişler yüklenemedi', 'error');
        } finally {
            this.showLoading(false);
        }
    }

    // Şimdilik mock data - sonra gerçek API'ye değiştirilecek
    async getMockOrders() {
        return new Promise(resolve => {
            setTimeout(() => {
                resolve({
                    orders: [
                        {
                            orderBatchId: '123e4567-e89b-12d3-a456-426614174000',
                            tableId: '456e7890-e89b-12d3-a456-426614174001',
                            tableName: 'Masa 5',
                            waiterName: 'Ahmet Yılmaz',
                            orderTime: new Date(Date.now() - 10 * 60000), // 10 dk önce
                            status: 'New',
                            totalAmount: 125.50,
                            items: [
                                {
                                    productName: this.department === 'Kitchen' ? 'Lahmacun' : 'Çay',
                                    quantity: 2,
                                    price: 25.00,
                                    productType: this.department,
                                    categoryName: this.department === 'Kitchen' ? 'Ana Yemek' : 'Sıcak İçecek'
                                }
                            ],
                            isNew: true
                        },
                        {
                            orderBatchId: '789e0123-e89b-12d3-a456-426614174002',
                            tableId: '012e3456-e89b-12d3-a456-426614174003',
                            tableName: 'Masa 12',
                            waiterName: 'Fatma Demir',
                            orderTime: new Date(Date.now() - 25 * 60000), // 25 dk önce
                            status: 'InProgress',
                            totalAmount: 87.25,
                            items: [
                                {
                                    productName: this.department === 'Kitchen' ? 'Döner' : 'Kahve',
                                    quantity: 1,
                                    price: 35.00,
                                    productType: this.department,
                                    categoryName: this.department === 'Kitchen' ? 'Ana Yemek' : 'Sıcak İçecek'
                                }
                            ],
                            isNew: false
                        }
                    ]
                });
            }, 500);
        });
    }

    renderOrders() {
        const container = document.getElementById('ordersContainer');
        if (!container) return;

        const filteredOrders = this.getFilteredOrders();

        if (filteredOrders.length === 0) {
            this.showEmptyState();
            return;
        }

        let html = '';
        filteredOrders.forEach(order => {
            html += this.renderOrderCard(order);
        });

        container.innerHTML = html;
        this.bindOrderEvents();
    }

    renderOrderCard(order) {
        const elapsedTime = TabletUtils.getElapsedTime(order.orderTime);
        const statusClass = order.status.toLowerCase();
        const statusText = this.getStatusText(order.status);

        return `
            <div class="order-card status-${statusClass}" data-order-id="${order.orderBatchId}">
                <div class="order-header">
                    <div class="order-meta">
                        <div class="table-info">
                            <h4>
                                <i class="fas fa-utensils"></i>
                                ${order.tableName}
                            </h4>
                            <small>Garson: ${order.waiterName}</small>
                        </div>
                        <div class="order-time">
                            <div class="time-badge">
                                ${TabletUtils.formatTime(order.orderTime)}
                            </div>
                            <div class="elapsed-time">${elapsedTime}</div>
                        </div>
                    </div>
                    <div class="order-status">
                        <span class="status-badge ${statusClass}">${statusText}</span>
                        <div class="waiter-info">
                            <i class="fas fa-user"></i>
                            ${order.waiterName}
                        </div>
                    </div>
                </div>
                
                <div class="order-items">
                    <ul class="item-list">
                        ${order.items.map(item => `
                            <li class="order-item">
                                <div class="item-info">
                                    <div class="item-name">${item.productName}</div>
                                    <div class="item-category">${item.categoryName}</div>
                                </div>
                                <div class="item-quantity">${item.quantity}</div>
                            </li>
                        `).join('')}
                    </ul>
                </div>
                
                <div class="order-actions">
                    <button class="btn-action btn-details" onclick="TabletDashboard.showOrderDetails('${order.orderBatchId}')">
                        <i class="fas fa-eye"></i>
                        Detay
                    </button>
                    ${order.status !== 'Ready' ? `
                        <button class="btn-action btn-ready" onclick="TabletDashboard.markAsReady('${order.orderBatchId}')">
                            <i class="fas fa-check"></i>
                            Hazır
                        </button>
                    ` : ''}
                </div>
            </div>
        `;
    }

    bindOrderEvents() {
        // Order card click events
        document.querySelectorAll('.order-card').forEach(card => {
            card.addEventListener('click', (e) => {
                if (e.target.closest('.btn-action')) return; // Button clicks ignore

                const orderId = card.dataset.orderId;
                this.showOrderDetails(orderId);
            });
        });
    }

    getFilteredOrders() {
        if (this.currentFilter === 'all') {
            return this.orders;
        }
        return this.orders.filter(order => order.status === this.currentFilter);
    }

    setFilter(status) {
        this.currentFilter = status;

        // Tab aktif/pasif
        document.querySelectorAll('.filter-tab').forEach(tab => {
            tab.classList.remove('active');
        });
        document.querySelector(`[data-status="${status}"]`).classList.add('active');

        this.renderOrders();
    }

    getStatusText(status) {
        const statusTexts = {
            'New': 'Yeni',
            'InProgress': 'Hazırlanıyor',
            'Ready': 'Hazır'
        };
        return statusTexts[status] || status;
    }

    showOrderDetails(orderId) {
        const order = this.orders.find(o => o.orderBatchId === orderId);
        if (!order) return;

        // Modal content
        const modalContent = document.getElementById('orderDetailContent');
        if (!modalContent) return;

        modalContent.innerHTML = `
            <div class="order-detail-header">
                <h4>${order.tableName} - Sipariş Detayı</h4>
                <div class="detail-meta">
                    <span><i class="fas fa-clock"></i> ${TabletUtils.formatTime(order.orderTime)}</span>
                    <span><i class="fas fa-user"></i> ${order.waiterName}</span>
                    <span class="status-badge ${order.status.toLowerCase()}">${this.getStatusText(order.status)}</span>
                </div>
            </div>
            
            <div class="order-detail-items">
                <h5>Sipariş Kalemleri</h5>
                <div class="items-list">
                    ${order.items.map(item => `
                        <div class="detail-item">
                            <div class="item-info">
                                <strong>${item.productName}</strong>
                                <small>${item.categoryName}</small>
                            </div>
                            <div class="item-quantity">
                                <span class="quantity-badge">${item.quantity}</span>
                                <small>${TabletUtils.formatCurrency(item.price)}</small>
                            </div>
                        </div>
                    `).join('')}
                </div>
            </div>
            
            <div class="order-detail-total">
                <strong>Toplam: ${TabletUtils.formatCurrency(order.totalAmount)}</strong>
            </div>
        `;

        // Modal'ı göster
        const modal = new bootstrap.Modal(document.getElementById('orderDetailModal'));
        modal.show();

        // Mark as ready button
        const markReadyBtn = document.getElementById('markAsReadyBtn');
        if (markReadyBtn) {
            markReadyBtn.style.display = order.status === 'Ready' ? 'none' : 'block';
            markReadyBtn.onclick = () => this.markAsReady(orderId);
        }
    }

    async markAsReady(orderId) {
        try {
            // Gerçek API call burada olacak
            console.log('Sipariş hazır olarak işaretleniyor:', orderId);

            // Mock update
            const order = this.orders.find(o => o.orderBatchId === orderId);
            if (order) {
                order.status = 'Ready';
                this.renderOrders();
                this.updateStats();
            }

            TabletUtils.showToast('Sipariş hazır olarak işaretlendi', 'success');

            // Modal'ı kapat
            const modal = bootstrap.Modal.getInstance(document.getElementById('orderDetailModal'));
            if (modal) modal.hide();

        } catch (error) {
            console.error('Sipariş güncellenemedi:', error);
            TabletUtils.showToast('Sipariş güncellenemedi', 'error');
        }
    }

    updateStats() {
        const pendingCount = this.orders.filter(o => o.status === 'New' || o.status === 'InProgress').length;
        const todayCount = this.orders.length;
        const completedCount = this.orders.filter(o => o.status === 'Ready').length;

        document.getElementById('pendingCount').textContent = pendingCount;
        document.getElementById('todayCount').textContent = todayCount;
        document.getElementById('completedCount').textContent = completedCount;
    }

    showLoading(show) {
        const spinner = document.querySelector('.loading-spinner');
        const container = document.getElementById('ordersContainer');

        if (show) {
            if (spinner) spinner.style.display = 'block';
        } else {
            if (spinner) spinner.style.display = 'none';
        }
    }

    showEmptyState() {
        const emptyState = document.getElementById('emptyState');
        const container = document.getElementById('ordersContainer');

        if (container) container.innerHTML = '';
        if (emptyState) emptyState.style.display = 'block';
    }

    startAutoRefresh() {
        // Her 30 saniyede bir siparişleri yenile
        this.refreshInterval = setInterval(() => {
            this.refreshOrders();
        }, 30000);
    }

    async refreshOrders() {
        try {
            await this.loadOrders();
        } catch (error) {
            console.error('Auto refresh hatası:', error);
        }
    }

    // Static methods for global access
    static showOrderDetails(orderId) {
        if (window.TabletDashboard) {
            window.TabletDashboard.showOrderDetails(orderId);
        }
    }

    static markAsReady(orderId) {
        if (window.TabletDashboard) {
            window.TabletDashboard.markAsReady(orderId);
        }
    }
}

// Global access
window.TabletDashboard = TabletDashboard;