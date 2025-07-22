// wwwroot/js/order-manager.js
window.OrderManager = {

    startNewOrder: function (tableId) {
        console.log('🔍 startNewOrder called with tableId:', tableId); // Debug

        if (!tableId) {
            ToastHelper.error('Masa ID bulunamadı!');
            return;
        }

        const tableName = $('#modalTableName').text();
        App.currentTableId = tableId; // ← Bu önemli

        console.log('🔍 App.currentTableId set to:', App.currentTableId); // Debug

        // Masa modal'ını kapat
        $('#tableModal').modal('hide');

        // Ürün seçim modal'ını aç
        OrderManager.openProductSelectionModal(tableId, tableName);
    },

    addNewOrder: function (tableId) {
        console.log('🔍 addNewOrder called with tableId:', tableId); // Debug

        if (!tableId) {
            ToastHelper.error('Masa ID bulunamadı!');
            return;
        }

        const tableName = $('#modalTableName').text();
        App.currentTableId = tableId; // ← Bu da önemli

        $('#tableModal').modal('hide');
        OrderManager.openProductSelectionModal(tableId, tableName);
    },

    openProductSelectionModal: function (tableId, tableName) {
        App.currentTableId = tableId;

        // Modal başlığını ayarla
        $('#productModalTableName').text(`${tableName} - Sipariş Al`);

        // Modal'ı aç
        const modal = new bootstrap.Modal(document.getElementById('productSelectionModal'));
        modal.show();

        // Sepeti temizle
        OrderManager.clearCart();

        // Ürünleri yükle
        OrderManager.loadProducts();
    },

    loadProducts: function () {
        $.ajax({
            url: App.endpoints.getProducts,
            method: 'GET',
            success: function (response) {
                if (response.success) {
                    App.currentProducts = response.data.allProducts;
                    OrderManager.renderProductTabs(response.data.categories);
                    OrderManager.renderProducts(App.currentProducts, 'allProductsGrid');
                } else {
                    OrderManager.showError('Ürünler yüklenemedi: ' + response.message);
                }
            },
            error: function () {
                OrderManager.showError('Bağlantı hatası! Ürünler yüklenemedi.');
            }
        });
    },

    renderProductTabs: function (categories) {
        const tabsContainer = $('#productCategoryTabs');
        const contentContainer = $('#productTabContent');

        // Tümü tab'ı hariç diğerlerini temizle
        tabsContainer.find('li:not(:first)').remove();
        contentContainer.find('.tab-pane:not(:first)').remove();

        // Her kategori için tab ekle
        categories.forEach((category, index) => {
            const categoryId = category.name.replace(/\s+/g, '').toLowerCase();
            const tabId = `${categoryId}-tab`;
            const contentId = `${categoryId}-products`;

            // Tab button
            const tabHtml = `
                <li class="nav-item" role="presentation">
                    <button class="nav-link" id="${tabId}" data-bs-toggle="pill"
                            data-bs-target="#${contentId}" type="button" role="tab">
                        <i class="fas fa-tag me-2"></i>${category.name}
                        <span class="badge bg-secondary ms-2">${category.count}</span>
                    </button>
                </li>
            `;
            tabsContainer.append(tabHtml);

            // Tab content
            const contentHtml = `
                <div class="tab-pane fade" id="${contentId}">
                    <div id="${categoryId}ProductsGrid" class="row g-3">
                        <!-- Ürünler buraya gelecek -->
                    </div>
                </div>
            `;
            contentContainer.append(contentHtml);

            // Tab tıklama event'i - event delegation kullan
            $(`#${tabId}`).off('click.categoryTab').on('click.categoryTab', function () {
                OrderManager.renderProducts(category.products, `${categoryId}ProductsGrid`);
            });
        });
    },

    renderProducts: function (products, containerId) {
        const container = $(`#${containerId}`);

        if (products.length === 0) {
            container.html(`
                <div class="col-12 text-center py-5">
                    <i class="fas fa-search fa-3x text-muted mb-3 opacity-25"></i>
                    <h6 class="text-muted">Ürün bulunamadı</h6>
                </div>
            `);
            return;
        }

        let html = '';
        products.forEach(product => {
            html += `
                <div class="col-lg-4 col-md-6">
                    <div class="card h-100 shadow-sm product-card" data-product-id="${product.id}">
                        <div class="card-body">
                            <div class="d-flex justify-content-between align-items-start mb-2">
                                <h6 class="card-title mb-0 fw-bold">${product.name}</h6>
                                ${product.hasCampaign ? `<span class="badge bg-warning text-dark">${product.campaignCaption}</span>` : ''}
                            </div>

                            ${product.description ? `<p class="card-text small text-muted mb-2">${product.description}</p>` : ''}

                            <div class="d-flex justify-content-between align-items-center">
                                <span class="h6 text-success mb-0">₺${product.price.toFixed(2)}</span>
                                <button class="btn btn-primary btn-sm" onclick="OrderManager.addToCart('${product.id}')">
                                    <i class="fas fa-plus me-1"></i>Ekle
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        });

        container.html(html);
    },

    addToCart: function (productId) {
        const product = App.currentProducts.find(p => p.id === productId);
        if (!product) return;

        const existingItem = App.cartItems.find(item => item.productId === productId);

        if (existingItem) {
            existingItem.quantity += 1;
            existingItem.totalPrice = existingItem.quantity * existingItem.price;
        } else {
            App.cartItems.push({
                productId: productId,
                name: product.name,
                price: product.price,
                quantity: 1,
                totalPrice: product.price,
                type: product.type
            });
        }

        OrderManager.updateCartDisplay();
        ToastHelper.success(`${product.name} sepete eklendi!`, 1500);
    },

    updateCartDisplay: function () {
        const cartContainer = $('#cartItems');
        const emptyMessage = $('#emptyCartMessage');
        const cartSummary = $('#cartSummary');
        const cartCount = $('#cartItemCount');

        cartCount.text(App.cartItems.reduce((sum, item) => sum + item.quantity, 0));

        if (App.cartItems.length === 0) {
            emptyMessage.show();
            cartSummary.hide();
            return;
        }

        emptyMessage.hide();
        cartSummary.show();

        let html = '';
        let total = 0;

        App.cartItems.forEach(item => {
            total += item.totalPrice;
            html += `
                <div class="card mb-2 cart-item">
                    <div class="card-body p-2">
                        <div class="d-flex justify-content-between align-items-center">
                            <div class="flex-grow-1">
                                <h6 class="mb-0 small fw-bold">${item.name}</h6>
                                <small class="text-muted">₺${item.price.toFixed(2)} x ${item.quantity}</small>
                            </div>
                            <div class="text-end">
                                <div class="btn-group btn-group-sm">
                                    <button class="btn btn-outline-danger btn-sm" onclick="OrderManager.updateCartQuantity('${item.productId}', -1)">
                                        <i class="fas fa-minus"></i>
                                    </button>
                                    <span class="btn btn-outline-secondary btn-sm">${item.quantity}</span>
                                    <button class="btn btn-outline-success btn-sm" onclick="OrderManager.updateCartQuantity('${item.productId}', 1)">
                                        <i class="fas fa-plus"></i>
                                    </button>
                                </div>
                                <div class="fw-bold small mt-1">₺${item.totalPrice.toFixed(2)}</div>
                            </div>
                        </div>
                    </div>
                </div>
            `;
        });

        cartContainer.html(html);
        $('#cartTotal').text(`₺${total.toFixed(2)}`);
    },

    updateCartQuantity: function (productId, change) {
        const item = App.cartItems.find(item => item.productId === productId);
        if (!item) return;

        item.quantity += change;

        if (item.quantity <= 0) {
            App.cartItems = App.cartItems.filter(i => i.productId !== productId);
            ToastHelper.info(`${item.name} sepetten çıkarıldı`, 1500);
        } else {
            item.totalPrice = item.quantity * item.price;

            if (change > 0) {
                ToastHelper.success(`${item.name} +${change}`, 1000);
            } else {
                ToastHelper.warning(`${item.name} ${change}`, 1000);
            }
        }

        OrderManager.updateCartDisplay();
    },

    submitOrder: function () {
        console.log('🔍 submitOrder called');
        console.log('🔍 App.currentTableId:', App.currentTableId); // Debug
        console.log('🔍 App.cartItems:', App.cartItems); // Debug

        const waiterNote = $('#waiterNote').val().trim();

        if (App.cartItems.length === 0) {
            ToastHelper.warning('Sepette ürün bulunmuyor!');
            return;
        }

        if (!App.currentTableId) {
            console.error('❌ currentTableId is missing!');
            ToastHelper.error('Masa bilgisi bulunamadı!');
            return;
        }

        // Sipariş verisini hazırla
        const orderData = {
            tableId: App.currentTableId,
            waiterNote: waiterNote || null,
            items: App.cartItems.map(item => ({
                productId: item.productId,
                productName: item.name,
                price: item.price,
                quantity: item.quantity
            }))
        };

        // Konfirmasyon dialog'u
        const totalAmount = App.cartItems.reduce((sum, item) => sum + item.totalPrice, 0);
        const itemCount = App.cartItems.reduce((sum, item) => sum + item.quantity, 0);

        const confirmMessage = `
🛒 ${itemCount} ürün
💰 Toplam: ₺${totalAmount.toFixed(2)}
${waiterNote ? `📝 Not: ${waiterNote}` : ''}

Siparişi göndermek istediğinizden emin misiniz?
        `;

        if (!confirm(confirmMessage.trim())) {
            return;
        }

        // Loading göster
        const submitBtn = $('#submitOrderBtn');
        const originalText = submitBtn.html();
        submitBtn.html('<i class="fas fa-spinner fa-spin me-2"></i>Gönderiliyor...').prop('disabled', true);

        // AJAX request
        $.ajax({
            url: App.endpoints.submitOrder,
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(orderData),
            success: function (response) {
                submitBtn.html(originalText).prop('disabled', false);

                if (response.success) {
                    ToastHelper.success(response.message);
                    $('#productSelectionModal').modal('hide');
                    TableManager.loadTables();

                    // 1.5 saniye sonra masa detayını göster
                    setTimeout(() => {
                        OrderManager.showTableWithNewOrder(App.currentTableId);
                    }, 1500);

                } else {
                    ToastHelper.error('Sipariş gönderilemedi: ' + response.message);
                }
            },
            error: function (xhr, status, error) {
                submitBtn.html(originalText).prop('disabled', false);
                console.error('Sipariş gönderme hatası:', error);
                ToastHelper.error('Bağlantı hatası! Sipariş gönderilemedi.');
            }
        });
    },

    showTableWithNewOrder: function (tableId) {
        const tableData = Object.values(App.currentTablesData).flat().find(t => t.id === tableId);
        if (tableData) {
            TableManager.openTableModal(tableId, tableData.name, true);
        }
    },

    clearCart: function () {
        App.cartItems = [];
        $('#waiterNote').val('');
        OrderManager.updateCartDisplay();
    },

    showError: function (message) {
        $('#allProductsGrid').html(`
            <div class="col-12 text-center py-5">
                <i class="fas fa-exclamation-triangle fa-3x text-danger mb-3"></i>
                <h6 class="text-danger">${message}</h6>
            </div>
        `);
    },

    handleProductSearch: function () {
        const searchTerm = $('#productSearch').val().toLowerCase();
        const filtered = App.currentProducts.filter(p =>
            p.name.toLowerCase().includes(searchTerm) ||
            (p.description && p.description.toLowerCase().includes(searchTerm))
        );

        // Aktif tab'ı bul ve o grid'i güncelle
        const activeTab = $('.nav-pills .nav-link.active').attr('id');
        if (activeTab === 'all-tab') {
            OrderManager.renderProducts(filtered, 'allProductsGrid');
        }
    }
};