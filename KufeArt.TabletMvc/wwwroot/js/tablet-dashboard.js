// KufeArt.TabletMvc/wwwroot/js/tablet-dashboard.js (API bağlantılı versiyon)
console.log('🔍 tablet-dashboard.js yüklendi');
class TabletDashboard {
    constructor() {
        this.orders = [];
        this.currentFilter = 'all';
        this.refreshInterval = null;
        this.department = window.tabletSession?.department || '';
        this.apiEndpoints = {
            getOrders: '/api/orders',
            getOrderDetail: '/api/orders',
            markAsReady: '/api/orders'
        };
    }


    static init() {
        console.log('🔍 TabletDashboard.init() BAŞLADI');

        // Her zaman yeni instance oluştur ve initialize et
        console.log('🔍 Yeni TabletDashboard instance oluşturuluyor...');
        window.TabletDashboard = new TabletDashboard();
        console.log('🔍 TabletDashboard instance oluşturuldu');

        console.log('🔍 initialize() çağrılıyor...');
        window.TabletDashboard.initialize();
        console.log('🔍 initialize() çağrıldı');

        console.log('🔍 TabletDashboard.init() BİTTİ');
        return window.TabletDashboard;
    }

    initialize() {
        console.log('📱 Tablet Dashboard başlatılıyor...', this.department);

        this.bindEvents();
        this.loadOrders();
        this.startAutoRefresh();
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


    // KufeArt.TabletMvc/wwwroot/js/tablet-dashboard.js class içine ekle:

    async showOrderDetailsModal(orderId) {
        console.log('🔍 Modal açılıyor:', orderId);

        try {
            // Modal'ı aç
            const modal = new bootstrap.Modal(document.getElementById('orderDetailModal'));
            modal.show();

            // Loading göster
            const modalContent = document.getElementById('orderDetailContent');
            modalContent.innerHTML = `
            <div class="text-center p-4">
                <i class="fas fa-spinner fa-spin fa-2x mb-3"></i>
                <p>Sipariş detayı yükleniyor...</p>
            </div>
        `;

            // API'den detay yükle
            await this.loadOrderDetails(orderId);

            // Mark as ready button'a orderId ekle
            const markReadyBtn = document.getElementById('markAsReadyBtn');
            if (markReadyBtn) {
                markReadyBtn.dataset.orderId = orderId;
            }

        } catch (error) {
            console.error('Modal açma hatası:', error);
            TabletUtils.showToast('Modal açılamadı', 'error');
        }
    }

    async markOrderAsReadyDirect(orderId) {
        console.log('🔍 Direkt hazır işaretleme:', orderId);

        if (confirm('Bu siparişi hazır olarak işaretlemek istediğinizden emin misiniz?')) {
            try {
                const response = await fetch(`${this.apiEndpoints.markAsReady}/${orderId}/ready`, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    credentials: 'same-origin',
                    body: JSON.stringify({})
                });

                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }

                const result = await response.json();

                if (result.success) {
                    TabletUtils.showToast('Sipariş hazır olarak işaretlendi!', 'success');
                    await this.loadOrders(); // Listeyi yenile
                } else {
                    throw new Error(result.message || 'Sipariş güncellenemedi');
                }

            } catch (error) {
                console.error('Hazır işaretleme hatası:', error);
                TabletUtils.showToast('Sipariş güncellenemedi: ' + error.message, 'error');
            }
        }
    }


    // 🔄 GERÇEK API BAĞLANTISI
    async loadOrders() {
        try {
            this.showLoading(true);

            const url = `${this.apiEndpoints.getOrders}?status=${this.currentFilter}`;
            const response = await fetch(url, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'same-origin'
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                this.orders = result.data.orders || [];
                this.renderOrders();
                this.updateStats();

                // Empty state kontrolü
                if (this.orders.length === 0) {
                    this.showEmptyState();
                }
            } else {
                throw new Error(result.message || 'Siparişler yüklenemedi');
            }

        } catch (error) {
            console.error('Siparişler yüklenemedi:', error);
            TabletUtils.showToast('Siparişler yüklenemedi: ' + error.message, 'error');
            this.showEmptyState();
        } finally {
            this.showLoading(false);
        }
    }

    // 🔍 SİPARİŞ DETAY YÜKLEME
    async loadOrderDetails(orderId) {
        try {
            const response = await fetch(`${this.apiEndpoints.getOrderDetail}/${orderId}`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'same-origin'
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                this.renderOrderDetails(result.data);
            } else {
                throw new Error(result.message || 'Sipariş detayı yüklenemedi');
            }

        } catch (error) {
            console.error('Sipariş detayı yüklenemedi:', error);
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
                    <span><i class="fas fa-clock"></i> ${TabletUtils.formatTime(new Date(orderDetail.orderTime))}</span>
                    <span><i class="fas fa-user"></i> ${orderDetail.waiterName}</span>
                    <span class="status-badge ${orderDetail.status.toLowerCase()}">${this.getStatusText(orderDetail.status)}</span>
                </div>
                ${orderDetail.note ? `<div class="order-note"><i class="fas fa-sticky-note"></i> ${orderDetail.note}</div>` : ''}
            </div>
            
            <div class="order-detail-items">
                <h5>Sipariş Kalemleri</h5>
                <div class="items-list">
                    ${orderDetail.items.map(item => `
                        <div class="detail-item">
                            <div class="item-info">
                                <strong>${item.productName}</strong>
                                ${item.description ? `<small>${item.description}</small>` : ''}
                            </div>
                            <div class="item-quantity">
                                <span class="quantity-badge">${item.quantity}</span>
                                <div class="item-prices">
                                    <small>Birim: ${TabletUtils.formatCurrency(item.unitPrice)}</small>
                                    <strong>Toplam: ${TabletUtils.formatCurrency(item.totalPrice)}</strong>
                                </div>
                            </div>
                        </div>
                    `).join('')}
                </div>
            </div>
            
            <div class="order-detail-total">
                <strong>Genel Toplam: ${TabletUtils.formatCurrency(orderDetail.totalAmount)}</strong>
            </div>
        `;

        // Mark as ready button durumu
        const markReadyBtn = document.getElementById('markAsReadyBtn');
        if (markReadyBtn) {
            markReadyBtn.style.display = orderDetail.status === 'Ready' ? 'none' : 'block';
            markReadyBtn.dataset.orderId = orderDetail.orderBatchId;
        }
    }

    // ✅ SİPARİŞ HAZIR İŞARETLEME
    async markOrderAsReady() {
        const markReadyBtn = document.getElementById('markAsReadyBtn');
        const orderId = markReadyBtn?.dataset.orderId;

        if (!orderId) {
            TabletUtils.showToast('Sipariş ID bulunamadı', 'error');
            return;
        }

        try {
            // Button loading state
            const originalText = markReadyBtn.innerHTML;
            markReadyBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> İşleniyor...';
            markReadyBtn.disabled = true;

            const response = await fetch(`${this.apiEndpoints.markAsReady}/${orderId}/ready`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                credentials: 'same-origin',
                body: JSON.stringify({}) // Boş body
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();

            if (result.success) {
                TabletUtils.showToast('Sipariş hazır olarak işaretlendi!', 'success');

                // Modal'ı kapat
                const modal = bootstrap.Modal.getInstance(document.getElementById('orderDetailModal'));
                if (modal) modal.hide();

                // Sipariş listesini yenile
                await this.loadOrders();

                // SignalR ile bildirim gönder (opsiyonel)
                if (window.tabletSignalR && window.tabletSignalR.isConnectionActive()) {
                    await window.tabletSignalR.sendMessage('NotifyOrderStatusChange', {
                        orderBatchId: orderId,
                        status: 'Ready',
                        department: this.department
                    });
                }

            } else {
                throw new Error(result.message || 'Sipariş durumu güncellenemedi');
            }

        } catch (error) {
            console.error('Sipariş güncellenemedi:', error);
            TabletUtils.showToast('Sipariş güncellenemedi: ' + error.message, 'error');
        } finally {
            // Button'u eski haline getir
            if (markReadyBtn) {
                markReadyBtn.innerHTML = originalText;
                markReadyBtn.disabled = false;
            }
        }
    }

    renderOrders() {
        const container = document.getElementById('ordersContainer');
        const filteredOrders = this.getFilteredOrders();

        if (filteredOrders.length === 0) {
            this.showEmptyState();
            return;
        }

        const ordersHTML = filteredOrders.map(order => this.renderOrderCard(order)).join('');
        container.innerHTML = ordersHTML;

        this.bindOrderEvents();
    }

    // KufeArt.TabletMvc/wwwroot/js/tablet-dashboard.js'de renderOrderCard method'unu bul ve değiştir:

    renderOrderCard(order) {
        const statusClass = order.status.toLowerCase();
        const timeElapsed = this.getTimeElapsed(new Date(order.orderTime));

        return `
        <div class="order-card status-${statusClass} ${order.isNew ? 'new-order' : ''}" 
             data-order-id="${order.orderBatchId}">
            
            <div class="order-header">
                <div class="order-meta">
                    <div class="table-info">
                        <h4><i class="fas fa-utensils"></i> ${order.tableName}</h4>
                        <small><i class="fas fa-user"></i> ${order.waiterName}</small>
                    </div>
                    <div class="order-time">
                        <div class="time-badge">${TabletUtils.formatTime(new Date(order.orderTime))}</div>
                        <small class="elapsed-time">${timeElapsed}</small>
                    </div>
                </div>
                
                <div class="order-status">
                    <span class="status-badge ${statusClass}">
                        ${this.getStatusIcon(order.status)} ${this.getStatusText(order.status)}
                    </span>
                    <div class="order-total">${TabletUtils.formatCurrency(order.totalAmount)}</div>
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
                ` : `
                    <span class="btn-action btn-completed">
                        <i class="fas fa-check-circle"></i>
                        Hazır
                    </span>
                `}
            </div>
        </div>
    `;
    }

    // Helper Methods
    getTimeElapsed(orderTime) {
        const elapsed = (Date.now() - orderTime.getTime()) / 1000 / 60; // dakika
        if (elapsed < 1) return 'Az önce';
        if (elapsed < 60) return Math.floor(elapsed) + ' dk önce';
        const hours = Math.floor(elapsed / 60);
        const minutes = Math.floor(elapsed % 60);
        return `${hours}s ${minutes}dk önce`;
    }

    getStatusIcon(status) {
        const icons = {
            'New': '<i class="fas fa-clock"></i>',
            'Preparing': '<i class="fas fa-fire"></i>',
            'Ready': '<i class="fas fa-check-circle"></i>'
        };
        return icons[status] || '<i class="fas fa-question"></i>';
    }

    getStatusText(status) {
        const texts = {
            'New': 'Yeni',
            'Preparing': 'Hazırlanıyor',
            'Ready': 'Hazır'
        };
        return texts[status] || status;
    }

    bindOrderEvents() {
        // Order card click events
        document.querySelectorAll('.order-card').forEach(card => {
            card.addEventListener('click', (e) => {
                if (e.target.closest('.btn-action')) return; // Button clicks ignore

                const orderId = card.dataset.orderId;
                // ✅ Doğru method ismi: showOrderDetailsModal
                this.showOrderDetailsModal(orderId);
            });
        });
    }

    getFilteredOrders() {
        if (this.currentFilter === 'all') {
            return this.orders;
        }

        const filterMap = {
            'new': 'New',
            'preparing': 'Preparing',
            'ready': 'Ready'
        };

        const targetStatus = filterMap[this.currentFilter.toLowerCase()];
        return this.orders.filter(order => order.status === targetStatus);
    }

    setFilter(status) {
        this.currentFilter = status;

        // Tab aktif/pasif
        document.querySelectorAll('.filter-tab').forEach(tab => {
            tab.classList.remove('active');
        });
        document.querySelector(`[data-status="${status}"]`)?.classList.add('active');

        this.loadOrders(); // Filtreyle yeniden yükle
    }

    updateStats() {
        const stats = {
            pending: this.orders.filter(o => o.status === 'New' || o.status === 'Preparing').length,
            today: this.orders.length,
            completed: this.orders.filter(o => o.status === 'Ready').length
        };

        document.getElementById('pendingCount').textContent = stats.pending;
        document.getElementById('todayCount').textContent = stats.today;
        document.getElementById('completedCount').textContent = stats.completed;
    }

    showLoading(show) {
        const spinner = document.querySelector('.loading-spinner');
        if (spinner) {
            spinner.style.display = show ? 'block' : 'none';
        }
    }

    showEmptyState() {
        const emptyState = document.getElementById('emptyState');
        const container = document.getElementById('ordersContainer');

        if (container) container.innerHTML = '';
        if (emptyState) emptyState.style.display = 'block';
    }

    startAutoRefresh() {
        this.refreshInterval = setInterval(() => {
            this.refreshOrders();
        }, 30000); // 30 saniye
    }

    async refreshOrders() {
        try {
            await this.loadOrders();
        } catch (error) {
            console.error('Auto refresh hatası:', error);
        }
    }

    // SignalR event handlers
    handleNewOrder(orderData) {
        // Yeni sipariş geldiğinde liste yenile
        this.refreshOrders();

        // Bildirim göster
        TabletUtils.showToast(`Yeni sipariş: ${orderData.TableName}`, 'info', 5000);
    }

    updateOrderStatus(statusData) {
        // Sipariş durumu değiştiğinde güncelle
        const order = this.orders.find(o => o.orderBatchId === statusData.orderBatchId);
        if (order) {
            order.status = statusData.status;
            this.renderOrders();
            this.updateStats();
        }
    }

    // Static methods for global access
    static showOrderDetails(orderId) {
        if (window.TabletDashboard) {
            window.TabletDashboard.loadOrderDetails(orderId);
        }
    }

    static markAsReady(orderId) {
        if (window.TabletDashboard) {
            // Modal açarak detaylı işlem yap
            window.TabletDashboard.loadOrderDetails(orderId);
        }
    }
}

// Global access
window.TabletDashboard = TabletDashboard;

TabletDashboard.showOrderDetails = function (orderId) {
    console.log('🔍 showOrderDetails çağrıldı:', orderId);
    if (window.TabletDashboard) {
        window.TabletDashboard.showOrderDetailsModal(orderId);
    }
};

TabletDashboard.markAsReady = function (orderId) {
    console.log('🔍 markAsReady çağrıldı:', orderId);
    if (window.TabletDashboard) {
        window.TabletDashboard.markOrderAsReadyDirect(orderId);
    }
};