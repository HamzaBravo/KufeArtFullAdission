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
        try {
            console.log('🔍 Modal açılıyor:', orderId);

            // Sipariş verilerini bul
            const order = this.orders.find(o => o.orderBatchId === orderId);
            if (!order) {
                TabletUtils.showToast('Sipariş bulunamadı!', 'error');
                return;
            }

            // Modal HTML'ini oluştur
            const modalHTML = this.createOrderDetailModal(order);

            // Mevcut modalı kaldır ve yenisini ekle
            const existingModal = document.getElementById('orderDetailModal');
            if (existingModal) existingModal.remove();

            document.body.insertAdjacentHTML('beforeend', modalHTML);

            // Modal'ı aç
            const modal = new bootstrap.Modal(document.getElementById('orderDetailModal'));
            modal.show();

            // Modal kapanma eventi
            document.getElementById('orderDetailModal').addEventListener('hidden.bs.modal', () => {
                document.getElementById('orderDetailModal').remove();
            });

        } catch (error) {
            console.error('Modal açılamadı:', error);
            TabletUtils.showToast('Detay yüklenemedi!', 'error');
        }
    }

    createOrderDetailModal(order) {
        const statusClass = order.status.toLowerCase();
        const timeElapsed = this.getTimeElapsed(new Date(order.orderTime));

        return `
        <div class="modal fade" id="orderDetailModal" tabindex="-1" data-bs-backdrop="static">
            <div class="modal-dialog modal-xl">
                <div class="modal-content order-detail-modal">
                    
                    <!-- 🎯 MODAL HEADER -->
                    <div class="modal-header order-detail-header">
                        <div class="header-content">
                            <div class="table-info-large">
                                <h2 class="table-title">
                                    <i class="fas fa-utensils"></i> ${order.tableName}
                                </h2>
                                <div class="order-meta-large">
                                    <span class="waiter-badge">
                                        <i class="fas fa-user"></i> ${order.waiterName}
                                    </span>
                                    <span class="time-badge">
                                        <i class="fas fa-clock"></i> ${TabletUtils.formatTime(new Date(order.orderTime))}
                                    </span>
                                    <span class="elapsed-badge">
                                        <i class="fas fa-hourglass-half"></i> ${timeElapsed}
                                    </span>
                                </div>
                            </div>
                            
                            <div class="status-info-large">
                                <div class="status-badge-large status-${statusClass}">
                                    ${this.getStatusIcon(order.status)} ${this.getStatusText(order.status)}
                                </div>
                                <div class="total-amount-large">
                                    ${TabletUtils.formatCurrency(order.totalAmount)}
                                </div>
                            </div>
                        </div>
                        
                        <button type="button" class="btn-close-custom" data-bs-dismiss="modal">
                            <i class="fas fa-times"></i>
                        </button>
                    </div>

                    <!-- 📋 MODAL BODY -->
                    <div class="modal-body order-detail-body">
                        
                        <!-- Sipariş Notu -->
                        ${order.note ? `
                            <div class="order-note-section">
                                <h5><i class="fas fa-sticky-note"></i> Sipariş Notu</h5>
                                <div class="note-content">${order.note}</div>
                            </div>
                        ` : ''}
                        
                        <!-- Ürün Listesi -->
                        <div class="products-section">
                            <h5><i class="fas fa-list"></i> Sipariş Detayları (${order.items.length} ürün)</h5>
                            <div class="products-grid">
                                ${order.items.map((item, index) => `
                                    <div class="product-card-detail" style="animation-delay: ${index * 0.1}s">
                                        <div class="product-icon">
                                            <i class="fas fa-utensils"></i>
                                        </div>
                                        <div class="product-info-detail">
                                            <h6 class="product-name-detail">${item.productName}</h6>
                                            <span class="product-category-detail">${item.categoryName || 'Genel'}</span>
                                        </div>
                                        <div class="product-pricing">
                                            <div class="product-quantity-large">x${item.quantity}</div>
                                            <div class="product-price-detail">${TabletUtils.formatCurrency(item.price * item.quantity)}</div>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                        </div>
                        
                        <!-- Özet Bilgiler -->
                        <div class="summary-section">
                            <div class="summary-row">
                                <span>Toplam Ürün:</span>
                                <strong>${order.items.reduce((sum, item) => sum + item.quantity, 0)} adet</strong>
                            </div>
                            <div class="summary-row">
                                <span>Ürün Çeşidi:</span>
                                <strong>${order.items.length} çeşit</strong>
                            </div>
                            <div class="summary-row total-row">
                                <span>Toplam Tutar:</span>
                                <strong class="total-price">${TabletUtils.formatCurrency(order.totalAmount)}</strong>
                            </div>
                        </div>
                    </div>

                    <!-- 🚀 MODAL FOOTER -->
                    <div class="modal-footer order-detail-footer">
                        <button type="button" class="btn-secondary-large" data-bs-dismiss="modal">
                            <i class="fas fa-arrow-left"></i> Kapat
                        </button>
                        
                        ${order.status !== 'Ready' ? `
                            <button type="button" class="btn-primary-large" 
                                    onclick="TabletDashboard.markAsReady('${order.orderBatchId}'); 
                                             bootstrap.Modal.getInstance(document.getElementById('orderDetailModal')).hide();">
                                <i class="fas fa-check-circle"></i> Siparişi Hazır Olarak İşaretle
                            </button>
                        ` : `
                            <div class="completed-badge-large">
                                <i class="fas fa-check-circle"></i> Sipariş Hazırlandı
                            </div>
                        `}
                    </div>
                    
                </div>
            </div>
        </div>
    `;
    }

    async markOrderAsReadyDirect(orderId) {
        console.log('🔍 Direkt hazır işaretleme:', orderId);

        // Sipariş bilgisini bul
        const order = this.orders.find(o => o.orderBatchId === orderId);
        if (!order) {
            TabletUtils.showToast('Sipariş bulunamadı!', 'error');
            return;
        }

        // ✅ Şık onay modalı oluştur
        this.showReadyConfirmModal(order);
    }

    showReadyConfirmModal(order) {
        const modalHTML = `
        <div class="modal fade" id="readyConfirmModal" tabindex="-1" data-bs-backdrop="static">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content ready-confirm-modal">
                    
                    <!-- Header -->
                    <div class="modal-header ready-confirm-header">
                        <div class="confirm-icon">
                            <i class="fas fa-check-circle"></i>
                        </div>
                        <div class="confirm-text">
                            <h4>Siparişi Hazır İşaretle</h4>
                            <p>Bu işlem geri alınamaz</p>
                        </div>
                    </div>

                    <!-- Body -->
                    <div class="modal-body ready-confirm-body">
                        <div class="order-summary-confirm">
                            <div class="table-info-confirm">
                                <h5><i class="fas fa-utensils"></i> ${order.tableName}</h5>
                                <span><i class="fas fa-user"></i> ${order.waiterName}</span>
                            </div>
                            
                            <div class="items-summary">
                                <div class="summary-item">
                                    <span>Ürün Sayısı:</span>
                                    <strong>${order.items.reduce((sum, item) => sum + item.quantity, 0)} adet</strong>
                                </div>
                                <div class="summary-item">
                                    <span>Toplam Tutar:</span>
                                    <strong class="amount">${TabletUtils.formatCurrency(order.totalAmount)}</strong>
                                </div>
                            </div>
                            
                            <div class="products-preview">
                                ${order.items.slice(0, 3).map(item => `
                                    <div class="preview-item">
                                        <span>${item.productName}</span>
                                        <span class="qty">x${item.quantity}</span>
                                    </div>
                                `).join('')}
                                ${order.items.length > 3 ? `
                                    <div class="more-products">+${order.items.length - 3} ürün daha</div>
                                ` : ''}
                            </div>
                        </div>
                    </div>

                    <!-- Footer -->
                    <div class="modal-footer ready-confirm-footer">
                        <button type="button" class="btn-cancel" data-bs-dismiss="modal">
                            <i class="fas fa-times"></i> İptal
                        </button>
                        <button type="button" class="btn-confirm" id="confirmReadyBtn" data-order-id="${order.orderBatchId}">
                            <i class="fas fa-check-circle"></i> Evet, Hazır İşaretle
                        </button>
                    </div>
                    
                </div>
            </div>
        </div>
    `;

        // Mevcut modalı kaldır ve yenisini ekle
        const existingModal = document.getElementById('readyConfirmModal');
        if (existingModal) existingModal.remove();

        document.body.insertAdjacentHTML('beforeend', modalHTML);

        // ✅ Event listener ile çöz
        document.getElementById('confirmReadyBtn').addEventListener('click', () => {
            const orderId = document.getElementById('confirmReadyBtn').dataset.orderId;
            this.confirmReadyOrder(orderId);
        });

        // Modal'ı aç
        const modal = new bootstrap.Modal(document.getElementById('readyConfirmModal'));
        modal.show();

        // Modal kapanma eventi
        document.getElementById('readyConfirmModal').addEventListener('hidden.bs.modal', () => {
            document.getElementById('readyConfirmModal').remove();
        });
    }

    // ✅ YENİ: Onay sonrası işlem
    async confirmReadyOrder(orderId) {
        try {
            // Modal'ı kapat
            const modal = bootstrap.Modal.getInstance(document.getElementById('readyConfirmModal'));
            if (modal) modal.hide();

            // Loading toast göster
            TabletUtils.showToast('Sipariş hazır olarak işaretleniyor...', 'info', 2000);

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
                // Success animasyonu
                this.showSuccessEffect();

                TabletUtils.showToast('✅ Sipariş hazır olarak işaretlendi!', 'success');
                await this.loadOrders(); // Listeyi yenile
            } else {
                throw new Error(result.message || 'Sipariş güncellenemedi');
            }

        } catch (error) {
            console.error('Hazır işaretleme hatası:', error);
            TabletUtils.showToast('❌ Sipariş güncellenemedi: ' + error.message, 'error');
        }
    }

    // ✅ YENİ: Success efekti
    showSuccessEffect() {
        // Yeşil konfeti efekti
        this.createSuccessConfetti();

        // Stats kartlarını yeşil highlight et
        document.querySelectorAll('.stat-card.completed').forEach(card => {
            card.classList.add('success-ripple');
            setTimeout(() => card.classList.remove('success-ripple'), 1500);
        });
    }

    createSuccessConfetti() {
        const container = document.createElement('div');
        container.className = 'confetti-container';
        document.body.appendChild(container);

        // Sadece yeşil renkler
        const colors = ['green', 'green', 'green'];

        for (let i = 0; i < 20; i++) {
            setTimeout(() => {
                const confetti = document.createElement('div');
                confetti.className = `confetti confetti-${colors[Math.floor(Math.random() * colors.length)]} confetti-fall`;
                confetti.style.left = Math.random() * 100 + '%';
                confetti.style.animationDelay = Math.random() * 0.3 + 's';
                confetti.style.animationDuration = (Math.random() * 1 + 1.5) + 's';
                container.appendChild(confetti);

                setTimeout(() => confetti.remove(), 2000);
            }, i * 30);
        }

        setTimeout(() => container.remove(), 3000);
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

    renderOrderCard(order) {
        const statusClass = order.status.toLowerCase();
        const timeElapsed = this.getTimeElapsed(new Date(order.orderTime));
        const isNewOrder = order.isNew || false;

        return `
        <div class="order-row status-${statusClass} ${isNewOrder ? 'new-order-effect' : ''}" 
             data-order-id="${order.orderBatchId}">
            
            <!-- Sol: Masa & Garson Bilgisi -->
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
                </div>
            </div>

            <!-- Orta: Ürün Listesi -->
            <div class="order-products">
                ${order.items.slice(0, 3).map(item => `
                    <div class="product-item">
                        <span class="product-name">${item.productName}</span>
                        <span class="product-quantity">x${item.quantity}</span>
                    </div>
                `).join('')}
                ${order.items.length > 3 ? `<div class="more-items">+${order.items.length - 3} ürün daha</div>` : ''}
            </div>

            <!-- Sağ: Durum & Zaman -->
            <div class="order-status-section">
                <div class="status-badge status-${statusClass}">
                    ${this.getStatusIcon(order.status)} ${this.getStatusText(order.status)}
                </div>
                <div class="time-info">
                    <div class="order-time">${TabletUtils.formatTime(new Date(order.orderTime))}</div>
                    <div class="elapsed-time">${timeElapsed}</div>
                </div>
                <div class="order-total">${TabletUtils.formatCurrency(order.totalAmount)}</div>
            </div>

            <!-- Aksiyonlar -->
            <div class="order-actions">
                <button class="btn-detail" onclick="TabletDashboard.showOrderDetails('${order.orderBatchId}')">
                    <i class="fas fa-eye"></i> Detay
                </button>
                ${order.status !== 'Ready' ? `
                    <button class="btn-ready" onclick="TabletDashboard.markAsReady('${order.orderBatchId}')">
                        <i class="fas fa-check"></i> Hazır
                    </button>
                ` : `
                    <span class="btn-completed">
                        <i class="fas fa-check-circle"></i> Tamamlandı
                    </span>
                `}
            </div>
        </div>
    `;
    }

    // ✅ bindOrderEvents method'unu da güncelleyin:
    bindOrderEvents() {
        // Order row click events (sadece detay için)
        document.querySelectorAll('.order-row').forEach(row => {
            row.addEventListener('click', (e) => {
                // Eğer button'a tıklanmışsa ignore et
                if (e.target.closest('.btn-detail') || e.target.closest('.btn-ready')) return;

                const orderId = row.dataset.orderId;
                this.showOrderDetailsModal(orderId);
            });
        });
    }

    renderOrders() {
        const container = document.getElementById('ordersContainer');
        const filteredOrders = this.getFilteredOrders();

        if (filteredOrders.length === 0) {
            this.showEmptyState();
            return;
        }

        // ✅ YENİ SİPARİŞLER ÜSTTE: Sıralama değiştir
        const sortedOrders = filteredOrders.sort((a, b) => {
            // İlk önce yeni siparişler
            if (a.isNew && !b.isNew) return -1;
            if (!a.isNew && b.isNew) return 1;

            // Sonra duruma göre: New -> Preparing -> Ready
            const statusOrder = { 'New': 1, 'Preparing': 2, 'Ready': 3 };
            const statusDiff = statusOrder[a.status] - statusOrder[b.status];
            if (statusDiff !== 0) return statusDiff;

            // Son olarak zamana göre (yeni olanlar üstte)
            return new Date(b.orderTime) - new Date(a.orderTime);
        });

        const ordersHTML = sortedOrders.map(order => this.renderOrderCard(order)).join('');
        container.innerHTML = ordersHTML;

        this.bindOrderEvents();
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
        this.loadOrders();
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