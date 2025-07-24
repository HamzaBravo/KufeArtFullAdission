
window.PaymentManager = window.PaymentManager || {};

// Global değişkenler
PaymentManager.currentCustomerData = null;
PaymentManager.pointDiscountApplied = false;
PaymentManager.appliedDiscountAmount = 0;
PaymentManager.appliedDiscountPoints = 0;


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

        // ❌ Küfe Point bölümünü KALDIR (artık parçalı ödeme modalında olmayacak)
        // Event listener'ları da kaldır
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

        // 🎯 YENİ: Küfe Point bilgilerini ekle
        const phoneInput = document.getElementById('customerPhoneInput');

        // Puan bilgilerini paymentData'ya ekle
        paymentData.customerPhone = phoneInput ? phoneInput.value.trim() : '';
        paymentData.customerName = '';
        paymentData.useKufePoints = PaymentManager.pointDiscountApplied;
        paymentData.requestedPoints = PaymentManager.appliedDiscountPoints;

        console.log('🎯 Gönderilen ödeme verisi:', paymentData);

        console.log('🎯 Gönderilen ödeme verisi:', paymentData);
        console.log('📊 Puan durumu:', {
            discountApplied: PaymentManager.pointDiscountApplied,
            discountAmount: PaymentManager.appliedDiscountAmount,
            pointsToUse: PaymentManager.appliedDiscountPoints
        });

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

                    // Puan durumunu sıfırla
                    PaymentManager.pointDiscountApplied = false;
                    PaymentManager.appliedDiscountAmount = 0;
                    PaymentManager.appliedDiscountPoints = 0;

                    if (response.data.accountClosed) {
                        $('#tableModal').modal('hide');
                        TableManager.loadTables();
                    } else {
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
    },

    checkCustomerPoints: function () {
        const phoneNumberInput = document.getElementById('customerPhoneInput');
        const phoneNumber = phoneNumberInput ? phoneNumberInput.value.trim() : '';

        console.log('Girilen telefon:', phoneNumber);

        if (!phoneNumber || phoneNumber === '') {
            ToastHelper.warning('Lütfen telefon numarası girin!');
            return;
        }

        if (phoneNumber.length !== 11 || !phoneNumber.startsWith('0')) {
            ToastHelper.error('Geçerli bir telefon numarası girin! (05XX XXX XX XX)');
            return;
        }

        // 🔧 BASİTLEŞTİRİLDİ: TableId'yi modal'dan al
        const currentTableId = $('#tableModal').data('current-table-id');

        console.log('🔍 Bulunan TableId:', currentTableId); // Debug

        LoaderHelper.show('Müşteri puanları sorgulanıyor...');

        $.ajax({
            url: '/Home/GetCustomerPoints',
            method: 'GET',
            data: {
                phoneNumber: phoneNumber,
                tableId: currentTableId
            },
            success: function (response) {
                LoaderHelper.hide();

                if (response.success) {
                    PaymentManager.displayCustomerPoints(response.data);
                } else {
                    ToastHelper.info(response.message || 'Müşteri bulunamadı, yeni üye olarak kaydedilecek!');
                    PaymentManager.displayNewCustomerPoints(phoneNumber);
                }
            },
            error: function (xhr, status, error) {
                LoaderHelper.hide();
                console.error('AJAX Error:', xhr.responseText); // Debug
                ToastHelper.error('Bağlantı hatası!');
            }
        });
    },

    // 🎯 YENİ: Yeni müşteri için puan gösterimi
    displayNewCustomerPoints: function (phoneNumber) {
        $('#currentPoints').text('0');
        $('#willEarnPoints').text('Hesaplanıyor...');

        $('#pointDiscountSection').hide(); // Puan yok, indirim yok
        $('#customerPointsResult').show();

        ToastHelper.info('Yeni müşteri olarak kaydedilecek ve puanlar hesabına eklenecek!');
    },

    // payment-manager.js dosyasında displayCustomerPoints fonksiyonunu şöyle güncelleyin:
    displayCustomerPoints: function (data) {
        PaymentManager.currentCustomerData = data;

        $('#currentPoints').text(data.currentPoints || 0);
        $('#willEarnPoints').text(data.willEarnPoints || 0);

        // İndirim bölümünü göster/gizle
        if (data.currentPoints >= 5000) {
            const maxDiscountAmount = Math.min(data.currentPoints / 100, App.currentTableRemainingAmount || 0);
            const maxDiscountPoints = Math.floor(maxDiscountAmount * 100);

            $('#discountButtonText').text(`${data.currentPoints} Puan Kullan`);
            $('#discountAmount').text(`₺${maxDiscountAmount.toFixed(2)} indirim`);
            $('#pointDiscountSection').show();

            // 🔧 ÖNEMLİ: Buton event'ini kur (Bu kısım eksikti!)
            $('#applyPointDiscountBtn').off('click').on('click', function () {
                PaymentManager.applyPointDiscount();
            });

        } else {
            $('#pointDiscountSection').hide();
            ToastHelper.info('Puan indirimi için minimum 5000 puan gerekli!');
        }

        $('#customerPointsResult').show();
    },

    // 🎯 YENİ: Puan indirimi uygula
    // 🎯 DÜZELTME: Ödeme tutarlarını güncelle
    updatePaymentAmounts: function () {
        const remainingAmount = App.currentTableRemainingAmount || 0;

        console.log('💰 UI Güncelleniyor - Kalan tutar:', remainingAmount);

        // 1. Siparişler tab'ındaki ödeme durumu alert'ini güncelle
        const paymentStatusAlert = $('.alert-info').filter(function () {
            return $(this).text().includes('Kalan');
        });

        if (paymentStatusAlert.length > 0) {
            // Ödeme durumu metnini güncelle
            paymentStatusAlert.find('.row.text-center').html(`
            <div class="col-4">
                <small class="text-muted">Toplam Sipariş</small><br>
                <strong>₺${(App.currentTableOrders.reduce((sum, order) => sum + order.totalPrice, 0) || 0).toFixed(2)}</strong>
            </div>
            <div class="col-4">
                <small class="text-muted">Ödenen</small><br>
                <strong class="text-success">₺${((App.currentTableOrders.reduce((sum, order) => sum + order.totalPrice, 0) || 0) - remainingAmount).toFixed(2)}</strong>
            </div>
            <div class="col-4">
                <small class="text-muted">Kalan</small><br>
                <strong class="text-warning">₺${remainingAmount.toFixed(2)}</strong>
            </div>
        `);

            console.log('✅ Ödeme durumu güncellendi');
        }

        // 2. Modal footer'daki ödeme butonlarının yakınındaki bilgileri güncelle
        $('.btn:contains("Nakit Kapat"), .btn:contains("Kart Kapat")').each(function () {
            const originalText = $(this).text().split(' ')[0]; // "Nakit" veya "Kart"
            $(this).html(`💰 ${originalText} Kapat<br><small>₺${remainingAmount.toFixed(2)}</small>`);
        });

        // 3. Parçalı ödeme modalındaki tutarları güncelle
        if ($('#partialPaymentModal').hasClass('show')) {
            $('#totalOrderAmount').html(`
            Toplam sipariş: ₺${(App.currentTableOrders.reduce((sum, order) => sum + order.totalPrice, 0) || 0).toFixed(2)}<br>
            <span class="text-success">Ödenen: ₺${((App.currentTableOrders.reduce((sum, order) => sum + order.totalPrice, 0) || 0) - remainingAmount).toFixed(2)}</span><br>
            <span class="text-warning fw-bold">Kalan: ₺${remainingAmount.toFixed(2)}</span>
        `);

            $('#customPaymentAmount').attr('max', remainingAmount).attr('placeholder', `Maksimum: ₺${remainingAmount.toFixed(2)}`);
        }
    },

    // 🎯 YENİ: Puan indirimi iptal et
    cancelPointDiscount: function () {
        if (!PaymentManager.pointDiscountApplied) return;

        // Kalan tutarı geri yükle
        App.currentTableRemainingAmount += PaymentManager.appliedDiscountAmount;

        // Durumu sıfırla
        PaymentManager.pointDiscountApplied = false;
        PaymentManager.appliedDiscountAmount = 0;
        PaymentManager.appliedDiscountPoints = 0;

        // UI'yi geri yükle
        $('#discountAppliedIndicator').hide();
        $('#pointDiscountSection').show();

        // Ödeme durumu bilgilerini güncelle
        PaymentManager.updatePaymentAmounts();

        ToastHelper.info('Puan indirimi iptal edildi.');
    },

    // 🎯 YENİ: Ödeme tutarlarını güncelle
    // 🎯 GELİŞTİRİLMİŞ: Ödeme tutarlarını güncelle
    updatePaymentAmounts: function () {
        const remainingAmount = App.currentTableRemainingAmount || 0;

        console.log('💰 UI Güncelleniyor - Kalan tutar:', remainingAmount);

        // 1. Ana modal'daki ödeme butonları
        $('#cashAmountText, #cardAmountText').text(`₺${remainingAmount.toFixed(2)}`);

        // 2. Ödeme durumu alert'i
        $('.alert-info .row.text-center .col-4:last-child strong').text(`₺${remainingAmount.toFixed(2)}`);

        // 3. Görsel feedback
        if (PaymentManager.pointDiscountApplied) {
            // Yeşil border ekle - indirim uygulandığını göster
            $('.alert-info').removeClass('alert-info').addClass('alert-success');
            $('.alert-success .col-4:last-child').html(`
            <small class="text-muted">Kalan (İndirimli)</small><br>
            <strong class="text-success">₺${remainingAmount.toFixed(2)}</strong>
            <br><small class="text-success">✅ ${PaymentManager.appliedDiscountPoints} puan kullanıldı</small>
        `);
        }

        console.log('✅ UI güncellendi');
    }
};