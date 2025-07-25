// KufeArtFullAdission.GarsonMvc/wwwroot/js/order-page.js
class OrderPage {
    constructor() {
        this.tableData = window.orderPageData;
        this.products = [];
        this.categories = [];
        this.cart = [];
        this.currentCategory = 'all';
        this.searchQuery = '';

        this.init();
    }

    init() {
        this.loadTableDetails();
        this.loadProducts();
        this.bindEvents();
        this.startPeriodicUpdates();
    }

    bindEvents() {
        // Header actions
        document.getElementById('showHistoryBtn').addEventListener('click', () => {
            this.showOrderHistory();
        });

        document.getElementById('refreshTableBtn').addEventListener('click', () => {
            this.refreshTableData();
        });

        // Search functionality
        const searchInput = document.getElementById('productSearch');
        searchInput.addEventListener('input', (e) => {
            this.searchQuery = e.target.value.toLowerCase();
            this.filterProducts();
            this.toggleClearButton();
        });

        document.getElementById('clearSearch').addEventListener('click', () => {
            searchInput.value = '';
            this.searchQuery = '';
            this.filterProducts();
            this.toggleClearButton();
        });

        // Cart events
        document.getElementById('openCartBtn').addEventListener('click', () => {
            this.openCartModal();
        });

        document.getElementById('closeCartBtn').addEventListener('click', () => {
            this.closeCartModal();
        });

        document.getElementById('cartOverlay').addEventListener('click', () => {
            this.closeCartModal();
        });

        document.getElementById('submitOrderBtn').addEventListener('click', () => {
            this.submitOrder();
        });

        // History modal events
        document.getElementById('closeHistoryBtn').addEventListener('click', () => {
            this.closeHistoryModal();
        });

        document.getElementById('historyOverlay').addEventListener('click', () => {
            this.closeHistoryModal();
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.closeAllModals();
            }
            if (e.key === 'Enter' && e.ctrlKey) {
                if (this.cart.length > 0) {
                    this.submitOrder();
                }
            }
        });

        // Touch events for better mobile experience
        this.bindTouchEvents();
    }

    async loadTableDetails() {
        try {
            const response = await fetch(`/Order/GetTableDetails?tableId=${this.tableData.tableId}`);
            const result = await response.json();

            if (result.success) {
                this.updateTableUI(result.data);
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            this.showError('Masa bilgileri yüklenemedi!');
            console.error('Load table details error:', error);
        }
    }

    async loadProducts() {
        try {
            const response = await fetch('/Order/GetProducts');
            const result = await response.json();

            if (result.success) {
                this.products = result.data.allProducts;
                this.categories = result.data.categories;
                this.renderCategories();
                this.renderProducts();
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            this.showError('Ürünler yüklenemedi!');
            console.error('Load products error:', error);
        }
    }

    updateTableUI(tableData) {
        if (tableData.isOccupied) {
            document.getElementById('tableSummary').style.display = 'block';
            document.getElementById('tableTotal').textContent = this.formatCurrency(tableData.totalAmount);

            if (tableData.openedAt) {
                const openTime = new Date(tableData.openedAt);
                document.getElementById('tableOpenTime').textContent = this.formatTime(openTime);

                // Süre hesaplama
                const duration = Math.floor((Date.now() - openTime.getTime()) / 60000);
                document.getElementById('tableDuration').textContent = this.formatDuration(duration);
            }

            // Status güncelle
            const statusBadge = document.querySelector('.status-badge');
            statusBadge.className = 'status-badge occupied';
            statusBadge.innerHTML = '<i class="fas fa-clock"></i> Aktif Masa';
        }
    }

    renderCategories() {
        const container = document.getElementById('categoryTabs');

        let html = `
            <button class="category-tab ${this.currentCategory === 'all' ? 'active' : ''}" 
                    data-category="all">
                <i class="fas fa-th-large"></i>
                Tümü (${this.products.length})
            </button>
        `;

        this.categories.forEach(category => {
            html += `
                <button class="category-tab ${this.currentCategory === category.name ? 'active' : ''}" 
                        data-category="${category.name}">
                    <i class="fas fa-utensils"></i>
                    ${category.name} (${category.count})
                </button>
            `;
        });

        container.innerHTML = html;

        // Category click events
        container.addEventListener('click', (e) => {
            if (e.target.classList.contains('category-tab') || e.target.closest('.category-tab')) {
                const tab = e.target.closest('.category-tab');
                const category = tab.dataset.category;
                this.switchCategory(category);
            }
        });
    }

    renderProducts() {
        const container = document.getElementById('productsContainer');
        const filteredProducts = this.getFilteredProducts();

        if (filteredProducts.length === 0) {
            container.innerHTML = `
                <div class="no-products">
                    <i class="fas fa-search fa-3x"></i>
                    <p>Aradığınız kriterde ürün bulunamadı</p>
                </div>
            `;
            return;
        }

        let html = '<div class="products-grid">';

        filteredProducts.forEach(product => {
            html += this.renderProductCard(product);
        });

        html += '</div>';
        container.innerHTML = html;

        // Product click events
        this.bindProductEvents();
    }

    renderProductCard(product) {
        const campaignBadge = product.hasCampaign ?
            `<div class="campaign-badge" title="${product.campaignCaption}">🎁</div>` : '';

        return `
            <div class="product-card ${product.hasCampaign ? 'has-campaign' : ''}" 
                 data-product-id="${product.id}">
                ${campaignBadge}
                <div class="product-image">
                    <i class="fas fa-utensils"></i>
                </div>
                <div class="product-name">${product.name}</div>
                <div class="product-price">${this.formatCurrency(product.price)}</div>
                ${product.description ? `<div class="product-description">${product.description}</div>` : ''}
                <button class="add-to-cart-btn" data-product-id="${product.id}">
                    <i class="fas fa-plus me-1"></i>
                    Sepete Ekle
                </button>
            </div>
        `;
    }

    bindProductEvents() {
        const addButtons = document.querySelectorAll('.add-to-cart-btn');

        addButtons.forEach(button => {
            button.addEventListener('click', (e) => {
                e.stopPropagation();
                const productId = button.dataset.productId;
                this.addToCart(productId);

                // Visual feedback
                button.style.transform = 'scale(0.95)';
                setTimeout(() => {
                    button.style.transform = '';
                }, 150);

                // Haptic feedback
                if ('vibrate' in navigator) {
                    navigator.vibrate(30);
                }
            });
        });

        // Product card click for details (optional)
        const productCards = document.querySelectorAll('.product-card');
        productCards.forEach(card => {
            card.addEventListener('click', (e) => {
                if (!e.target.classList.contains('add-to-cart-btn')) {
                    const productId = card.dataset.productId;
                    this.showProductDetails(productId);
                }
            });
        });
    }

    addToCart(productId) {
        const product = this.products.find(p => p.id === productId);
        if (!product) return;

        const existingItem = this.cart.find(item => item.productId === productId);

        if (existingItem) {
            existingItem.quantity += 1;
        } else {
            this.cart.push({
                productId: productId,
                name: product.name,
                price: product.price,
                quantity: 1
            });
        }

        this.updateCartUI();
        this.showToast(`${product.name} sepete eklendi`, 'success');
    }

    removeFromCart(productId) {
        const itemIndex = this.cart.findIndex(item => item.productId === productId);
        if (itemIndex > -1) {
            const item = this.cart[itemIndex];
            if (item.quantity > 1) {
                item.quantity -= 1;
            } else {
                this.cart.splice(itemIndex, 1);
            }
            this.updateCartUI();
        }
    }

    updateCartQuantity(productId, quantity) {
        const item = this.cart.find(item => item.productId === productId);
        if (item) {
            if (quantity <= 0) {
                this.removeFromCart(productId);
            } else {
                item.quantity = quantity;
                this.updateCartUI();
            }
        }
    }

    updateCartUI() {
        const totalItems = this.cart.reduce((sum, item) => sum + item.quantity, 0);
        const totalAmount = this.cart.reduce((sum, item) => sum + (item.price * item.quantity), 0);

        // Floating cart
        const floatingCart = document.getElementById('floatingCart');
        if (totalItems > 0) {
            floatingCart.style.display = 'flex';
            document.getElementById('cartCount').textContent = `${totalItems} ürün`;
            document.getElementById('cartFloatingTotal').textContent = this.formatCurrency(totalAmount);
        } else {
            floatingCart.style.display = 'none';
        }

        // Cart modal
        this.renderCartItems();

        // Submit button
        const submitBtn = document.getElementById('submitOrderBtn');
        submitBtn.disabled = totalItems === 0;
    }

    renderCartItems() {
        const container = document.getElementById('cartItems');
        const totalAmount = this.cart.reduce((sum, item) => sum + (item.price * item.quantity), 0);

        if (this.cart.length === 0) {
            container.innerHTML = `
                <div class="empty-cart">
                    <i class="fas fa-shopping-cart fa-3x"></i>
                    <p>Sepet boş</p>
                    <small>Ürün seçerek sepete ekleyin</small>
                </div>
            `;
        } else {
            let html = '';
            this.cart.forEach(item => {
                html += `
                    <div class="cart-item" data-product-id="${item.productId}">
                        <div class="item-info">
                            <div class="item-name">${item.name}</div>
                            <div class="item-price">${this.formatCurrency(item.price)} x ${item.quantity}</div>
                        </div>
                        <div class="item-controls">
                            <button class="qty-btn minus-btn" data-product-id="${item.productId}">
                                <i class="fas fa-minus"></i>
                            </button>
                            <span class="qty-display">${item.quantity}</span>
                            <button class="qty-btn plus-btn" data-product-id="${item.productId}">
                                <i class="fas fa-plus"></i>
                            </button>
                            <div class="item-total">${this.formatCurrency(item.price * item.quantity)}</div>
                        </div>
                    </div>
                `;
            });
            container.innerHTML = html;

            // Bind quantity controls
            this.bindCartItemEvents();
        }

        // Update total
        document.getElementById('cartTotalAmount').textContent = this.formatCurrency(totalAmount);
    }

    bindCartItemEvents() {
        const minusButtons = document.querySelectorAll('.minus-btn');
        const plusButtons = document.querySelectorAll('.plus-btn');

        minusButtons.forEach(btn => {
            btn.addEventListener('click', () => {
                const productId = btn.dataset.productId;
                const item = this.cart.find(i => i.productId === productId);
                if (item && item.quantity > 1) {
                    this.updateCartQuantity(productId, item.quantity - 1);
                } else {
                    this.removeFromCart(productId);
                }
            });
        });

        plusButtons.forEach(btn => {
            btn.addEventListener('click', () => {
                const productId = btn.dataset.productId;
                const item = this.cart.find(i => i.productId === productId);
                if (item) {
                    this.updateCartQuantity(productId, item.quantity + 1);
                }
            });
        });
    }

    async submitOrder() {
        if (this.cart.length === 0) {
            this.showError('Sepet boş!');
            return;
        }

        const submitBtn = document.getElementById('submitOrderBtn');
        const originalText = submitBtn.innerHTML;

        // Loading state
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Gönderiliyor...';

        try {
            const orderData = {
                tableId: this.tableData.tableId,
                waiterNote: document.getElementById('orderNote').value.trim(),
                items: this.cart.map(item => ({
                    productId: item.productId,
                    quantity: item.quantity,
                    price: item.price
                }))
            };

            const response = await fetch('/Order/SubmitOrder', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(orderData)
            });

            const result = await response.json();

            if (result.success) {
                this.showToast(result.message, 'success');
                this.clearCart();
                this.closeCartModal();
                this.refreshTableData();

                // Haptic feedback
                if ('vibrate' in navigator) {
                    navigator.vibrate([100, 50, 100]);
                }
            } else {
                this.showError(result.message);
            }
        } catch (error) {
            this.showError('Sipariş gönderilemedi! Bağlantı hatası.');
            console.error('Submit order error:', error);
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;
        }
    }

    async showOrderHistory() {
        const modal = document.getElementById('historyModal');
        const content = document.getElementById('historyContent');

        modal.style.display = 'block';

        try {
            const response = await fetch(`/Order/GetOrderHistory?tableId=${this.tableData.tableId}`);
            const result = await response.json();

            if (result.success) {
                this.renderOrderHistory(result.data);
            } else {
                content.innerHTML = `
                    <div class="history-error">
                        <i class="fas fa-exclamation-triangle"></i>
                        <p>${result.message}</p>
                    </div>
                `;
            }
        } catch (error) {
            content.innerHTML = `
                <div class="history-error">
                    <i class="fas fa-wifi"></i>
                    <p>Bağlantı hatası!</p>
                </div>
            `;
        }
    }

    renderOrderHistory(orders) {
        const content = document.getElementById('historyContent');

        if (orders.length === 0) {
            content.innerHTML = `
                <div class="no-history">
                    <i class="fas fa-history fa-3x"></i>
                    <p>Henüz sipariş geçmişi yok</p>
                </div>
            `;
            return;
        }

        let html = '<div class="history-items">';
        orders.forEach(order => {
            html += `
                <div class="history-item">
                    <div class="history-header">
                        <span class="order-time">${order.formattedTime}</span>
                        <span class="order-waiter">${order.personFullName}</span>
                    </div>
                    <div class="history-details">
                        <div class="product-name">${order.productName}</div>
                        <div class="order-info">
                            ${order.productQuantity} x ${this.formatCurrency(order.productPrice)} = 
                            <strong>${this.formatCurrency(order.totalPrice)}</strong>
                        </div>
                        ${order.shorLabel ? `<div class="order-note">${order.shorLabel}</div>` : ''}
                    </div>
                </div>
            `;
        });
        html += '</div>';

        content.innerHTML = html;
    }

    // Utility methods
    switchCategory(category) {
        this.currentCategory = category;

        // Update active tab
        document.querySelectorAll('.category-tab').forEach(tab => {
            tab.classList.remove('active');
        });
        document.querySelector(`[data-category="${category}"]`).classList.add('active');

        this.renderProducts();
    }

    getFilteredProducts() {
        let filtered = this.products;

        // Category filter
        if (this.currentCategory !== 'all') {
            filtered = filtered.filter(p => p.categoryName === this.currentCategory);
        }

        // Search filter
        if (this.searchQuery) {
            filtered = filtered.filter(p =>
                p.name.toLowerCase().includes(this.searchQuery) ||
                (p.description && p.description.toLowerCase().includes(this.searchQuery))
            );
        }

        return filtered;
    }

    filterProducts() {
        this.renderProducts();
    }

    toggleClearButton() {
        const clearBtn = document.getElementById('clearSearch');
        clearBtn.style.display = this.searchQuery ? 'block' : 'none';
    }

    openCartModal() {
        document.getElementById('cartModal').style.display = 'block';
        document.body.style.overflow = 'hidden';
    }

    closeCartModal() {
        document.getElementById('cartModal').style.display = 'none';
        document.body.style.overflow = '';
    }

    closeHistoryModal() {
        document.getElementById('historyModal').style.display = 'none';
    }

    closeAllModals() {
        this.closeCartModal();
        this.closeHistoryModal();
    }

    clearCart() {
        this.cart = [];
        this.updateCartUI();
        document.getElementById('orderNote').value = '';
    }

    refreshTableData() {
        this.loadTableDetails();

        // Animate refresh button
        const btn = document.getElementById('refreshTableBtn');
        btn.style.transform = 'rotate(360deg)';
        setTimeout(() => {
            btn.style.transform = '';
        }, 500);
    }

    startPeriodicUpdates() {
        // Her 2 dakikada bir masa durumunu güncelle
        setInterval(() => {
            this.loadTableDetails();
        }, 120000);
    }

    bindTouchEvents() {
        // Prevent double-tap zoom
        let lastTouchEnd = 0;
        document.addEventListener('touchend', (event) => {
            const now = (new Date()).getTime();
            if (now - lastTouchEnd <= 300) {
                event.preventDefault();
            }
            lastTouchEnd = now;
        }, false);

        // Pull to refresh products
        let startY = 0;
        let isDragging = false;

        const productsContainer = document.getElementById('productsContainer');

        productsContainer.addEventListener('touchstart', (e) => {
            startY = e.touches[0].clientY;
            isDragging = true;
        });

        productsContainer.addEventListener('touchmove', (e) => {
            if (!isDragging || productsContainer.scrollTop > 0) return;

            const currentY = e.touches[0].clientY;
            const diff = currentY - startY;

            if (diff > 100) {
                this.loadProducts();
                isDragging = false;
            }
        });

        productsContainer.addEventListener('touchend', () => {
            isDragging = false;
        });
    }

    // Helper methods
    formatCurrency(amount) {
        return new Intl.NumberFormat('tr-TR', {
            style: 'currency',
            currency: 'TRY',
            minimumFractionDigits: 0,
            maximumFractionDigits: 2
        }).format(amount);
    }

    formatTime(date) {
        return date.toLocaleTimeString('tr-TR', {
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    formatDuration(minutes) {
        if (minutes < 60) {
            return `${Math.floor(minutes)} dk`;
        } else {
            const hours = Math.floor(minutes / 60);
            const mins = Math.floor(minutes % 60);
            return `${hours}s ${mins}dk`;
        }
    }

    showToast(message, type = 'info') {
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.innerHTML = `
            <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-triangle'}"></i>
            ${message}
        `;

        document.body.appendChild(toast);

        setTimeout(() => {
            toast.classList.add('show');
        }, 100);

        setTimeout(() => {
            toast.classList.remove('show');
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    showError(message) {
        this.showToast(message, 'error');
    }

    showProductDetails(productId) {
        const product = this.products.find(p => p.id === productId);
        if (product) {
            // Simple product details modal - implement if needed
            console.log('Product details:', product);
        }
    }
}

// Toast CSS
const toastStyles = `
.toast {
    position: fixed;
    top: 90px;
    left: 50%;
    transform: translateX(-50%) translateY(-100%);
    background: var(--white);
    color: var(--dark);
    padding: 15px 20px;
    border-radius: 10px;
    box-shadow: 0 8px 30px rgba(0,0,0,0.2);
    z-index: 10000;
    display: flex;
    align-items: center;
    gap: 10px;
    transition: transform 0.3s ease;
    max-width: 90%;
}

.toast.show {
    transform: translateX(-50%) translateY(0);
}

.toast-success {
    border-left: 4px solid var(--success);
}

.toast-error {
    border-left: 4px solid var(--danger);
}

.toast i {
    font-size: 1.2rem;
}

.toast-success i {
    color: var(--success);
}

.toast-error i {
    color: var(--danger);
}
`;

// Add toast styles to head
const styleSheet = document.createElement('style');
styleSheet.textContent = toastStyles;
document.head.appendChild(styleSheet);

// Initialize Order Page
document.addEventListener('DOMContentLoaded', () => {
    new OrderPage();
});