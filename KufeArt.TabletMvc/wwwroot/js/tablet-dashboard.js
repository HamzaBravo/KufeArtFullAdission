// KufeArt.TabletMvc/wwwroot/js/tablet-dashboard.js (Sorunlar düzeltildi)
console.log('🔍 tablet-dashboard.js yüklendi');

class TabletDashboard {
    constructor() {
        this.orders = [];
        this.currentFilter = 'all';
        this.refreshInterval = null;
        this.department = window.tabletSession?.department || '';
        this.currentOrderId = null;
        this.isLoading = false; // ✅ Loading kontrolü eklendi
        this.apiEndpoints = {
            getOrders: '/api/orders',
            getOrderDetail: '/api/orders',
            markAsReady: '/api/orders'
        };
    }

    static init() {
        console.log('🔍 TabletDashboard.init() BAŞLADI');
        window.TabletDashboard = new TabletDashboard();
        window.TabletDashboard.initialize();
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

        // Modal events - Düzeltildi
        const modal = document.getElementById('orderDetailModal');
        if (modal) {
            modal.addEventListener('show.bs.modal', (e) => {
                const orderId = e.relatedTarget?.dataset.orderId;
                if (orderId) {
                    this.currentOrderId = orderId;
                    this.loadOrderDetails(orderId);
                }
            });

            // ✅ Modal kapandığında currentOrderId'yi temizle
            modal.addEventListener('hidden.bs.modal', () => {
                this.currentOrderId = null;
                console.log('🚪 Modal kapandı, currentOrderId temizlendi');
            });
        }

        // Mark as ready button
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

        // Tab görünümünü güncelle
        document.querySelectorAll('.filter-tab').forEach(tab => {
            tab.classList.toggle('active', tab.dataset.status === status);
        });

        this.renderOrders();
    }

    // 📊 SİPARİŞLERİ YÜKLEME (Düzeltildi - dalgalanma yok)
    // 📊 SİPARİŞLERİ YÜKLEME (Düzeltildi - dalgalanma yok)
    async loadOrders() {
        // ✅ Eğer zaten yükleme yapılıyorsa, bekle
        if (this.isLoading) {
            console.log('⏳ Zaten yükleme yapılıyor, atlaniyor...');
            return;
        }

        this.isLoading = true;

        // ✅ İlk yüklemede loading göster, refresh'lerde gösterme
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

                // ✅ Modal açıksa ve sipariş array'i değişecekse, dikkatli güncelle
                if (!isFirstLoad && this.currentOrderId) {
                    // Modal açık durumda, mevcut siparişi koru
                    const currentOrder = newOrders.find(o => o.orderBatchId === this.currentOrderId);
                    if (!currentOrder) {
                        console.log('⚠️ Modal\'daki sipariş artık bulunamıyor, modal\'ı kapat');
                        const modal = bootstrap.Modal.getInstance(document.getElementById('orderDetailModal'));
                        if (modal) modal.hide();
                        this.currentOrderId = null;
                    }
                }

                // Sipariş listesini güncelle
                if (isFirstLoad || this.orders.length !== newOrders.length) {
                    this.orders = newOrders;
                    console.log(`✅ ${this.orders.length} sipariş yüklendi (${isFirstLoad ? 'İlk yükleme' : 'Tam güncelleme'})`);
                } else {
                    this.updateOrderStatuses(newOrders);
                    console.log(`✅ ${this.orders.length} sipariş güncellendi`);
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

    // ✅ YENİ: Sipariş durumlarını smooth güncelleme
    updateOrderStatuses(newOrders) {
        // Sipariş sayısı değiştiyse tam güncelleme yap
        if (this.orders.length !== newOrders.length) {
            console.log('📝 Sipariş sayısı değişti, tam güncelleme yapılıyor');
            this.orders = newOrders;
            return;
        }

        // Mevcut siparişleri güncelle
        this.orders.forEach((existingOrder, index) => {
            const updatedOrder = newOrders.find(o => o.orderBatchId === existingOrder.orderBatchId);
            if (updatedOrder) {
                if (updatedOrder.status !== existingOrder.status) {
                    console.log(`📝 Sipariş durumu güncellendi: ${existingOrder.tableName} -> ${updatedOrder.status}`);
                }
                this.orders[index] = updatedOrder; // Tüm veriyi güncelle
            }
        });

        // Yeni sipariş var mı kontrol et
        const newOrderIds = newOrders.map(o => o.orderBatchId);
        const existingOrderIds = this.orders.map(o => o.orderBatchId);
        const hasNewOrders = newOrderIds.some(id => !existingOrderIds.includes(id));

        if (hasNewOrders) {
            console.log('📝 Yeni sipariş tespit edildi, tam güncelleme yapılıyor');
            this.orders = newOrders;
        }
    }

    // 🔍 SİPARİŞ DETAY YÜKLEME (Düzeltildi)
    // 🔍 SİPARİŞ DETAY YÜKLEME (Debug eklendi)
    async loadOrderDetails(orderId) {
        try {
            console.log('🔍 Sipariş detayı yükleniyor:', orderId);
            console.log('📋 Mevcut sipariş listesi:', this.orders.map(o => ({ id: o.orderBatchId, table: o.tableName, status: o.status })));

            const order = this.orders.find(o => o.orderBatchId === orderId);
            if (!order) {
                console.log('❌ Sipariş bulunamadı! Aranan ID:', orderId);
                console.log('📋 Listede olan ID\'ler:', this.orders.map(o => o.orderBatchId));

                // ✅ Backend'den tekrar sipariş listesini al
                console.log('🔄 Backend\'den yeniden sipariş listesi alınıyor...');
                await this.loadOrders();

                // Yeniden ara
                const orderAfterRefresh = this.orders.find(o => o.orderBatchId === orderId);
                if (!orderAfterRefresh) {
                    throw new Error('Sipariş 5 dakikadan uzun süre hazır durumda olduğu için listeden kaldırılmış olabilir');
                }

                console.log('✅ Yenileme sonrası sipariş bulundu:', orderAfterRefresh.tableName);
                this.renderOrderDetails(orderAfterRefresh);
                return;
            }

            console.log('✅ Sipariş bulundu:', order.tableName, order.status);
            this.renderOrderDetails(order);

        } catch (error) {
            console.error('Sipariş detayı yüklenemedi:', error);
            TabletUtils.showToast('Sipariş detayı yüklenemedi: ' + error.message, 'error');
        }
    }

    // 🎨 SİPARİŞ DETAYI MODAL İÇERİĞİ (Düzeltildi)
    // 🎨 SİPARİŞ DETAYI MODAL İÇERİĞİ (Düzeltildi)
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

        // Modal butonlarını güncelle
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

    // ✅ SİPARİŞ HAZIR İŞARETLEME
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

                // Modal'ı kapat
                const modal = bootstrap.Modal.getInstance(document.getElementById('orderDetailModal'));
                if (modal) modal.hide();

                // ✅ Hemen sipariş listesini güncelle
                await this.loadOrders();

            } else {
                throw new Error(result.message || 'Sipariş durumu güncellenemedi');
            }

        } catch (error) {
            console.error('Sipariş güncellenemedi:', error);
            TabletUtils.showToast('Sipariş güncellenemedi: ' + error.message, 'error');
        } finally {
            if (markReadyBtn) {
                markReadyBtn.innerHTML = originalText;
                markReadyBtn.disabled = false;
            }
        }
    }

    // 🎨 SİPARİŞLERİ RENDER ETME (Düzeltildi - Sıralama eklendi)
    renderOrders() {
        const container = document.getElementById('ordersContainer');
        if (!container) return;

        let filteredOrders = this.orders;

        // Filtre uygula
        if (this.currentFilter !== 'all') {
            filteredOrders = this.orders.filter(order => {
                if (this.currentFilter === 'New') return order.status === 'New';
                if (this.currentFilter === 'InProgress') return order.status === 'InProgress';
                if (this.currentFilter === 'Ready') return order.status === 'Ready';
                return true;
            });
        }

        // ✅ SIRALAMA: Önce yeni siparişler, sonra hazır olanlar
        filteredOrders.sort((a, b) => {
            // Durum önceliği: New > InProgress > Ready
            const statusPriority = { 'New': 1, 'InProgress': 2, 'Ready': 3 };
            const aPriority = statusPriority[a.status] || 4;
            const bPriority = statusPriority[b.status] || 4;

            if (aPriority !== bPriority) {
                return aPriority - bPriority;
            }

            // Aynı durumdaysa, zamana göre sırala (eski siparişler önce)
            return new Date(a.orderTime) - new Date(b.orderTime);
        });

        if (filteredOrders.length === 0) {
            this.showEmptyState();
            return;
        }

        // Boş state'i gizle
        const emptyState = document.getElementById('emptyState');
        if (emptyState) emptyState.style.display = 'none';

        // ✅ Smooth render - mevcut scroll pozisyonunu koru
        const currentScrollTop = container.scrollTop;
        container.innerHTML = filteredOrders.map(order => this.renderOrderCard(order)).join('');
        container.scrollTop = currentScrollTop;
    }

    // 🎨 SİPARİŞ KARTI RENDER (Düzeltildi)
    // 🎨 SİPARİŞ KARTI RENDER (Debug eklendi)
    renderOrderCard(order) {
        const statusClass = order.status.toLowerCase();
        const timeElapsed = this.getTimeElapsed(new Date(order.orderTime));
        const isNewOrder = order.isNew || false;

        // Debug için sipariş ID'sini kontrol et
        if (!order.orderBatchId) {
            console.error('❌ Sipariş ID eksik:', order);
            return '';
        }

        // Tamamlanan siparişler için kalan süreyi hesapla
        let readyTimeRemaining = '';
        if (order.status === 'Ready' && order.completedAt) {
            const completedTime = new Date(order.completedAt);
            const fiveMinutesAfter = new Date(completedTime.getTime() + (5 * 60 * 1000));
            const now = new Date();
            const remaining = Math.max(0, Math.ceil((fiveMinutesAfter - now) / 1000 / 60));

            if (remaining > 0) {
                readyTimeRemaining = `<span class="ready-countdown">🕒 ${remaining} dk sonra gizlenecek</span>`;
            } else {
                // ✅ 5 dakika geçmiş, bu sipariş artık gözükmemeli
                console.log('⏰ Sipariş süresi dolmuş:', order.tableName, order.orderBatchId);
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

    // 📊 İSTATİSTİKLERİ GÜNCELLE
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

    // 🔄 YÜKLENİYOR DURUMU (Düzeltildi)
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

    // 📭 BOŞ DURUM
    showEmptyState() {
        const emptyState = document.getElementById('emptyState');
        const container = document.getElementById('ordersContainer');

        if (container) container.innerHTML = '';
        if (emptyState) emptyState.style.display = 'block';
    }

    // 🔄 OTOMATİK YENİLEME (Düzeltildi - daha yumuşak)
    startAutoRefresh() {
        this.loadOrders();

        // ✅ 60 saniyede bir yenile (30 saniye çok sık)
        this.refreshInterval = setInterval(() => {
            console.log('🔄 Otomatik yenileme...');
            this.loadOrders(); // Artık dalgalanma yapmayacak
        }, 60000); // 60 saniye
    }

    async refreshOrders() {
        try {
            await this.loadOrders();
        } catch (error) {
            console.error('Auto refresh hatası:', error);
        }
    }

    // 🛠️ YARDIMCI METODLAR
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

    // SignalR event handlers
    handleNewOrder(orderData) {
        console.log('🔔 Yeni sipariş geldi:', orderData.TableName);
        this.loadOrders(); // Yeni sipariş geldiğinde listeyı yenile
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

    // Cleanup
    destroy() {
        if (this.refreshInterval) {
            clearInterval(this.refreshInterval);
            this.refreshInterval = null;
        }
    }
}

// Global access
window.TabletDashboard = TabletDashboard;

// Global functions for HTML onclick events
TabletDashboard.showOrderDetails = function (orderId) {
    console.log('🔍 showOrderDetails çağrıldı:', orderId);
    if (window.TabletDashboard) {
        window.TabletDashboard.loadOrderDetails(orderId);
    }
};

TabletDashboard.markAsReady = function (orderId) {
    console.log('🔍 markAsReady çağrıldı:', orderId);
    if (window.TabletDashboard) {
        window.TabletDashboard.markOrderAsReadyDirect(orderId);
    }
};