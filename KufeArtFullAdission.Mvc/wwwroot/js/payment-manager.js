// wwwroot/js/payment-manager.js
window.PaymentManager = {

    processFullPayment: function (tableId, paymentType) {
        if (App.isPaymentProcessing) {
            ToastHelper.warning('İşlem devam ediyor, lütfen bekleyin...');
            return;
        }

        if (!confirm(`${paymentType === 'cash' ? 'Nakit' : 'Kart'} ile tüm hesabı kapatmak istediğinizden emin misiniz?`)) {
            return;
        }

        const paymentData = {
            tableId: tableId,
            paymentMode: 'full',
            paymentType: paymentType === 'cash' ? 0 : 1,
            paymentLabel: `Tam ödeme - ${paymentType === 'cash' ? 'Nakit' : 'Kart'}`,
            customAmount: 0
        };

        PaymentManager.executePayment(paymentData);
    },

    openPartialPaymentModal: function (tableId) {
        if (!App.currentTableOrders || App.currentTableOrders.length === 0) {
            ToastHelper.error('Sipariş verisi bulunamadı!');
            return;
        }

        // Güncel masa bilgisini al
        LoaderHelper.show('Ödeme seçenekleri hazırlanıyor...');

        $.ajax({
            url: App.endpoints.getTableDetails,
            method: 'GET',
            data: { tableId: tableId },
            success: function (response) {
                LoaderHelper.hide();

                if (response.success) {
                    const data = response.data;
                    App.currentTableOrders = data.orders || [];
                    App.currentTableRemainingAmount = data.remainingAmount || 0;

                    // Eski modal'ı kapat
                    $('#tableModal').modal('hide');

                    setTimeout(() => {
                        const modal = new bootstrap.Modal(document.getElementById('partialPaymentModal'));
                        modal.show();
                        PaymentManager.populatePartialPaymentModal(tableId, data);
                    }, 300);

                } else {
                    ToastHelper.error('Güncel veriler alınamadı!');
                }
            },
            error: function () {
                LoaderHelper.hide();
                ToastHelper.error('Bağlantı hatası!');
            }
        });
    },

    populatePartialPaymentModal: function (tableId, data) {
        const totalOrderAmount = data.totalOrderAmount || 0;
        const totalPaidAmount = data.totalPaidAmount || 0;
        const remainingAmount = data.remainingAmount || 0;

        // Modal'a tableId kaydet
        $('#partialPaymentModal').data('current-table-id', tableId);

        // 1. Ürün listesini oluştur
        PaymentManager.populateOrderItems();

        // 2. Etiket listesini oluştur
        PaymentManager.populateLabels();

        // 3. Manuel tutar ayarları
        $('#totalOrderAmount').html(`
            Toplam sipariş: ₺${totalOrderAmount.toFixed(2)}<br>
            <span class="text-success">Ödenen: ₺${totalPaidAmount.toFixed(2)}</span><br>
            <span class="text-warning fw-bold">Kalan: ₺${remainingAmount.toFixed(2)}</span>
        `);

        $('#customPaymentAmount')
            .attr('max', remainingAmount)
            .attr('placeholder', `Maksimum: ₺${remainingAmount.toFixed(2)}`)
            .val('');
    },

    populateOrderItems: function () {
        let html = '';

        App.currentTableOrders.forEach((order, index) => {
            html += `
                <div class="form-check mb-2">
                    <input class="form-check-input order-item-checkbox" type="checkbox" 
                           value="${order.totalPrice}" data-order-index="${index}" 
                           id="order_${index}">
                    <label class="form-check-label w-100" for="order_${index}">
                        <div class="d-flex justify-content-between">
                            <div>
                                <strong>${order.productName}</strong>
                                <small class="text-muted d-block">
                                    ${order.productQuantity} adet × ₺${order.productPrice.toFixed(2)}
                                    ${order.shorLabel ? ` • ${order.shorLabel}` : ''}
                                </small>
                            </div>
                            <div class="fw-bold text-success">₺${order.totalPrice.toFixed(2)}</div>
                        </div>
                    </label>
                </div>
            `;
        });

        $('#orderItemsList').html(html);

        // Checkbox event'lerini temizle ve yeniden ekle
        $('.order-item-checkbox').off('change.orderSelection');
        $('.order-item-checkbox').on('change.orderSelection', PaymentManager.updateSelectedItemsTotal);
    },

    populateLabels: function () {
        const labels = [...new Set(App.currentTableOrders.filter(o => o.shorLabel).map(o => o.shorLabel))];

        if (labels.length === 0) {
            $('#labelsList').html(`
                <div class="alert alert-warning">
                    <i class="fas fa-info-circle me-2"></i>
                    Bu siparişte etiket bulunmuyor.
                </div>
            `);
            return;
        }

        let html = '';
        labels.forEach((label, index) => {
            const labelTotal = App.currentTableOrders
                .filter(o => o.shorLabel === label)
                .reduce((sum, o) => sum + o.totalPrice, 0);

            html += `
                <div class="form-check mb-2">
                    <input class="form-check-input label-checkbox" type="radio" 
                           name="labelRadio" value="${labelTotal}" 
                           data-label="${label}" id="label_${index}">
                    <label class="form-check-label w-100" for="label_${index}">
                        <div class="d-flex justify-content-between align-items-center">
                            <strong>${label}</strong>
                            <span class="badge bg-success">₺${labelTotal.toFixed(2)}</span>
                        </div>
                    </label>
                </div>
            `;
        });

        $('#labelsList').html(html);
    },

    updateSelectedItemsTotal: function () {
        let total = 0;
        $('.order-item-checkbox:checked').each(function () {
            total += parseFloat($(this).val());
        });
        $('#selectedItemsTotal').text(`₺${total.toFixed(2)}`);
    },

    processPartialPayment: function (paymentType) {
        if (App.isPaymentProcessing) {
            ToastHelper.warning('İşlem devam ediyor...');
            return;
        }

        const tableId = $('#partialPaymentModal').data('current-table-id');
        const activeTab = $('#partialPaymentTabs .nav-link.active').attr('data-bs-target');
        let amount = 0;
        let label = '';
        const remainingAmount = App.currentTableRemainingAmount;

        if (activeTab === '#selectItemsTab') {
            // Seçilen ürünler
            const checkedBoxes = $('.order-item-checkbox:checked');
            if (checkedBoxes.length === 0) {
                ToastHelper.warning('Lütfen ödenecek ürünleri seçin!');
                return;
            }
            checkedBoxes.each(function () {
                amount += parseFloat($(this).val());
            });
            label = `Seçili ürünler (${checkedBoxes.length} adet)`;

        } else if (activeTab === '#selectLabelTab') {
            // Seçilen etiket
            const selectedLabel = $('.label-checkbox:checked');
            if (selectedLabel.length === 0) {
                ToastHelper.warning('Lütfen bir etiket seçin!');
                return;
            }
            amount = parseFloat(selectedLabel.val());
            label = `Etiket: ${selectedLabel.data('label')}`;

        } else if (activeTab === '#customAmountTab') {
            // Manuel tutar
            amount = parseFloat($('#customPaymentAmount').val());
            if (!amount || amount <= 0) {
                ToastHelper.warning('Lütfen geçerli bir tutar girin!');
                return;
            }
            label = 'Manuel tutar';
        }

        // ✅ GENEL TUTAR KONTROLÜ - HEM NAKİT HEM KART İÇİN
        if (amount > remainingAmount) {
            const paymentTypeText = paymentType === 0 ? 'Nakit' : 'Kart';
            ToastHelper.error(`${paymentTypeText} ile maksimum ₺${remainingAmount.toFixed(2)} ödeme alabilirsiniz!\n\nGirilen tutar: ₺${amount.toFixed(2)}\nKalan borç: ₺${remainingAmount.toFixed(2)}`);

            // Manuel tutar tab'ındaysa değeri düzelt
            if (activeTab === '#customAmountTab') {
                $('#customPaymentAmount').val(remainingAmount.toFixed(2));
            }
            return;
        }

        // ✅ SIFIR VEYA NEGATİF TUTAR KONTROLÜ
        if (amount <= 0) {
            ToastHelper.warning('Ödeme tutarı sıfırdan büyük olmalıdır!');
            return;
        }

        const paymentData = {
            tableId: tableId,
            paymentMode: 'partial',
            paymentType: paymentType,
            paymentLabel: label,
            customAmount: amount
        };

        // Konfirmasyon
        const paymentTypeText = paymentType === 0 ? 'Nakit' : 'Kart';
        const confirmMessage = `${paymentTypeText} ile ₺${amount.toFixed(2)} ödeme almak istediğinizden emin misiniz?\n\nKalan borç: ₺${(remainingAmount - amount).toFixed(2)}`;

        if (!confirm(confirmMessage)) {
            return;
        }

        $('#partialPaymentModal').modal('hide');
        PaymentManager.executePayment(paymentData);
    },

    executePayment: function (paymentData) {
        if (App.isPaymentProcessing) return;

        App.isPaymentProcessing = true;
        LoaderHelper.show('Ödeme işleniyor...');

        $.ajax({
            url: App.endpoints.processQuickPayment,
            method: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(paymentData),
            success: function (response) {
                App.isPaymentProcessing = false;
                LoaderHelper.hide();

                if (response.success) {
                    ToastHelper.success(response.message);

                    if (response.data.accountClosed) {
                        // Hesap kapatıldı - dashboard'a dön
                        $('#tableModal').modal('hide');
                        TableManager.loadTables();
                    } else {
                        // Parçalı ödeme - masa detayını yenile
                        PaymentManager.updateAfterPartialPayment(paymentData.tableId);
                    }
                } else {
                    ToastHelper.error(response.message);
                }
            },
            error: function () {
                App.isPaymentProcessing = false;
                LoaderHelper.hide();
                ToastHelper.error('Bağlantı hatası!');
            }
        });
    },

    updateAfterPartialPayment: function (tableId) {
        // Dashboard'ı sessizce güncelle
        TableManager.loadTables(true);

        // Modal'ı yenile
        setTimeout(() => {
            const tableName = $('#modalTableName').text() || 'Masa';
            $('#tableModal').modal('hide');

            setTimeout(() => {
                TableManager.openTableModal(tableId, tableName, true);
            }, 500);
        }, 1000);
    }
};