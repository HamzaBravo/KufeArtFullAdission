﻿// wwwroot/js/table-management.js
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
        // 🎯 YENİ: TableId'yi modal'a kaydet (sadece bu satırı ekleyin)
        $('#tableModal').data('current-table-id', tableId);

        // ✅ Gerisi AYNI kalacak - hiçbir değişiklik yapmayın
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

        // Modal açıldıktan sonra event listener'ları kur
        $('#tableModal').on('shown.bs.modal', function () {
            // Küfe Point event listener'ları
            $('#usePointsCheckbox').off('change').on('change', function () {
                $('#pointAmountSection').toggle(this.checked);
            });
        });
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
                        <!-- 🎯 YENİ: Küfe Point Bölümü (Siparişler tab'ının en üstünde) -->
                        <div class="card mb-3 kufe-point-section">
                            <div class="card-header bg-warning text-dark">
                                <h6 class="mb-0">🏆 Küfe Point Sistemi</h6>
                            </div>
                            <div class="card-body">
                                <div class="mb-3">
                                    <label class="form-label">Müşteri Telefon Numarası (Opsiyonel)</label>
                                    <div class="input-group">
                                      <input type="tel" id="customerPhoneInput" class="form-control"
                                               placeholder="05XX XXX XX XX" maxlength="11"
                                               autocomplete="off"
                                               spellcheck="false">
                                        <button type="button" class="btn btn-outline-primary"
                                                onclick="PaymentManager.checkCustomerPoints()">
                                            Sorgula
                                        </button>
                                    </div>
                                </div>

                                <div id="customerPointsResult" style="display:none;">
                                    <div class="alert alert-info">
                                        <div class="row">
                                            <div class="col-6">
                                                <strong>Mevcut Puan:</strong><br>
                                                <span id="currentPoints" class="text-primary fs-5">0</span>
                                            </div>
                                            <div class="col-6">
                                                <strong>Kazanacağı Puan:</strong><br>
                                                <span id="willEarnPoints" class="text-success fs-5">0</span>
                                            </div>
                                        </div>

                                        <!-- 🎯 YENİ: Basitleştirilmiş indirim butonu -->
                                        <div id="pointDiscountSection" style="display:none;" class="mt-3">
                                            <div class="d-grid">
                                                <button type="button" id="applyPointDiscountBtn" class="btn btn-success">
                                                    <i class="fas fa-percent me-2"></i>
                                                    <span id="discountButtonText">Tüm Puanları İndirim Olarak Uygula</span>
                                                    <br><small id="discountAmount">₺0.00 indirim</small>
                                                </button>
                                            </div>
                                            <div class="text-center mt-2">
                                                <small class="text-muted">İndirim uygulanırsa tüm mevcut puanlarınız harcanacak</small>
                                            </div>
                                        </div>

                                        <!-- 🎯 YENİ: İndirim durumu göstergesi -->
                                        <div id="discountAppliedIndicator" style="display:none;" class="mt-3">
                                            <div class="alert alert-success mb-0">
                                                <i class="fas fa-check-circle me-2"></i>
                                                <strong>İndirim Uygulandı!</strong><br>
                                                <span id="appliedDiscountText">0 puan kullanıldı (₺0.00)</span>
                                                <button type="button" class="btn btn-sm btn-outline-danger float-end"
                                                        onclick="PaymentManager.cancelPointDiscount()">
                                                    İptali Et
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>

        <!-- ✅ Siparişler listesi (aynı kalacak) -->
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
        <!-- Ödeme Durumu -->
        <div class="row mt-3">
            <div class="col-12">
                <div class="alert alert-info mb-0 py-2">
                    <div class="row text-center">
                        <div class="col-4">
                            <small class="text-muted">Toplam Sipariş</small><br>
                            <strong>₺${safeOrderAmount.toFixed(2)}</strong>
                        </div>
                        <div class="col-4">
                            <small class="text-muted">Ödenen</small><br>
                            <strong class="text-success">₺${safePaidAmount.toFixed(2)}</strong>
                        </div>
                        <div class="col-4">
                            <small class="text-muted">Kalan</small><br>
                            <strong class="text-warning">₺${safeRemainingAmount.toFixed(2)}</strong>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `;

        // ✅ ÖNEMLİ: Modal footer'a ödeme butonlarını ekle
        const tableId = table.id;
        html += `
        </div> <!-- siparişler content biter -->
        
        <!-- ✅ YENİ: Ödeme Butonları (Ana Modal'da) -->
<div class="modal-footer bg-light">
    <div class="container-fluid">
        <div class="row g-2">
            <!-- Hızlı Ödemeler -->
            <div class="col-md-6">
                <div class="row g-2">
                    <div class="col-6">
                        <button type="button" id="cashPaymentBtn" class="btn btn-success w-100"
                                onclick="PaymentManager.processFullPayment('${tableId}', 'cash')">
                            💰 Nakit Kapat<br>
                            <small id="cashAmountText">₺${safeRemainingAmount.toFixed(2)}</small>
                        </button>
                    </div>
                    <div class="col-6">
                        <button type="button" id="cardPaymentBtn" class="btn btn-primary w-100" 
                                onclick="PaymentManager.processFullPayment('${tableId}', 'card')">
                            💳 Kart Kapat<br>
                            <small id="cardAmountText">₺${safeRemainingAmount.toFixed(2)}</small>
                        </button>
                    </div>
                </div>
            </div>
            
            <!-- Parçalı Ödeme -->
            <div class="col-md-6">
                <button type="button" class="btn btn-warning w-100" 
                        onclick="PaymentManager.openPartialPaymentModal('${tableId}')">
                    📝 Parçalı Ödeme
                </button>
            </div>
        </div>
        
        <!-- Sipariş Ekleme -->
        <div class="row mt-2">
            <div class="col-12">
                <button type="button" class="btn btn-outline-primary w-100" 
                        onclick="OrderManager.addNewOrder('${tableId}')">
                    ➕ Sipariş Ekle
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