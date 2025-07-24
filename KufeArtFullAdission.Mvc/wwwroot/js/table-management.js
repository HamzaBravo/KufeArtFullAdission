// wwwroot/js/table-management.js
window.TableManager = {

    loadTables: function (silent = false) {
        $.ajax({
            url: App.endpoints.getTables,
            method: 'GET',
            success: function (response) {
                if (response.success) {
                    App.currentTablesData = response.data;
                    TableManager.renderTables(response.data);
                    if (!silent) {
                        console.log('✅ Tables loaded successfully');
                    }
                }
            },
            error: function (xhr, status, error) {
                if (!silent) {
                    console.error('AJAX Hatası:', error);
                    ToastHelper.error('Bağlantı hatası!');
                }
            }
        });
    },

    renderTables: function (tablesData) {
        const activeTabId = $('.nav-tabs .nav-link.active').attr('id');
        let html = '';

        if (Object.keys(tablesData).length === 0) {
            html = `
                <div class="text-center py-5">
                    <i class="fas fa-chair fa-3x text-muted mb-3"></i>
                    <h5 class="text-muted">Henüz masa tanımlanmamış</h5>
                </div>
            `;
        } else {
            // İstatistikler
            let totalTables = 0;
            let occupiedTables = 0;

            Object.values(tablesData).forEach(tables => {
                totalTables += tables.length;
                occupiedTables += tables.filter(t => t.isOccupied).length;
            });

            const occupancyRate = totalTables > 0 ? Math.round((occupiedTables / totalTables) * 100) : 0;
            const freeTableCount = totalTables - occupiedTables;

            // Üst bilgi çubuğu
            html += `
                <div class="row mb-3">
                    <div class="col-md-8">
                        <div class="d-flex align-items-center">
                            <div class="me-4">
                                <small class="text-muted">
                                    <i class="fas fa-chair me-1 text-warning"></i>
                                    <span class="text-warning fw-bold">${occupiedTables}</span> Dolu
                                </small>
                            </div>
                            <div>
                                <small class="text-muted">
                                    <i class="fas fa-chair me-1 text-success"></i>
                                    <span class="text-success fw-bold">${freeTableCount}</span> Boş
                                </small>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4 text-end">
                        <span class="badge bg-light text-dark fs-6 px-3 py-2">
                            <i class="fas fa-chart-pie me-2 text-primary"></i>
                            Doluluk: <strong class="text-primary">${occupancyRate}%</strong>
                        </span>
                    </div>
                </div>
            `;

            // Tab Navigation
            const categories = Object.keys(tablesData);
            html += '<ul class="nav nav-tabs mb-4" id="categoryTabs" role="tablist">';

            categories.forEach((category, index) => {
                const categoryId = category.replace(/\s+/g, '').toLowerCase();
                const isActive = (activeTabId && activeTabId === `${categoryId}-tab`) || (!activeTabId && index === 0) ? 'active' : '';
                const tables = tablesData[category];
                const categoryOccupied = tables.filter(t => t.isOccupied).length;

                html += `
                    <li class="nav-item" role="presentation">
                        <button class="nav-link ${isActive} d-flex align-items-center"
                                id="${categoryId}-tab"
                                data-bs-toggle="tab"
                                data-bs-target="#${categoryId}"
                                type="button"
                                role="tab">
                            <i class="fas fa-map-marker-alt me-2"></i>
                            ${category}
                            <span class="badge bg-secondary ms-2">${tables.length}</span>
                            ${categoryOccupied > 0 ? `<span class="badge bg-warning ms-1">${categoryOccupied}</span>` : ''}
                        </button>
                    </li>
                `;
            });
            html += '</ul>';

            // Tab Content
            html += '<div class="tab-content" id="categoryTabContent">';
            categories.forEach((category, index) => {
                const categoryId = category.replace(/\s+/g, '').toLowerCase();
                const isActive = (activeTabId && activeTabId === `${categoryId}-tab`) || (!activeTabId && index === 0) ? 'show active' : '';
                const tables = tablesData[category];

                html += `
                    <div class="tab-pane fade ${isActive}"
                         id="${categoryId}"
                         role="tabpanel">
                        <div class="row g-3">
                `;

                tables.forEach(table => {
                    const isOccupied = table.isOccupied;
                    const cardClass = isOccupied ? 'border-warning bg-warning bg-opacity-10' : 'border-success bg-success bg-opacity-10';
                    const iconClass = isOccupied ? 'text-warning' : 'text-success';
                    const statusIcon = isOccupied ? 'fas fa-clock' : 'fas fa-check-circle';

                    html += `
                        <div class="col-xl-2 col-lg-3 col-md-4 col-6">
                            <div class="card ${cardClass} table-card h-100"
                                 style="cursor: pointer;"
                                 data-table-id="${table.id}"
                                 data-opened-at="${table.openedAt || ''}">
                                <div class="card-body text-center p-3">
                                    <div class="${iconClass} mb-2">
                                        <i class="fas fa-chair fa-2x"></i>
                                    </div>
                                    <h6 class="card-title mb-1">${table.name}</h6>
                                    <div class="small ${iconClass}">
                                        <i class="${statusIcon} me-1"></i>
                                        ${isOccupied ? 'Dolu' : 'Boş'}
                                    </div>
                                    ${isOccupied ? `
                                        <div class="mt-2 pt-2 border-top">
                                            <div class="small text-muted time-display">
                                                ${Utils.getTimeAgo(table.openedAt)}
                                            </div>
                                            <div class="fw-bold text-warning">
                                                ₺${(table.totalAmount || 0).toFixed(2)}
                                            </div>
                                        </div>
                                    ` : ''}
                                </div>
                            </div>
                        </div>
                    `;
                });

                html += '</div></div>';
            });
            html += '</div>';
        }

        $('#tablesContainer').html(html);
    },

    openTableModal: function (tableId, tableName, isOccupied) {
        $('#modalTableName').text(tableName);

        const statusBadge = $('#modalTableStatus');
        if (isOccupied) {
            statusBadge.text('Dolu').removeClass('bg-success').addClass('bg-warning');
        } else {
            statusBadge.text('Boş').removeClass('bg-warning').addClass('bg-success');
        }

        const modal = new bootstrap.Modal(document.getElementById('tableModal'));
        modal.show();

        // Loading states
        $('#ordersTab').html('<div class="text-center py-4"><div class="spinner-border"></div><p class="mt-2">Yükleniyor...</p></div>');
        $('#paymentsTab').html('<div class="text-center py-4"><div class="spinner-border"></div><p class="mt-2">Yükleniyor...</p></div>');
        $('#summaryTab').html('<div class="text-center py-4"><div class="spinner-border"></div><p class="mt-2">Yükleniyor...</p></div>');

        TableManager.loadTableDetails(tableId);
    },

    loadTableDetails: function (tableId) {
        $.ajax({
            url: App.endpoints.getTableDetails,
            method: 'GET',
            data: { tableId: tableId },
            success: function (response) {
                if (response.success) {
                    const data = response.data;
                    const table = data.table;
                    const orders = data.orders || [];
                    const payments = data.payments || [];

                    // Global değişkenleri güncelle
                    App.currentTableOrders = orders;
                    App.currentTableRemainingAmount = data.remainingAmount || 0;

                    if (!table.isOccupied) {
                        $('#ordersTab').html(TableManager.generateEmptyTableHTML(table.id));
                    } else {
                        $('#ordersTab').html(TableManager.generateOccupiedTableHTML(table, orders, data.totalOrderAmount || 0, data.totalPaidAmount || 0, data.remainingAmount || 0));
                    }

                    $('#paymentsTab').html(TableManager.generatePaymentsTabContent(payments, table.id));
                    $('#summaryTab').html(TableManager.generateSummaryTabContent(data.totalOrderAmount || 0, data.totalPaidAmount || 0, data.remainingAmount || 0));

                    $('#orderCount').text(orders.length);
                    $('#paymentCount').text(payments.length);
                }
            },
            error: function () {
                $('#ordersTab').html('<div class="alert alert-danger">Bağlantı hatası!</div>');
            }
        });
    },

    generateEmptyTableHTML: function (tableId) {
        console.log('🔍 generateEmptyTableHTML called with tableId:', tableId); // Debug

        return `
        <div class="text-center py-4">
            <i class="fas fa-utensils fa-4x text-primary mb-3"></i>
            <h5 class="text-muted mb-3">Masa Boş</h5>
            <p class="text-muted mb-4">Bu masaya sipariş almak için ürün seçim ekranını açın.</p>
            <button class="btn btn-primary btn-lg" onclick="OrderManager.startNewOrder('${tableId}')">
                <i class="fas fa-plus me-2"></i>Sipariş Al
            </button>
        </div>
    `;
    },

    generateOccupiedTableHTML: function (table, orders, totalAmount, totalPaidAmount, remainingAmount) {
        App.currentTableOrders = orders;

        const safeOrderAmount = totalAmount || 0;
        const safePaidAmount = totalPaidAmount || 0;
        const safeRemainingAmount = remainingAmount || 0;

        // Batch'leri grupla
        const batches = {};
        orders.forEach(order => {
            if (!batches[order.orderBatchId]) {
                batches[order.orderBatchId] = [];
            }
            batches[order.orderBatchId].push(order);
        });

        let html = `
            <!-- Masa Özeti -->
            <div class="row mb-4">
                <div class="col-md-4">
                    <div class="card bg-warning bg-opacity-10 border-warning">
                        <div class="card-body text-center">
                            <i class="fas fa-clock fa-2x text-warning mb-2"></i>
                            <h6 class="text-muted">Açılış Zamanı</h6>
                            <p class="fw-bold mb-0">${Utils.getTimeAgo(orders[0]?.createdAt)}</p>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card bg-info bg-opacity-10 border-info">
                        <div class="card-body text-center">
                            <i class="fas fa-shopping-cart fa-2x text-info mb-2"></i>
                            <h6 class="text-muted">Toplam Ürün</h6>
                            <p class="fw-bold mb-0">${orders.reduce((sum, order) => sum + order.productQuantity, 0)} adet</p>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card bg-success bg-opacity-10 border-success">
                        <div class="card-body text-center">
                            <i class="fas fa-lira-sign fa-2x text-success mb-2"></i>
                            <h6 class="text-muted">Toplam Tutar</h6>
                            <p class="fw-bold mb-0">₺${totalAmount.toFixed(2)}</p>
                        </div>
                    </div>
                </div>
            </div>

            <!-- Sipariş Geçmişi -->
            <div class="card mb-4">
                <div class="card-header d-flex justify-content-between align-items-center">
                    <h6 class="mb-0">
                        <i class="fas fa-list me-2"></i>Sipariş Geçmişi
                    </h6>
                    <button class="btn btn-sm btn-outline-primary" onclick="OrderManager.addNewOrder('${table.id}')">
                        <i class="fas fa-plus me-1"></i>Ürün Ekle
                    </button>
                </div>
                <div class="card-body">
        `;

        // Batch'leri render et
        Object.keys(batches).forEach((batchId, index) => {
            const batchOrders = batches[batchId];
            const batchTime = new Date(batchOrders[0].createdAt).toLocaleString('tr-TR');
            const batchTotal = batchOrders.reduce((sum, o) => sum + o.totalPrice, 0);

            html += `
                <div class="border rounded mb-3 p-3 ${index % 2 === 0 ? 'bg-light' : ''}">
                        <div class="d-flex justify-content-between align-items-center mb-2">
                            <h6 class="text-primary mb-0">
                                <i class="fas fa-clock me-1"></i>
                                ${Utils.getTimeAgo(batchOrders[0].createdAt)} - ${batchOrders[0].personFullName}
                            </h6>
                            <span class="badge bg-success">₺${batchTotal.toFixed(2)}</span>
                        </div>
                               <div class="table-responsive">
                <table class="table table-sm table-borderless mb-0" style="table-layout: fixed;">
                    <colgroup>
                        <col style="width: 45%;">  <!-- Ürün adı -->
                        <col style="width: 15%;">  <!-- Adet -->
                        <col style="width: 20%;">  <!-- Birim fiyat -->
                        <col style="width: 20%;">  <!-- Toplam -->
                    </colgroup>
                    <thead>
                        <tr>
                            <th>Ürün</th>
                            <th class="text-center">Adet</th>
                            <th class="text-end">Birim Fiyat</th>
                            <th class="text-end">Toplam</th>
                        </tr>
                    </thead>
                    <tbody>
            `;

            batchOrders.forEach(order => {
                html += `
                    <tr>
                        <td>${order.productName}</td>
                        <td class="text-center"><span class="badge bg-primary">${order.productQuantity}</span></td>
                        <td class="text-end">₺${order.productPrice.toFixed(2)}</td>
                        <td class="text-end fw-bold">₺${order.totalPrice.toFixed(2)}</td>
                    </tr>
                `;
            });

            html += '</table></div></div>';
        });

        html += `
                </div>
            </div>

                     <!-- Ödeme Butonları -->
            <div class="card mb-3">
                <div class="card-header">
                    <h6 class="mb-0"><i class="fas fa-credit-card me-2"></i>Ödeme Al</h6>
                </div>
                <div class="card-body">
                    <div class="row g-2">
                        <div class="col-md-4">
                            <button class="btn btn-success w-100 d-flex flex-column align-items-center" onclick="PaymentManager.processFullPayment('${table.id}', 'cash')">
                                <i class="fas fa-money-bill-wave fa-lg mb-1"></i>
                                <span class="fw-bold">Nakit</span>
                                <small>₺${safeRemainingAmount.toFixed(2)}</small>
                            </button>
                        </div>
                        <div class="col-md-4">
                            <button class="btn btn-primary w-100 d-flex flex-column align-items-center" onclick="PaymentManager.processFullPayment('${table.id}', 'card')">
                                <i class="fas fa-credit-card fa-lg mb-1"></i>
                                <span class="fw-bold">Kart</span>
                                <small>₺${safeRemainingAmount.toFixed(2)}</small>
                            </button>
                        </div>
                        <div class="col-md-4">
                            <button class="btn btn-warning w-100 d-flex flex-column align-items-center" onclick="PaymentManager.openPartialPaymentModal('${table.id}')">
                                <i class="fas fa-calculator fa-lg mb-1"></i>
                                <span class="fw-bold">Parçalı</span>
                                <small>Seç & Öde</small>
                            </button>
                        </div>
                    </div>

                </div>
            </div>
        `;

        return html;
    },

    generatePaymentsTabContent: function (payments, tableId) {
        if (payments.length === 0) {
            return `
                <div class="text-center py-4">
                    <i class="fas fa-credit-card fa-3x text-muted mb-3"></i>
                    <p class="text-muted mb-3">Henüz ödeme yapılmamış</p>
                </div>
            `;
        }

        let html = '<div class="list-group">';
        payments.forEach(payment => {
            const paymentType = (payment.paymentType || payment.PaymentType) === 0 ? 'Nakit' : 'Kart';
            const amount = payment.amount || payment.Amount || 0;
            const createdAt = payment.createdAt || payment.CreatedAt;
            const shortLabel = payment.shortLabel || payment.ShortLabel || '';

            const date = new Date(createdAt).toLocaleString('tr-TR');

            html += `
                <div class="list-group-item">
                    <div class="d-flex justify-content-between">
                        <div>
                            <strong>${paymentType}</strong>
                            <small class="text-muted d-block">${date}</small>
                            ${shortLabel ? `<small class="text-info">${shortLabel}</small>` : ''}
                        </div>
                        <div class="text-success fw-bold">₺${amount.toFixed(2)}</div>
                    </div>
                </div>
            `;
        });
        html += '</div>';
        return html;
    },

    generateSummaryTabContent: function (totalAmount, totalPaidAmount, remainingAmount) {
        return `
            <div class="row">
                <div class="col-md-4">
                    <div class="card bg-info bg-opacity-10">
                        <div class="card-body text-center">
                            <i class="fas fa-receipt fa-2x text-info mb-2"></i>
                            <h6>Toplam Sipariş</h6>
                            <h4 class="text-info">₺${totalAmount.toFixed(2)}</h4>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card bg-success bg-opacity-10">
                        <div class="card-body text-center">
                            <i class="fas fa-check fa-2x text-success mb-2"></i>
                            <h6>Ödenen</h6>
                            <h4 class="text-success">₺${totalPaidAmount.toFixed(2)}</h4>
                        </div>
                    </div>
                </div>
                <div class="col-md-4">
                    <div class="card ${remainingAmount > 0 ? 'bg-warning bg-opacity-10' : 'bg-success bg-opacity-10'}">
                        <div class="card-body text-center">
                            <i class="fas fa-balance-scale fa-2x ${remainingAmount > 0 ? 'text-warning' : 'text-success'} mb-2"></i>
                            <h6>Kalan</h6>
                            <h4 class="${remainingAmount > 0 ? 'text-warning' : 'text-success'}">₺${remainingAmount.toFixed(2)}</h4>
                        </div>
                    </div>
                </div>
            </div>
        `;
    },

    updateTableTimes: function () {
        $('.table-card[data-opened-at]').each(function () {
            const openedAt = $(this).data('opened-at');
            if (openedAt) {
                const timeDisplay = $(this).find('.time-display');
                if (timeDisplay.length > 0) {
                    timeDisplay.text(Utils.getTimeAgo(openedAt));
                }
            }
        });
    }
};